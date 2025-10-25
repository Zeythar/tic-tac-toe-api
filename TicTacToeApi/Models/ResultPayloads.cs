using System.Collections.Immutable;

namespace TicTacToeApi.Models
{
 /// <summary>
    /// Payload for a successful game join operation
    /// Uses immutable collections for thread safety
    /// </summary>
    public sealed record GameJoinPayload(
    string Code,
        string PlayerId,
     ImmutableArray<int> Board,
        string Symbol,
      string CurrentTurn);

    /// <summary>
/// Payload for a successful move operation
    /// Uses immutable collections for thread safety
    /// </summary>
    public sealed record MovePayload(
        ImmutableArray<int> Board,
        string CurrentTurn,
bool IsGameOver,
        string? Winner,
      GameOverDto? GameOver = null);

    /// <summary>
    /// Payload for a successful game state retrieval
    /// Uses immutable collections for thread safety
    /// </summary>
    public sealed record GameStatePayload(
        ImmutableArray<int> Board,
      string? Symbol,
        string? CurrentTurn,
  bool IsGameOver,
     string? Winner);
}
