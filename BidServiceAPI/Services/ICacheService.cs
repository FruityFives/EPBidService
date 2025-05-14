using BidServiceAPI.Models;

namespace BidServiceAPI.Services
{
    public interface ICacheService
    {
        Task<IEnumerable<AuctionDTO>> GetTodaysAuctionsAsync();
    }
}
