namespace TicTacToeApi.Interfaces
{
    /// <summary>
    /// Interface for managing player disconnection grace periods
    /// </summary>
    public interface IReconnectionService
    {
        /// <summary>
        /// Starts a grace period timer for a disconnected player
        /// </summary>
        Task StartGracePeriodAsync(string roomCode, string playerId);
    }
}
