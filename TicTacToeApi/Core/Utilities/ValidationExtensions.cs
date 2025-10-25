namespace TicTacToeApi.Core.Utilities
{
    /// <summary>
    /// Extension methods for common validation patterns
    /// Provides DRY helpers to reduce duplicate validation checks throughout the codebase
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Checks if a string is null or empty (convenience wrapper for null-coalescing patterns)
        /// Used as a common validation point across repositories and services
        /// </summary>
 /// <param name="value">The string to validate</param>
        /// <returns>True if the string is null or empty, false otherwise</returns>
     public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

        /// <summary>
        /// Checks if a string is null, empty, or contains only whitespace
        /// More comprehensive than IsNullOrEmpty
        /// </summary>
      /// <param name="value">The string to validate</param>
        /// <returns>True if the string is null, empty, or whitespace, false otherwise</returns>
        public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

        /// <summary>
  /// Throws ArgumentException if the string is null or empty
   /// Useful for parameter validation in a fluent style
        /// </summary>
   /// <param name="value">The string to validate</param>
        /// <param name="paramName">Name of the parameter for the exception</param>
        /// <exception cref="ArgumentException">Thrown if value is null or empty</exception>
        /// <returns>The original value for method chaining</returns>
        public static string ThrowIfNullOrEmpty(this string? value, string paramName)
        {
       if (value.IsNullOrEmpty())
     throw new ArgumentException("Value cannot be null or empty.", paramName);

            return value;
        }
    }
}
