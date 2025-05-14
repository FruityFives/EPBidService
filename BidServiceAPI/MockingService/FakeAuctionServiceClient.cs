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
                    Id = Guid.NewGuid(),
                    Status = "Active",
                    MinBid = 100,


                },
                new AuctionDTO
                {
                    Id = Guid.NewGuid(),
                    Status = "Closed",
                    MinBid = 200,

                }
            };

            return Task.FromResult<IEnumerable<AuctionDTO>>(dummyData);
        }
    }
}
