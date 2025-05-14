using BidServiceAPI.Models;

public class MockAuctionCacheService : IAuctionCacheService
{
    private readonly List<AuctionDTO> _auctions = new();

    public MockAuctionCacheService()
    {
        // Seedet data ved opstart – klar til brug i tests og controller

        var auction1 = new AuctionDTO
        {
            AuctionId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MinBid = 800,
            CurrentBid = 1000,
            Status = "Active"
        };

        var auction2 = new AuctionDTO
        {
            AuctionId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            MinBid = 500,
            CurrentBid = 750,
            Status = "Closed"
        };

        var auction3 = new AuctionDTO
        {
            AuctionId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            MinBid = 200,
            CurrentBid = 200,
            Status = "Active"
        };

        _auctions.AddRange(new[] { auction1, auction2, auction3 });
    }

    public Task<AuctionDTO?> GetAuctionByIdInCache(Guid auctionId)
    {
        var auction = _auctions.Find(a => a.AuctionId == auctionId);
        return Task.FromResult(auction);
    }

    public Task UpdateAuctionInCache(AuctionDTO auction)
    {
        var existingAuction = _auctions.Find(a => a.AuctionId == auction.AuctionId);

        if (existingAuction != null)
        {
            existingAuction.CurrentBid = auction.CurrentBid;
        }
        else
        {
            throw new InvalidOperationException("Auktionen findes ikke i cachen.");
        }

        return Task.CompletedTask;
    }

    public Task<List<AuctionDTO>> GetAuctionsForTodayInCache()
    {
        return Task.FromResult(_auctions.ToList());
    }
}
