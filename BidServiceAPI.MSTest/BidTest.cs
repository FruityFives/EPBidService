using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using BidServiceAPI.Services;
using BidServiceAPI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        /// <summary>
        /// Tester at metoden returnerer false, hvis auktionen ikke findes i cache.
        /// </summary>
        [TestMethod]
        public async Task PlaceBid_AuctionNotFoundInCache_ReturnsFalse()
        {
            // Arrange
            _mockCache.Setup(c => c.GetAuctionsByStatusInCache(AuctionStatus.Active))
                      .ReturnsAsync(new List<AuctionDTO>());

            var bid = new BidDTO { AuctionId = Guid.NewGuid() };

            // Act
            var result = await _service.PlaceBidAsync(bid);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tester at metoden returnerer false, hvis auktionen er lukket og derfor ikke findes i listen over aktive auktioner.
        /// </summary>
        [TestMethod]
        public async Task PlaceBid_AuctionClosed_NotInActiveList_ReturnsFalse()
        {
            // Arrange
            var auctionId = Guid.NewGuid();
            var bid = new BidDTO
            {
                AuctionId = auctionId,
                Amount = 200,
                UserId = Guid.NewGuid()
            };

            // Auktionen er teknisk set lukket, men PlaceBidAsync henter kun "aktive" auktioner,
            // så vi simulerer at den ikke findes i listen over aktive.
            _mockCache.Setup(c => c.GetAuctionsByStatusInCache(AuctionStatus.Active))
                      .ReturnsAsync(new List<AuctionDTO>()); // tom liste

            // Act
            var result = await _service.PlaceBidAsync(bid);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tester at metoden returnerer false, hvis buddet er under minimum eller eksisterende bud.
        /// </summary>
        [TestMethod]
        public async Task PlaceBid_BelowMinBid_ReturnsFalse()
        {
            // Arrange
            var auctionId = Guid.NewGuid();
            var auction = new AuctionDTO
            {
                AuctionId = auctionId,
                Status = AuctionStatus.Active,
                MinBid = 500,
                CurrentBid = 600
            };

            _mockCache.Setup(c => c.GetAuctionsByStatusInCache(AuctionStatus.Active))
                      .ReturnsAsync(new List<AuctionDTO> { auction });

            var bid = new BidDTO { AuctionId = auctionId, Amount = 450 };

            // Act
            var result = await _service.PlaceBidAsync(bid);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Tester at et gyldigt bud opdaterer cache og sendes til RabbitMQ.
        /// </summary>
        [TestMethod]
        public async Task PlaceBid_ValidBid_UpdatesCacheAndPublishes_ReturnsTrue()
        {
            // Arrange
            var auctionId = Guid.NewGuid();
            var auction = new AuctionDTO
            {
                AuctionId = auctionId,
                Status = AuctionStatus.Active,
                MinBid = 100,
                CurrentBid = 150
            };

            _mockCache.Setup(c => c.GetAuctionsByStatusInCache(AuctionStatus.Active))
                      .ReturnsAsync(new List<AuctionDTO> { auction });

            var bid = new BidDTO
            {
                AuctionId = auctionId,
                Amount = 200,
                UserId = Guid.NewGuid()
            };

            // Act
            var result = await _service.PlaceBidAsync(bid);

            // Assert
            Assert.IsTrue(result);
            _mockCache.Verify(c => c.UpdateAuctionInCache(It.Is<AuctionDTO>(a => a.CurrentBid == 200)), Times.Once);
            _mockPublisher.Verify(p => p.PublishBidAsync(It.IsAny<Bid>()), Times.Once);
        }

        /// <summary>
        /// Tester at GetActiveAuctionsAsync returnerer den cachede liste af aktive auktioner.
        /// </summary>
        [TestMethod]
        public async Task GetActiveAuctions_ReturnsCachedList()
        {
            // Arrange
            var expected = new List<AuctionDTO> { new AuctionDTO(), new AuctionDTO() };

            _mockCache
                .Setup(c => c.GetAuctionsByStatusInCache(AuctionStatus.Active))
                .ReturnsAsync(expected);

            // Act
            var result = await _service.GetActiveAuctionsAsync();

            // Assert
            Assert.AreEqual(2, result.Count);
        }
    }
}
