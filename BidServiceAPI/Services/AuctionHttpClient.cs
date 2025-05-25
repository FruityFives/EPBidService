using BidServiceAPI.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace BidServiceAPI.Services
{
    public class AuctionHttpClient : IAuctionHttpClient
    {
        private readonly HttpClient _httpClient;

        public AuctionHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<AuctionDTO>> GetTodaysAuctionsAsync()
        {
            var auctions = await _httpClient.GetFromJsonAsync<List<AuctionRaw>>("api/catalog/all-auctions-today");

            if (auctions == null)
                return new List<AuctionDTO>();

            return auctions.Select(a => new AuctionDTO
            {
                AuctionId = a.AuctionId,
                Status = a.Status,
                MinBid = Convert.ToDecimal(a.MinPrice),
                CurrentBid = a.CurrentBid?.Amount ?? 0
            });
        }

        // Intern type til parsing af JSON fra AuctionService
        private class AuctionRaw
        {
            public Guid AuctionId { get; set; }
            public AuctionStatus Status { get; set; }
            public double MinPrice { get; set; }
            public BidRaw? CurrentBid { get; set; }

            public class BidRaw
            {
                public decimal Amount { get; set; }
            }
        }
    }
}
