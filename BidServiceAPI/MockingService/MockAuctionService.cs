using BidServiceAPI.Models;
public class MockAuctionService : IAuctionService
{
    public Task<AuctionDTO?> GetAuctionByIdAsync(string auctionId)
    {
        return Task.FromResult(new AuctionDTO
        {
            Id = Guid.Parse(auctionId),
            MinBid = 800,
            CurrentBid = 1000,
            Status = "Active"
        });
    }
}
