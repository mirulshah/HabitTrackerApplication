using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Auth
{
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
