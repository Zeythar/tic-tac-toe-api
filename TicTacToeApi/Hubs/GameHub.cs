using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TicTacToeApi.Configuration;
using TicTacToeApi.Core.StateMachine;
using TicTacToeApi.Interfaces;
using TicTacToeApi.Logging;
using TicTacToeApi.Models;
using TicTacToeApi.Validation;
using System;
using System.Linq;

namespace TicTacToeApi.Hubs
{
    public partial class GameHub : Hub
    {
        private readonly IRoomService _roomService;
        private readonly IGameValidator _validator;
        private readonly IGameLogger _gameLogger;
        private readonly Random _rng = new();
        private readonly GameSettings _settings;
        private readonly ILogger<GameHub> _logger;
        private readonly ILogger<GameStateMachine> _stateLogger;

        public GameHub(IRoomService roomService, IGameValidator validator, IGameLogger gameLogger, IOptions<GameSettings> settings, ILogger<GameHub> logger, ILogger<GameStateMachine> stateLogger)
        {
            _roomService = roomService;
            _validator = validator;
            _gameLogger = gameLogger ?? throw new ArgumentNullException(nameof(gameLogger));
            _settings = settings.Value;
            _logger = logger;
            _stateLogger = stateLogger;
        }

        public async Task<ApiResult<GameJoinPayload>> CreateGame()
        {
            var correlationId = Guid.NewGuid().ToString("N");
            _gameLogger.CurrentCorrelationId = correlationId;

            var room = _roomService.CreateRoom();
            _gameLogger.LogRoomEvent("Created", room.Code, $"CorrelationId: {correlationId}");
            _logger.LogInformation("CreateGame: created room {Code} for connection {ConnectionId}", room.Code, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

            var playerId = Guid.NewGuid().ToString("N");
            lock (room)
            {
                room.AddConnection(playerId, Context.ConnectionId);
            }

            _gameLogger.LogPlayerAction("JoinedAsCreator", playerId, room.Code, Context.ConnectionId);

            // Host is added to room but game hasn't started yet (no symbols assigned)
            // They'll receive GameStarted event when second player joins
            var assignedSymbol = room.Players[playerId].Symbol;
            var currentTurn = room.CurrentTurn;
            var board = room.Board.ToImmutableBoard();
            await Clients.Caller.SendAsync("GameCreated", room.Code, board.ToArray(), playerId);

            var payload = new GameJoinPayload(room.Code, playerId, board, assignedSymbol ?? string.Empty, currentTurn ?? string.Empty);
            return ApiResult<GameJoinPayload>.Ok(payload, correlationId);
        }

        public async Task<ApiResult<GameJoinPayload>> JoinGame(string code, string? clientPlayerId = null)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            _gameLogger.CurrentCorrelationId = correlationId;

            // Validate input
            var joinRequest = new JoinGameRequest { Code = code, ClientPlayerId = clientPlayerId };
            var validationError = _validator.ValidateJoinRequest(joinRequest);
            if (validationError != null)
            {
                _gameLogger.LogValidationFailure("JoinGame", validationError, $"Code: {code}");
                _logger.LogWarning("JoinGame: validation failed for room {Code}: {Error}", code, validationError);
                return ApiResult<GameJoinPayload>.Fail(validationError, ErrorMessage.GetMessage(validationError), correlationId);
            }

            if (!_roomService.TryGetRoom(code, out var room))
            {
                _gameLogger.LogRoomEvent("NotFound", code);
                _logger.LogInformation("JoinGame: room {Code} not found for connection {ConnectionId}", code, Context.ConnectionId);
                return ApiResult<GameJoinPayload>.Fail(ErrorCode.NotFound, ErrorMessage.GetMessage(ErrorCode.NotFound), correlationId);
            }

            string? playerId = null;
            string? assignedSymbol = null;
            string? currentTurn = null;
            System.Collections.Immutable.ImmutableArray<int> board = System.Collections.Immutable.ImmutableArray<int>.Empty;
            bool wasFull = false;
            bool gameStarted = false;

            lock (room)
            {
                if (!string.IsNullOrEmpty(clientPlayerId))
                {
                    if (room.Players.TryGetValue(clientPlayerId, out var existingPlayer))
                    {
                        if (existingPlayer.ConnectionId == Context.ConnectionId)
                        {
                            _gameLogger.LogPlayerAction("JoinAttempt", clientPlayerId, code, Context.ConnectionId, "Already in room");
                            _logger.LogInformation("JoinGame: connection {ConnectionId} already holds playerId {PlayerId} in room {Code}", Context.ConnectionId, clientPlayerId, code);
                            return ApiResult<GameJoinPayload>.Fail(ErrorCode.AlreadyInRoom, ErrorMessage.GetMessage(ErrorCode.AlreadyInRoom), correlationId);
                        }
                        else if (existingPlayer.ConnectionId == null)
                        {
                            _gameLogger.LogPlayerAction("JoinAttempt", clientPlayerId, code, Context.ConnectionId, "Needs reconnect");
                            _logger.LogInformation("JoinGame: playerId {PlayerId} exists but disconnected in room {Code}; prompt reconnect", clientPlayerId, code);
                            return ApiResult<GameJoinPayload>.Fail(ErrorCode.ReconnectRequired, ErrorMessage.GetMessage(ErrorCode.ReconnectRequired), correlationId);
                        }
                        else
                        {
                            _gameLogger.LogPlayerAction("JoinAttempt", clientPlayerId, code, Context.ConnectionId, "Player ID in use");
                            _logger.LogInformation("JoinGame: playerId {PlayerId} in use by another connection in room {Code}", clientPlayerId, code);
                            return ApiResult<GameJoinPayload>.Fail(ErrorCode.PlayerIdInUse, ErrorMessage.GetMessage(ErrorCode.PlayerIdInUse), correlationId);
                        }
                    }
                }
            }

            var currentPlayer = room.GetPlayerByConnectionId(Context.ConnectionId);
            if (currentPlayer != null)
            {
                // If the player is already in the room but the game has started, return their state
                // This handles the case where the creator was added in CreateGame and now needs updated state after GameStarted
                bool gameHasStarted = room.Players.Values.All(p => p.HasSymbol);

                if (gameHasStarted)
                {
                    // Game has started - return the player's current state instead of failing
                    _gameLogger.LogPlayerAction("JoinAttempt", currentPlayer.PlayerId, code, Context.ConnectionId, "Already in room, returning current state");
                    _logger.LogInformation("JoinGame: connection {ConnectionId} already in room {Code}, game started, returning current state", Context.ConnectionId, code);

                    var existingBoard = room.Board.ToImmutableBoard();
                    var existingSymbol = currentPlayer.Symbol;
                    var existingTurn = room.CurrentTurn;

                    var existingPayload = new GameJoinPayload(code, currentPlayer.PlayerId, existingBoard, existingSymbol ?? string.Empty, existingTurn ?? string.Empty);
                    return ApiResult<GameJoinPayload>.Ok(existingPayload, correlationId);
                }

                // Game hasn't started yet - this is an error
                _gameLogger.LogPlayerAction("JoinAttempt", currentPlayer.PlayerId, code, Context.ConnectionId, "Already in room");
                _logger.LogInformation("JoinGame: connection {ConnectionId} attempted to re-join room {Code}", Context.ConnectionId, code);
                return ApiResult<GameJoinPayload>.Fail(ErrorCode.AlreadyInRoom, ErrorMessage.GetMessage(ErrorCode.AlreadyInRoom), correlationId);
            }

            var hasDisconnected = room.GetDisconnectedPlayers().Any();
            if (hasDisconnected || !room.CanJoin(_settings.MaxPlayersPerRoom))
            {
                wasFull = true;
            }
            else
            {
                playerId = Guid.NewGuid().ToString("N");
                room.AddConnection(playerId, Context.ConnectionId);
                _gameLogger.LogPlayerAction("Joined", playerId, code, Context.ConnectionId, "As second player");
                _logger.LogInformation("JoinGame: connection {ConnectionId} added as player {PlayerId} to room {Code}", Context.ConnectionId, playerId, code);

                if (room.PlayerOrder.Count == 2)
                {
                    var started = room.TryStartGame(_rng);
                    if (started)
                    {
                        // Notify state machine that game has started
                        var stateManager = new GameStateManager(room, _stateLogger);
                        stateManager.NotifyPlayerJoined();
                        _gameLogger.LogRoomEvent("Started", code, "Both players ready");
                        gameStarted = true;
                    }
                }

                assignedSymbol = room.Players[playerId].Symbol;
                currentTurn = room.CurrentTurn;
                board = room.Board.ToImmutableBoard();
            }

            if (wasFull)
            {
                await Clients.Caller.SendAsync("GameFull", code);
                _gameLogger.LogRoomEvent("Full", code);
                return ApiResult<GameJoinPayload>.Fail(ErrorCode.RoomFull, ErrorMessage.GetMessage(ErrorCode.RoomFull), correlationId);
            }

            if (playerId == null)
                return ApiResult<GameJoinPayload>.Fail(ErrorCode.NotFound, ErrorMessage.GetMessage(ErrorCode.NotFound), correlationId);

            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            await Clients.Caller.SendAsync("GameJoined", code, board.ToArray(), assignedSymbol, currentTurn, playerId);
            await Clients.GroupExcept(code, Context.ConnectionId).SendAsync("PlayerJoined");

            // Notify all clients about game start if it just started
            if (gameStarted)
            {
                await Clients.Group(code).SendAsync("GameStarted", board.ToArray(), currentTurn);
                _ = _roomService.StartTurnTimeoutAsync(code);
            }

            await SendPerClientSyncedStateAsync(room);

            var payload = new GameJoinPayload(code, playerId, board, assignedSymbol ?? string.Empty, currentTurn ?? string.Empty);
            return ApiResult<GameJoinPayload>.Ok(payload, correlationId);
        }

