using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using HabitTracker.Domain.Entities;

namespace HabitTracker.Infrastructure.Auth
{
    public interface IJwtTokenGenerator
    {
        (string token, DateTime ExpiresAt) GenerateToken(User user);
    }
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _config;

        public JwtTokenGenerator(IConfiguration config) 
        {
            _config = config;
        }

        public (string token, DateTime ExpiresAt) GenerateToken(User user)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(jwtSection["ExpiryMinutes"]!));

            // claims is a dictionary that contains the claims for the JWT token. The claims include the user's ID, email, a unique identifier for the token (JTI), and the issued at time (IAT).
            var claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id,
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
                [JwtRegisteredClaimNames.Iat] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = jwtSection["Issuer"],
                Audience = jwtSection["Audience"],
                Claims = claims,
                Expires = expiresAt,
                SigningCredentials = creds
            };

            var handler = new JsonWebTokenHandler();
            var jwtToken = handler.CreateToken(descriptor);

            return new (jwtToken, expiresAt);

        }

    }
}
