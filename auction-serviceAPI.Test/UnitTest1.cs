using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using auctionServiceAPI.Controllers;
using auctionServiceAPI.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Linq;
global using NUnit.Framework;
using auctionServiceAPI.Models;
namespace auction_serviceAPI.Test;

public class Tests
{
    private readonly AuctionController _auctionController;
    private readonly Mock<ILogger<AuctionController>> _loggerMock;
    private readonly Mock<AuctionService> _auctionServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;

    public AuctionControllerTests()
    {
        _loggerMock = new Mock<ILogger<AuctionController>>();
        _auctionServiceMock = new Mock<AuctionService>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.SetupGet(x => x["server"]).Returns("localhost");
        _configurationMock.SetupGet(x => x["port"]).Returns("27017");
        _configurationMock.SetupGet(x => x["collection"]).Returns("auctionCol");
        _configurationMock.SetupGet(x => x["database"]).Returns("auctionDB");

        _auctionController = new AuctionController(_loggerMock.Object, _configurationMock.Object);

        [Test]
        public void PostAuctionOK_Test()
        {
            //arrange

            //act

            //assert
            Assert.Pass();

        }
        [Test]
        public void PostAuctionBid_Test()
        {
            //arrange

            //act

            //assert

        }
    }
}
