using TicTacToeApi.Models;

namespace TicTacToeApi.Core.Utilities
{
    /// <summary>
    /// Extension methods for safe and consistent resource disposal
    /// Provides DRY helpers for cancellation token cleanup and other disposable resources
    /// </summary>
    public static class DisposalExtensions
    {
        /// <summary>
        /// Safely disposes a CancellationTokenSource without throwing exceptions
        /// Handles null references and already-disposed tokens gracefully
        /// </summary>
        /// <param name="cts">The CancellationTokenSource to dispose</param>
        /// <returns>True if disposal was successful, false if already disposed or null</returns>
        public static bool SafeDispose(this CancellationTokenSource? cts)
        {
            if (cts == null)
                return false;

            try
            {
                cts.Dispose();
                return true;
            }
            catch (ObjectDisposedException)
            {
                // Already disposed
                return false;
            }
            catch (Exception)
            {
                // Other disposal errors - log but don't throw
                return false;
            }
        }

        /// <summary>
        /// Safely cancels and disposes a CancellationTokenSource
        /// Attempts cancellation first, then disposal, ignoring any exceptions
        /// </summary>
        /// <param name="cts">The CancellationTokenSource to cancel and dispose</param>
        /// <returns>True if both cancel and dispose succeeded, false otherwise</returns>
        public static bool SafeCancelAndDispose(this CancellationTokenSource? cts)
        {
            if (cts == null)
                return false;

            var cancelSucceeded = cts.SafeCancel();
            var disposeSucceeded = cts.SafeDispose();

            return cancelSucceeded && disposeSucceeded;
        }

        /// <summary>
        /// Safely cancels a CancellationTokenSource without throwing exceptions
        /// </summary>
        /// <param name="cts">The CancellationTokenSource to cancel</param>
        /// <returns>True if cancellation was successful, false otherwise</returns>
        public static bool SafeCancel(this CancellationTokenSource? cts)
        {
            if (cts == null)
                return false;

            try
            {
                cts.Cancel();
                return true;
            }
            catch (ObjectDisposedException)
            {
                // Already disposed
                return false;
            }
            catch (OperationCanceledException)
            {
                // Already cancelled
                return true;
            }
            catch (Exception)
            {
                // Other cancellation errors - ignore
                return false;
            }
        }

        /// <summary>
        /// Disposes multiple disposable resources safely
        /// Collects all exceptions and logs them, ensuring all resources are attempted to dispose
        /// </summary>
        /// <param name="disposables">Collection of disposable resources</param>
        /// <param name="logger">Optional logger for disposal errors</param>
        /// <exception cref="AggregateException">If any disposal fails and exceptions are collected</exception>
        public static void SafeDisposeAll(this IEnumerable<IDisposable?> disposables, ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(disposables);

            var exceptions = new List<Exception>();

            foreach (var disposable in disposables)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    logger?.LogWarning(ex, "Error disposing resource");
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException("One or more resources failed to dispose", exceptions);
            }
        }

        /// <summary>
        /// Safely disposes a disposable resource and nulls the reference
        /// Useful for ensuring resources are properly cleaned up in complex scenarios
        /// </summary>
        /// <typeparam name="T">Type implementing IDisposable</typeparam>
        /// <param name="disposable">The resource to dispose</param>
        /// <returns>Null reference for chaining assignment</returns>
        public static T? SafeDisposeAndNull<T>(this T? disposable) where T : class, IDisposable
        {
            try
            {
                disposable?.Dispose();
            }
            catch (Exception)
            {
                // Suppress exceptions during cleanup
            }

            return null;
        }

