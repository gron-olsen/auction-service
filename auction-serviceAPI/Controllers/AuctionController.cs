using auctionServiceAPI.Models;
using auctionServiceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text;
using System;
using Microsoft.AspNetCore.Http.Features;
using RabbitMQ.Client;
using Newtonsoft.Json;

namespace auctionServiceAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AuctionController : ControllerBase
{
    private readonly ILogger<AuctionController> _logger;
    private readonly ConnectionFactory factory;
    private IModel channel;
    private IConnection connection;
    private readonly AuctionService auctionService;

    public AuctionController(ILogger<AuctionController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _logger.LogInformation("redis connection: " + configuration["redisConnection"]);
        auctionService = new AuctionService(configuration);

        try
        {
            _logger.LogInformation("Connecting to RabbitMQ at {0}:{1}", configuration["rabbitUrl"], configuration["rabbitMQPort"]);
            factory = new ConnectionFactory()
            {
                HostName = configuration["rabbitUrl"] ?? "localhost",
                Port = Convert.ToInt16(configuration["rabbitMQPort"] ?? "5672"),
                UserName = configuration["rabbitmqUsername"] ?? "guest",
                Password = configuration["rabbitmqUserpassword"] ?? "guest"
            };

            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.QueueDeclare(queue: "auction",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("Connected to RabbitMQ successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ at {0}:{1}", configuration["rabbitUrl"], configuration["rabbitMQPort"]);
            throw;
        }
    }

    [HttpGet("Version")]
    public Dictionary<string, string> GetVersion()
    {
        var properties = new Dictionary<string, string>();
        var assembly = typeof(Program).Assembly;

        properties.Add("service", "Auction");
        var ver = System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).FileVersion ?? "Undefined";
        Console.WriteLine($"Version before: {ver}");
        properties.Add("version", ver);

        var feature = HttpContext.Features.Get<IHttpConnectionFeature>();
        var localIPAddr = feature?.LocalIpAddress?.ToString() ?? "N/A";
        properties.Add("local-host-address", localIPAddr);

        return properties;
    }
    

    [HttpPost("AuctionPost")]
    public async Task<IActionResult> AuctionPost([FromBody] AuctionProduct[] auctionProduct)
    {
        string productCreated = "Auctions with the following ProductIDs have been posted: ";
        string productExists = "The following products were NOT posted, as they already exist: ";

        foreach (var info in auctionProduct)
        {
            if (!auctionService.AuctionExists(info.ProductID))
            {
                productCreated += $"{info.ProductID} ";
            }
            else
            {
                productExists += $"{info.ProductID} ";
            }

            // Adding to Redis cache
            auctionService.AddToAuction(info.ProductID, info.ProductStartPrice, info.ProductEndDate);
        }
        _logger.LogInformation($"{productCreated} \n{productExists}");

        return Ok($"{productCreated} \n{productExists}");
    }
    
    [HttpGet("GetAuctions")]
    public IActionResult GetAuctions()
    {
        var auctions = auctionService.GetAllAuctions();
        return Ok(auctions);
    }

    //remember to re-enable Authorize
    //[Authorize]
    [HttpPost("AuctionBid")]
    public async Task<IActionResult> AuctionBid([FromBody] Bid bid)
    {
        try
        {
            var checkAuctionPrice = auctionService.GetAuctionPrice(bid.AuctionID);

            if (checkAuctionPrice == -1)
            {
                string check = $"User with ID {bid.BidUserID} placed a bid on a non-existing auction (AuctionID: {bid.AuctionID}).";
                _logger.LogWarning(check);
                return BadRequest(check);
            }

            if (checkAuctionPrice >= bid.BidPrice)
            {
                string feedback = $"User with ID {bid.BidUserID} placed a bid of {bid.BidPrice}, which is below or equal to the current auction price of {checkAuctionPrice}.";
                _logger.LogWarning(feedback);
                return BadRequest(feedback);
            }

            auctionService.UpdateAuctionPrice(bid.AuctionID, bid.BidPrice);

            string message = JsonConvert.SerializeObject(bid);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: string.Empty,
                        routingKey: "auction",
                        basicProperties: null,
                        body: body);

            _logger.LogInformation($"{message}");
            return Ok($"{message}");
        }
        catch (Exception ex)
        {
            string feedback = "An error occurred while handling the Auction Bid.";
            _logger.LogError(ex, feedback);
            return BadRequest(feedback);
        }
    }
}