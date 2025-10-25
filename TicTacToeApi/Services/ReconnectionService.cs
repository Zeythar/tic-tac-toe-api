using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TicTacToeApi.Configuration;
using TicTacToeApi.Hubs;
using TicTacToeApi.Interfaces;
using TicTacToeApi.Models;

namespace TicTacToeApi.Services
{
    /// <summary>
    /// Manages player reconnection grace periods and forfeit logic
    /// </summary>
    public sealed class ReconnectionService : IReconnectionService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly GameSettings _settings;
        private readonly ILogger<ReconnectionService> _logger;

        public ReconnectionService(
            IServiceProvider serviceProvider,
            IHubContext<GameHub> hubContext,
            IOptions<GameSettings> settings,
            ILogger<ReconnectionService> logger)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task StartGracePeriodAsync(string roomCode, string playerId)
        {
            var roomService = _serviceProvider.GetRequiredService<IRoomService>();
            if (!roomService.TryGetRoom(roomCode, out var room))
                return;

            bool immediateForfeit = false;
            CancellationTokenSource? cts = null;

            lock (room)
            {
                if (!room.Players.TryGetValue(playerId, out var player))
                    return;

                // If the player already reconnected, do nothing
                if (player.IsConnected)
                {
                    _logger.LogInformation("Player {PlayerId} in room {RoomCode} already reconnected", playerId, roomCode);
                    return;
                }

                // If grace was already used, forfeit immediately (pending sanity check below)
                if (player.GraceUsed)
                {
                    _logger.LogInformation("Player {PlayerId} in room {RoomCode} exhausted grace period - forfeiting", playerId, roomCode);
                    immediateForfeit = true;
                }
                else
                {
                    // Consume the one-time grace when starting the timer
                    player.GraceUsed = true;
                    cts = new CancellationTokenSource();
                    player.ReconnectionTimeoutCts = cts;
                    player.ReconnectionTimeoutExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_settings.ReconnectionGracePeriodSeconds);
                }
            }

            if (immediateForfeit)
            {
                // Sanity: verify current mapping/state under the room lock before performing the forfeit to avoid races with reconnect
                if (roomService.TryGetRoom(roomCode, out var r))
                {
                    bool performed = false;
                    int[]? boardSnapshot = null;
                    string? currentTurn = null;
                    bool isGameOver = false;
                    string? winner = null;
                    string? winnerId = null;
                    string? winnerSymbol = null;

                    lock (r)
                    {
                        if (r.Players.TryGetValue(playerId, out var player) && !player.IsConnected && player.GraceUsed)
                        {
                            // capture authoritative state
                            boardSnapshot = r.Board.ToArray();
                            currentTurn = r.CurrentTurn;
                            isGameOver = r.IsGameOver;
                            winner = r.Winner;

                            var opponent = r.Players.Values.FirstOrDefault(p => p.PlayerId != playerId);
                            winnerId = opponent?.PlayerId;
                            winnerSymbol = opponent?.Symbol;

                            r.Forfeit(playerId);
                            player.ReconnectionTimeoutCts = null;
                            performed = true;
                            _logger.LogInformation("Player {PlayerId} in room {RoomCode} forfeited (immediate path)", playerId, roomCode);
                        }
                        else
                        {
                            _logger.LogInformation("Immediate forfeit aborted for player {PlayerId} in room {RoomCode} because player reconnected or state changed", playerId, roomCode);
                        }
                    }

                    if (performed)
                    {
                        // Build GameOver DTO
                        // Use Winner result since the remaining player wins by opponent abandonment
                        var gameOver = new GameOverDto(roomCode, GameOverResult.Winner, winnerId, winnerSymbol, boardSnapshot?.ToImmutableBoard(), currentTurn, true, "Opponent disconnected and failed to reconnect");
                        await _hubContext.Clients.Group(roomCode).SendAsync("GameOver", gameOver);

                        try
                        {
                            if (roomService.TryRemoveRoom(roomCode))
                            {
                                _logger.LogInformation("Removed room {RoomCode} after opponent disconnection", roomCode);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error removing room {RoomCode} after opponent disconnection", roomCode);
                        }
                    }
                }
            }
            else
            {
                // Notify group that this player left
                await _hubContext.Clients.Group(roomCode).SendAsync("PlayerLeft", playerId);

                // Start grace period countdown
                await RunGracePeriodCountdownAsync(roomCode, playerId, cts!);
            }
        }

