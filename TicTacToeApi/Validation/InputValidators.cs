using FluentValidation;

namespace TicTacToeApi.Validation
{
    /// <summary>
    /// Validator for game room codes
    /// Ensures room codes meet format and length requirements
    /// </summary>
    public class RoomCodeValidator : AbstractValidator<string>
    {
        public RoomCodeValidator()
  {
            RuleFor(code => code)
                .NotEmpty()
   .WithMessage("Room code is required.")
    .Length(4, 6)
  .WithMessage("Room code must be between 4 and 6 characters.")
          .Matches(@"^[A-Z0-9]+$")
  .WithMessage("Room code must contain only uppercase letters and numbers.");
        }
    }

    /// <summary>
    /// Validator for board move positions
    /// Ensures move index is within valid board bounds
    /// </summary>
    public class MovePositionValidator : AbstractValidator<int>
    {
        private const int BoardSize = 9;

        public MovePositionValidator()
        {
   RuleFor(index => index)
         .InclusiveBetween(0, BoardSize - 1)
             .WithMessage($"Move index must be between 0 and {BoardSize - 1}.");
        }
    }

    /// <summary>
    /// Validator for player IDs
    /// Ensures player IDs are valid GUIDs in string format
    /// </summary>
    public class PlayerIdValidator : AbstractValidator<string>
    {
        public PlayerIdValidator()
   {
  RuleFor(playerId => playerId)
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
}
