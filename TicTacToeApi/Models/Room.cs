using TicTacToeApi.Core;

namespace TicTacToeApi.Models
{
    /// <summary>
    /// Represents a game room with board state and players
    /// </summary>
    public sealed class Room
 {
     private readonly GameEngine _gameEngine;

 public string Code { get; }
        public int[] Board { get; }
      public Dictionary<string, Player> Players { get; }
 public List<string> PlayerOrder { get; }
   public string? CurrentTurn { get; set; }
      public bool IsGameOver { get; set; }
        public string? Winner { get; set; }

        // Tracks rematch offers from players (playerId set)
      public HashSet<string> RematchOffers { get; } = new();

  // Optional expiry for rematch window
 public DateTimeOffset? RematchExpiresAt { get; set; }

   // Timestamps for idle/cleanup logic
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset LastActivityAt { get; private set; }

        // Version for per-room turn timer to prevent stale timers acting after reset
        public int TurnTimerVersion { get; set; } = 0;

    public Room(string code, int boardSize = 9) : this(code, new GameEngine())
  {
     }

        public Room(string code, GameEngine gameEngine)
        {
   Code = code;
    _gameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));
            Board = _gameEngine.CreateBoard();
       Players = new Dictionary<string, Player>();
            PlayerOrder = new List<string>();
   CreatedAt = DateTimeOffset.UtcNow;
     LastActivityAt = CreatedAt;
      }

        private void Touch() => LastActivityAt = DateTimeOffset.UtcNow;

        /// <summary>
   /// Checks if the room can accept new players
  /// </summary>
    public bool CanJoin(int maxPlayers) =>
         Players.Count < maxPlayers && PlayerOrder.Count < maxPlayers;

        /// <summary>
        /// Adds or updates a player connection
        /// </summary>
        public void AddConnection(string playerId, string connectionId)
        {
            // Prevent a single connection from occupying multiple player slots
   if (Players.Values.Any(p => p.ConnectionId == connectionId))
     return;

  if (!Players.ContainsKey(playerId))
      {
    var player = new Player(playerId, connectionId);
        Players[playerId] = player;
   PlayerOrder.Add(playerId);
      }
    else
          {
    Players[playerId].ConnectionId = connectionId;
        }

        Touch();
    }

 /// <summary>
 /// Removes a connection from a player slot
    /// </summary>
public void RemoveConnection(string connectionId)
   {
            var player = Players.Values.FirstOrDefault(p => p.ConnectionId == connectionId);
       if (player != null)
      {
     player.ConnectionId = null;
 }

        Touch();
        }

/// <summary>
        /// Finds a player by connection ID
     /// </summary>
        public Player? GetPlayerByConnectionId(string connectionId) =>
            Players.Values.FirstOrDefault(p => p.ConnectionId == connectionId);

 /// <summary>
      /// Attempts to start the game if both players are present
        /// </summary>
        public bool TryStartGame(Random rng)
        {
if (PlayerOrder.Count < 2)
    return false;

   // If symbols already assigned, game already started
         if (Players.Values.Any(p => p.HasSymbol))
        return false;

            var firstId = PlayerOrder[0];
        var secondId = PlayerOrder[1];

         var (firstSymbol, secondSymbol) = _gameEngine.AssignSymbols(rng);
     Players[firstId].Symbol = firstSymbol;
     Players[secondId].Symbol = secondSymbol;

 // X always starts
           CurrentTurn = "X";
 Touch();
   return true;
        }

      /// <summary>
  /// Attempts to make a move on the board
        /// </summary>
     public MoveAttemptResult TryMakeMove(string connectionId, int index)
     {
  if (IsGameOver)
                return MoveAttemptResult.Fail(ErrorCode.GameOver, Board, CurrentTurn, IsGameOver, Winner);

   var player = GetPlayerByConnectionId(connectionId);
       if (player == null || player.Symbol == null)
 return MoveAttemptResult.Fail(ErrorCode.NotInGame, Board, CurrentTurn, IsGameOver, Winner);

      // Prevent moves when any player is disconnected; freeze board until opponent reconnects or forfeits
 if (!AllPlayersConnected())
       return MoveAttemptResult.Fail(ErrorCode.OpponentDisconnected, Board, CurrentTurn, IsGameOver, Winner);

  if (CurrentTurn != player.Symbol)
    return MoveAttemptResult.Fail(ErrorCode.NotYourTurn, Board, CurrentTurn, IsGameOver, Winner);

  var result = _gameEngine.TryApplyMove(Board, player.Symbol, index);

    if (!result.IsValid)
  {
     return MoveAttemptResult.Fail(result.ErrorCode ?? ErrorCode.Invalid, Board, CurrentTurn, IsGameOver, Winner);
 }

if (result.IsGameOver)
       {
      IsGameOver = true;
    Winner = result.Winner;
    }
 else
    {
           CurrentTurn = result.NextTurn;
    }

      Touch();
   return MoveAttemptResult.CreateSuccess(Board, CurrentTurn, IsGameOver, Winner);
        }

   /// <summary>
    /// Forfeits the game for a player (opponent wins)
  /// </summary>
        public void Forfeit(string playerId)
     {
   var opponent = Players.Values.FirstOrDefault(p => p.PlayerId != playerId);
    IsGameOver = true;
            Winner = opponent?.Symbol;
  Touch();
  }

  /// <summary>
        /// Forfeits the current player because of turn timeout
        /// </summary>
 public void ForfeitCurrentPlayerDueToTimeout()
        {
    if (CurrentTurn == null)
 return;

    var currentPlayer = Players.Values.FirstOrDefault(p => p.Symbol == CurrentTurn);
    if (currentPlayer == null)
   return;

           Forfeit(currentPlayer.PlayerId);
        }

  /// <summary>
        /// Gets all disconnected players
        /// </summary>
     public IEnumerable<Player> GetDisconnectedPlayers() =>
            Players.Values.Where(p => !p.IsConnected);

      /// <summary>
       /// Checks if all players are connected
        /// </summary>
        public bool AllPlayersConnected() =>
       Players.Values.All(p => p.IsConnected);

