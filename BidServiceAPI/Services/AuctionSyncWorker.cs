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
            _logger.LogInformation("AuctionSyncWorker started");

            var rabbitMQHost = _configuration["RABBITMQ_HOST"] ?? "localhost";
            const int maxAttempts = 10;
            int attempt = 0;

            while (attempt < maxAttempts && !stoppingToken.IsCancellationRequested)
            {
                try
                {
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

                    _logger.LogInformation("Listening on queue: syncAuctionQueue");

                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        _logger.LogInformation("Received message: {Json}", json);

                        try
                        {
                            var dto = JsonSerializer.Deserialize<AuctionDTO>(json, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                Converters = { new JsonStringEnumConverter() }
                            });

                            if (dto is null)
                            {
                                _logger.LogWarning("❌ Could not deserialize AuctionDTO");
                                return;
                            }

                            await _cacheService.UpdateAuctionInCache(dto);
                            _logger.LogInformation("✅ Updated auction in cache: {AuctionId}", dto.AuctionId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Error processing message");
                        }
                    };

                    await channel.BasicConsumeAsync("syncAuctionQueue", autoAck: true, consumer: consumer);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(1000, stoppingToken);
                    }

                    break;
                }
                catch (Exception ex)
                {
                    attempt++;
                    _logger.LogWarning(ex, "Connection attempt {Attempt}/{MaxAttempts} failed. Retrying in 5s...", attempt, maxAttempts);
                    await Task.Delay(5000, stoppingToken);
                }
            }

            if (attempt == maxAttempts)
            {
                _logger.LogError("Could not connect to RabbitMQ after {MaxAttempts} attempts. Worker is shutting down.", maxAttempts);
            }

            _logger.LogInformation("AuctionSyncWorker stopped");
        }
    }
}
