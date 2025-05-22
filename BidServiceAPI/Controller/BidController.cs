using Microsoft.AspNetCore.Mvc;
using BidServiceAPI.Models;
using BidServiceAPI.Services;

namespace BidServiceAPI.Controller;

[ApiController]
[Route("[controller]")]
public class BidController : ControllerBase
{
    private readonly ILogger<BidController> _logger;
    private readonly BidService _bidService;

    public BidController(ILogger<BidController> logger, BidService bidService)
    {
        _logger = logger;
        _bidService = bidService;
    }

    [HttpGet("auctions")]
    public async Task<IActionResult> GetTodaysAuctions()
    {
        _logger.LogInformation("Endpoint /auctions called");

        try
        {
            var auctions = await _bidService.GetTodaysAuctionsAsync();
            _logger.LogInformation("Successfully retrieved today's auctions");
            return Ok(auctions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving today's auctions");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("placebid")]
    public async Task<IActionResult> PlaceBid([FromBody] BidDTO bidRequest)
    {
        _logger.LogInformation("Received new bid from user {UserId} on auction {AuctionId}",
            bidRequest.UserId, bidRequest.AuctionId);

        try
        {
            var result = await _bidService.PlaceBidAsync(bidRequest);

            if (result != "Bud accepteret")
            {
                _logger.LogWarning("Bid rejected: {Reason}", result);
                return BadRequest(result);
            }

            _logger.LogInformation("Bid accepted and sent to RabbitMQ for auction {AuctionId}", bidRequest.AuctionId);
            return Ok(result);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while placing bid for auction {AuctionId}", bidRequest.AuctionId);
            return StatusCode(500, "Internal server error");
        }
    }
}
