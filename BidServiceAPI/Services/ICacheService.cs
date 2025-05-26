using BidServiceAPI.Models;

namespace BidServiceAPI.Services
{
    public interface ICacheService
    {
        Task<AuctionDTO?> GetAuctionByIdInCache(Guid auctionId);

        Task UpdateAuctionInCache(AuctionDTO auction);

        Task<List<AuctionDTO>> GetTodaysAuctionsInCache();

        Task AddAuctionToCache(AuctionDTO auction);


    }

}
