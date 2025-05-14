using BidServiceAPI.Models;
using Microsoft.Extensions.Logging;

namespace BidServiceAPI.Services
{
    public class BidService
    {
        private readonly ICacheService _cache;
        private readonly IBidMessagePublisher _publisher;
        private readonly ILogger<BidService> _logger;

        public BidService(ICacheService cache, IBidMessagePublisher publisher, ILogger<BidService> logger)
        {
            _cache = cache;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<List<AuctionDTO>> GetTodaysAuctionsAsync()
        {
            return await _cache.GetTodaysAuctionsInCache();
        }

        public async Task<string> PlaceBidAsync(BidDTO bidRequest)
        {
            _logger.LogInformation("Modtager bud fra bruger {UserId} på auktion {AuctionId} med beløb {Amount}",
                bidRequest.UserId, bidRequest.AuctionId, bidRequest.Amount);

            var auction = await _cache.GetAuctionByIdInCache(bidRequest.AuctionId);

            if (auction == null)
            {
                _logger.LogWarning("Auktion med ID {AuctionId} blev ikke fundet i cachen", bidRequest.AuctionId);
                return "Auktion ikke fundet";
            }

            if (auction.Status != "Active")
            {
                _logger.LogWarning("Auktion {AuctionId} er ikke aktiv. Status: {Status}", auction.AuctionId, auction.Status);
                return "Auktionen er ikke aktiv";
            }

            if (bidRequest.Amount < auction.MinBid || bidRequest.Amount <= auction.CurrentBid)
            {
                _logger.LogWarning("Bud på {Amount} for auktion {AuctionId} afvist. MinBud: {MinBid}, AktuelBud: {CurrentBid}",
                    bidRequest.Amount, auction.AuctionId, auction.MinBid, auction.CurrentBid);
                return "Buddet er ugyldigt";
            }

            auction.CurrentBid = bidRequest.Amount;
            await _cache.UpdateAuctionInCache(auction);
            _logger.LogInformation("Auktion {AuctionId} opdateret med nyt bud: {Amount}", auction.AuctionId, auction.CurrentBid);

            var bid = new Bid
            {
                BidId = Guid.NewGuid(),
                AuctionId = bidRequest.AuctionId,
                UserId = bidRequest.UserId,
                Amount = bidRequest.Amount,
                Timestamp = DateTime.UtcNow
            };

            await _publisher.PublishBidAsync(bid);
            _logger.LogInformation("Bud sendt til RabbitMQ. BidId: {BidId}, Auktion: {AuctionId}", bid.BidId, bid.AuctionId);

            return "Bud accepteret";
        }

    }
}