        private async Task RunGracePeriodCountdownAsync(string roomCode, string playerId, CancellationTokenSource cts)
        {
            try
            {
                int totalSeconds = _settings.ReconnectionGracePeriodSeconds;

                // Send initial tick with full remaining time
                await _hubContext.Clients.Group(roomCode).SendAsync("CountdownTick", playerId, totalSeconds);

                // Tick down every second
                for (int elapsed = 1; elapsed < totalSeconds; elapsed++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                    int remaining = totalSeconds - elapsed;
                    await _hubContext.Clients.Group(roomCode).SendAsync("CountdownTick", playerId, remaining);
                }

                // Final delay to reach zero
                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);

                // Before checking, clear the active reconnection CTS reference under the room lock to indicate the timer completed
                var roomService = _serviceProvider.GetRequiredService<IRoomService>();
                if (roomService.TryGetRoom(roomCode, out var roomForClear))
                {
                    lock (roomForClear)
                    {
                        if (roomForClear.Players.TryGetValue(playerId, out var player) && ReferenceEquals(player.ReconnectionTimeoutCts, cts))
                        {
                            player.ReconnectionTimeoutCts = null;
                            player.ReconnectionTimeoutExpiresAt = null;
                        }
                    }
                }

                // Check if still disconnected and forfeit if necessary
                await CheckAndForfeitIfDisconnectedAsync(roomCode, playerId);
            }
            catch (OperationCanceledException)
            {
                // Determine reason for cancellation: reconnection vs room removal/other
                var roomService = _serviceProvider.GetRequiredService<IRoomService>();
                if (roomService.TryGetRoom(roomCode, out var room))
                {
                    lock (room)
                    {
                        if (room.Players.TryGetValue(playerId, out var player))
                        {
                            // If the player's active reconnection CTS is a different object than the one that was passed in,
                            // the cancellation was caused by replacement/cleanup; log accordingly.
                            if (!ReferenceEquals(player.ReconnectionTimeoutCts, cts))
                            {
                                _logger.LogInformation("Grace period cancelled for player {PlayerId} in room {RoomCode} - timer replaced or cleared", playerId, roomCode);
                            }
                            else if (player.IsConnected)
                            {
                                _logger.LogInformation("Grace period cancelled for player {PlayerId} in room {RoomCode} - player reconnected", playerId, roomCode);
                                // Clear expiry since player reconnected
                                player.ReconnectionTimeoutExpiresAt = null;
                            }
                            else
                            {
                                _logger.LogInformation("Grace period cancelled for player {PlayerId} in room {RoomCode} - timer cancelled (room state changed)", playerId, roomCode);
                                player.ReconnectionTimeoutExpiresAt = null;
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Grace period cancelled for player {PlayerId} in room {RoomCode} - player record missing", playerId, roomCode);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Grace period cancelled for player {PlayerId} in room {RoomCode} - room removed", playerId, roomCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during grace period for player {PlayerId} in room {RoomCode}", playerId, roomCode);
            }
        }

        private async Task CheckAndForfeitIfDisconnectedAsync(string roomCode, string playerId)
        {
            var roomService = _serviceProvider.GetRequiredService<IRoomService>();
            if (!roomService.TryGetRoom(roomCode, out var room))
                return;

            bool stillDisconnected = false;
            string? winnerId = null;
            string? winnerSymbol = null;
            int[]? boardSnapshot = null;
            string? currentTurn = null;
            bool isGameOver = false;
            string? winner = null;

            lock (room)
            {
                if (room.Players.TryGetValue(playerId, out var player))
                {
                    // Sanity: ensure the target player is still disconnected and that grace was consumed.
                    if (!player.IsConnected && player.GraceUsed && player.ReconnectionTimeoutCts == null)
                    {
                        stillDisconnected = true;

                        // Capture final state
                        boardSnapshot = room.Board.ToArray();
                        currentTurn = room.CurrentTurn;
                        isGameOver = room.IsGameOver;
                        winner = room.Winner;

                        // Identify winner (opponent)
                        var opponent = room.Players.Values.FirstOrDefault(p => p.PlayerId != playerId);
                        winnerId = opponent?.PlayerId;
                        winnerSymbol = opponent?.Symbol;

                        // Mark forfeit using canonical player id
                        room.Forfeit(playerId);
                        _logger.LogInformation("Player {PlayerId} in room {RoomCode} timed out - forfeiting", playerId, roomCode);

                        // clear reconnection timer reference
                        player.ReconnectionTimeoutCts = null;
                    }
                }
            }

            if (stillDisconnected)
            {
                // Use Winner result since the remaining player wins by opponent abandonment
                var gameOver2 = new GameOverDto(roomCode, GameOverResult.Winner, winnerId, winnerSymbol, boardSnapshot?.ToImmutableBoard(), currentTurn, true, "Opponent disconnected and failed to reconnect");
                await _hubContext.Clients.Group(roomCode).SendAsync("GameOver", gameOver2);

                try
                {
                    if (roomService.TryRemoveRoom(roomCode))
                    {
                        _logger.LogInformation("Removed room {RoomCode} after opponent disconnection", roomCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing room {RoomCode} after opponent disconnection", roomCode);
                }
            }
            else
            {
                // Fallback: if we didn't forfeit but the room appears idle beyond the grace window, try to detect and forfeit
                try
                {
                    if (roomService.TryGetRoom(roomCode, out var r2))
                    {
                        lock (r2)
                        {
                            var now = DateTimeOffset.UtcNow;
                            var candidates = r2.Players.Values.Where(p => !p.IsConnected && p.GraceUsed && p.ReconnectionTimeoutCts == null).ToList();
                            foreach (var cand in candidates)
                            {
                                var idle = now - r2.LastActivityAt;
                                if (idle > TimeSpan.FromSeconds(_settings.ReconnectionGracePeriodSeconds +1))
                                {
                                    _logger.LogWarning("Fallback forfeit: room {RoomCode} candidate {PlayerId} idle {Idle}s", roomCode, cand.PlayerId, idle.TotalSeconds);

                                    // perform forfeit
                                    int[]? fbBoard = r2.Board.ToArray();
                                    var fbCurrentTurn = r2.CurrentTurn;
                                    var opponent = r2.Players.Values.FirstOrDefault(p => p.PlayerId != cand.PlayerId);
                                    var fbWinnerId = opponent?.PlayerId;
                                    var fbWinnerSymbol = opponent?.Symbol;

                                    r2.Forfeit(cand.PlayerId);

                                    // Use Winner result since remaining player wins by opponent abandonment
                                    var gameOverFallback = new GameOverDto(roomCode, GameOverResult.Winner, fbWinnerId, fbWinnerSymbol, fbBoard?.ToImmutableBoard(), fbCurrentTurn, true, "Opponent disconnected and failed to reconnect (fallback)");

                                    // send outside lock
                                    _ = _hubContext.Clients.Group(roomCode).SendAsync("GameOver", gameOverFallback);

                                    try { roomService.TryRemoveRoom(roomCode); } catch { }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during fallback forfeit check for room {RoomCode}", roomCode);
                }
            }
        }
    }
}
