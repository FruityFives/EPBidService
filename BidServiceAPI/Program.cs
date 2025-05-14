using BidServiceAPI.MockingService;
using BidServiceAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Logging til vores console (vises i Docker logs)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

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