        public async Task<ApiResult<GameJoinPayload>> Reconnect(string code, string playerId)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            _gameLogger.CurrentCorrelationId = correlationId;

            // Validate input
            var reconnectRequest = new ReconnectRequest { Code = code, PlayerId = playerId };
            var validationError = _validator.ValidateReconnectRequest(reconnectRequest);
            if (validationError != null)
            {
                _gameLogger.LogValidationFailure("Reconnect", validationError, $"Code: {code}, PlayerId: {playerId}");
                _logger.LogWarning("Reconnect: validation failed for room {Code} and player {PlayerId}: {Error}", code, playerId, validationError);
                return ApiResult<GameJoinPayload>.Fail(validationError, ErrorMessage.GetMessage(validationError), correlationId);
            }

            // Attempt reconnection
            var reconnected = await _roomService.ReconnectAsync(code, playerId, Context.ConnectionId);
            if (!reconnected)
            {
                _gameLogger.LogConnectionEvent(code, "ReconnectFailed", Context.ConnectionId, playerId);
                _logger.LogWarning("Reconnect: failed to reconnect player {PlayerId} to room {Code}", playerId, code);
                return ApiResult<GameJoinPayload>.Fail(ErrorCode.NotFound, ErrorMessage.GetMessage(ErrorCode.NotFound), correlationId);
            }

