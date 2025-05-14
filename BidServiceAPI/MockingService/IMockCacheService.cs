using BidServiceAPI.Models;

public interface IMockCacheService
{
    Task<AuctionDTO?> GetAuctionByIdInCache(Guid auctionId);

    Task UpdateAuctionInCache(AuctionDTO auction);

    Task<List<AuctionDTO>> GetAuctionsForTodayInCache();
}