        /// <summary>
        /// Executes an action within a try-catch, logging and suppressing exceptions
        /// Useful for cleanup operations that shouldn't fail the calling operation
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="errorMessage">Message to log if exception occurs</param>
        /// <param name="logger">Logger for error messages</param>
        public static void SafeExecute(Action action, string? errorMessage = null, ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                if (logger != null && errorMessage != null)
                {
                    logger.LogWarning(ex, errorMessage);
                }
            }
        }

        /// <summary>
        /// Executes an action asynchronously within a try-catch, logging and suppressing exceptions
        /// </summary>
        /// <param name="action">The async action to execute</param>
        /// <param name="errorMessage">Message to log if exception occurs</param>
        /// <param name="logger">Logger for error messages</param>
        public static async Task SafeExecuteAsync(Func<Task> action, string? errorMessage = null, ILogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            try
            {
                await action.Invoke();
            }
            catch (Exception ex)
            {
                if (logger != null && errorMessage != null)
                {
                    logger.LogWarning(ex, errorMessage);
                }
            }
        }
    }

    /// <summary>
    /// Helper class for managing cancellation token cleanup patterns
    /// Provides utilities for safe cancellation token handling with logging support
    /// </summary>
    public static class CancellationTokenCleanupHelper
    {
        /// <summary>
        /// Disposes a cancellation token and sets it to null atomically
        /// Returns the computed remaining time if available
        /// </summary>
        /// <param name="cts">The CancellationTokenSource to dispose</param>
        /// <param name="expiresAt">Optional expiry time to compute remaining seconds</param>
        /// <returns>Remaining seconds if expiresAt provided, null otherwise</returns>
        public static int? DisposeCtsAndGetRemaining(this CancellationTokenSource? cts, DateTimeOffset? expiresAt = null)
        {
            cts.SafeCancelAndDispose();

            if (expiresAt == null)
                return null;

            var remaining = (int)Math.Ceiling((expiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds);
            return Math.Max(0, remaining);
        }

        /// <summary>
        /// Batches cleanup of multiple cancellation tokens
        /// Used for cleaning up turn timeout and reconnection timeout tokens together
        /// </summary>
        public struct CancellationTokenBatch
        {
            public CancellationTokenSource? TurnTimeoutCts { get; set; }
            public CancellationTokenSource? ReconnectionCts { get; set; }
            public DateTimeOffset? TurnExpiresAt { get; set; }
            public DateTimeOffset? ReconnectionExpiresAt { get; set; }

            /// <summary>
            /// Disposes all tokens in the batch
            /// </summary>
            public void DisposeAll()
            {
                TurnTimeoutCts.SafeCancelAndDispose();
                ReconnectionCts.SafeCancelAndDispose();
                TurnExpiresAt = null;
                ReconnectionExpiresAt = null;
            }

            /// <summary>
            /// Gets remaining time for turn timeout
            /// </summary>
            public int? GetTurnRemaining()
            {
                if (TurnExpiresAt == null)
                    return null;

                var remaining = (int)Math.Ceiling((TurnExpiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds);
                return Math.Max(0, remaining);
            }
        }
    }

    /// <summary>
    /// Helper class for managing resource disposal patterns in game operations
    /// Provides semantic methods for cleanup operations common in game logic
    /// </summary>
    public static class GameResourceCleanupHelper
    {
        /// <summary>
        /// Cleans up all timers for a player
        /// Disposes both turn timeout and reconnection timeout tokens
        /// </summary>
        /// <param name="player">The player whose timers to clean up</param>
        /// <returns>Remaining turn seconds if paused, null otherwise</returns>
        public static int? CleanupPlayerTimers(this Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            var turnRemaining = player.TurnTimeoutCts.DisposeCtsAndGetRemaining(player.TurnTimeoutExpiresAt);
            player.TurnTimeoutCts = null;
            player.TurnTimeoutExpiresAt = null;

            player.ReconnectionTimeoutCts.SafeCancelAndDispose();
            player.ReconnectionTimeoutCts = null;
            player.ReconnectionTimeoutExpiresAt = null;

            return turnRemaining;
        }

        /// <summary>
        /// Cleans up all player timers in a room
        /// </summary>
        /// <param name="room">The room whose players' timers to clean up</param>
        public static void CleanupAllPlayerTimers(this Room room)
        {
            ArgumentNullException.ThrowIfNull(room);

            foreach (var player in room.Players.Values)
            {
                player.CleanupPlayerTimers();
            }
        }

        /// <summary>
        /// Safely resets a turn timeout state for a player
        /// Disposes existing token and resets expiry times
        /// </summary>
        /// <param name="player">The player to reset</param>
        public static void ResetTurnTimeout(this Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            player.TurnTimeoutCts.SafeCancelAndDispose();
            player.TurnTimeoutCts = null;
            player.TurnTimeoutExpiresAt = null;
            player.RemainingTurnSeconds = null;
        }

        /// <summary>
        /// Safely resets a reconnection timeout state for a player
        /// </summary>
        /// <param name="player">The player to reset</param>
        public static void ResetReconnectionTimeout(this Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            player.ReconnectionTimeoutCts.SafeCancelAndDispose();
            player.ReconnectionTimeoutCts = null;
            player.ReconnectionTimeoutExpiresAt = null;
        }

        /// <summary>
        /// Computes remaining turn seconds and preserves for later resume
        /// Used when a turn timer is paused due to disconnection
        /// </summary>
        /// <param name="player">The player whose remaining time to compute</param>
        /// <returns>Remaining seconds, or null if not applicable</returns>
        public static int? ComputeAndPreserveRemainingTurnTime(this Player player)
        {
            ArgumentNullException.ThrowIfNull(player);

            if (player.TurnTimeoutExpiresAt == null)
                return null;

            var remaining = (int)Math.Ceiling((player.TurnTimeoutExpiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds);
            var remainingClamped = Math.Max(0, remaining);

            player.RemainingTurnSeconds = remainingClamped;
            player.TurnTimeoutExpiresAt = null;

            return remainingClamped;
        }

        /// <summary>
        /// Safely disposes all resources for a room during cleanup
        /// Intended for use when a room is being removed
        /// </summary>
        /// <param name="room">The room to clean up</param>
        public static void DisposeAllResources(this Room room)
        {
            ArgumentNullException.ThrowIfNull(room);

            // Clean up all players' resources
            room.CleanupAllPlayerTimers();

            // Reset game state
            room.RematchOffers.Clear();
            room.RematchExpiresAt = null;
        }
    }
}
