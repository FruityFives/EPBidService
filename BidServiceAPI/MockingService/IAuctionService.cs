using BidServiceAPI.Models;

public interface IAuctionService
{
    Task<AuctionDTO?> GetAuctionByIdAsync(string auctionId);
}
