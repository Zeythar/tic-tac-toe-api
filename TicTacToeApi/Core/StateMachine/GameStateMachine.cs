namespace TicTacToeApi.Core.StateMachine
{
    /// <summary>
    /// Represents all possible game states in Tic-Tac-Toe
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Game has been created but not started (waiting for second player)
        /// </summary>
        WaitingForPlayers,

        /// <summary>
        /// Both players present, symbols assigned, game has started
        /// </summary>
   Active,

  /// <summary>
        /// Game is over - someone won or draw occurred
        /// </summary>
        GameOver,

        /// <summary>
        /// Game over, rematch window is open
        /// </summary>
        RematchOffered,

        /// <summary>
  /// Game over, rematch accepted and ready to start
        /// </summary>
    RematchAccepted,

  /// <summary>
        /// Rematch window expired without acceptance
        /// </summary>
        RematchExpired,

        /// <summary>
        /// Room has been closed and cleaned up
        /// </summary>
        Closed
    }

    /// <summary>
    /// Represents game state transition events that trigger state changes
    /// </summary>
    public enum GameStateEvent
    {
        /// <summary>
/// Second player joined the room
        /// </summary>
        PlayerJoined,

        /// <summary>
        /// First move made (game started transitioning from waiting)
        /// </summary>
        FirstMoveMade,

        /// <summary>
        /// Move was made during active game
        /// </summary>
        MoveMade,

  /// <summary>
        /// Game ended in a win
  /// </summary>
        GameWon,

        /// <summary>
      /// Game ended in a draw
        /// </summary>
        GameDrawn,

        /// <summary>
 /// Player forfeited
  /// </summary>
     PlayerForfeited,

        /// <summary>
      /// Rematch offer initiated
     /// </summary>
        RematchOffered,

        /// <summary>
        /// Both players accepted rematch
      /// </summary>
 RematchAccepted,

      /// <summary>
        /// Rematch window expired without both accepting
    /// </summary>
        RematchExpired,

        /// <summary>
    /// Room is being closed/cleaned up
        /// </summary>
        RoomClosed,

        /// <summary>
 /// Player disconnected
   /// </summary>
    PlayerDisconnected,

  /// <summary>
     /// Player reconnected
        /// </summary>
        PlayerReconnected
    }

    /// <summary>
    /// State machine for managing game state transitions
    /// Ensures only valid state transitions occur
    /// </summary>
    public sealed class GameStateMachine
    {
     private GameState _currentState;
private readonly Dictionary<(GameState state, GameStateEvent evt), GameState> _transitionMap;
        private readonly ILogger<GameStateMachine> _logger;

        public GameState CurrentState => _currentState;

        public GameStateMachine(ILogger<GameStateMachine> logger)
        {
  _logger = logger ?? throw new ArgumentNullException(nameof(logger));
       _currentState = GameState.WaitingForPlayers;
            _transitionMap = BuildTransitionMap();
 }

        /// <summary>
        /// Attempts to transition to a new state based on an event
        /// </summary>
        /// <returns>True if transition was successful, false if invalid transition</returns>
        public bool TryTransition(GameStateEvent evt, out GameState newState)
    {
       var key = (_currentState, evt);

            if (_transitionMap.TryGetValue(key, out var nextState))
      {
          _logger.LogInformation(
          "Game state transition: {CurrentState} + {Event} -> {NextState}",
  _currentState, evt, nextState);

    _currentState = nextState;
                newState = nextState;
         return true;
    }

            _logger.LogWarning(
       "Invalid game state transition attempted: {CurrentState} + {Event}",
_currentState, evt);

            newState = _currentState;
            return false;
      }

        /// <summary>
     /// Checks if a transition is valid without actually performing it
      /// </summary>
        public bool CanTransition(GameStateEvent evt)
        {
       return _transitionMap.ContainsKey((_currentState, evt));
        }

    /// <summary>
    /// Gets a description of the current state
        /// </summary>
    public string GetStateDescription() => _currentState switch
        {
            GameState.WaitingForPlayers => "Waiting for second player to join",
     GameState.Active => "Game is active - players can make moves",
       GameState.GameOver => "Game has ended - waiting for rematch decision",
        GameState.RematchOffered => "Rematch has been offered - waiting for acceptance",
     GameState.RematchAccepted => "Both players accepted rematch - game resetting",
            GameState.RematchExpired => "Rematch window has expired",
GameState.Closed => "Room has been closed and cleaned up",
      _ => $"Unknown state: {_currentState}"
        };

        /// <summary>
        /// Gets all valid transitions from the current state
        /// Useful for debugging and understanding what operations are allowed
        /// </summary>
        public IEnumerable<GameStateEvent> GetValidTransitions()
        {
     return _transitionMap
                .Keys
         .Where(k => k.state == _currentState)
         .Select(k => k.evt);
        }

        /// <summary>
  /// Builds the state transition map that defines all valid transitions
        /// </summary>
        private Dictionary<(GameState, GameStateEvent), GameState> BuildTransitionMap()
        {
            var map = new Dictionary<(GameState, GameStateEvent), GameState>
            {
    // WaitingForPlayers transitions
    { (GameState.WaitingForPlayers, GameStateEvent.PlayerJoined), GameState.Active },
         { (GameState.WaitingForPlayers, GameStateEvent.RoomClosed), GameState.Closed },

      // Active transitions
  { (GameState.Active, GameStateEvent.MoveMade), GameState.Active },
          { (GameState.Active, GameStateEvent.GameWon), GameState.GameOver },
     { (GameState.Active, GameStateEvent.GameDrawn), GameState.GameOver },
           { (GameState.Active, GameStateEvent.PlayerForfeited), GameState.GameOver },
    { (GameState.Active, GameStateEvent.PlayerDisconnected), GameState.Active },
           { (GameState.Active, GameStateEvent.RoomClosed), GameState.Closed },

            // GameOver transitions
       { (GameState.GameOver, GameStateEvent.RematchOffered), GameState.RematchOffered },
            { (GameState.GameOver, GameStateEvent.RoomClosed), GameState.Closed },

     // RematchOffered transitions
              { (GameState.RematchOffered, GameStateEvent.RematchAccepted), GameState.RematchAccepted },
        { (GameState.RematchOffered, GameStateEvent.RematchExpired), GameState.RematchExpired },
    { (GameState.RematchOffered, GameStateEvent.RoomClosed), GameState.Closed },

      // RematchAccepted transitions
    { (GameState.RematchAccepted, GameStateEvent.FirstMoveMade), GameState.Active },
        { (GameState.RematchAccepted, GameStateEvent.RoomClosed), GameState.Closed },

       // RematchExpired transitions
                { (GameState.RematchExpired, GameStateEvent.RoomClosed), GameState.Closed },

 // All states can transition to Closed
        { (GameState.Closed, GameStateEvent.RoomClosed), GameState.Closed }
    };

         return map;
   }
    }
}
