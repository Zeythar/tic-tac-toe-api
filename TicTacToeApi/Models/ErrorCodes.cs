namespace TicTacToeApi.Models
{
    /// <summary>
    /// Centralized error codes for all game operations.
    /// Provides compile-time safety and prevents typos/inconsistencies.
    /// </summary>
    public static class ErrorCode
  {
        // Game operation errors
        public const string InvalidIndex = nameof(InvalidIndex);
      public const string CellTaken = nameof(CellTaken);
 public const string NotYourTurn = nameof(NotYourTurn);
        public const string OpponentDisconnected = nameof(OpponentDisconnected);
        public const string GameOver = nameof(GameOver);
        public const string Invalid = nameof(Invalid);

        // Room/Join errors
        public const string NotFound = nameof(NotFound);
        public const string RoomFull = nameof(RoomFull);
      public const string AlreadyInRoom = nameof(AlreadyInRoom);
   public const string ReconnectRequired = nameof(ReconnectRequired);
    public const string PlayerIdInUse = nameof(PlayerIdInUse);

    // Player/Connection errors
        public const string NotInGame = nameof(NotInGame);
        public const string ReconnectFailed = nameof(ReconnectFailed);

        // Rematch errors
        public const string OfferFailed = nameof(OfferFailed);
  public const string AcceptFailed = nameof(AcceptFailed);

        /// <summary>
        /// Gets all error codes as a collection.
        /// Useful for validation or documentation.
   /// </summary>
        public static IEnumerable<string> GetAll()
        {
      return new[]
            {
       InvalidIndex,
   CellTaken,
         NotYourTurn,
      OpponentDisconnected,
   GameOver,
        Invalid,
        NotFound,
          RoomFull,
             AlreadyInRoom,
 ReconnectRequired,
  PlayerIdInUse,
    NotInGame,
           ReconnectFailed,
        OfferFailed,
     AcceptFailed
    };
        }
    }

    /// <summary>
    /// User-friendly error messages corresponding to error codes.
    /// Useful for logging and client communication.
    /// </summary>
    public static class ErrorMessage
    {
        private static readonly Dictionary<string, string> Messages = new()
 {
  { ErrorCode.InvalidIndex, "The board index is invalid." },
  { ErrorCode.CellTaken, "The cell is already occupied." },
      { ErrorCode.NotYourTurn, "It is not your turn." },
            { ErrorCode.OpponentDisconnected, "Opponent is disconnected." },
            { ErrorCode.GameOver, "The game is already over." },
    { ErrorCode.Invalid, "Invalid move." },
    { ErrorCode.NotFound, "Room not found." },
        { ErrorCode.RoomFull, "Room is full." },
   { ErrorCode.AlreadyInRoom, "Caller already in room." },
   { ErrorCode.ReconnectRequired, "PlayerId exists, call Reconnect with your playerId." },
      { ErrorCode.PlayerIdInUse, "PlayerId is in use by a different connection." },
          { ErrorCode.NotInGame, "Player not in game or wrong connection." },
         { ErrorCode.ReconnectFailed, "Could not reclaim player slot." },
            { ErrorCode.OfferFailed, "Could not offer rematch." },
 { ErrorCode.AcceptFailed, "Could not accept rematch or window expired." }
        };

    /// <summary>
/// Gets the standard error message for the given error code.
        /// </summary>
        public static string GetMessage(string errorCode)
        {
       return Messages.TryGetValue(errorCode, out var message)
            ? message
        : $"An error occurred: {errorCode}";
        }
    }
}
