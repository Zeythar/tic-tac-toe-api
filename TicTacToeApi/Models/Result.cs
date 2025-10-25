namespace TicTacToeApi.Models
{
    /// <summary>
    /// Unified result type using discriminated union pattern.
    /// Represents the outcome of an operation with either success or failure.
    /// </summary>
    /// <typeparam name="T">The type of payload returned on success</typeparam>
  public abstract record Result<T>
    {
    /// <summary>
      /// Successful result with payload
        /// </summary>
        public sealed record Success(
            T Payload,
    object? Details = null,
            string? CorrelationId = null) : Result<T>;

        /// <summary>
 /// Failed result with error details
        /// </summary>
        public sealed record Failure(
    string ErrorCode,
            string ErrorMessage,
 object? Details = null,
            string? CorrelationId = null) : Result<T>;

     /// <summary>
        /// Factory method to create a successful result
        /// </summary>
        public static Result<T> Ok(
    T payload,
       string? correlationId = null,
            object? details = null) =>
            new Success(payload, details, correlationId);

        /// <summary>
        /// Factory method to create a failed result
   /// </summary>
        public static Result<T> Fail(
     string errorCode,
            string errorMessage,
        string? correlationId = null,
     object? details = null) =>
      new Failure(errorCode, errorMessage, details, correlationId);
    }

    /// <summary>
    /// HTTP API response wrapper that can include server metadata
    /// </summary>
    public sealed record ApiResult<T>(
        bool Success,
        T? Payload = default,
        string? ErrorCode = null,
        string? ErrorMessage = null,
        object? Details = null,
      string? CorrelationId = null,
string? ServerTimestamp = null)
    {
        /// <summary>
        /// Creates an ApiResult from a Result{T}
        /// </summary>
   public static ApiResult<T> FromResult(Result<T> result) =>
            result switch
          {
                Result<T>.Success success => new ApiResult<T>(
        true,
  success.Payload,
         null,
          null,
             success.Details,
    success.CorrelationId,
        DateTimeOffset.UtcNow.ToString("o")),
            Result<T>.Failure failure => new ApiResult<T>(
      false,
     default,
          failure.ErrorCode,
    failure.ErrorMessage,
         failure.Details,
    failure.CorrelationId,
         DateTimeOffset.UtcNow.ToString("o")),
   _ => throw new InvalidOperationException("Unknown result type")
        };

        /// <summary>
        /// Factory method to create a successful API result
        /// </summary>
        public static ApiResult<T> Ok(T payload, string? correlationId = null, object? details = null) =>
      new(true, payload, null, null, details, correlationId, DateTimeOffset.UtcNow.ToString("o"));

        /// <summary>
        /// Factory method to create a failed API result
        /// </summary>
        public static ApiResult<T> Fail(string errorCode, string errorMessage, string? correlationId = null, object? details = null) =>
            new(false, default, errorCode, errorMessage, details, correlationId, DateTimeOffset.UtcNow.ToString("o"));
    }
}
