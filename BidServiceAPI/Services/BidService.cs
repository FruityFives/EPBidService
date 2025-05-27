using BidServiceAPI.Models;
using Microsoft.Extensions.Logging;

namespace BidServiceAPI.Services
{
    /// <summary>
    /// Service, der håndterer logik for budgivning og interaktion med cache og RabbitMQ.
    /// </summary>
    public class BidService
    {
        private readonly ICacheService _cache;
        private readonly IBidMessagePublisher _publisher;
        private readonly ILogger<BidService> _logger;

        /// <summary>
        /// Initialiserer en ny instans af <see cref="BidService"/>.
        /// </summary>
        /// <param name="cache">Cache-service til håndtering af auktioner.</param>
        /// <param name="publisher">RabbitMQ publisher til afsendelse af bud.</param>
        /// <param name="logger">Logger til logning af hændelser.</param>
        public BidService(ICacheService cache, IBidMessagePublisher publisher, ILogger<BidService> logger)
        {
            _cache = cache;
            _publisher = publisher;
            _logger = logger;
        }

        /// <summary>
        /// Henter en liste over alle aktive auktioner fra cachen.
        /// </summary>
        /// <returns>En liste med <see cref="AuctionDTO"/> objekter, der har status "Active".</returns>
        public async Task<List<AuctionDTO>> GetActiveAuctionsAsync()
        {
            return await _cache.GetAuctionsByStatusInCache(AuctionStatus.Active);
        }

        /// <summary>
        /// Validerer og forsøger at placere et bud på en auktion.
        /// Returnerer <c>true</c>, hvis buddet accepteres og sendes.
        /// Returnerer <c>false</c>, hvis buddet er ugyldigt eller auktionen ikke findes.
        /// </summary>
        /// <param name="bidRequest">Data transfer object med oplysninger om buddet.</param>
        /// <returns><c>true</c> hvis buddet blev accepteret, ellers <c>false</c>.</returns>
        public async Task<bool> PlaceBidAsync(BidDTO bidRequest)
        {
            _logger.LogInformation("Modtager bud fra bruger {UserId} på auktion {AuctionId} med beløb {Amount}",
                bidRequest.UserId, bidRequest.AuctionId, bidRequest.Amount);

            var activeAuctions = await _cache.GetAuctionsByStatusInCache(AuctionStatus.Active);
            var auction = activeAuctions.FirstOrDefault(a => a.AuctionId == bidRequest.AuctionId);

            if (auction == null)
            {
                _logger.LogWarning("❌ Auktionen er ikke aktiv eller findes ikke. ID: {AuctionId}", bidRequest.AuctionId);
                return false;
            }

            if (bidRequest.Amount < auction.MinBid || bidRequest.Amount <= auction.CurrentBid)
            {
                _logger.LogWarning("Bud på {Amount} for auktion {AuctionId} afvist. MinBud: {MinBid}, AktuelBud: {CurrentBid}",
                    bidRequest.Amount, auction.AuctionId, auction.MinBid, auction.CurrentBid);
                return false;
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

            return true;
        }

    }
}