            // Get room state after reconnection
            if (!_roomService.TryGetRoom(code, out var room))
            {
                _gameLogger.LogRoomEvent("NotFound", code);
                _logger.LogError("Reconnect: room {Code} not found after successful reconnection", code);
                return ApiResult<GameJoinPayload>.Fail(ErrorCode.NotFound, ErrorMessage.GetMessage(ErrorCode.NotFound), correlationId);
            }

            string? assignedSymbol = null;
            string? currentTurn = null;
            System.Collections.Immutable.ImmutableArray<int> board;

            lock (room)
            {
                if (room.Players.TryGetValue(playerId, out var player))
                {
                    assignedSymbol = player.Symbol;
                }
                currentTurn = room.CurrentTurn;
                board = room.Board.ToImmutableBoard();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, code);
            _gameLogger.LogConnectionEvent(code, "Reconnected", Context.ConnectionId, playerId);

            var payload = new GameJoinPayload(code, playerId, board, assignedSymbol ?? string.Empty, currentTurn ?? string.Empty);
            return ApiResult<GameJoinPayload>.Ok(payload, correlationId);
        }

        public Task<ApiResult<GameStatePayload>> GetGameState(string code, string playerId)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            _gameLogger.CurrentCorrelationId = correlationId;

            // Validate input
            var getStateRequest = new GetGameStateRequest { Code = code, PlayerId = playerId };
            var validationError = _validator.ValidateGetGameStateRequest(getStateRequest);
            if (validationError != null)
            {
                _gameLogger.LogValidationFailure("GetGameState", validationError, $"Code: {code}, PlayerId: {playerId}");
                _logger.LogWarning("GetGameState: validation failed for room {Code} and player {PlayerId}: {Error}", code, playerId, validationError);
                return Task.FromResult(ApiResult<GameStatePayload>.Fail(validationError, ErrorMessage.GetMessage(validationError), correlationId));
            }

