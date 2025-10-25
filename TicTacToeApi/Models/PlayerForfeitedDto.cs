namespace TicTacToeApi.Models
{
 /// <summary>
 /// DTO for PlayerForfeited hub event
 /// </summary>
 public sealed record PlayerForfeitedDto(
 string ForfeiterId,
 string? WinnerId,
 string? WinnerSymbol,
 int[]? BoardSnapshot,
 string? CurrentTurn,
 bool IsGameOver);
}
