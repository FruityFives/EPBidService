using BidServiceAPI.Models;

namespace BidServiceAPI.Services
{
    public interface IBidService
    {
        Task<string> PlaceBidAsync(BidDTO bidRequest);
        Task<List<AuctionDTO>> GetTodaysAuctionsAsync();
    }

}
