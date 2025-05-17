using BidServiceAPI.Models;
using RabbitMQ.Client;
using System.Text.Json;

public class RabbitMqBidPublisher : IBidMessagePublisher
{
    private readonly ILogger<RabbitMqBidPublisher> _logger;

    public RabbitMqBidPublisher(ILogger<RabbitMqBidPublisher> logger)
    {
        _logger = logger;
    }

    public async Task PublishBidAsync(Bid bid)
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        var factory = new ConnectionFactory() { HostName = host };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "bidQueue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var body = JsonSerializer.SerializeToUtf8Bytes(bid);

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: "bidQueue",
            mandatory: false,
            basicProperties: new BasicProperties(),
            body: body
        );

        _logger.LogInformation("Published bid {id} to RabbitMQ", bid.BidId);
    }
}