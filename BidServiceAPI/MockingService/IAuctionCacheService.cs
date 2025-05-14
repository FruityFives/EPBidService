using BidServiceAPI.Models;

public interface IAuctionCacheService
{
    Task<AuctionDTO?> GetAuctionByIdInCache(Guid auctionId);

    Task UpdateAuctionInCache(AuctionDTO auction);

    Task<List<AuctionDTO>> GetAuctionsForTodayInCache();
}
