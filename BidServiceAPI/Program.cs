using BidServiceAPI.Services;
using BidServiceAPI.Workers;
using NLog;
using NLog.Web;

var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
logger.Debug("Start BidService");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ✅ Gør miljøvariabler tilgængelige for DI (fx AuctionSyncWorker)
    builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

    // ✅ Setup logging
    builder.Logging.ClearProviders();
    builder.Host.UseNLog(); // Bruger NLog.config

    // ✅ Tilføj nødvendige services
    builder.Services.AddControllers();
    builder.Services.AddMemoryCache();

    builder.Services.AddScoped<ICacheService, CacheService>();
    builder.Services.AddScoped<BidService>();
    builder.Services.AddSingleton<IBidMessagePublisher, RabbitMqBidPublisher>();
    builder.Services.AddHostedService<AuctionSyncWorker>();

    // ✅ HTTP Client til AuctionService (Docker service-navn)
    builder.Services.AddScoped<IAuctionHttpClient, AuctionHttpClient>();
    builder.Services.AddHttpClient<IAuctionHttpClient, AuctionHttpClient>(client =>
    {
        client.BaseAddress = new Uri("http://auctionserviceapi:5002/");
    });

    // ✅ Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
