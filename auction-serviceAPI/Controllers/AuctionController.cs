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
    public async Task<IActionResult> AuctionPost([FromBody] AuctionProduct[] ProductList)
    {
        string ProductCreated = "Auctions with the following ProductIDs have been posted: ";
        string ProductExists = "The following products were NOT posted, as they already exist: ";

        for (int i = 0; i < ProductList.Length; i++)
        {
            if (auctionService.AuctionExists(ProductList[i].ProductID) == false)
            {
                ProductCreated += ProductList[i].ProductID.ToString() + " ";
            }
            if (auctionService.AuctionExists(ProductList[i].ProductID) == true)
            {
                ProductExists += ProductList[i].ProductID.ToString() + " ";
            }
            // Adding to Redis cache
            auctionService.AddToAuction(ProductList[i].ProductID, ProductList[i].ProductStartPrice, ProductList[i].ProductEndDate);
        }
        _logger.LogInformation(ProductCreated, ProductExists);

        return Ok($"{ProductCreated} \n{ProductExists}");
    }
    [HttpGet("getPrice/{id}")]
    public async Task<IActionResult> GetAuctionPrice(int id)
    {
        // Retrieving the auction price from Redis cache
        var checkAuctionPrice = auctionService.GetAuctionPrice(id);

        if (checkAuctionPrice == -1)
        {
            _logger.LogError("Auction not found for AuctionID: {AuctionID}", id);
            return BadRequest("Id does not exist");
        }

        _logger.LogInformation("Auction price retrieved for AuctionID: {AuctionID}. Price: {AuctionPrice}", id, checkAuctionPrice);
        return Ok(checkAuctionPrice);
    }

    [Authorize]
    [HttpPost("AuctionBid")]
    public async Task<IActionResult> PostAuctionBid([FromBody] Bid bid)
    {
        try
        {
            var checkAuctionPrice = auctionService.GetAuctionPrice(bid.AuctionID);

            if (checkAuctionPrice == -1)
            {
                // Logging a warning if the auction does not exist
                string feedback = $"User with ID {bid.BidUserID} placed a bid on a non-existing auction (AuctionID: {bid.AuctionID}).";
                _logger.LogWarning(feedback);
                return BadRequest(feedback);
            }

            if (checkAuctionPrice >= bid.BidPrice)
            {
                // Logging a warning if the bid price is not higher than the current auction price
                string feedback = $"User with ID {bid.BidUserID} placed a bid of {bid.BidPrice}, which is below or equal to the current auction price of {checkAuctionPrice}.";
                _logger.LogWarning(feedback);
                return BadRequest(feedback);
            }

            // Updating the auction price in Redis cache
            auctionService.SetAuctionPrice(bid.AuctionID, bid.BidPrice);

            string message = JsonConvert.SerializeObject(bid);
            var body = Encoding.UTF8.GetBytes(message);

            // Publishing the bid to RabbitMQ for further processing
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