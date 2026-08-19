using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace HabitTracker.Infrastructure.Auth
{
    public class RefreshTokenGenerator
    {
        public static string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public static string Hash(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
