using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TicTacToeApi.Configuration;
using TicTacToeApi.Core.Utilities;
using TicTacToeApi.Hubs;
using TicTacToeApi.Interfaces;
using TicTacToeApi.Models;
using TicTacToeApi.Repositories;
using System.Threading.Tasks;

namespace TicTacToeApi.Services
{
    public sealed class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
 private readonly Random _rng = new();
        private readonly IHubContext<GameHub> _hubContext;
   private readonly IRoomCodeGenerator _codeGenerator;
     private readonly IReconnectionService _reconnectionService;
        private readonly GameSettings _settings;
     private readonly ILogger<RoomService> _logger;

        public RoomService(
 IRoomRepository roomRepository,
          IHubContext<GameHub> hubContext,
         IRoomCodeGenerator codeGenerator,
            IReconnectionService reconnectionService,
            IOptions<GameSettings> settings,
  ILogger<RoomService> logger)
        {
     _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
         _hubContext = hubContext;
          _codeGenerator = codeGenerator;
            _reconnectionService = reconnectionService;
            _settings = settings.Value;
            _logger = logger;
        }

        public bool TryRemoveRoom(string code)
        {
            if (!_roomRepository.TryGetById(code, out var room))
        return false;

   lock (room)
         {
     foreach (var player in room.Players.Values)
       {
        player.CancelAndDisposeTimers();
         }
        }

          _roomRepository.Delete(code);
    _logger.LogInformation("Removed room {Code} from repository", code);
 return true;
}

    public Room CreateRoom()
        {
            string code;
       do
            {
    code = _codeGenerator.GenerateCode();
       }
   while (_roomRepository.Exists(code));

  var room = new Room(code, _settings.BoardSize);
            _roomRepository.Create(room);

 _logger.LogInformation("Created room with code {Code}", code);
 return room;
        }

  public bool TryGetRoom(string code, out Room room) =>
            _roomRepository.TryGetById(code, out room);

        public IEnumerable<Room> GetAllRooms() => _roomRepository.GetAll();

