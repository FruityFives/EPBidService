using BidServiceAPI.Models;

namespace BidServiceAPI.MockingService
{
    public class MockAuctionService : IMockAuctionService
    {
        public Task<IEnumerable<AuctionDTO>> GetTodaysAuctionsAsync()
        {
            var dummyData = new List<AuctionDTO>
            {
                new AuctionDTO
                {
                    AuctionId = Guid.NewGuid(),
                    Status = AuctionStatus.Active,
                    MinBid = 100,
                    CurrentBid = 150


                },
                new AuctionDTO
                {
                    AuctionId = Guid.NewGuid(),
                    Status = AuctionStatus.Active,
                    MinBid = 200,
                    CurrentBid = 250

                }
            };

            return Task.FromResult<IEnumerable<AuctionDTO>>(dummyData);
        }
    }
}
