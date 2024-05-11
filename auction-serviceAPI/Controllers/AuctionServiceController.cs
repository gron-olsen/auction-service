using Microsoft.AspNetCore.Mvc;
using auctionServiceAPI.Models;

namespace auction_serviceAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AuctionServiceController : ControllerBase
{

    private readonly ILogger<AuctionServiceController> _logger;

    public AuctionServiceController(ILogger<AuctionServiceController> logger)
    {
        _logger = logger;
    }

    [HttpGet("getAuction/{id}")]

    [HttpPost("postAuction")]

    [HttpGet("version")]
    public IEnumerable<string> GetVersion()
    {
        var properties = new List<string>();
        var assembly = typeof(Program).Assembly;
        foreach (var attribute in assembly.GetCustomAttributesData())
        {
            properties.Add($"{attribute.AttributeType.Name} - {attribute.ToString()} \n");
        }
        return properties;
    }

}
