namespace TicTacToeApi.Core.Strategies
{
    /// <summary>
    /// Strategy interface for checking win conditions in the game
    /// Allows different win condition implementations to be plugged in
    /// </summary>
    public interface IWinConditionStrategy
    {
        /// <summary>
        /// Checks if there is a winner on the current board state
        /// </summary>
        /// <param name="board">The current game board</param>
        /// <returns>The winning symbol ("X" or "O"), or null if no winner</returns>
        string? CheckWinner(int[] board);

        /// <summary>
        /// Gets a description of the win condition (for logging/UI)
        /// </summary>
  string Description { get; }
    }

    /// <summary>
    /// Strategy interface for checking draw conditions
    /// Allows different draw condition implementations
    /// </summary>
    public interface IDrawConditionStrategy
    {
        /// <summary>
  /// Checks if the game is in a draw state
        /// </summary>
        /// <param name="board">The current game board</param>
        /// <returns>True if the game is a draw, false otherwise</returns>
 bool IsDraw(int[] board);

        /// <summary>
        /// Gets a description of the draw condition
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// Strategy interface for symbol assignment
    /// Allows different ways to assign X and O to players
  /// </summary>
    public interface ISymbolAssignmentStrategy
    {
  /// <summary>
        /// Assigns symbols to two players
        /// </summary>
   /// <param name="rng">Random number generator</param>
        /// <returns>Tuple of (firstPlayerSymbol, secondPlayerSymbol)</returns>
  (string firstPlayerSymbol, string secondPlayerSymbol) AssignSymbols(Random rng);

 /// <summary>
      /// Gets a description of the assignment strategy
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// Strategy interface for board initialization
    /// Allows different board sizes and layouts
    /// </summary>
    public interface IBoardStrategy
    {
   /// <summary>
     /// Creates a new empty board
/// </summary>
    /// <returns>A new board array</returns>
        int[] CreateBoard();

        /// <summary>
   /// Gets the size of the board
        /// </summary>
        int BoardSize { get; }

        /// <summary>
    /// Gets the width of the board (for rectangular boards)
        /// </summary>
        int Width { get; }

 /// <summary>
     /// Gets the height of the board (for rectangular boards)
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Validates if an index is within board bounds
        /// </summary>
        bool IsValidIndex(int index);

        /// <summary>
  /// Gets a description of the board strategy
  /// </summary>
        string Description { get; }
    }
}
