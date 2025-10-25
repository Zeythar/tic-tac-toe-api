namespace TicTacToeApi.Core.Strategies
{
    /// <summary>
    /// Configuration class that holds all game rule strategies
    /// Can be swapped out to support different game variants
    /// </summary>
    public sealed class GameRulesConfiguration
    {
        public IWinConditionStrategy WinCondition { get; }
        public IDrawConditionStrategy DrawCondition { get; }
   public ISymbolAssignmentStrategy SymbolAssignment { get; }
   public IBoardStrategy Board { get; }

        public string Description => $"Game Rules: {Board.Description}, {WinCondition.Description}";

public GameRulesConfiguration(
            IWinConditionStrategy winCondition,
  IDrawConditionStrategy drawCondition,
  ISymbolAssignmentStrategy symbolAssignment,
            IBoardStrategy board)
      {
            WinCondition = winCondition ?? throw new ArgumentNullException(nameof(winCondition));
    DrawCondition = drawCondition ?? throw new ArgumentNullException(nameof(drawCondition));
        SymbolAssignment = symbolAssignment ?? throw new ArgumentNullException(nameof(symbolAssignment));
         Board = board ?? throw new ArgumentNullException(nameof(board));
    }

   /// <summary>
     /// Creates the standard Tic-Tac-Toe 3x3 game rules
        /// </summary>
        public static GameRulesConfiguration CreateStandardTicTacToe()
        {
   return new GameRulesConfiguration(
       winCondition: new StandardTicTacToeWinStrategy(),
          drawCondition: new StandardDrawConditionStrategy(),
        symbolAssignment: new RandomSymbolAssignmentStrategy(),
 board: new StandardBoardStrategy()
   );
   }

        /// <summary>
     /// Allows custom game rules configuration
/// Useful for testing and creating game variants
  /// </summary>
        public static GameRulesConfiguration CreateCustom(
IWinConditionStrategy? winCondition = null,
  IDrawConditionStrategy? drawCondition = null,
     ISymbolAssignmentStrategy? symbolAssignment = null,
            IBoardStrategy? board = null)
        {
  return new GameRulesConfiguration(
        winCondition: winCondition ?? new StandardTicTacToeWinStrategy(),
          drawCondition: drawCondition ?? new StandardDrawConditionStrategy(),
symbolAssignment: symbolAssignment ?? new RandomSymbolAssignmentStrategy(),
        board: board ?? new StandardBoardStrategy()
         );
        }
    }
}
