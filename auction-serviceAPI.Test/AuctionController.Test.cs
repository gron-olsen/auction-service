using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using auctionServiceAPI.Controllers;
using auctionServiceAPI.Services;
using auctionServiceAPI.Models;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Linq;
global using NUnit.Framework;


namespace auction_serviceAPI.Test;


public class AuctionControllerTests
{
    private readonly AuctionController _auctionController;
    private readonly Mock<ILogger<AuctionController>> _loggerMock;
    private readonly Mock<AuctionService> _auctionServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    [SetUp]
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
        public async Task PostAuction_Test()
        {
            //Arrange
            var auctionsproducts = new AuctionProduct[]
            {
                new AuctionProduct { ProductId = 1, ProductStartPrice = 100, ProductEndDate = DateTime.Now.AddDays(1) },
                new AuctionProduct { ProductId = 2, ProductStartPrice = 200, ProductEndDate = DateTime.Now.AddDays(2) },
                new AuctionProduct { ProductId = 3, ProductStartPrice = 300, ProductEndDate = DateTime.Now.AddDays(7) }
            };

            //Act
            var result = await _auctionController.AuctionPost(auctionsproducts);

            //Assert
            Assert.AreEqual(200, (result as OkObjectResult).StatusCode);
        }


        [Test]
        public async void GetAuctionPrice_Test()
        {
            //Arrange
            var auctionsproducts = new AuctionProduct[]
            {
                new AuctionProduct { ProductId = 1, ProductStartPrice = 100, ProductEndDate = DateTime.Now.AddDays(1) },
                new AuctionProduct { ProductId = 2, ProductStartPrice = 200, ProductEndDate = DateTime.Now.AddDays(2) },
                new AuctionProduct { ProductId = 3, ProductStartPrice = 300, ProductEndDate = DateTime.Now.AddDays(7) }
            };

            //Act
            var result = await _auctionController.GetAuctionPrice(3);

            //Assert
            Assert.AreEqual(200, (result as OkObjectResult));
        }
    }
}
