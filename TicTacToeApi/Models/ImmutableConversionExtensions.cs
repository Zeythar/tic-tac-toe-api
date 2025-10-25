using System.Collections.Immutable;

namespace TicTacToeApi.Models
{
    /// <summary>
    /// Extension methods for converting between mutable and immutable collections
    /// Used throughout the API to ensure thread-safe data transfer
    /// </summary>
    public static class ImmutableConversionExtensions
    {
      /// <summary>
        /// Converts a mutable int array to an immutable array
        /// Creates a defensive copy to prevent external mutation
        /// </summary>
        public static ImmutableArray<int> ToImmutableBoard(this int[]? board)
        {
    if (board == null)
      return ImmutableArray<int>.Empty;

            return ImmutableArray.CreateRange(board);
        }

        /// <summary>
     /// Converts an immutable array back to a mutable array
        /// Used when passing data to game engine or mutable structures
        /// </summary>
        public static int[]? ToMutableArray(this ImmutableArray<int> immutableBoard)
        {
     if (immutableBoard.IsEmpty)
     return null;

        return immutableBoard.ToArray();
   }

        /// <summary>
  /// Converts a collection of DisconnectedPlayerInfoDto to immutable
        /// </summary>
   public static ImmutableArray<DisconnectedPlayerInfoDto> ToImmutableDisconnected(
this IEnumerable<DisconnectedPlayerInfoDto>? items)
        {
         if (items == null)
  return ImmutableArray<DisconnectedPlayerInfoDto>.Empty;

            return ImmutableArray.CreateRange(items);
        }

        /// <summary>
        /// Creates an immutable copy of board data for thread-safe transmission
        /// </summary>
        public static ImmutableArray<int> CreateBoardSnapshot(this int[] board)
        {
    return board.ToImmutableBoard();
  }
    }
}
