using BidServiceAPI.MockingService;
using BidServiceAPI.Services;
using NLog;
using NLog.Web;

var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
logger.Debug("Start BidService");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog(); // Bruger NLog.config

    builder.Services.AddControllers();

    builder.Services.AddScoped<ICacheService, CacheService>();
    builder.Services.AddScoped<BidService>();
    builder.Services.AddSingleton<IBidMessagePublisher, RabbitMqBidPublisher>();
    builder.Services.AddScoped<IMockAuctionService, MockAuctionService>();

    builder.Services.AddMemoryCache();

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
