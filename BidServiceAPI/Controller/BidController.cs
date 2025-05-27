using Microsoft.AspNetCore.Mvc;
using BidServiceAPI.Models;
using BidServiceAPI.Services;

namespace BidServiceAPI.Controller;

[ApiController]
[Route("api/bid")]
public class BidController : ControllerBase
{
    private readonly ILogger<BidController> _logger;
    private readonly BidService _bidService;

    public BidController(ILogger<BidController> logger, BidService bidService)
    {
        _logger = logger;
        _bidService = bidService;
    }

    /// <summary>
    /// Modtager og behandler et bud fra en bruger på en auktion.
    /// </summary>
    /// <param name="bidRequest">DTO med information om buddet, herunder bruger-ID, auktion-ID og beløb.</param>
    /// <returns>
    /// 200 OK hvis buddet accepteres og sendes til RabbitMQ.  
    /// 400 Bad Request hvis buddet afvises.  
    /// 500 Internal Server Error ved fejl under behandling.
    /// </returns>
    [HttpPost("placebid")]
    public async Task<IActionResult> PlaceBid([FromBody] BidDTO bidRequest)
    {
        _logger.LogInformation("Received new bid from user {UserId} on auction {AuctionId}",
            bidRequest.UserId, bidRequest.AuctionId);

        try
        {
            var success = await _bidService.PlaceBidAsync(bidRequest);

            if (!success)
            {
                _logger.LogWarning("Bid rejected for auction {AuctionId}", bidRequest.AuctionId);
                return BadRequest("Bud blev afvist");
            }

            _logger.LogInformation("Bid accepted and sent to RabbitMQ for auction {AuctionId}", bidRequest.AuctionId);
            return Ok("Bud accepteret");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while placing bid for auction {AuctionId}", bidRequest.AuctionId);
            return StatusCode(500, "Internal server error");
        }
    }


    /// <summary>
    /// Henter en liste over alle aktive auktioner fra cache.
    /// </summary>
    /// <returns>
    /// 200 OK med listen over aktive auktioner.  
    /// 500 Internal Server Error hvis noget går galt.
    /// </returns>
    [HttpGet("activeauctions")]
    public async Task<IActionResult> GetActiveAuctions()
    {
        _logger.LogInformation("Henter aktive auktioner...");

        try
        {
            var auctions = await _bidService.GetActiveAuctionsAsync();
            return Ok(auctions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af aktive auktioner");
            return StatusCode(500, "Internal server error");
        }
    }
}