            if (!_roomService.TryGetRoom(code, out var room))
            {
                _gameLogger.LogRoomEvent("NotFound", code);
                _logger.LogInformation("GetGameState: room {Code} not found", code);
                return Task.FromResult(ApiResult<GameStatePayload>.Fail(ErrorCode.NotFound, ErrorMessage.GetMessage(ErrorCode.NotFound), correlationId));
            }

            string? assignedSymbol = null;
            string? currentTurn;
            bool isGameOver;
            string? winner;
            System.Collections.Immutable.ImmutableArray<int> board;

            lock (room)
            {
                // Verify the player is in this room
                if (!room.Players.TryGetValue(playerId, out var player))
                {
                    _gameLogger.LogValidationFailure("GetGameState", ErrorCode.NotInGame, $"PlayerId: {playerId} not in room {code}");
                    _logger.LogWarning("GetGameState: player {PlayerId} not found in room {Code}", playerId, code);
                    return Task.FromResult(ApiResult<GameStatePayload>.Fail(ErrorCode.NotInGame, ErrorMessage.GetMessage(ErrorCode.NotInGame), correlationId));
                }

                assignedSymbol = player.Symbol;
                currentTurn = room.CurrentTurn;
                isGameOver = room.IsGameOver;
                winner = room.Winner;
                board = room.Board.ToImmutableBoard();
            }

            _gameLogger.LogPlayerAction("GetGameState", playerId, code, Context.ConnectionId, $"IsGameOver: {isGameOver}");

