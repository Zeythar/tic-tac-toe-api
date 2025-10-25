namespace TicTacToeApi.Core.Strategies
{
    /// <summary>
    /// Standard 3x3 Tic-Tac-Toe win condition strategy
    /// Checks for 3-in-a-row horizontally, vertically, or diagonally
    /// </summary>
    public sealed class StandardTicTacToeWinStrategy : IWinConditionStrategy
    {
        private static readonly int[][] WinLines =
        [
            [0, 1, 2], [3, 4, 5], [6, 7, 8],  // rows
            [0, 3, 6], [1, 4, 7], [2, 5, 8],  // columns
    [0, 4, 8], [2, 4, 6]    // diagonals
        ];

        public string Description => "Standard 3x3 Tic-Tac-Toe (3-in-a-row to win)";

        public string? CheckWinner(int[] board)
        {
            foreach (var line in WinLines)
            {
     int value = board[line[0]];
   if (value != 0 && value == board[line[1]] && value == board[line[2]])
     {
             return value == 1 ? "X" : "O";
  }
            }
        return null;
        }
    }

    /// <summary>
    /// Extended 4x4 win condition strategy
    /// Checks for 4-in-a-row (useful for larger boards)
    /// </summary>
 public sealed class FourInARowWinStrategy : IWinConditionStrategy
    {
        private readonly int _boardWidth;

     public string Description => $"4-in-a-row win condition on {_boardWidth}x{_boardWidth} board";

        public FourInARowWinStrategy(int boardWidth = 4)
  {
            _boardWidth = boardWidth;
        }

        public string? CheckWinner(int[] board)
   {
 int boardHeight = _boardWidth;

    // Check rows
  for (int row = 0; row < boardHeight; row++)
        {
     for (int col = 0; col <= _boardWidth - 4; col++)
                {
               int index = row * _boardWidth + col;
    int value = board[index];
           if (value != 0 && 
       value == board[index + 1] && 
    value == board[index + 2] && 
         value == board[index + 3])
         {
 return value == 1 ? "X" : "O";
           }
    }
          }

            // Check columns
      for (int col = 0; col < _boardWidth; col++)
 {
      for (int row = 0; row <= boardHeight - 4; row++)
     {
       int index = row * _boardWidth + col;
  int value = board[index];
              if (value != 0 && 
  value == board[index + _boardWidth] && 
            value == board[index + 2 * _boardWidth] && 
            value == board[index + 3 * _boardWidth])
{
            return value == 1 ? "X" : "O";
          }
   }
   }

   // Check diagonals (down-right)
       for (int row = 0; row <= boardHeight - 4; row++)
{
     for (int col = 0; col <= _boardWidth - 4; col++)
     {
        int index = row * _boardWidth + col;
 int value = board[index];
        if (value != 0 && 
value == board[index + _boardWidth + 1] && 
         value == board[index + 2 * (_boardWidth + 1)] && 
        value == board[index + 3 * (_boardWidth + 1)])
   {
      return value == 1 ? "X" : "O";
             }
         }
   }

            // Check diagonals (down-left)
            for (int row = 0; row <= boardHeight - 4; row++)
            {
        for (int col = 3; col < _boardWidth; col++)
       {
              int index = row * _boardWidth + col;
        int value = board[index];
            if (value != 0 && 
            value == board[index + _boardWidth - 1] && 
    value == board[index + 2 * (_boardWidth - 1)] && 
      value == board[index + 3 * (_boardWidth - 1)])
              {
               return value == 1 ? "X" : "O";
          }
   }
       }

  return null;
        }
    }

    /// <summary>
    /// Standard draw condition strategy - board is full with no winner
  /// </summary>
    public sealed class StandardDrawConditionStrategy : IDrawConditionStrategy
 {
        public string Description => "Draw when board is full with no winner";

        public bool IsDraw(int[] board)
        {
 foreach (var cell in board)
         {
       if (cell == 0)
       return false;
            }
       return true;
        }
    }

    /// <summary>
    /// Random symbol assignment strategy - 50/50 chance for X or O
/// </summary>
    public sealed class RandomSymbolAssignmentStrategy : ISymbolAssignmentStrategy
    {
        public string Description => "Random assignment (50% chance for each player to get X)";

   public (string firstPlayerSymbol, string secondPlayerSymbol) AssignSymbols(Random rng)
        {
            bool firstPlayerGetsX = rng.Next(2) == 0;
     return firstPlayerGetsX ? ("X", "O") : ("O", "X");
   }
    }

    /// <summary>
    /// First player always gets X strategy
    /// </summary>
  public sealed class FirstPlayerXAssignmentStrategy : ISymbolAssignmentStrategy
    {
      public string Description => "First player always gets X";

        public (string firstPlayerSymbol, string secondPlayerSymbol) AssignSymbols(Random rng)
    {
 return ("X", "O");
    }
    }

    /// <summary>
    /// Standard 3x3 board strategy
    /// </summary>
    public sealed class StandardBoardStrategy : IBoardStrategy
    {
     public int BoardSize => 9;
        public int Width => 3;
    public int Height => 3;
        public string Description => "Standard 3x3 Tic-Tac-Toe board";

    public int[] CreateBoard()
        {
     return new int[9];
        }

        public bool IsValidIndex(int index)
 {
   return index >= 0 && index < 9;
        }
    }

    /// <summary>
    /// 4x4 board strategy for larger games
 /// </summary>
public sealed class FourByFourBoardStrategy : IBoardStrategy
    {
        public int BoardSize => 16;
        public int Width => 4;
        public int Height => 4;
public string Description => "4x4 board for extended gameplay";

        public int[] CreateBoard()
    {
            return new int[16];
        }

        public bool IsValidIndex(int index)
        {
         return index >= 0 && index < 16;
        }
    }

    /// <summary>
    /// 5x5 board strategy
    /// </summary>
    public sealed class FiveByFiveBoardStrategy : IBoardStrategy
    {
        public int BoardSize => 25;
        public int Width => 5;
  public int Height => 5;
        public string Description => "5x5 board for large-scale gameplay";

        public int[] CreateBoard()
        {
return new int[25];
        }

        public bool IsValidIndex(int index)
        {
            return index >= 0 && index < 25;
        }
    }
}
