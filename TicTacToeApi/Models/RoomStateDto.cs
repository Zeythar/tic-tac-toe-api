using System;
using System.Collections.Immutable;

namespace TicTacToeApi.Models
{
 public sealed record OpponentDto(string? PlayerId, bool Present, bool IsConnected, string? Symbol);

 public sealed record DisconnectedPlayerInfoDto(string PlayerId, bool GraceUsed, bool IsConnected, int? RemainingReconnectionSeconds);

 /// <summary>
 /// Aggregated room state used for initial sync / reconnect
 /// Uses immutable collections for thread safety during SignalR transmission
 /// </summary>
 public sealed record RoomStateDto(
 string RoomCode,
 ImmutableArray<int> Board,
 string? YourSymbol,
 string? CurrentTurn,
 bool IsGameOver,
 string? Winner,
 OpponentDto? Opponent,
 bool OpponentPresent,
 bool OpponentConnected,
 string? DisconnectedPlayerId,
 DateTimeOffset? TurnExpiryUtc,
 ImmutableArray<DisconnectedPlayerInfoDto> DisconnectedPlayers,
 DateTimeOffset ServerTimestamp
 );
}
