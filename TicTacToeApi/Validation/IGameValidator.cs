using FluentValidation;
using TicTacToeApi.Models;

namespace TicTacToeApi.Validation
{
    /// <summary>
    /// Service interface for input validation
 /// Provides centralized validation for game operations
    /// </summary>
    public interface IGameValidator
    {
    /// <summary>
        /// Validates a room code
        /// </summary>
   /// <returns>Error code if invalid, null if valid</returns>
        string? ValidateRoomCode(string? code);

        /// <summary>
        /// Validates a move position
     /// </summary>
        /// <returns>Error code if invalid, null if valid</returns>
      string? ValidateMovePosition(int index);

        /// <summary>
 /// Validates a player ID
        /// </summary>
        /// <returns>Error code if invalid, null if valid</returns>
        string? ValidatePlayerId(string? playerId);

        /// <summary>
/// Validates a join game request
        /// </summary>
        /// <returns>Error code if invalid, null if valid</returns>
      string? ValidateJoinRequest(JoinGameRequest request);

        /// <summary>
  /// Validates a make move request
        /// </summary>
        /// <returns>Error code if invalid, null if valid</returns>
        string? ValidateMakeMoveRequest(MakeMoveRequest request);

    /// <summary>
        /// Validates a reconnect request
        /// </summary>
        /// <returns>Error code if invalid, null if valid</returns>
        string? ValidateReconnectRequest(ReconnectRequest request);

        /// <summary>
  /// Validates a get game state request
        /// </summary>
        /// <returns>Error code if invalid, null if valid</returns>
        string? ValidateGetGameStateRequest(GetGameStateRequest request);

        /// <summary>
        /// Validates a rematch action request
        /// </summary>
   /// <returns>Error code if invalid, null if valid</returns>
    string? ValidateRematchActionRequest(RematchActionRequest request);
    }

    /// <summary>
    /// Implementation of game validator using FluentValidation
    /// </summary>
    public sealed class GameValidator : IGameValidator
    {
        private readonly IValidator<string> _roomCodeValidator;
        private readonly IValidator<int> _movePositionValidator;
        private readonly IValidator<string> _playerIdValidator;
        private readonly IValidator<JoinGameRequest> _joinGameValidator;
        private readonly IValidator<MakeMoveRequest> _makeMoveValidator;
        private readonly IValidator<ReconnectRequest> _reconnectValidator;
        private readonly IValidator<GetGameStateRequest> _getGameStateValidator;
        private readonly IValidator<RematchActionRequest> _rematchActionValidator;

  public GameValidator()
        {
         _roomCodeValidator = new RoomCodeValidator();
       _movePositionValidator = new MovePositionValidator();
 _playerIdValidator = new PlayerIdValidator();
            _joinGameValidator = new JoinGameRequestValidator();
       _makeMoveValidator = new MakeMoveRequestValidator();
          _reconnectValidator = new ReconnectRequestValidator();
        _getGameStateValidator = new GetGameStateRequestValidator();
            _rematchActionValidator = new RematchActionRequestValidator();
        }

        public string? ValidateRoomCode(string? code)
        {
            if (string.IsNullOrEmpty(code))
   return ErrorCode.NotFound;

            var result = _roomCodeValidator.Validate(code);
            return result.IsValid ? null : ErrorCode.Invalid;
        }

      public string? ValidateMovePosition(int index)
        {
         var result = _movePositionValidator.Validate(index);
     return result.IsValid ? null : ErrorCode.InvalidIndex;
        }

  public string? ValidatePlayerId(string? playerId)
        {
  if (string.IsNullOrEmpty(playerId))
       return ErrorCode.NotInGame;

    var result = _playerIdValidator.Validate(playerId);
            return result.IsValid ? null : ErrorCode.NotInGame;
   }

        public string? ValidateJoinRequest(JoinGameRequest request)
        {
   if (request == null)
          return ErrorCode.Invalid;

    var result = _joinGameValidator.Validate(request);
     return result.IsValid ? null : ErrorCode.Invalid;
        }

  public string? ValidateMakeMoveRequest(MakeMoveRequest request)
        {
            if (request == null)
                return ErrorCode.Invalid;

         var result = _makeMoveValidator.Validate(request);
  if (!result.IsValid)
       return ErrorCode.InvalidIndex; // Return specific error based on what failed

   return null;
        }

        public string? ValidateReconnectRequest(ReconnectRequest request)
        {
        if (request == null)
   return ErrorCode.Invalid;

var result = _reconnectValidator.Validate(request);
          return result.IsValid ? null : ErrorCode.ReconnectFailed;
      }

      public string? ValidateGetGameStateRequest(GetGameStateRequest request)
 {
         if (request == null)
      return ErrorCode.Invalid;

        var result = _getGameStateValidator.Validate(request);
 return result.IsValid ? null : ErrorCode.NotFound;
  }

    public string? ValidateRematchActionRequest(RematchActionRequest request)
     {
         if (request == null)
            return ErrorCode.Invalid;

            var result = _rematchActionValidator.Validate(request);
    return result.IsValid ? null : ErrorCode.Invalid;
  }
    }
}