            var payload = new GameStatePayload(board, assignedSymbol, currentTurn, isGameOver, winner);
            return Task.FromResult(ApiResult<GameStatePayload>.Ok(payload, correlationId));
        }

        public async Task<ApiResult<MovePayload>> MakeMove(string code, int index, string? playerId = null)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            _gameLogger.CurrentCorrelationId = correlationId;

            // Validate input
            var moveRequest = new MakeMoveRequest { Code = code, Index = index, PlayerId = playerId };
            var validationError = _validator.ValidateMakeMoveRequest(moveRequest);
            if (validationError != null)
            {
                _gameLogger.LogValidationFailure("MakeMove", validationError, $"Code: {code}, Index: {index}");
                _logger.LogWarning("MakeMove: validation failed for room {Code} at index {Index}: {Error}", code, index, validationError);
                return ApiResult<MovePayload>.Fail(validationError, ErrorMessage.GetMessage(validationError), correlationId);
            }

            if (!_roomService.TryGetRoom(code, out var room))
                return ApiResult<MovePayload>.Fail(ErrorCode.NotFound, ErrorMessage.GetMessage(ErrorCode.NotFound), correlationId);

            var stateManager = new GameStateManager(room, _stateLogger);

            // Verify game is in active state
            if (!stateManager.CanMakeMove())
            {
                _gameLogger.LogValidationFailure("MakeMove", ErrorCode.GameOver, $"Current state: {stateManager.CurrentState}");
                _logger.LogWarning("MakeMove: attempted move in invalid game state {State}", stateManager.CurrentState);
                return ApiResult<MovePayload>.Fail(ErrorCode.GameOver, ErrorMessage.GetMessage(ErrorCode.GameOver), correlationId);
            }

            Room.MoveAttemptResult result;
            lock (room)
            {
                Player? player = null;
                if (!string.IsNullOrEmpty(playerId))
                    player = room.Players.GetValueOrDefault(playerId);
                else
                    player = room.GetPlayerByConnectionId(Context.ConnectionId);

                if (player == null || player.ConnectionId != Context.ConnectionId)
                    return ApiResult<MovePayload>.Fail(ErrorCode.NotInGame, ErrorMessage.GetMessage(ErrorCode.NotInGame), correlationId);

                result = room.TryMakeMove(Context.ConnectionId, index);
            }

            if (!result.Success)
            {
                _gameLogger.LogMove(code, playerId ?? "unknown", index, false, result.ErrorCode);
                return ApiResult<MovePayload>.Fail(result.ErrorCode ?? ErrorCode.Invalid, ErrorMessage.GetMessage(result.ErrorCode ?? ErrorCode.Invalid), correlationId);
            }

            _gameLogger.LogMove(code, playerId ?? "unknown", index, true);
            // Notify state manager of move
            stateManager.NotifyMoveMade();

            var boardImmutable = result.Board.ToImmutableBoard();
            await Clients.Group(code).SendAsync("BoardUpdated", boardImmutable.ToArray(), result.CurrentTurn, result.IsGameOver, result.Winner);

            if (result.IsGameOver)
            {
                // Notify state manager of game end
                if (result.Winner != null)
                {
                    stateManager.NotifyGameWon();
                    _gameLogger.LogGameOutcome(code, "Win", result.Winner);
                }
                else
                {
                    stateManager.NotifyGameDrawn();
                    _gameLogger.LogGameOutcome(code, "Draw");
                }

                var serverTimestamp = DateTimeOffset.UtcNow.ToString("o");
                var gameOverDto = new GameOverDto(code, GameOverResult.Winner, null, result.Winner, result.Board.ToImmutableBoard(), result.CurrentTurn, true, "Game won by move", correlationId, serverTimestamp);
                await Clients.Group(code).SendAsync("GameOver", gameOverDto);

                try
                {
                    _roomService.StartRematchWindow(code);
                    _gameLogger.LogRoomEvent("RematchWindowStarted", code);
                    _logger.LogInformation("Game ended for room {Code}; started rematch window", code);
                }
                catch (Exception ex)
                {
                    _gameLogger.LogError("StartRematchWindow", ex, correlationId);
                    _logger.LogError(ex, "Error starting rematch window for room {Code} after game end", code);
                }

                var payload = new MovePayload(result.Board.ToImmutableBoard(), result.CurrentTurn ?? string.Empty, result.IsGameOver, result.Winner, gameOverDto);
                return ApiResult<MovePayload>.Ok(payload, correlationId);
            }
            else
            {
                _ = _roomService.StartTurnTimeoutAsync(code);
            }

            var movePayload = new MovePayload(result.Board.ToImmutableBoard(), result.CurrentTurn ?? string.Empty, result.IsGameOver, result.Winner);
            return ApiResult<MovePayload>.Ok(movePayload, correlationId);
        }

        public Task<ApiResult<object>> OfferRematch(string code)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            _gameLogger.CurrentCorrelationId = correlationId;

            // Validate input
            var rematchRequest = new RematchActionRequest { Code = code };
            var validationError = _validator.ValidateRematchActionRequest(rematchRequest);
            if (validationError != null)
            {
                _gameLogger.LogValidationFailure("OfferRematch", validationError);
                _logger.LogWarning("OfferRematch: validation failed for room {Code}: {Error}", code, validationError);
                return Task.FromResult(ApiResult<object>.Fail(validationError, ErrorMessage.GetMessage(validationError), correlationId));
            }

            if (!_roomService.TryGetRoom(code, out var room))
                return Task.FromResult(ApiResult<object>.Fail(ErrorCode.NotFound, ErrorMessage.GetMessage(ErrorCode.NotFound), correlationId));

            var stateManager = new GameStateManager(room, _stateLogger);

            // Verify game is in GameOver state
            if (!stateManager.CanOfferRematch())
            {
                _gameLogger.LogValidationFailure("OfferRematch", ErrorCode.OfferFailed, $"Current state: {stateManager.CurrentState}");
                _logger.LogWarning("OfferRematch: attempted in invalid game state {State}", stateManager.CurrentState);
                return Task.FromResult(ApiResult<object>.Fail(ErrorCode.OfferFailed, ErrorMessage.GetMessage(ErrorCode.OfferFailed), correlationId));
            }

            var player = room.GetPlayerByConnectionId(Context.ConnectionId);
            if (player == null)
                return Task.FromResult(ApiResult<object>.Fail(ErrorCode.NotInGame, ErrorMessage.GetMessage(ErrorCode.NotInGame), correlationId));

            if (!_roomService.OfferRematch(code, player.PlayerId, out var expiresAt))
            {
                _gameLogger.LogRematchOperation(code, "Offer", player.PlayerId, false);
                return Task.FromResult(ApiResult<object>.Fail(ErrorCode.OfferFailed, ErrorMessage.GetMessage(ErrorCode.OfferFailed), correlationId));
            }

            // Notify state machine of rematch offer
            stateManager.NotifyRematchOffered();
            _gameLogger.LogRematchOperation(code, "Offer", player.PlayerId, true, $"ExpiresAt: {expiresAt}");

            _ = Clients.Group(code).SendAsync("RematchOffered", player.PlayerId, expiresAt);
            return Task.FromResult(ApiResult<object>.Ok(new { ExpiresAt = expiresAt }, correlationId));
        }

        public async Task<ApiResult<object>> AcceptRematch(string code)
        {
            var correlationId = Guid.NewGuid().ToString("N");
            _gameLogger.CurrentCorrelationId = correlationId;

            // Validate input
            var rematchRequest = new RematchActionRequest { Code = code };
            var validationError = _validator.ValidateRematchActionRequest(rematchRequest);
            if (validationError != null)
            {
                _gameLogger.LogValidationFailure("AcceptRematch", validationError);
                _logger.LogWarning("AcceptRematch: validation failed for room {Code}: {Error}", code, validationError);
                return ApiResult<object>.Fail(validationError, ErrorMessage.GetMessage(validationError), correlationId);
            }

            if (!_roomService.TryGetRoom(code, out var room))
                return ApiResult<object>.Fail(ErrorCode.NotFound, ErrorMessage.GetMessage(ErrorCode.NotFound), correlationId);

            var stateManager = new GameStateManager(room, _stateLogger);

            // Verify game is in RematchOffered state
            if (!stateManager.CanAcceptRematch())
            {
                _gameLogger.LogValidationFailure("AcceptRematch", ErrorCode.AcceptFailed, $"Current state: {stateManager.CurrentState}");
                _logger.LogWarning("AcceptRematch: attempted in invalid game state {State}", stateManager.CurrentState);
                return ApiResult<object>.Fail(ErrorCode.AcceptFailed, ErrorMessage.GetMessage(ErrorCode.AcceptFailed), correlationId);
            }

            var player = room.GetPlayerByConnectionId(Context.ConnectionId);
            if (player == null)
                return ApiResult<object>.Fail(ErrorCode.NotInGame, ErrorMessage.GetMessage(ErrorCode.NotInGame), correlationId);

            var rematchStarted = await _roomService.AcceptRematch(code, player.PlayerId);
            if (!rematchStarted)
            {
                _gameLogger.LogRematchOperation(code, "Accept", player.PlayerId, false);
                return ApiResult<object>.Fail(ErrorCode.AcceptFailed, ErrorMessage.GetMessage(ErrorCode.AcceptFailed), correlationId);
            }

            // Notify state manager of rematch acceptance and transition back to active
            stateManager.NotifyRematchAccepted();
            stateManager.NotifyMoveMade(isFirstMove: true);
            _gameLogger.LogRematchOperation(code, "Accept", player.PlayerId, true);

            return rematchStarted ?
                ApiResult<object>.Ok(new { Started = true }, correlationId) :
                ApiResult<object>.Ok(new { Started = false }, correlationId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await _roomService.HandleDisconnectAsync(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        private async Task SendPerClientSyncedStateAsync(Room room)
        {
            var snapshots = new List<(string connectionId, string? symbol, int[] board, string? currentTurn, bool isGameOver, string? winner)>();

            lock (room)
            {
                if (room.PlayerOrder.Count == 2 && room.Players.Values.All(p => p.HasSymbol))
                {
                    var boardCopy = room.Board.ToArray();
                    var currentTurn = room.CurrentTurn;
                    var isGameOver = room.IsGameOver;
                    var winner = room.Winner;

                    foreach (var player in room.Players.Values)
                    {
                        if (player.IsConnected)
                            snapshots.Add((player.ConnectionId!, player.Symbol, boardCopy, currentTurn, isGameOver, winner));
                    }
                }
            }

            foreach (var snapshot in snapshots)
            {
                await Clients.Client(snapshot.connectionId).SendAsync("SyncedState", snapshot.board, snapshot.symbol, snapshot.currentTurn, snapshot.isGameOver, snapshot.winner);
            }
        }
    }
}
