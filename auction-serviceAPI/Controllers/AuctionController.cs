using auctionServiceAPI.Models;
using auctionServiceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;

namespace auctionServiceAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AuctionController : ControllerBase
{

    private readonly ILogger<AuctionController> _logger;


    private AuctionService auctionService;
    public AuctionController(ILogger<AuctionController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _logger.LogInformation("redis connection: " + configuration["redisConnection"]);
        auctionService = new AuctionService(configuration);
    }
    
     [HttpGet("Version")]
    public Dictionary<string, string> GetVersion()
    {
        var properties = new Dictionary<string, string>();
        var assembly = typeof(Program).Assembly;

        properties.Add("service", "Auction");
        var ver = System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).FileVersion ?? "Undefined";
        Console.WriteLine($"Version before: {ver}");
        properties.Add("version",ver);

        var feature = HttpContext.Features.Get<IHttpConnectionFeature>();
        var localIPAddr = feature?.LocalIpAddress?.ToString() ?? "N/A";
        properties.Add("local-host-address", localIPAddr);

        return properties;
    }
    
 [HttpPost("AuctionPost")]
    public async Task<IActionResult> AuctionPost([FromBody] AuctionProduct[] ProductList)
    {
        string ProductCreated = "The following products are hereby active: ";
        string ProductExists = "These products already exist: ";

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
}