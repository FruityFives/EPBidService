using BidServiceAPI.Services;

using NLog.Web;

var logger = NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
logger.Info("Starter BidServiceAPI...");

var builder = WebApplication.CreateBuilder(args);

// Konfigurer logging
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Dependency Injection
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddSingleton<BidService>();
builder.Services.AddSingleton<IBidMessagePublisher, RabbitMqBidPublisher>();

// Hosted Worker (RabbitMQ listener)
builder.Services.AddHostedService<AuctionSyncWorker>();

// HTTP Client til AuctionService
builder.Services.AddScoped<IAuctionHttpClient, AuctionHttpClient>();
builder.Services.AddHttpClient<IAuctionHttpClient, AuctionHttpClient>(client =>
{
    client.BaseAddress = new Uri("http://auctionserviceapi:5002/");
});

// Controllers og Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
