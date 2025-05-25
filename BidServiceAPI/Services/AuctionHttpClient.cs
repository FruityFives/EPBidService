using BidServiceAPI.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            var response = await _httpClient.GetAsync("api/catalog/all-auctions-today");
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine("🔎 RAW JSON fra AuctionService:");
            Console.WriteLine(content);

            response.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() } // 🔄 understøtter enum som "Active"
            };

            var auctions = JsonSerializer.Deserialize<List<AuctionRaw>>(content, options);

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

            [JsonConverter(typeof(JsonStringEnumConverter))] // 🔄 konverter "Active" → enum
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
