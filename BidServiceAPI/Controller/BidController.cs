using Microsoft.AspNetCore.Mvc;
using BidServiceAPI.Models;

namespace BidServiceAPI.Controller;

[ApiController]
[Route("[controller]")]
public class BidController : ControllerBase
{
    private readonly IAuctionCacheService _auctionService;
    private readonly ILogger<BidController> _logger;
    private readonly IBidMessagePublisher _publisher;

    public BidController(
        ILogger<BidController> logger,
        IAuctionCacheService auctionService,
        IBidMessagePublisher publisher)
    {
        _logger = logger;
        _auctionService = auctionService;
        _publisher = publisher;
    }

    [HttpPost("placebid")]
    public async Task<IActionResult> PlaceBid([FromBody] BidDTO bidRequest)
    {
        _logger.LogInformation("Modtog bud: {Amount} på auktion {AuctionId} fra bruger {UserId}",
            bidRequest.Amount, bidRequest.AuctionId, bidRequest.UserId);

        var auction = await _auctionService.GetAuctionByIdInCache(bidRequest.AuctionId);

        if (auction == null)
        {
            _logger.LogWarning("Auktion med ID {AuctionId} ikke fundet", bidRequest.AuctionId);
            return NotFound("Auktion ikke fundet");
        }

        if (auction.Status != "Active")
        {
            _logger.LogWarning("Auktion {AuctionId} er ikke aktiv", bidRequest.AuctionId);
            return BadRequest("Auktionen er ikke aktiv");
        }

        if (bidRequest.Amount < auction.MinBid || bidRequest.Amount <= auction.CurrentBid)
        {
            _logger.LogWarning("Ugyldigt bud ({Amount}) på auktion {AuctionId}. MinBud: {Min}, AktuelBud: {Current}",
                bidRequest.Amount, auction.AuctionId, auction.MinBid, auction.CurrentBid);
            return BadRequest("Buddet er ugyldigt");
        }

        // Buddet er gyldigt — opdater cache
        auction.CurrentBid = bidRequest.Amount;
        await _auctionService.UpdateAuctionInCache(auction);

        var bid = new Bid
        {
            BidId = Guid.NewGuid(),
            AuctionId = bidRequest.AuctionId,
            UserId = bidRequest.UserId,
            Amount = bidRequest.Amount,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Bud valideret. Klar til at sende til RabbitMQ. BidId: {BidId}", bid.BidId);

        await _publisher.PublishBidAsync(bid);

        _logger.LogInformation("Bud {BidId} sendt til RabbitMQ for auktion {AuctionId}", bid.BidId, bid.AuctionId);

        return Ok("Bud accepteret og sendt til RabbitMQ");
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAuctionsForToday()
    {
        var auctions = await _auctionService.GetAuctionsForTodayInCache();

        if (auctions == null || !auctions.Any())
        {
            _logger.LogWarning("Ingen auktioner fundet i cache");
            return NotFound("Ingen auktioner fundet.");
        }

        _logger.LogInformation("Returnerer {Count} auktioner fra cache", auctions.Count);
        return Ok(auctions);
    }
}
