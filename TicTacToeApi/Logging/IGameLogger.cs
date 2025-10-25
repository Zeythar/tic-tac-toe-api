namespace TicTacToeApi.Logging
{
    /// <summary>
    /// Domain-specific logging interface for game operations
    /// Provides structured logging with semantic methods for game events
    /// Wraps ILogger<T> with game-specific context and correlation tracking
    /// </summary>
    public interface IGameLogger
    {
        /// <summary>
        /// Logs a player action (joining, moving, disconnecting)
        /// </summary>
        void LogPlayerAction(string actionType, string playerId, string roomCode, string connectionId, string? details = null);

        /// <summary>
  /// Logs a room lifecycle event (created, joined, closed)
/// </summary>
        void LogRoomEvent(string eventType, string roomCode, string? details = null);

        /// <summary>
    /// Logs a game state transition
        /// </summary>
        void LogGameStateTransition(string roomCode, string fromState, string toState, string? reason = null);

        /// <summary>
        /// Logs a move operation
        /// </summary>
        void LogMove(string roomCode, string playerId, int moveIndex, bool success, string? errorCode = null);

        /// <summary>
        /// Logs game outcome (win, draw, forfeit)
        /// </summary>
        void LogGameOutcome(string roomCode, string outcomeType, string? winner = null, string? details = null);

        /// <summary>
     /// Logs rematch operation
        /// </summary>
 void LogRematchOperation(string roomCode, string operationType, string playerId, bool success, string? details = null);

        /// <summary>
        /// Logs validation failure
        /// </summary>
        void LogValidationFailure(string operationType, string errorCode, string? details = null);

    /// <summary>
        /// Logs timeout event
        /// </summary>
        void LogTimeout(string roomCode, string timeoutType, string? details = null);

   /// <summary>
   /// Logs connection/disconnection events
        /// </summary>
     void LogConnectionEvent(string roomCode, string eventType, string connectionId, string playerId, string? details = null);

        /// <summary>
        /// Logs error with correlation context
        /// </summary>
        void LogError(string operation, Exception ex, string? correlationId = null, string? details = null);

        /// <summary>
        /// Logs a debug message with context
   /// </summary>
     void LogDebug(string message, string? correlationId = null, string? details = null);

 /// <summary>
        /// Gets or sets the current correlation ID for this logger instance
        /// </summary>
        string? CurrentCorrelationId { get; set; }
    }

    /// <summary>
    /// Implementation of domain-specific game logger using .NET 9 structured logging
    /// Provides semantic, structured logging with correlation ID support
    /// </summary>
    public sealed class GameLogger : IGameLogger
 {
        private readonly ILogger<GameLogger> _logger;
        private string? _correlationId;

        public string? CurrentCorrelationId
        {
            get => _correlationId;
            set => _correlationId = value;
      }

        public GameLogger(ILogger<GameLogger> logger)
        {
     ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
        }

     /// <summary>
        /// Helper method to execute logging within a correlation scope
        /// Reduces code duplication by centralizing the scope creation and logging pattern
   /// </summary>
  private void LogWithScope(Dictionary<string, object> scopeData, Action<ILogger> logAction)
        {
            using (_logger.BeginScope(scopeData))
      {
        logAction(_logger);
          }
     }

        public void LogPlayerAction(string actionType, string playerId, string roomCode, string connectionId, string? details = null)
   {
 LogWithScope(
                new Dictionary<string, object>
       {
             { "CorrelationId", _correlationId ?? "unknown" },
          { "PlayerId", playerId },
           { "RoomCode", roomCode },
       { "ConnectionId", connectionId }
      },
 logger => logger.LogInformation(
             "Player action: {ActionType} | Player: {PlayerId} | Room: {RoomCode} | Connection: {ConnectionId} | Details: {Details}",
         actionType, playerId, roomCode, connectionId, details ?? "none"));
  }

    public void LogRoomEvent(string eventType, string roomCode, string? details = null)
        {
            LogWithScope(
     new Dictionary<string, object>
       {
         { "CorrelationId", _correlationId ?? "unknown" },
       { "RoomCode", roomCode }
 },
logger => logger.LogInformation(
    "Room event: {EventType} | Room: {RoomCode} | Details: {Details}",
           eventType, roomCode, details ?? "none"));
        }

 public void LogGameStateTransition(string roomCode, string fromState, string toState, string? reason = null)
        {
            LogWithScope(
new Dictionary<string, object>
 {
           { "CorrelationId", _correlationId ?? "unknown" },
          { "RoomCode", roomCode },
     { "FromState", fromState },
       { "ToState", toState }
              },
                logger => logger.LogInformation(
         "State transition: {FromState} -> {ToState} | Room: {RoomCode} | Reason: {Reason}",
         fromState, toState, roomCode, reason ?? "none"));
        }

        public void LogMove(string roomCode, string playerId, int moveIndex, bool success, string? errorCode = null)
        {
     var logLevel = success ? LogLevel.Information : LogLevel.Warning;
            LogWithScope(
           new Dictionary<string, object>
   {
   { "CorrelationId", _correlationId ?? "unknown" },
                    { "RoomCode", roomCode },
 { "PlayerId", playerId },
      { "MoveIndex", moveIndex }
       },
                logger => logger.Log(
            logLevel,
    "Move attempt: {Outcome} | Player: {PlayerId} | Room: {RoomCode} | Index: {MoveIndex} | Error: {ErrorCode}",
             success ? "SUCCESS" : "FAILED", playerId, roomCode, moveIndex, errorCode ?? "none"));
        }

        public void LogGameOutcome(string roomCode, string outcomeType, string? winner = null, string? details = null)
    {
       LogWithScope(
           new Dictionary<string, object>
 {
      { "CorrelationId", _correlationId ?? "unknown" },
         { "RoomCode", roomCode },
             { "OutcomeType", outcomeType }
            },
       logger => logger.LogInformation(
              "Game outcome: {OutcomeType} | Room: {RoomCode} | Winner: {Winner} | Details: {Details}",
           outcomeType, roomCode, winner ?? "none", details ?? "none"));
        }

        public void LogRematchOperation(string roomCode, string operationType, string playerId, bool success, string? details = null)
        {
        var logLevel = success ? LogLevel.Information : LogLevel.Warning;
            LogWithScope(
   new Dictionary<string, object>
           {
         { "CorrelationId", _correlationId ?? "unknown" },
       { "RoomCode", roomCode },
   { "PlayerId", playerId },
             { "OperationType", operationType }
 },
    logger => logger.Log(
             logLevel,
              "Rematch operation: {OperationType} | {Outcome} | Room: {RoomCode} | Player: {PlayerId} | Details: {Details}",
      operationType, success ? "SUCCESS" : "FAILED", roomCode, playerId, details ?? "none"));
        }

        public void LogValidationFailure(string operationType, string errorCode, string? details = null)
        {
        LogWithScope(
            new Dictionary<string, object>
                {
         { "CorrelationId", _correlationId ?? "unknown" },
   { "OperationType", operationType },
 { "ErrorCode", errorCode }
      },
                logger => logger.LogWarning(
 "Validation failed: {OperationType} | Error: {ErrorCode} | Details: {Details}",
     operationType, errorCode, details ?? "none"));
  }

        public void LogTimeout(string roomCode, string timeoutType, string? details = null)
        {
LogWithScope(
    new Dictionary<string, object>
     {
        { "CorrelationId", _correlationId ?? "unknown" },
         { "RoomCode", roomCode },
         { "TimeoutType", timeoutType }
       },
           logger => logger.LogWarning(
            "Timeout event: {TimeoutType} | Room: {RoomCode} | Details: {Details}",
             timeoutType, roomCode, details ?? "none"));
        }

        public void LogConnectionEvent(string roomCode, string eventType, string connectionId, string playerId, string? details = null)
        {
            LogWithScope(
     new Dictionary<string, object>
                {
   { "CorrelationId", _correlationId ?? "unknown" },
  { "RoomCode", roomCode },
  { "ConnectionId", connectionId },
        { "PlayerId", playerId }
                },
   logger => logger.LogInformation(
   "Connection event: {EventType} | Room: {RoomCode} | Connection: {ConnectionId} | Player: {PlayerId} | Details: {Details}",
   eventType, roomCode, connectionId, playerId, details ?? "none"));
        }

     public void LogError(string operation, Exception ex, string? correlationId = null, string? details = null)
  {
            ArgumentNullException.ThrowIfNull(ex);
            var corrId = correlationId ?? _correlationId ?? "unknown";
       LogWithScope(
          new Dictionary<string, object>
      {
        { "CorrelationId", corrId },
{ "Operation", operation },
          { "ExceptionType", ex.GetType().Name }
       },
         logger => logger.LogError(
ex,
         "Error occurred: {Operation} | Exception: {ExceptionType} | Message: {Message} | Details: {Details}",
            operation, ex.GetType().Name, ex.Message, details ?? "none"));
        }

 public void LogDebug(string message, string? correlationId = null, string? details = null)
        {
       var corrId = correlationId ?? _correlationId ?? "unknown";
      LogWithScope(
                new Dictionary<string, object>
          {
         { "CorrelationId", corrId }
         },
           logger => logger.LogDebug(
        "Debug: {Message} | Details: {Details}",
  message, details ?? "none"));
      }
    }
}
