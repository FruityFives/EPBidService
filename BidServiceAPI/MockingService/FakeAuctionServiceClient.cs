using BidServiceAPI.Models;

namespace BidServiceAPI.MockingService
{
    public class FakeAuctionServiceClient : IAuctionServiceClient
    {
        public Task<IEnumerable<AuctionDTO>> GetTodaysAuctionsAsync()
        {
            var dummyData = new List<AuctionDTO>
            {
                new AuctionDTO
                {
                    AuctionId = Guid.NewGuid(),
                    Status = "Active",
                    MinBid = 100,
                    CurrentBid = 150


                },
                new AuctionDTO
                {
                    AuctionId = Guid.NewGuid(),
                    Status = "Closed",
                    MinBid = 200,
                    CurrentBid = 250

                }
            };

            return Task.FromResult<IEnumerable<AuctionDTO>>(dummyData);
        }
    }
}
