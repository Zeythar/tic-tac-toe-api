using System.Collections.Immutable;

namespace TicTacToeApi.Models
{
 /// <summary>
 /// Enumerates possible game-over results
 /// </summary>
 public enum GameOverResult
 {
    /// <summary>
     /// A player won the game (either by completing a winning pattern or opponent timeout/disconnect)
        /// </summary>
 Winner,
    /// <summary>
        /// The game ended in a draw (board full, no winner)
     /// </summary>
 Draw,
        /// <summary>
      /// The game was cancelled (room expired, abandoned, etc.)
        /// </summary>
 Cancelled
 }

 /// <summary>
 /// DTO for GameOver hub event using immutable arrays for thread-safety
 /// </summary>
 public sealed record GameOverDto(
 string RoomCode,
 GameOverResult Result,
 string? WinnerId,
 string? WinnerSymbol,
 ImmutableArray<int>? BoardSnapshot,
 string? CurrentTurn,
 bool IsGameOver,
 string? Message = null,
 string? CorrelationId = null,
 string? ServerTimestamp = null);
}