  public Task HandleDisconnectAsync(string connectionId)
     {
   var disconnectedPlayers = new List<(string code, string playerId)>();
   var roomsToPauseTurnTimer = new HashSet<string>();
    var roomsToClose = new HashSet<string>();
      var pausedTimers = new List<(string code, string currentPlayerId, int remainingSeconds)>();

    foreach (var room in _roomRepository.GetAll())
      {
      string? playerId = null;
      bool shouldPauseTimer = false;
    int pausedRemainingSeconds = 0;
      string? pausedPlayerId = null;

   lock (room)
    {
   var player = room.GetPlayerByConnectionId(connectionId);
   if (player != null)
    {
      playerId = player.PlayerId;

 if (room.IsGameOver && room.RematchExpiresAt != null)
   {
  roomsToClose.Add(room.Code);
      _logger.LogInformation("Closing room {Code} because player {PlayerId} disconnected during rematch window", room.Code, playerId);
    room.RemoveConnection(connectionId);
     continue;
     }

  // Pause turn timer if game is active and any player disconnects
    if (!room.IsGameOver && room.CurrentTurn != null)
   {
   // Find the current turn player and save their remaining time
       var currentTurnPlayer = room.Players.Values.FirstOrDefault(p => p.Symbol == room.CurrentTurn);
     if (currentTurnPlayer?.TurnTimeoutCts != null)
   {
  if (currentTurnPlayer.TurnTimeoutExpiresAt != null)
    {
    var remaining = (int)Math.Ceiling((currentTurnPlayer.TurnTimeoutExpiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds);
    currentTurnPlayer.RemainingTurnSeconds = Math.Max(0, remaining);
         pausedRemainingSeconds = currentTurnPlayer.RemainingTurnSeconds.Value;
  }
    else if (currentTurnPlayer.RemainingTurnSeconds == null)
  {
       currentTurnPlayer.RemainingTurnSeconds = _settings.TurnTimeoutSeconds;
     pausedRemainingSeconds = currentTurnPlayer.RemainingTurnSeconds.Value;
  }

    pausedPlayerId = currentTurnPlayer.PlayerId;
    shouldPauseTimer = true;
 }
    }

room.RemoveConnection(connectionId);

      // Check if all players are now disconnected - if so, close the room
       if (room.Players.Values.All(p => !p.IsConnected))
      {
       roomsToClose.Add(room.Code);
           _logger.LogInformation("Closing room {Code} because all players have disconnected", room.Code);
     continue; // Skip further processing for this room
    }

 _roomRepository.Update(room);
 _logger.LogInformation("Player {PlayerId} disconnected from room {Code}", playerId, room.Code);
  }
        }

    if (playerId != null)
  disconnectedPlayers.Add((room.Code, playerId));

     if (shouldPauseTimer && pausedPlayerId != null)
     {
    roomsToPauseTurnTimer.Add(room.Code);
    pausedTimers.Add((room.Code, pausedPlayerId, pausedRemainingSeconds));
       }
   }

      foreach (var code in roomsToPauseTurnTimer)
     {
 try
      {
 CancelTurnTimeout(code);
  _logger.LogInformation("Paused per-turn timer for room {Code} because a player disconnected", code);
 }
   catch (Exception ex)
      {
  _logger.LogError(ex, "Error pausing per-turn timer for room {Code}", code);
      }
  }

      // Notify remaining players that turn timer has been paused
      foreach (var (code, currentPlayerId, remainingSeconds) in pausedTimers)
 {
     try
 {
      var serverTimestamp = DateTimeOffset.UtcNow.ToString("o");
  _ = _hubContext.Clients.Group(code).SendAsync("TurnCountdownPaused", currentPlayerId, remainingSeconds, serverTimestamp);
    _logger.LogInformation("Notified room {Code} that turn timer paused at {Remaining} seconds", code, remainingSeconds);
}
         catch (Exception ex)
          {
    _logger.LogError(ex, "Error notifying room {Code} about paused timer", code);
       }
 }

       foreach (var (code, playerId) in disconnectedPlayers)
      {
    if (roomsToClose.Contains(code))
      continue;

      _ = _reconnectionService.StartGracePeriodAsync(code, playerId);
 }

 foreach (var code in roomsToClose)
{
   try
       {
      // Notify remaining clients (if any) that room is closing
 _ = _hubContext.Clients.Group(code).SendAsync("RoomClosed", code);
  }
  catch (Exception ex)
  {
 _logger.LogError(ex, "Error notifying group about room closure for room {Code}", code);
  }

   try
       {
    if (TryRemoveRoom(code))
        {
     _logger.LogInformation("Closed and removed room {Code}", code);
     }
       }
  catch (Exception ex)
   {
        _logger.LogError(ex, "Error removing room {Code}", code);
    }
  }

  return Task.CompletedTask;
   }

        public async Task<bool> ReconnectAsync(string code, string playerId, string connectionId)
 {
            if (!_roomRepository.TryGetById(code, out var room))
            {
          _logger.LogWarning("Reconnect failed: room {Code} not found", code);
        return false;
            }

    Player? player = null;
            int[] boardSnapshot;
            string? currentTurn;
            bool isGameOver;
            string? winner;
   string? playerSymbol;

    lock (room)
      {
        if (!room.Players.TryGetValue(playerId, out player))
    {
      _logger.LogWarning("Reconnect failed: player {PlayerId} not found in room {Code}", playerId, code);
     return false;
         }

         if (player.ConnectionId != null && player.ConnectionId != connectionId)
          {
               _logger.LogWarning("Reconnect failed: player {PlayerId} in room {Code} is already connected", playerId, code);
        return false;
    }

             player.ConnectionId = connectionId;
          player.CancelAndDisposeReconnectionTimeout();

           boardSnapshot = room.Board.ToArray();
           currentTurn = room.CurrentTurn;
   isGameOver = room.IsGameOver;
            winner = room.Winner;
        playerSymbol = player.Symbol;

        _roomRepository.Update(room);
      }

     _logger.LogInformation("Player {PlayerId} reconnected to room {Code}", playerId, code);

await _hubContext.Clients.Client(connectionId).SendAsync("SyncedState", boardSnapshot, playerSymbol, currentTurn, isGameOver, winner);
         await _hubContext.Clients.Group(code).SendAsync("PlayerReconnected", playerId);

     bool shouldStartTimer = false;
            lock (room)
         {
 if (room.PlayerOrder.Count == 2)
      {
      if (room.Players.Values.Any(p => p.HasSymbol) && !room.IsGameOver)
       {
            var currentPlayer = room.Players.Values.FirstOrDefault(p => p.Symbol == room.CurrentTurn);
      if (currentPlayer != null && currentPlayer.IsConnected)
     {
     shouldStartTimer = true;
        }
  }
        else
         {
       room.TryStartGame(_rng);
       _ = _hubContext.Clients.Group(code).SendAsync("GameStarted", room.Board, room.CurrentTurn);
       shouldStartTimer = true;
          _roomRepository.Update(room);
                 }
          }
            }

            if (shouldStartTimer)
            {
    _ = StartTurnTimeoutAsync(code);
            }

 return true;
   }

        public async Task StartTurnTimeoutAsync(string code)
        {
            if (!_roomRepository.TryGetById(code, out var room))
   return;

CancellationTokenSource? cts = null;
            string? timedOutPlayerId = null;

            int totalSeconds;
            int initialRemaining;
      string? currentPlayerId = null;
       DateTimeOffset? turnExpiry = null;
   int capturedTimerVersion;

         lock (room)
   {
            foreach (var p in room.Players.Values)
    {
       p.CancelAndDisposeTurnTimeout();
      }

       if (room.IsGameOver || room.CurrentTurn == null)
     return;

    var currentPlayer = room.Players.Values.FirstOrDefault(p => p.Symbol == room.CurrentTurn);
          if (currentPlayer == null)
   return;

 currentPlayerId = currentPlayer.PlayerId;

       if (currentPlayer.RemainingTurnSeconds != null)
    {
         initialRemaining = currentPlayer.RemainingTurnSeconds.Value;
       }
    else
   {
      initialRemaining = _settings.TurnTimeoutSeconds;
   }

    totalSeconds = initialRemaining;

         cts = new CancellationTokenSource();
           currentPlayer.TurnTimeoutCts = cts;
  currentPlayer.TurnTimeoutExpiresAt = DateTimeOffset.UtcNow.AddSeconds(totalSeconds);
    turnExpiry = currentPlayer.TurnTimeoutExpiresAt;
        currentPlayer.RemainingTurnSeconds = null;

        capturedTimerVersion = room.TurnTimerVersion;
                _roomRepository.Update(room);
            }

   if (cts == null)
        return;

      try
{
     if (!_roomRepository.TryGetById(code, out var verifyRoom) || verifyRoom.TurnTimerVersion != capturedTimerVersion)
             return;

        var serverTimestamp = DateTimeOffset.UtcNow.ToString("o");
             _ = _hubContext.Clients.Group(code).SendAsync("TurnCountdownResumed", currentPlayerId, totalSeconds, turnExpiry, serverTimestamp);

             serverTimestamp = DateTimeOffset.UtcNow.ToString("o");
         await _hubContext.Clients.Group(code).SendAsync("TurnCountdownTick", currentPlayerId, totalSeconds, turnExpiry, serverTimestamp);

        for (int elapsed = 1; elapsed < totalSeconds; elapsed++)
          {
 await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);

       if (!_roomRepository.TryGetById(code, out var loopRoom) || loopRoom.TurnTimerVersion != capturedTimerVersion)
            return;

       int remaining = totalSeconds - elapsed;
  serverTimestamp = DateTimeOffset.UtcNow.ToString("o");
       await _hubContext.Clients.Group(code).SendAsync("TurnCountdownTick", currentPlayerId, remaining, turnExpiry, serverTimestamp);
     }

       await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);

       if (!_roomRepository.TryGetById(code, out var finalRoom) || finalRoom.TurnTimerVersion != capturedTimerVersion)
     return;

         int[]? boardSnapshot = null;
      string? winnerId = null;
        string? winnerSymbol = null;
          string? currentTurn = null;
 bool isGameOver = false;
   string? winner = null;

          lock (finalRoom)
     {
   var player = finalRoom.Players.Values.FirstOrDefault(p => p.TurnTimeoutCts == cts);
if (player != null)
          {
      player.CancelAndDisposeTurnTimeout();
   }

        if (!finalRoom.IsGameOver && finalRoom.CurrentTurn != null)
          {
     var playerBySymbol = finalRoom.Players.Values.FirstOrDefault(p => p.Symbol == finalRoom.CurrentTurn);
      if (playerBySymbol != null)
   {
           timedOutPlayerId = playerBySymbol.PlayerId;

             boardSnapshot = finalRoom.Board.ToArray();
             currentTurn = finalRoom.CurrentTurn;
     isGameOver = finalRoom.IsGameOver;
     winner = finalRoom.Winner;

           var opponent = finalRoom.Players.Values.FirstOrDefault(p => p.PlayerId != timedOutPlayerId);
    winnerId = opponent?.PlayerId;
         winnerSymbol = opponent?.Symbol;

            finalRoom.Forfeit(playerBySymbol.PlayerId);
  _roomRepository.Update(finalRoom);
   }
 }
      }

       if (timedOutPlayerId != null)
  {
        // Player timed out on their turn - opponent wins
     var gameOver = new GameOverDto(code, GameOverResult.Winner, winnerId, winnerSymbol, boardSnapshot?.ToImmutableBoard(), currentTurn, true, "Player timed out on their turn");
   await _hubContext.Clients.Group(code).SendAsync("GameOver", gameOver);

    try
   {
  if (TryRemoveRoom(code))
 {
   _logger.LogInformation("Removed room {Code} after turn timeout, winner: {WinnerId}", code, winnerId);
     }
 }
         catch (Exception ex)
  {
       _logger.LogError(ex, "Error removing room {Code} after turn timeout", code);
    }
          }
       }
 catch (OperationCanceledException)
       {
                _logger.LogInformation("Turn timeout cancelled for room {Code}", code);

             if (_roomRepository.TryGetById(code, out var cancelRoom))
         {
     lock (cancelRoom)
    {
        var player = cancelRoom.Players.Values.FirstOrDefault(p => p.TurnTimeoutCts == null && p.TurnTimeoutExpiresAt != null);
             if (player != null)
 {
       var remaining = (int)Math.Ceiling((player.TurnTimeoutExpiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds);
             player.RemainingTurnSeconds = Math.Max(0, remaining);
        player.TurnTimeoutExpiresAt = null;

       var pauseServerTs = DateTimeOffset.UtcNow.ToString("o");
            _ = _hubContext.Clients.Group(code).SendAsync("TurnCountdownPaused", player.PlayerId, player.RemainingTurnSeconds, pauseServerTs);
    _roomRepository.Update(cancelRoom);
    }
          }
  }
}
  catch (Exception ex)
         {
      _logger.LogError(ex, "Error during turn timeout for room {Code}", code);
            }
   finally
          {
        if (_roomRepository.TryGetById(code, out var roomCleanup))
        {
         lock (roomCleanup)
              {
       var p = roomCleanup.Players.Values.FirstOrDefault(pl => pl.TurnTimeoutCts == cts);
         if (p != null)
    {
          p.CancelAndDisposeTurnTimeout();
     _roomRepository.Update(roomCleanup);
 }
          }
            }
   }
        }

