using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using BidServiceAPI.Controller;
using BidServiceAPI.Models;

namespace BidServiceAPI.MSTest
{
    [TestClass]
    public class BidControllerTests
    {
        private Mock<IAuctionCacheService> _mockAuctionCacheService;
        private Mock<ILogger<BidController>> _mockLogger;

        private Mock<IBidMessagePublisher> _mockPublisher;

        private BidController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockAuctionCacheService = new Mock<IAuctionCacheService>();
            _mockLogger = new Mock<ILogger<BidController>>();
            _mockPublisher = new Mock<IBidMessagePublisher>();
            _controller = new BidController(_mockLogger.Object, _mockAuctionCacheService.Object, _mockPublisher.Object);
        }

        [TestMethod]
        public async Task PlaceBid_BelowMinimumAndCurrentBid_ReturnsBadRequest()
        {
            // Arrange
            var auctionId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _mockAuctionCacheService.Setup(s => s.GetAuctionByIdInCache(auctionId))
                .ReturnsAsync(new AuctionDTO
                {
                    AuctionId = auctionId,
                    Status = "Active",
                    MinBid = 800,
                    CurrentBid = 1000
                });

            var bidRequest = new BidDTO
            {
                AuctionId = auctionId,
                Amount = 700, // < MinBid og CurrentBid
                UserId = userId
            };

            // Act
            var result = await _controller.PlaceBid(bidRequest);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        }
    }
}
