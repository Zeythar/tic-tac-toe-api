using TicTacToeApi.Core.StateMachine;

namespace TicTacToeApi.Models
{
    /// <summary>
    /// Extension methods for Room to integrate state machine functionality
    /// </summary>
    public static class RoomStateMachineExtensions
    {
        private static readonly Dictionary<Room, GameStateMachine> StateMachines = new();
        private static readonly object StateMachineLock = new();

  /// <summary>
        /// Gets or creates the state machine for a room
        /// </summary>
        public static GameStateMachine GetStateMachine(this Room room, ILogger<GameStateMachine> logger)
        {
            lock (StateMachineLock)
          {
          if (!StateMachines.TryGetValue(room, out var stateMachine))
     {
           stateMachine = new GameStateMachine(logger);
    StateMachines[room] = stateMachine;
            }
                return stateMachine;
          }
        }

        /// <summary>
   /// Gets the current state of the room
        /// </summary>
        public static GameState GetGameState(this Room room, ILogger<GameStateMachine> logger)
        {
      var stateMachine = room.GetStateMachine(logger);
        return stateMachine.CurrentState;
        }

        /// <summary>
        /// Attempts a state transition based on a game event
        /// </summary>
  public static bool TryTransition(this Room room, GameStateEvent evt, ILogger<GameStateMachine> logger)
        {
           var stateMachine = room.GetStateMachine(logger);
            return stateMachine.TryTransition(evt, out _);
        }

        /// <summary>
        /// Checks if a transition is valid
        /// </summary>
      public static bool CanTransition(this Room room, GameStateEvent evt, ILogger<GameStateMachine> logger)
        {
       var stateMachine = room.GetStateMachine(logger);
      return stateMachine.CanTransition(evt);
        }

        /// <summary>
        /// Gets the current state description
     /// </summary>
        public static string GetStateDescription(this Room room, ILogger<GameStateMachine> logger)
        {
   var stateMachine = room.GetStateMachine(logger);
  return stateMachine.GetStateDescription();
        }

        /// <summary>
   /// Gets all valid transitions from current state
        /// </summary>
  public static IEnumerable<GameStateEvent> GetValidTransitions(this Room room, ILogger<GameStateMachine> logger)
        {
   var stateMachine = room.GetStateMachine(logger);
         return stateMachine.GetValidTransitions();
        }

        /// <summary>
   /// Removes state machine for a room (cleanup)
     /// </summary>
   public static void RemoveStateMachine(this Room room)
        {
  lock (StateMachineLock)
      {
  StateMachines.Remove(room);
             }
 }
    }
}
