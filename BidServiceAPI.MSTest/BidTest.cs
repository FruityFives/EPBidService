using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using BidServiceAPI.Services;
using BidServiceAPI.Models;

namespace BidServiceAPI.MSTest
{
    [TestClass]
    public class BidServiceTests
    {
        private Mock<ICacheService> _mockCache;
        private Mock<IBidMessagePublisher> _mockPublisher;
        private Mock<ILogger<BidService>> _mockLogger;

        private BidService _service;

        [TestInitialize]
        public void Setup()
        {
            _mockCache = new Mock<ICacheService>();
            _mockPublisher = new Mock<IBidMessagePublisher>();
            _mockLogger = new Mock<ILogger<BidService>>();

            _service = new BidService(_mockCache.Object, _mockPublisher.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task PlaceBid_InvalidAuction_ReturnsNotFound()
        {
            _mockCache.Setup(c => c.GetAuctionByIdInCache(It.IsAny<Guid>()))
                      .ReturnsAsync((AuctionDTO?)null);

            var result = await _service.PlaceBidAsync(new BidDTO { AuctionId = Guid.NewGuid() });

            Assert.AreEqual("Auktion ikke fundet", result);
        }

        [TestMethod]
        public async Task PlaceBid_InactiveAuction_ReturnsBadRequest()
        {
            _mockCache.Setup(c => c.GetAuctionByIdInCache(It.IsAny<Guid>()))
                      .ReturnsAsync(new AuctionDTO { Status = "Closed" });

            var result = await _service.PlaceBidAsync(new BidDTO { AuctionId = Guid.NewGuid() });

            Assert.AreEqual("Auktionen er ikke aktiv", result);
        }

        [TestMethod]
        public async Task PlaceBid_BelowMinBid_ReturnsBadRequest()
        {
            _mockCache.Setup(c => c.GetAuctionByIdInCache(It.IsAny<Guid>()))
                      .ReturnsAsync(new AuctionDTO
                      {
                          Status = "Active",
                          MinBid = 500,
                          CurrentBid = 600
                      });

            var result = await _service.PlaceBidAsync(new BidDTO { Amount = 450, AuctionId = Guid.NewGuid() });

            Assert.AreEqual("Buddet er ugyldigt", result);
        }

        [TestMethod]
        public async Task PlaceBid_ValidBid_UpdatesCacheAndPublishes()
        {
            var auctionId = Guid.NewGuid();
            var auction = new AuctionDTO
            {
                AuctionId = auctionId,
                Status = "Active",
                MinBid = 100,
                CurrentBid = 150
            };

            _mockCache.Setup(c => c.GetAuctionByIdInCache(auctionId))
                      .ReturnsAsync(auction);

            var bid = new BidDTO
            {
                AuctionId = auctionId,
                Amount = 200,
                UserId = Guid.NewGuid()
            };

            var result = await _service.PlaceBidAsync(bid);

            _mockCache.Verify(c => c.UpdateAuctionInCache(It.Is<AuctionDTO>(a => a.CurrentBid == 200)), Times.Once);
            _mockPublisher.Verify(p => p.PublishBidAsync(It.IsAny<Bid>()), Times.Once);

            Assert.AreEqual("Bud accepteret", result);
        }

        [TestMethod]
        public async Task GetTodaysAuctions_ReturnsCachedList()
        {
            var expected = new List<AuctionDTO> { new AuctionDTO(), new AuctionDTO() };

            _mockCache.Setup(c => c.GetTodaysAuctionsInCache()).ReturnsAsync(expected);

            var result = await _service.GetTodaysAuctionsAsync();

            Assert.AreEqual(2, result.Count);
        }
    }
}
