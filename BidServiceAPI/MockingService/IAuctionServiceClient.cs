using BidServiceAPI.Models;

namespace BidServiceAPI.MockingService
{
    public interface IAuctionServiceClient
    {
        Task<IEnumerable<AuctionDTO>> GetTodaysAuctionsAsync();
    }
}
