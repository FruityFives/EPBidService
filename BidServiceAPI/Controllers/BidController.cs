using BidServiceAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BidServiceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BidController : ControllerBase
    {
        private readonly ICacheService _bidService;

        public BidController(ICacheService bidService) // ← korrekt: bruger interface
        {
            _bidService = bidService;
        }


        [HttpGet("auctions")]
        public async Task<IActionResult> GetTodaysAuctions()
        {
            var auctions = await _bidService.GetTodaysAuctionsAsync();
            return Ok(auctions);
        }
    }
}
