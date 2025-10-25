using Microsoft.Extensions.Options;
using TicTacToeApi.Configuration;
using TicTacToeApi.Interfaces;

namespace TicTacToeApi.Services
{
    /// <summary>
    /// Generates unique room codes for game rooms
    /// </summary>
    public sealed class RoomCodeGenerator : IRoomCodeGenerator
    {
        private readonly Random _rng;
        private readonly GameSettings _settings;

        public RoomCodeGenerator(IOptions<GameSettings> settings)
        {
            _rng = new Random();
            _settings = settings.Value;
        }

        public string GenerateCode()
        {
            var chars = new char[_settings.RoomCodeLength];
            var alphabet = _settings.RoomCodeAlphabet;

            for (int i = 0; i < _settings.RoomCodeLength; i++)
            {
                chars[i] = alphabet[_rng.Next(alphabet.Length)];
            }

            return new string(chars);
        }
    }
}
