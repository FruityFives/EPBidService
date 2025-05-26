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
            foreach (AuctionStatus status in Enum.GetValues(typeof(AuctionStatus)))
            {
                var cacheKey = $"auctions-{status.ToString().ToLowerInvariant()}";

                if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
                {
                    var match = auctions.FirstOrDefault(a => a.AuctionId == auctionId);
                    if (match != null)
                        return Task.FromResult<AuctionDTO?>(match);
                }
            }

            return Task.FromResult<AuctionDTO?>(null);
        }

        public Task<List<AuctionDTO>> GetAuctionsByStatusInCache(AuctionStatus status)
        {
            var cacheKey = $"auctions-{status.ToString().ToLowerInvariant()}";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                _logger.LogInformation("✅ Cache hit for status: {Status}", status);
                return Task.FromResult(auctions.ToList());
            }

            _logger.LogWarning("❌ Ingen cache fundet for status: {Status}", status);
            return Task.FromResult(new List<AuctionDTO>());
        }

        public Task UpdateAuctionInCache(AuctionDTO auction)
        {
            var cacheKey = $"auctions-{auction.Status.ToString().ToLowerInvariant()}";
            _logger.LogInformation("♻️ Opdaterer cache for status: {Status}, key: {CacheKey}", auction.Status, cacheKey);

            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                var updated = auctions
                    .Where(a => a.AuctionId != auction.AuctionId)
                    .Append(auction)
                    .ToList();

                _cache.Set(cacheKey, updated, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                    Priority = CacheItemPriority.High
                });

                _logger.LogInformation("♻️ Opdaterede auktion i cache: {AuctionId}", auction.AuctionId);
            }
            else
            {
                _cache.Set(cacheKey, new List<AuctionDTO> { auction }, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                    Priority = CacheItemPriority.High
                });

                _logger.LogInformation("🆕 Oprettede cache med ny auktion: {AuctionId}", auction.AuctionId);
            }

            return Task.CompletedTask;
        }

        public Task AddAuctionToCache(AuctionDTO auction)
        {
            var cacheKey = $"auctions-{auction.Status.ToString().ToLowerInvariant()}";
            _logger.LogInformation("➕ Tilføjer auktion med status: {Status}, key: {CacheKey}", auction.Status, cacheKey);

            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                var updated = auctions.Append(auction).ToList();
                _cache.Set(cacheKey, updated, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                    Priority = CacheItemPriority.High
                });

                _logger.LogInformation("🆕 Tilføjede auktion til eksisterende cache: {AuctionId}", auction.AuctionId);
            }
            else
            {
                _cache.Set(cacheKey, new List<AuctionDTO> { auction }, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                    Priority = CacheItemPriority.High
                });

                _logger.LogInformation("🆕 Oprettede ny cache og tilføjede auktion: {AuctionId}", auction.AuctionId);
            }

            return Task.CompletedTask;
        }
    }
}
