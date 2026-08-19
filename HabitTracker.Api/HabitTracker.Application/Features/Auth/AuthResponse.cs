using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Auth
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
    }
}
