namespace TicTacToeApi.Configuration
{
    /// <summary>
    /// Configuration settings for the Tic-Tac-Toe game
    /// </summary>
    public sealed class GameSettings
    {
        /// <summary>
        /// Length of the room code (default: 6 characters)
        /// </summary>
        public int RoomCodeLength { get; set; } = 6;

        /// <summary>
        /// Grace period in seconds before a disconnected player forfeits (default: 30)
        /// </summary>
        public int ReconnectionGracePeriodSeconds { get; set; } = 30;

        /// <summary>
        /// Per-turn timeout in seconds before a player loses the game (default: 30)
        /// </summary>
        public int TurnTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Rematch offer window in seconds after a game ends before room is removed (default: 30)
        /// </summary>
        public int RematchWindowSeconds { get; set; } = 30;

        /// <summary>
        /// Characters used for generating room codes (ambiguous characters removed)
        /// </summary>
        public string RoomCodeAlphabet { get; set; } = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        /// <summary>
        /// Maximum number of players per room
        /// </summary>
        public int MaxPlayersPerRoom { get; set; } = 2;

        /// <summary>
        /// Board size (default: 9 for 3x3 grid)
        /// </summary>
        public int BoardSize { get; set; } = 9;

        /// <summary>
        /// Time in seconds after which an idle room (e.g., single-player waiting) will be removed (default: 300s = 5m)
        /// </summary>
        public int IdleRoomTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Interval in seconds between sweeper runs (default: 60s)
        /// </summary>
        public int RoomSweepIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// Cache timeout in hours for individual room entries (default: 1 hour)
        /// </summary>
        public int RoomCacheTimeoutHours { get; set; } = 1;

        /// <summary>
        /// Cache timeout in minutes for the "all rooms" collection cache (default: 5 minutes)
        /// </summary>
        public int AllRoomsCacheTimeoutMinutes { get; set; } = 5;

        /// <summary>
        /// Gets the room cache timeout as a TimeSpan
        /// </summary>
        /// <returns>TimeSpan configured for room cache duration</returns>
        public TimeSpan GetRoomCacheTimeout() => TimeSpan.FromHours(RoomCacheTimeoutHours);

        /// <summary>
        /// Gets the all-rooms cache timeout as a TimeSpan
        /// </summary>
        /// <returns>TimeSpan configured for all-rooms cache duration</returns>
        public TimeSpan GetAllRoomsCacheTimeout() => TimeSpan.FromMinutes(AllRoomsCacheTimeoutMinutes);
    }
}
