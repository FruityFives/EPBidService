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
        var auctions = await _bidService.GetTodaysAuctionsAsync();
        return Ok(auctions);
    }

    [HttpPost("placebid")]
    public async Task<IActionResult> PlaceBid([FromBody] BidDTO bidRequest)
    {
        var result = await _bidService.PlaceBidAsync(bidRequest);

        if (result != "Bud accepteret og sendt til rabbitMQ")
            return BadRequest(result);

        return Ok(result);
    }
}