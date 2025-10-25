using TicTacToeApi.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using TicTacToeApi.Configuration;
using Microsoft.Extensions.Options;
using TicTacToeApi.Core.Utilities;

namespace TicTacToeApi.Repositories
{
/// <summary>
    /// Repository interface for room persistence
    /// Abstracts room storage implementation to support multiple backends
    /// (in-memory, database, caching, etc.)
    /// </summary>
    public interface IRoomRepository
    {
  /// <summary>
      /// Creates a new room and stores it
        /// </summary>
        /// <param name="room">The room to create</param>
   /// <returns>The created room</returns>
        Room Create(Room room);

     /// <summary>
        /// Retrieves a room by its code
        /// </summary>
        /// <param name="code">The room code</param>
        /// <param name="room">The retrieved room, or null if not found</param>
        /// <returns>True if room found, false otherwise</returns>
      bool TryGetById(string code, out Room room);

        /// <summary>
        /// Updates an existing room
    /// </summary>
        /// <param name="room">The room to update</param>
 /// <returns>True if update successful, false if room not found</returns>
        bool Update(Room room);

  /// <summary>
        /// Deletes a room by its code
  /// </summary>
     /// <param name="code">The room code</param>
      /// <returns>True if deletion successful, false if room not found</returns>
    bool Delete(string code);

     /// <summary>
   /// Retrieves all active rooms
     /// </summary>
  /// <returns>Collection of all rooms</returns>
    IEnumerable<Room> GetAll();

   /// <summary>
        /// Gets the count of active rooms
    /// Useful for monitoring and metrics
        /// </summary>
        /// <returns>Number of active rooms</returns>
    int GetCount();

        /// <summary>
 /// Checks if a room exists by code
  /// </summary>
      /// <param name="code">The room code</param>
  /// <returns>True if room exists, false otherwise</returns>
    bool Exists(string code);

        /// <summary>
        /// Clears all rooms from storage
     /// Typically used for cleanup or testing
        /// </summary>
        void Clear();
    }

 /// <summary>
 /// In-memory implementation of IRoomRepository
    /// Uses ConcurrentDictionary for thread-safe access
    /// Suitable for single-instance deployments
    /// </summary>
    public sealed class InMemoryRoomRepository : IRoomRepository
    {
      private readonly ConcurrentDictionary<string, Room> _rooms;
 private readonly ILogger<InMemoryRoomRepository> _logger;

        public InMemoryRoomRepository(ILogger<InMemoryRoomRepository> logger)
    {
      ArgumentNullException.ThrowIfNull(logger);
   _rooms = new ConcurrentDictionary<string, Room>();
         _logger = logger;
  }

        public Room Create(Room room)
      {
      ArgumentNullException.ThrowIfNull(room);

   if (!_rooms.TryAdd(room.Code, room))
{
       _logger.LogWarning("Failed to create room {RoomCode}: room already exists", room.Code);
  throw new InvalidOperationException($"Room {room.Code} already exists");
  }

          _logger.LogInformation("Created room {RoomCode} in repository", room.Code);
         return room;
     }

    public bool TryGetById(string code, out Room room)
        {
     if (code.IsNullOrEmpty())
       {
        room = null!;
       return false;
 }

       var found = _rooms.TryGetValue(code, out var result);
     room = result!;
       return found;
        }

        public bool Update(Room room)
        {
  ArgumentNullException.ThrowIfNull(room);

       // ConcurrentDictionary.AddOrUpdate returns the value, but we need to check if it existed
    var existed = _rooms.ContainsKey(room.Code);
     if (!existed)
   {
       _logger.LogWarning("Failed to update room {RoomCode}: room not found", room.Code);
        return false;
        }

 _rooms[room.Code] = room;
    _logger.LogDebug("Updated room {RoomCode} in repository", room.Code);
 return true;
    }

    public bool Delete(string code)
   {
      if (code.IsNullOrEmpty())
   return false;

  var deleted = _rooms.TryRemove(code, out _);
if (deleted)
         {
  _logger.LogInformation("Deleted room {RoomCode} from repository", code);
       }
     else
    {
      _logger.LogWarning("Failed to delete room {RoomCode}: room not found", code);
 }

   return deleted;
        }

 public IEnumerable<Room> GetAll()
    {
          return _rooms.Values.ToList();
        }

        public int GetCount()
      {
         return _rooms.Count;
 }

        public bool Exists(string code)
      {
         if (code.IsNullOrEmpty())
        return false;

  return _rooms.ContainsKey(code);
  }

        public void Clear()
    {
 _rooms.Clear();
      _logger.LogInformation("Cleared all rooms from repository");
        }
    }

