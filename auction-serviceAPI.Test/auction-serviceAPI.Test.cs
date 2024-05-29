using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using auctionServiceAPI.Controllers;
using auctionServiceAPI.Services;
using auctionServiceAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace auctionServiceAPI.Test
{
    [TestClass]
    public class AuctionControllerTests
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
            _configurationMock.SetupGet(x => x["redisConnection"]).Returns("redis-16065.c56.east-us.azure.redns.redis-cloud.com:16065,password=1234");
            _configurationMock.SetupGet(x => x["rabbitMQPort"]).Returns("5672");
            
            _auctionController = new AuctionController(_loggerMock.Object, _configurationMock.Object);
        }

        [TestMethod]
        public async Task PostAuction_Test()
        {
            // Arrange
            var auctionsproducts = new AuctionProduct[]
            {
                new AuctionProduct { ProductID = 1, ProductStartPrice = 100, ProductEndDate = DateTime.Now.AddDays(1) },
                new AuctionProduct { ProductID = 2, ProductStartPrice = 200, ProductEndDate = DateTime.Now.AddDays(2) },
                new AuctionProduct { ProductID = 3, ProductStartPrice = 300, ProductEndDate = DateTime.Now.AddDays(7) }
            };

            // Act
            var result = await _auctionController.AuctionPost(auctionsproducts);

            // Assert
            Assert.AreEqual(200, (result as OkObjectResult).StatusCode);
        }

        [TestMethod]
        public async Task GetAuctionPrice_Test()
        {
            // Arrange
            var auctionsproducts = new AuctionProduct[]
            {
                new AuctionProduct { ProductID = 1, ProductStartPrice = 100, ProductEndDate = DateTime.Now.AddDays(1) },
                new AuctionProduct { ProductID = 2, ProductStartPrice = 200, ProductEndDate = DateTime.Now.AddDays(2) },
                new AuctionProduct { ProductID = 3, ProductStartPrice = 300, ProductEndDate = DateTime.Now.AddDays(7) }
            };

            // Act
            var result = await _auctionController.GetAuctionPrice(3);

            // Assert
            Assert.AreEqual(200, (result as OkObjectResult).StatusCode);
        }
    }
}
