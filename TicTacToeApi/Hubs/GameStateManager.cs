using TicTacToeApi.Core.StateMachine;
using TicTacToeApi.Models;

namespace TicTacToeApi.Hubs
{
    /// <summary>
    /// Helper class for managing game state transitions in the hub
    /// Encapsulates state machine logic and provides semantic methods
 /// </summary>
    public sealed class GameStateManager
    {
        private readonly Room _room;
        private readonly ILogger<GameStateMachine> _logger;

  public GameStateManager(Room room, ILogger<GameStateMachine> logger)
        {
    _room = room ?? throw new ArgumentNullException(nameof(room));
  _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current game state
 /// </summary>
  public GameState CurrentState => _room.GetGameState(_logger);

        /// <summary>
        /// Notifies the state machine that a second player joined
        /// </summary>
  public bool NotifyPlayerJoined()
 {
     return _room.TryTransition(GameStateEvent.PlayerJoined, _logger);
        }

     /// <summary>
        /// Notifies the state machine that a move was made
        /// </summary>
 public bool NotifyMoveMade(bool isFirstMove = false)
{
     var evt = isFirstMove ? GameStateEvent.FirstMoveMade : GameStateEvent.MoveMade;
         return _room.TryTransition(evt, _logger);
   }

      /// <summary>
        /// Notifies the state machine that the game was won
    /// </summary>
 public bool NotifyGameWon()
        {
        return _room.TryTransition(GameStateEvent.GameWon, _logger);
     }

        /// <summary>
     /// Notifies the state machine that the game ended in a draw
        /// </summary>
 public bool NotifyGameDrawn()
        {
 return _room.TryTransition(GameStateEvent.GameDrawn, _logger);
        }

  /// <summary>
        /// Notifies the state machine that a player forfeited
        /// </summary>
        public bool NotifyPlayerForfeited()
    {
   return _room.TryTransition(GameStateEvent.PlayerForfeited, _logger);
      }

        /// <summary>
    /// Notifies the state machine that a rematch was offered
   /// </summary>
  public bool NotifyRematchOffered()
     {
   return _room.TryTransition(GameStateEvent.RematchOffered, _logger);
        }

        /// <summary>
        /// Notifies the state machine that rematch was accepted
        /// </summary>
   public bool NotifyRematchAccepted()
   {
   return _room.TryTransition(GameStateEvent.RematchAccepted, _logger);
        }

  /// <summary>
     /// Notifies the state machine that rematch window expired
        /// </summary>
        public bool NotifyRematchExpired()
   {
             return _room.TryTransition(GameStateEvent.RematchExpired, _logger);
        }

        /// <summary>
  /// Notifies the state machine that player disconnected
        /// </summary>
  public bool NotifyPlayerDisconnected()
     {
   return _room.TryTransition(GameStateEvent.PlayerDisconnected, _logger);
        }

        /// <summary>
  /// Notifies the state machine that player reconnected
        /// </summary>
 public bool NotifyPlayerReconnected()
        {
 return _room.TryTransition(GameStateEvent.PlayerReconnected, _logger);
      }

        /// <summary>
        /// Notifies the state machine that room is being closed
     /// </summary>
   public bool NotifyRoomClosed()
        {
   var result = _room.TryTransition(GameStateEvent.RoomClosed, _logger);
         if (result)
     {
    _room.RemoveStateMachine();
         }
 return result;
        }

    /// <summary>
     /// Checks if a move can be made in the current state
        /// </summary>
      public bool CanMakeMove()
        {
    return CurrentState == GameState.Active;
        }

     /// <summary>
    /// Checks if game can be started (all players joined)
      /// </summary>
    public bool CanStartGame()
        {
         return CurrentState == GameState.WaitingForPlayers;
        }

    /// <summary>
     /// Checks if rematch can be offered
        /// </summary>
        public bool CanOfferRematch()
       {
   return CurrentState == GameState.GameOver;
        }

 /// <summary>
   /// Checks if rematch can be accepted
     /// </summary>
     public bool CanAcceptRematch()
{
    return CurrentState == GameState.RematchOffered;
     }

 /// <summary>
  /// Gets a description of the current state for logging
    /// </summary>
        public string GetStateDescription()
       {
      return _room.GetStateDescription(_logger);
        }

   /// <summary>
        /// Gets all valid operations in the current state
        /// </summary>
        public IEnumerable<GameStateEvent> GetValidOperations()
        {
     return _room.GetValidTransitions(_logger);
       }
    }
}
