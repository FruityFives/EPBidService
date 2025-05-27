using BidServiceAPI.Models;
using RabbitMQ.Client;
using System.Text.Json;

/// <summary>
/// Publisher, der sender bud til RabbitMQ køen "bidQueue".
/// </summary>
public class BidMessagePublisher : IBidMessagePublisher
{
    private readonly ILogger<BidMessagePublisher> _logger;

    /// <summary>
    /// Initialiserer en ny instance af <see cref="BidMessagePublisher"/>.
    /// </summary>
    /// <param name="logger">Logger til at logge hændelser i forbindelse med bud-publicering.</param>
    public BidMessagePublisher(ILogger<BidMessagePublisher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Publicerer et bud til RabbitMQ køen "bidQueue".
    /// </summary>
    /// <param name="bid">Bud-objektet, der skal serialiseres og sendes.</param>
    /// <returns>En asynkron opgave, der fuldføres, når buddet er sendt.</returns>
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

        _logger.LogInformation("Published the bid {id} to RabbitMQ", bid.BidId);
    }
}
