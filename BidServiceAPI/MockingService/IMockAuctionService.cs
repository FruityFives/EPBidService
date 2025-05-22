using BidServiceAPI.Models;

namespace BidServiceAPI.MockingService
{
    public interface IMockAuctionService
    {
        Task<IEnumerable<AuctionDTO>> GetTodaysAuctionsAsync();
    }
}
