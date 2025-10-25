using System.Threading;
using TicTacToeApi.Configuration;
using TicTacToeApi.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using TicTacToeApi.Hubs;
using TicTacToeApi.Models;

namespace TicTacToeApi.Services
{
 /// <summary>
 /// Background service that periodically removes idle rooms from RoomService.
 /// </summary>
 public sealed class RoomCleanupService : BackgroundService
 {
 private readonly IRoomService _roomService;
 private readonly GameSettings _settings;
 private readonly ILogger<RoomCleanupService> _logger;
 private readonly IHubContext<GameHub> _hubContext;

 public RoomCleanupService(IRoomService roomService, IOptions<GameSettings> settings, ILogger<RoomCleanupService> logger, IHubContext<GameHub> hubContext)
 {
 _roomService = roomService;
 _settings = settings.Value;
 _logger = logger;
 _hubContext = hubContext;
 }

 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
 {
 var sweepInterval = TimeSpan.FromSeconds(Math.Max(1, _settings.RoomSweepIntervalSeconds));
 var idleThreshold = TimeSpan.FromSeconds(Math.Max(1, _settings.IdleRoomTimeoutSeconds));

 while (!stoppingToken.IsCancellationRequested)
 {
 try
 {
 foreach (var room in _roomService.GetAllRooms())
 {
 if (room.IsIdleForCleanup(idleThreshold))
 {
 try
 {
 // Emit new GameOver event with Result.Cancelled for unified handling
 var dto = new GameOverDto(room.Code, GameOverResult.Cancelled, null, null, room.Board.ToImmutableBoard(), room.CurrentTurn, true, "Room expired due to inactivity");
 await _hubContext.Clients.Group(room.Code).SendAsync("GameOver", dto);

 if (_roomService.TryRemoveRoom(room.Code))
 {
 _logger.LogInformation("Removed idle room {RoomCode} by sweeper", room.Code);
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error removing idle room {RoomCode}", room.Code);
 }
 }
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error during room sweeper run");
 }

 try
 {
 await Task.Delay(sweepInterval, stoppingToken);
 }
 catch (OperationCanceledException) { }
 }
 }
 }
}