      public void CancelTurnTimeout(string code)
{
          if (!_roomRepository.TryGetById(code, out var room))
        return;

          lock (room)
   {
  foreach (var p in room.Players.Values)
        {
 if (p.TurnTimeoutCts != null)
        {
       p.CancelAndDisposeTurnTimeout();
            }
    }
              _roomRepository.Update(room);
            }
        }

        public void StartRematchWindow(string code)
        {
         if (!_roomRepository.TryGetById(code, out var room))
      return;

            lock (room)
            {
            if (room.RematchExpiresAt != null && room.RematchExpiresAt > DateTimeOffset.UtcNow)
           return;

       room.RematchExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_settings.RematchWindowSeconds);
        _roomRepository.Update(room);
   }

            _ = _hubContext.Clients.Group(code).SendAsync("RematchWindowStarted", room.RematchExpiresAt);

    _ = Task.Run(async () =>
            {
    try
       {
      var waitMs = (int)Math.Ceiling((room.RematchExpiresAt.Value - DateTimeOffset.UtcNow).TotalMilliseconds);
    if (waitMs > 0)
         await Task.Delay(waitMs);

            lock (room)
     {
          if (room.RematchExpiresAt == null || room.RematchExpiresAt > DateTimeOffset.UtcNow)
      return;
    }

        await _hubContext.Clients.Group(code).SendAsync("RematchWindowExpired", code);
 TryRemoveRoom(code);
     }
         catch (Exception ex)
  {
          _logger.LogError(ex, "Error in rematch window task for room {Code}", code);
    }
        });
        }

        public bool OfferRematch(string code, string playerId, out DateTimeOffset? expiresAt)
        {
 expiresAt = null;
    if (!_roomRepository.TryGetById(code, out var room))
    return false;

    lock (room)
            {
     if (!room.Players.ContainsKey(playerId))
         return false;

      if (room.RematchExpiresAt == null || room.RematchExpiresAt < DateTimeOffset.UtcNow)
   {
      room.RematchExpiresAt = DateTimeOffset.UtcNow.AddSeconds(_settings.RematchWindowSeconds);
      }

   room.RematchOffers.Add(playerId);
         expiresAt = room.RematchExpiresAt;
      _roomRepository.Update(room);
            }

            StartRematchWindow(code);
       return true;
      }

     public async Task<bool> AcceptRematch(string code, string playerId)
        {
bool rematchStarted = false;
            if (!_roomRepository.TryGetById(code, out var room))
  return false;

   lock (room)
          {
       if (room.RematchExpiresAt == null || room.RematchExpiresAt < DateTimeOffset.UtcNow)
      return false;

            if (!room.Players.ContainsKey(playerId))
   return false;

      room.RematchOffers.Add(playerId);

           if (room.RematchOffers.Count >= room.Players.Count)
                {
  room.ResetForRematch(_rng);
         room.RematchExpiresAt = null;
         room.RematchOffers.Clear();

   rematchStarted = true;
    _roomRepository.Update(room);
              }
            }

            if (rematchStarted)
     {
    _logger.LogInformation("Rematch started for room {Code}", code);

    await _hubContext.Clients.Group(code).SendAsync("RematchStarted", code);

  _ = StartTurnTimeoutAsync(code);
  }

     return rematchStarted;
   }
    }
}