/// <summary>
  /// Returns true if the room is considered idle for cleanup (e.g., waiting for opponent)
      /// </summary>
      public bool IsIdleForCleanup(TimeSpan idleThreshold)
 {
  // Consider idle if not started (no symbols assigned) and fewer than max players joined, or if no connected players
       bool notStarted = !Players.Values.Any(p => p.HasSymbol);
       bool hasNoConnections = Players.Values.All(p => !p.IsConnected);

        return (notStarted && Players.Count < 2 && (DateTimeOffset.UtcNow - LastActivityAt) > idleThreshold) || hasNoConnections;
        }

    /// <summary>
 /// Resets the room state for a rematch. Clears the board, clears symbols and rematch offers.
 /// Starts a new game by assigning symbols. Caller should hold the room lock when invoking.
        /// </summary>
        public void ResetForRematch(Random rng)
  {
    // Clear board
 for (int i = 0; i < Board.Length; i++) Board[i] = 0;

        // Reset game flags
     IsGameOver = false;
 Winner = null;
     CurrentTurn = null;

          // Clear player symbols and timers state
   foreach (var p in Players.Values)
    {
  p.Symbol = null;
      p.RemainingTurnSeconds = null;
        p.CancelAndDisposeTurnTimeout();
    p.CancelAndDisposeReconnectionTimeout();
        // Reset grace usage for a fresh rematch
                p.GraceUsed = false;
    }

            // Clear rematch offers
     RematchOffers.Clear();
  RematchExpiresAt = null;

   // Bump timer version to invalidate any stale timers
        TurnTimerVersion++;

 // Assign symbols and set current turn if possible
        TryStartGame(rng);
    }

     /// <summary>
  /// Result of attempting to make a move
        /// </summary>
        public sealed class MoveAttemptResult
        {
          public bool Success { get; init; }
  public string? ErrorCode { get; init; }
      public int[] Board { get; init; }
  public string? CurrentTurn { get; init; }
    public bool IsGameOver { get; init; }
 public string? Winner { get; init; }

   private MoveAttemptResult(bool success, int[] board, string? currentTurn, bool isGameOver, string? winner, string? errorCode = null)
     {
        Success = success;
      Board = board.ToArray();
        CurrentTurn = currentTurn;
            IsGameOver = isGameOver;
 Winner = winner;
 ErrorCode = errorCode;
        }

 public static MoveAttemptResult CreateSuccess(int[] board, string? currentTurn, bool isGameOver, string? winner) =>
  new(true, board, currentTurn, isGameOver, winner);

       public static MoveAttemptResult Fail(string errorCode, int[] board, string? currentTurn, bool isGameOver, string? winner) =>
     new(false, board, currentTurn, isGameOver, winner, errorCode);
        }
    }
}
