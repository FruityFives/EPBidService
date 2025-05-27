using BidServiceAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BidServiceAPI.Services
{
    /// <summary>
    /// Service, der håndterer caching af auktioner baseret på deres status.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;

        /// <summary>
        /// Initialiserer en ny instans af <see cref="CacheService"/>.
        /// </summary>
        /// <param name="cache">In-memory cache implementering.</param>
        /// <param name="logger">Logger til hændelseslogning.</param>
        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Finder en specifik auktion i cache uanset dens status.
        /// </summary>
        /// <param name="auctionId">ID på den ønskede auktion.</param>
        /// <returns>
        /// En auktion som <see cref="AuctionDTO"/>, hvis den findes i cache, ellers <c>null</c>.
        /// </returns>
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

        /// <summary>
        /// Henter alle auktioner fra cache med en bestemt status.
        /// </summary>
        /// <param name="status">Statussen der ønskes hentet (fx Active, Inactive).</param>
        /// <returns>
        /// En liste af <see cref="AuctionDTO"/> objekter med den angivne status.
        /// Hvis ingen findes i cache, returneres en tom liste.
        /// </returns>
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

        /// <summary>
        /// Opdaterer en auktion i cache baseret på dens nuværende status og fjerner den fra andre statustyper.
        /// </summary>
        /// <param name="auction">Auktionsobjektet, der skal opdateres i cache.</param>
        /// <returns>En opgave, der indikerer, at opdateringen er fuldført.</returns>
        public Task UpdateAuctionInCache(AuctionDTO auction)
        {
            var newKey = $"auctions-{auction.Status.ToString().ToLowerInvariant()}";
            _logger.LogInformation("Opdaterer cache for status: {Status}, key: {CacheKey}", auction.Status, newKey);

            // Tilføj til ny status-liste
            if (_cache.TryGetValue(newKey, out IEnumerable<AuctionDTO> currentList))
            {
                var updated = currentList
                    .Where(a => a.AuctionId != auction.AuctionId)
                    .Append(auction)
                    .ToList();

                _cache.Set(newKey, updated, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                    Priority = CacheItemPriority.High
                });
            }
            else
            {
                _cache.Set(newKey, new List<AuctionDTO> { auction }, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                    Priority = CacheItemPriority.High
                });
            }

            _logger.LogInformation("Opdaterede auktion i cache: {AuctionId}", auction.AuctionId);

            // Fjern auktionen fra andre status-lister
            foreach (AuctionStatus status in Enum.GetValues(typeof(AuctionStatus)))
            {
                if (status == auction.Status) continue;

                var otherKey = $"auctions-{status.ToString().ToLowerInvariant()}";
                if (_cache.TryGetValue(otherKey, out IEnumerable<AuctionDTO> otherList))
                {
                    var cleaned = otherList.Where(a => a.AuctionId != auction.AuctionId).ToList();
                    _cache.Set(otherKey, cleaned, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                        Priority = CacheItemPriority.Normal
                    });

                    _logger.LogInformation("Fjernede auktion {AuctionId} fra {OtherKey}", auction.AuctionId, otherKey);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Tilføjer en ny auktion til cache baseret på dens status.
        /// </summary>
        /// <param name="auction">Auktionsobjektet, der skal tilføjes.</param>
        /// <returns>En opgave, der indikerer, at tilføjelsen er fuldført.</returns>
        public Task AddAuctionToCache(AuctionDTO auction)
        {
            var cacheKey = $"auctions-{auction.Status.ToString().ToLowerInvariant()}";
            _logger.LogInformation("Tilføjer auktion med status: {Status}, key: {CacheKey}", auction.Status, cacheKey);

            if (_cache.TryGetValue(cacheKey, out IEnumerable<AuctionDTO> auctions))
            {
                var updated = auctions.Append(auction).ToList();
                _cache.Set(cacheKey, updated, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                    Priority = CacheItemPriority.High
                });

                _logger.LogInformation("Tilføjede auktion til eksisterende cache: {AuctionId}", auction.AuctionId);
            }
            else
            {
                _cache.Set(cacheKey, new List<AuctionDTO> { auction }, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(10),
                    Priority = CacheItemPriority.High
                });

                _logger.LogInformation("Oprettede ny cache og tilføjede auktion: {AuctionId}", auction.AuctionId);
            }

            return Task.CompletedTask;
        }
    }
}
