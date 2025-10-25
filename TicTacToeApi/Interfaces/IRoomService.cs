using TicTacToeApi.Models;

namespace TicTacToeApi.Interfaces
{
    /// <summary>
    /// Interface for managing game rooms
    /// </summary>
    public interface IRoomService
    {
        /// <summary>
        /// Creates a new game room with a unique code
        /// </summary>
        Room CreateRoom();

        /// <summary>
        /// Attempts to retrieve a room by its code
        /// </summary>
        bool TryGetRoom(string code, out Room room);

        /// <summary>
        /// Gets all active rooms
        /// </summary>
        IEnumerable<Room> GetAllRooms();

        /// <summary>
        /// Handles player disconnection and initiates grace period
        /// </summary>
        Task HandleDisconnectAsync(string connectionId);

        /// <summary>
        /// Attempts to reconnect a player to their room
        /// </summary>
        Task<bool> ReconnectAsync(string code, string playerId, string connectionId);
        /// <summary>
        /// Removes a room from in-memory storage. Returns true when removed.
        /// </summary>
        bool TryRemoveRoom(string code);

        /// <summary>
        /// Starts the per-turn timeout countdown for the current player in the specified room.
        /// </summary>
        Task StartTurnTimeoutAsync(string code);

        /// <summary>
        /// Cancels any active per-turn timeout for the specified room.
        /// </summary>
        void CancelTurnTimeout(string code);

        // Rematch API
        /// <summary>
        /// Offers a rematch to the opponent player
        /// </summary>
        bool OfferRematch(string code, string playerId, out DateTimeOffset? expiresAt);

        /// <summary>
        /// Accepts a rematch offer from the opponent player
        /// Indicates whether the rematch actually started
        /// </summary>
        Task<bool> AcceptRematch(string code, string playerId);

        /// <summary>
        /// Starts the rematch window for the specified room
        /// </summary>
        void StartRematchWindow(string code);
    }
}
