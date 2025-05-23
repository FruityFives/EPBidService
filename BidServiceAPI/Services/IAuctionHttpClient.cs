using BidServiceAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BidServiceAPI.Services
{
    public interface IAuctionHttpClient
    {
        Task<IEnumerable<AuctionDTO>> GetTodaysAuctionsAsync();
    }
}
