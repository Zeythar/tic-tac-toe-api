using TicTacToeApi.Core.Utilities;

namespace TicTacToeApi.Models
{
    /// <summary>
    /// Represents a player in a game room
    /// </summary>
    public sealed class Player : IDisposable
    {
        public string PlayerId { get; }
        public string? ConnectionId { get; set; }
        public string? Symbol { get; set; }
        public bool GraceUsed { get; set; }

        // Cancellation token for reconnection grace period timer
        public CancellationTokenSource? ReconnectionTimeoutCts { get; set; }

        // Absolute expiry time for reconnection grace period (nullable)
        public DateTimeOffset? ReconnectionTimeoutExpiresAt { get; set; }

        // Cancellation token for per-turn timeout timer
        public CancellationTokenSource? TurnTimeoutCts { get; set; }

        // Remaining seconds when a turn timer is paused; null when not paused
        public int? RemainingTurnSeconds { get; set; }

        // Absolute expiry used by active turn timer to compute remaining ticks
        public DateTimeOffset? TurnTimeoutExpiresAt { get; set; }

        private bool _disposed = false;

        public Player(string playerId, string? connectionId)
        {
            PlayerId = playerId;
            ConnectionId = connectionId;
            Symbol = null;
            GraceUsed = false;
            ReconnectionTimeoutCts = null;
            ReconnectionTimeoutExpiresAt = null;
            TurnTimeoutCts = null;
            RemainingTurnSeconds = null;
            TurnTimeoutExpiresAt = null;
        }

        /// <summary>
        /// Checks if the player is currently connected
        /// </summary>
        public bool IsConnected => ConnectionId != null;

        /// <summary>
        /// Checks if the player has been assigned a symbol (game started)
        /// </summary>
        public bool HasSymbol => Symbol != null;

        /// <summary>
        /// Cancels and disposes all active cancellation tokens
        /// </summary>
        public void CancelAndDisposeTimers()
        {
            CancelAndDisposeTurnTimeout();
            CancelAndDisposeReconnectionTimeout();
        }

        /// <summary>
        /// Cancels and disposes the turn timeout token if active
        /// </summary>
        public void CancelAndDisposeTurnTimeout()
        {
            TurnTimeoutCts.SafeCancelAndDispose();
            TurnTimeoutCts = null;
            TurnTimeoutExpiresAt = null;
        }

        /// <summary>
        /// Cancels and disposes the reconnection timeout token if active
        /// </summary>
        public void CancelAndDisposeReconnectionTimeout()
        {
            ReconnectionTimeoutCts.SafeCancelAndDispose();
            ReconnectionTimeoutCts = null;
            ReconnectionTimeoutExpiresAt = null;
        }

        /// <summary>
        /// Disposes all resources held by this player
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                CancelAndDisposeTimers();
            }

            _disposed = true;
        }

        ~Player()
        {
            Dispose(false);
        }
    }
}
