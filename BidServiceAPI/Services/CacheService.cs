using BidServiceAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<CacheService> _logger;

        public CacheService(IAuctionHttpClient auctionClient, IMemoryCache cache, ILogger<CacheService> logger)
        {
            _auctionClient = auctionClient;
            _cache = cache;
            _logger = logger;
        }

        public Task<AuctionDTO?> GetAuctionByIdInCache(Guid auctionId)
        {
            var cacheKey = $"auctions-{DateTime.Today:yyyy-MM-dd}";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                return Task.FromResult(auctions.FirstOrDefault(a => a.AuctionId == auctionId));
            }

            return Task.FromResult<AuctionDTO?>(null);
        }

        public async Task<List<AuctionDTO>> GetTodaysAuctionsInCache()
        {
            var cacheKey = $"auctions-{DateTime.Today:yyyy-MM-dd}";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                _logger.LogInformation("✅ Cache hit – henter fra cache");
                return auctions.ToList();
            }

            _logger.LogInformation("❌ Cache miss – henter fra AuctionService");
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

                _logger.LogInformation("♻️ Opdaterede eksisterende auktion i cache: {AuctionId}", auction.AuctionId);
            }
            else
            {
                updatedList = new List<AuctionDTO> { auction };
                _logger.LogInformation("🆕 Oprettede ny cache og tilføjede auktion: {AuctionId}", auction.AuctionId);
            }

            _cache.Set(cacheKey, updatedList, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                Priority = CacheItemPriority.High
            });

            return Task.CompletedTask;
        }

        public Task AddAuctionToCache(AuctionDTO auction)
        {
            var cacheKey = $"auctions-{DateTime.Today:yyyy-MM-dd}";
            List<AuctionDTO> updatedList;

            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                updatedList = auctions.Append(auction).ToList();
                _logger.LogInformation("🆕 Tilføjede auktion til eksisterende cache: {AuctionId}", auction.AuctionId);
            }
            else
            {
                updatedList = new List<AuctionDTO> { auction };
                _logger.LogInformation("🆕 Oprettede ny cache og tilføjede auktion: {AuctionId}", auction.AuctionId);
            }

            _cache.Set(cacheKey, updatedList, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.High
            });

            return Task.CompletedTask;
        }
    }
}
