using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using BidServiceAPI.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using BidServiceAPI.Services;

namespace BidServiceAPI.Workers
{
    public class AuctionSyncWorker : BackgroundService
    {
        private readonly ILogger<AuctionSyncWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;

        public AuctionSyncWorker(
            ILogger<AuctionSyncWorker> logger,
            IConfiguration configuration,
            ICacheService cacheService)
        {
            _logger = logger;
            _configuration = configuration;
            _cacheService = cacheService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var rabbitMQHost = _configuration["RABBITMQ_HOST"] ?? "localhost";
            _logger.LogInformation("🔄 AuctionSyncWorker initialized. RabbitMQ host: {Host}", rabbitMQHost);

            const int maxAttempts = 10;
            int attempt = 0;

            while (attempt < maxAttempts && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("🔌 Forsøger at oprette forbindelse til RabbitMQ... (forsøg {Attempt}/{Max})", attempt + 1, maxAttempts);

                    var factory = new ConnectionFactory
                    {
                        HostName = rabbitMQHost,
                        Port = 5672,
                        UserName = "guest",
                        Password = "guest"
                    };

                    using var connection = await factory.CreateConnectionAsync();
                    using var channel = await connection.CreateChannelAsync();

                    await channel.QueueDeclareAsync(
                        queue: "syncAuctionQueue",
                        durable: false,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    );

                    _logger.LogInformation("📡 Lytter på kø: syncAuctionQueue");

                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        _logger.LogInformation("📥 Modtog besked: {Json}", json);

                        try
                        {
                            var dto = JsonSerializer.Deserialize<AuctionDTO>(json, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                Converters = { new JsonStringEnumConverter() }
                            });

                            if (dto is null)
                            {
                                _logger.LogWarning("❌ Kunne ikke deserialisere AuctionDTO");
                                return;
                            }

                            var existingAuction = await _cacheService.GetAuctionByIdInCache(dto.AuctionId);

                            if (existingAuction != null)
                            {
                                await _cacheService.UpdateAuctionInCache(dto);
                                _logger.LogInformation("♻️ Opdaterede auktion i cache: {AuctionId}", dto.AuctionId);
                            }
                            else
                            {
                                await _cacheService.AddAuctionToCache(dto);
                                _logger.LogInformation("🆕 Tilføjede ny auktion til cache: {AuctionId}", dto.AuctionId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Fejl under behandling af RabbitMQ-besked");
                        }
                    };

                    await channel.BasicConsumeAsync("syncAuctionQueue", autoAck: true, consumer: consumer);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(1000, stoppingToken);
                    }

                    break; // connection lykkedes, lyt aktivt
                }
                catch (Exception ex)
                {
                    attempt++;
                    _logger.LogWarning(ex, "⚠️ Forbindelse til RabbitMQ fejlede (forsøg {Attempt}/{Max}). Prøver igen om 5 sekunder...", attempt, maxAttempts);
                    await Task.Delay(5000, stoppingToken);
                }
            }

            if (attempt == maxAttempts)
            {
                _logger.LogError("❌ Kunne ikke oprette forbindelse til RabbitMQ efter {Max} forsøg. AuctionSyncWorker afsluttes.", maxAttempts);
            }

            _logger.LogInformation("🛑 AuctionSyncWorker stoppet");
        }
    }
}
