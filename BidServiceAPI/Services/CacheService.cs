using BidServiceAPI.MockingService;
using BidServiceAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BidServiceAPI.Services
{
    public class CacheService : ICacheService

    {
        private readonly IAuctionServiceClient _auctionClient;
        private readonly IMemoryCache _cache;

        public CacheService(IAuctionServiceClient auctionClient, IMemoryCache cache)
        {
            _auctionClient = auctionClient;
            _cache = cache;
        }

        public async Task<IEnumerable<AuctionDTO>> GetTodaysAuctionsAsync()
        {
            var cacheKey = $"auctions-{DateTime.Today:yyyy-MM-dd}";

            // 🔍 Midlertidig linje for at inspicere cache
            var existing = _cache.Get<IEnumerable<AuctionDTO>>(cacheKey);

            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                Console.WriteLine("✅ Cache hit");
                return auctions!;
            }

            Console.WriteLine("❌ Cache miss – henter fra AuctionServiceClient");
            auctions = await _auctionClient.GetTodaysAuctionsAsync();

            // 👇 Her definerer du cache-indstillingerne
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.High
            };

            // 👇 Her bruger du dem
            _cache.Set(cacheKey, auctions, cacheEntryOptions);

            return auctions;
        }
    }
}
