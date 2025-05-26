using BidServiceAPI.Models;

namespace BidServiceAPI.Services
{
    public interface ICacheService
    {
        Task<AuctionDTO?> GetAuctionByIdInCache(Guid auctionId);

        Task UpdateAuctionInCache(AuctionDTO auction);

        Task<List<AuctionDTO>> GetAuctionsByStatusInCache(AuctionStatus status);

        Task AddAuctionToCache(AuctionDTO auction);


    }

}
