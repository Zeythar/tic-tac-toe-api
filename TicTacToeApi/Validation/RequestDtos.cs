using FluentValidation;

namespace TicTacToeApi.Validation
{
    /// <summary>
    /// Request DTO for joining a game
    /// </summary>
    public class JoinGameRequest
    {
        public string Code { get; set; } = string.Empty;
        public string? ClientPlayerId { get; set; }
    }

    /// <summary>
    /// Validator for JoinGameRequest
    /// </summary>
    public class JoinGameRequestValidator : AbstractValidator<JoinGameRequest>
 {
        public JoinGameRequestValidator()
  {
            RuleFor(r => r.Code)
      .NotEmpty()
       .WithMessage("Room code is required.")
  .Length(4, 6)
      .WithMessage("Room code must be between 4 and 6 characters.")
   .Matches(@"^[A-Z0-9]+$")
    .WithMessage("Room code must contain only uppercase letters and numbers.");

  RuleFor(r => r.ClientPlayerId)
 .Must(id => id == null || Guid.TryParse(id, out _))
         .WithMessage("Client player ID, if provided, must be a valid GUID format.");
        }
    }

  /// <summary>
    /// Request DTO for making a move
    /// </summary>
    public class MakeMoveRequest
    {
        public string Code { get; set; } = string.Empty;
        public int Index { get; set; }
        public string? PlayerId { get; set; }
    }

    /// <summary>
    /// Validator for MakeMoveRequest
    /// </summary>
    public class MakeMoveRequestValidator : AbstractValidator<MakeMoveRequest>
    {
 private const int BoardSize = 9;

        public MakeMoveRequestValidator()
  {
            RuleFor(r => r.Code)
   .NotEmpty()
          .WithMessage("Room code is required.")
      .Length(4, 6)
     .WithMessage("Room code must be between 4 and 6 characters.")
           .Matches(@"^[A-Z0-9]+$")
      .WithMessage("Room code must contain only uppercase letters and numbers.");

      RuleFor(r => r.Index)
      .InclusiveBetween(0, BoardSize - 1)
      .WithMessage($"Move index must be between 0 and {BoardSize - 1}.");

      RuleFor(r => r.PlayerId)
    .Must(id => id == null || Guid.TryParse(id, out _))
       .WithMessage("Player ID, if provided, must be a valid GUID format.");
         }
    }

    /// <summary>
  /// Request DTO for reconnecting to a game
    /// </summary>
    public class ReconnectRequest
    {
        public string Code { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validator for ReconnectRequest
    /// </summary>
    public class ReconnectRequestValidator : AbstractValidator<ReconnectRequest>
{
  public ReconnectRequestValidator()
        {
     RuleFor(r => r.Code)
    .NotEmpty()
  .WithMessage("Room code is required.")
       .Length(4, 6)
           .WithMessage("Room code must be between 4 and 6 characters.")
 .Matches(@"^[A-Z0-9]+$")
   .WithMessage("Room code must contain only uppercase letters and numbers.");

  RuleFor(r => r.PlayerId)
   .NotEmpty()
          .WithMessage("Player ID is required.")
         .Must(IsValidGuid)
   .WithMessage("Player ID must be a valid GUID format.");
        }

private static bool IsValidGuid(string? value)
 {
     if (string.IsNullOrEmpty(value))
    return false;

   return Guid.TryParse(value, out _);
   }
    }

   /// <summary>
   /// Request DTO for getting game state
    /// </summary>
  public class GetGameStateRequest
    {
        public string Code { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validator for GetGameStateRequest
    /// </summary>
    public class GetGameStateRequestValidator : AbstractValidator<GetGameStateRequest>
    {
      public GetGameStateRequestValidator()
    {
           RuleFor(r => r.Code)
.NotEmpty()
    .WithMessage("Room code is required.")
  .Length(4, 6)
     .WithMessage("Room code must be between 4 and 6 characters.")
     .Matches(@"^[A-Z0-9]+$")
      .WithMessage("Room code must contain only uppercase letters and numbers.");

 RuleFor(r => r.PlayerId)
        .NotEmpty()
   .WithMessage("Player ID is required.")
    .Must(IsValidGuid)
  .WithMessage("Player ID must be a valid GUID format.");
       }

        private static bool IsValidGuid(string? value)
        {
    if (string.IsNullOrEmpty(value))
       return false;

   return Guid.TryParse(value, out _);
 }
    }

 /// <summary>
  /// Request DTO for offering a rematch
  /// </summary>
    public class RematchActionRequest
    {
public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validator for RematchActionRequest
    /// </summary>
    public class RematchActionRequestValidator : AbstractValidator<RematchActionRequest>
    {
        public RematchActionRequestValidator()
 {
RuleFor(r => r.Code)
.NotEmpty()
       .WithMessage("Room code is required.")
              .Length(4, 6)
        .WithMessage("Room code must be between 4 and 6 characters.")
        .Matches(@"^[A-Z0-9]+$")
     .WithMessage("Room code must contain only uppercase letters and numbers.");
       }
    }
}
