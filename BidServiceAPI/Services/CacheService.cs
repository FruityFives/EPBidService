using BidServiceAPI.Models;
using BidServiceAPI.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BidServiceAPI.Services
{
    public class CacheService : ICacheService
    {
        private readonly IAuctionHttpClient _auctionClient;
        private readonly IMemoryCache _cache;

        public CacheService(IAuctionHttpClient auctionClient, IMemoryCache cache)
        {
            _auctionClient = auctionClient;
            _cache = cache;
        }

        public async Task<AuctionDTO?> GetAuctionByIdInCache(Guid auctionId)
        {
            var cacheKey = $"auctions-{DateTime.Today:yyyy-MM-dd}";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                return auctions.FirstOrDefault(a => a.AuctionId == auctionId);
            }

            return null;
        }

        public async Task<List<AuctionDTO>> GetTodaysAuctionsInCache()
        {
            var cacheKey = $"auctions-{DateTime.Today:yyyy-MM-dd}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                Console.WriteLine("✅ Cache hit – henter fra cache");
                return auctions.ToList();
            }

            Console.WriteLine("❌ Cache miss – henter fra AuctionService");
            auctions = await _auctionClient.GetTodaysAuctionsAsync();

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.High
            };

            _cache.Set(cacheKey, auctions, cacheEntryOptions);

            return auctions.ToList();
        }

        public Task UpdateAuctionInCache(AuctionDTO auction)
        {
            var cacheKey = $"auctions-{DateTime.Today:yyyy-MM-dd}";

            List<AuctionDTO> updatedList;

            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                updatedList = auctions
                    .Where(a => a.AuctionId != auction.AuctionId)
                    .Append(auction)
                    .ToList();
            }
            else
            {
                // Hvis listen ikke findes i cachen endnu
                updatedList = new List<AuctionDTO> { auction };
            }

            _cache.Set(cacheKey, updatedList, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                Priority = CacheItemPriority.High
            });

            return Task.CompletedTask;
        }

    }
}
