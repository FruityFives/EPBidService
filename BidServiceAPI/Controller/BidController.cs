using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;

namespace BidServiceAPI.Controller;

[ApiController]
[Route("[controller]")]
public class BidController : ControllerBase
{
    private readonly IAuctionService _auctionService;
    private readonly ILogger<BidController> _logger;

    public BidController(ILogger<BidController> logger, IAuctionService auctionService)
    {
        _logger = logger;
        _auctionService = auctionService;
    }

    [HttpPost("placebid")]
    public async Task<IActionResult> PlaceBid([FromBody] BidDTO bidRequest)
    {
        var auction = await _auctionService.GetAuctionByIdAsync(bidRequest.AuctionId);

        if (auction == null)
            return NotFound("Auktion ikke fundet");

        if (auction.Status != "Active")
            return BadRequest("Auktionen er ikke aktiv");

        if (bidRequest.Amount < auction.MinBid || bidRequest.Amount <= auction.CurrentBid)
            return BadRequest("Buddet er ugyldigt");

        return Ok("Bud accepteret");
    }
}
