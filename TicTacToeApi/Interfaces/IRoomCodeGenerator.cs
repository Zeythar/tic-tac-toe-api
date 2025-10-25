namespace TicTacToeApi.Interfaces
{
    /// <summary>
    /// Interface for generating unique room codes
    /// </summary>
    public interface IRoomCodeGenerator
    {
        /// <summary>
        /// Generates a unique room code
        /// </summary>
        string GenerateCode();
    }
}