  /// <summary>
/// Cached repository implementation with fallback to a base repository
    /// Supports both in-memory caching and distributed cache strategies
    /// Useful for performance optimization when database operations are slow
    /// </summary>
 public sealed class CachedRoomRepository : IRoomRepository
    {
        private readonly IRoomRepository _baseRepository;
        private readonly IMemoryCache _cache;
   private readonly ILogger<CachedRoomRepository> _logger;
        private readonly GameSettings _settings;
        private const string CacheKeyPrefix = "room_";
        private const string AllRoomsCacheKey = "all_rooms";

        public CachedRoomRepository(
   IRoomRepository baseRepository,
      IMemoryCache cache,
       ILogger<CachedRoomRepository> logger,
IOptions<GameSettings> settings)
        {
     ArgumentNullException.ThrowIfNull(baseRepository);
    ArgumentNullException.ThrowIfNull(cache);
     ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(settings);
     _baseRepository = baseRepository;
 _cache = cache;
      _logger = logger;
     _settings = settings.Value;
        }

        public Room Create(Room room)
     {
   var created = _baseRepository.Create(room);
       
     // Cache the new room
_cache.Set(GetRoomCacheKey(created.Code), created, _settings.GetRoomCacheTimeout());
  
   // Invalidate all rooms cache
      _cache.Remove(AllRoomsCacheKey);

 _logger.LogDebug("Cached new room {RoomCode}", room.Code);
  return created;
      }

        public bool TryGetById(string code, out Room room)
        {
if (code.IsNullOrEmpty())
     {
      room = null!;
  return false;
       }

            var cacheKey = GetRoomCacheKey(code);

    // Try to get from cache first
    if (_cache.TryGetValue(cacheKey, out Room? cachedRoom))
     {
         room = cachedRoom!;
     _logger.LogDebug("Cache hit for room {RoomCode}", code);
     return true;
    }

 // Fall back to base repository
     if (_baseRepository.TryGetById(code, out var result))
       {
      // Cache the retrieved room
           _cache.Set(cacheKey, result, _settings.GetRoomCacheTimeout());
    room = result;
    _logger.LogDebug("Cache miss for room {RoomCode}, loaded from repository", code);
   return true;
 }

      room = null!;
            return false;
        }

        public bool Update(Room room)
 {
      ArgumentNullException.ThrowIfNull(room);

  var updated = _baseRepository.Update(room);
 if (updated)
       {
            // Update cache
    var cacheKey = GetRoomCacheKey(room.Code);
   _cache.Set(cacheKey, room, _settings.GetRoomCacheTimeout());

// Invalidate all rooms cache
           _cache.Remove(AllRoomsCacheKey);

_logger.LogDebug("Updated and cached room {RoomCode}", room.Code);
          }

return updated;
   }

   public bool Delete(string code)
    {
 if (code.IsNullOrEmpty())
       return false;

   var deleted = _baseRepository.Delete(code);
       if (deleted)
            {
    // Remove from cache
     var cacheKey = GetRoomCacheKey(code);
    _cache.Remove(cacheKey);

             // Invalidate all rooms cache
   _cache.Remove(AllRoomsCacheKey);

    _logger.LogDebug("Deleted and removed from cache room {RoomCode}", code);
   }

 return deleted;
        }

   public IEnumerable<Room> GetAll()
    {
  // Try to get from cache first
    if (_cache.TryGetValue(AllRoomsCacheKey, out IEnumerable<Room>? cachedRooms))
          {
 _logger.LogDebug("Cache hit for all rooms");
          return cachedRooms!;
  }

    // Fall back to base repository
 var rooms = _baseRepository.GetAll().ToList();
   
      // Cache the results
            _cache.Set(AllRoomsCacheKey, rooms, _settings.GetAllRoomsCacheTimeout());
        
        _logger.LogDebug("Cache miss for all rooms, loaded {RoomCount} rooms from repository", rooms.Count);
            return rooms;
        }

        public int GetCount()
    {
    return _baseRepository.GetCount();
        }

        public bool Exists(string code)
      {
     if (code.IsNullOrEmpty())
    return false;

      var cacheKey = GetRoomCacheKey(code);
       
    // Check cache first
      if (_cache.TryGetValue(cacheKey, out Room? _))
            {
      return true;
      }

        return _baseRepository.Exists(code);
        }

        public void Clear()
        {
  _baseRepository.Clear();
   _cache.Remove(AllRoomsCacheKey);
       _logger.LogInformation("Cleared repository and invalidated cache");
     }

   private static string GetRoomCacheKey(string roomCode) => $"{CacheKeyPrefix}{roomCode}";
    }
}
