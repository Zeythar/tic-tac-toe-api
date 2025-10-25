using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TicTacToeApi.Configuration;
using TicTacToeApi.Hubs;
using TicTacToeApi.Interfaces;
using TicTacToeApi.Logging;
using TicTacToeApi.Repositories;
using TicTacToeApi.Services;
using TicTacToeApi.Validation;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use PORT environment variable (required for Render.com)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(int.Parse(port));
});

// Configure settings
builder.Services.Configure<GameSettings>(builder.Configuration.GetSection("GameSettings"));

// Add services to the container
builder.Services.AddOpenApi();

// Add memory cache for repository caching
builder.Services.AddMemoryCache();

// Add SignalR with camelCase JSON payloads to match JavaScript clients
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

// Register repository and services
// Stack: CachedRoomRepository wraps InMemoryRoomRepository for optimal performance
builder.Services.AddSingleton<IRoomRepository>(provider =>
{
    var baseRepository = new InMemoryRoomRepository(provider.GetRequiredService<ILogger<InMemoryRoomRepository>>());
    var cache = provider.GetRequiredService<IMemoryCache>();
    var logger = provider.GetRequiredService<ILogger<CachedRoomRepository>>();
    var settings = provider.GetRequiredService<IOptions<GameSettings>>();
    
    return new CachedRoomRepository(baseRepository, cache, logger, settings);
});

builder.Services.AddSingleton<IRoomCodeGenerator, RoomCodeGenerator>();
builder.Services.AddSingleton<IRoomService, RoomService>();
builder.Services.AddSingleton<IReconnectionService, ReconnectionService>();
builder.Services.AddSingleton<IGameValidator, GameValidator>();
builder.Services.AddScoped<IGameLogger, GameLogger>();

// Register background cleanup
builder.Services.AddHostedService<RoomCleanupService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
   if (builder.Environment.IsDevelopment())
        {
     // Allow all origins in development
       policy.AllowAnyHeader()
      .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true);
        }
        else
        {
         // Read allowed origins from configuration (supports env variables)
       var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
    ?? Array.Empty<string>();
            
        // Also check for comma-separated CORS_ORIGINS env variable as fallback
            var corsOriginsEnv = Environment.GetEnvironmentVariable("CORS_ORIGINS");
            if (!string.IsNullOrWhiteSpace(corsOriginsEnv))
          {
             var envOrigins = corsOriginsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
             allowedOrigins = allowedOrigins.Concat(envOrigins).Distinct().ToArray();
            }
            
            if (allowedOrigins.Length > 0)
    {
      policy.WithOrigins(allowedOrigins)
 .AllowAnyHeader()
       .AllowAnyMethod()
     .AllowCredentials();
            }
      else
       {
      // Log warning and allow all (not recommended for production)
       var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
    logger.LogWarning("No CORS origins configured. Allowing all origins. Set AllowedOrigins__0 or CORS_ORIGINS environment variable.");
   
      policy.AllowAnyHeader()
    .AllowAnyMethod()
       .AllowCredentials()
   .SetIsOriginAllowed(_ => true);
    }
  }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Don't force HTTPS redirect on Render (they handle SSL termination)
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors();

// Map SignalR hub for the game
app.MapHub<GameHub>("/gameHub");

app.Run();