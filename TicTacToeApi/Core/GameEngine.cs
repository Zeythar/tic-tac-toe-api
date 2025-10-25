namespace TicTacToeApi.Core
{
    using TicTacToeApi.Models;
    using TicTacToeApi.Core.Strategies;

    /// <summary>
    /// Core game engine for Tic-Tac-Toe logic
    /// Uses strategy pattern to support different game rules and variants
    /// </summary>
    public sealed class GameEngine
    {
        private readonly GameRulesConfiguration _rules;

   /// <summary>
        /// Creates a game engine with standard Tic-Tac-Toe rules
        /// </summary>
     public GameEngine() : this(GameRulesConfiguration.CreateStandardTicTacToe())
    {
        }

        /// <summary>
        /// Creates a game engine with custom rules
        /// </summary>
   public GameEngine(GameRulesConfiguration rules)
        {
  _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>
        /// Checks if a move is valid and applies it to the board
   /// </summary>
        public MoveValidationResult TryApplyMove(int[] board, string currentSymbol, int index)
        {
        if (!_rules.Board.IsValidIndex(index))
    return MoveValidationResult.InvalidIndex();

            if (board[index] != 0)
                return MoveValidationResult.CellTaken();

    // Apply the move
            int cellValue = currentSymbol == "X" ? 1 : 2;
        board[index] = cellValue;

        // Check for win using strategy
     string? winner = _rules.WinCondition.CheckWinner(board);
        if (winner != null)
   {
 return MoveValidationResult.GameWon(winner);
   }

          // Check for draw using strategy
            if (_rules.DrawCondition.IsDraw(board))
    {
                return MoveValidationResult.Draw();
            }

      // Game continues - toggle turn
 string nextTurn = currentSymbol == "X" ? "O" : "X";
       return MoveValidationResult.Success(nextTurn);
        }

        /// <summary>
        /// Checks if there is a winner on the board using the win strategy
        /// </summary>
        public string? CheckWinner(int[] board)
     {
            return _rules.WinCondition.CheckWinner(board);
        }

  /// <summary>
  /// Checks if the game is a draw using the draw strategy
        /// </summary>
        public bool IsBoardFull(int[] board)
      {
  return _rules.DrawCondition.IsDraw(board);
        }

        /// <summary>
    /// Randomly assigns symbols to two players using the assignment strategy
        /// </summary>
        public (string firstPlayerSymbol, string secondPlayerSymbol) AssignSymbols(Random rng)
        {
          return _rules.SymbolAssignment.AssignSymbols(rng);
 }

        /// <summary>
      /// Creates a new board using the board strategy
        /// </summary>
    public int[] CreateBoard()
 {
            return _rules.Board.CreateBoard();
 }

     /// <summary>
        /// Gets the board size from the current rules
        /// </summary>
        public int BoardSize => _rules.Board.BoardSize;

        /// <summary>
 /// Gets the game rules description
      /// </summary>
        public string RulesDescription => _rules.Description;

        /// <summary>
        /// Result of attempting a move validation
        /// </summary>
        public sealed class MoveValidationResult
        {
      public bool IsValid { get; init; }
            public bool IsGameOver { get; init; }
    public string? Winner { get; init; }
            public string? NextTurn { get; init; }
     public string? ErrorCode { get; init; }

      public static MoveValidationResult Success(string nextTurn) =>
  new() { IsValid = true, IsGameOver = false, NextTurn = nextTurn };

            public static MoveValidationResult GameWon(string winner) =>
       new() { IsValid = true, IsGameOver = true, Winner = winner };

       public static MoveValidationResult Draw() =>
         new() { IsValid = true, IsGameOver = true, Winner = null };

  public static MoveValidationResult InvalidIndex() =>
          new() { IsValid = false, ErrorCode = Models.ErrorCode.InvalidIndex };

            public static MoveValidationResult CellTaken() =>
     new() { IsValid = false, ErrorCode = Models.ErrorCode.CellTaken };
        }
    }
}
