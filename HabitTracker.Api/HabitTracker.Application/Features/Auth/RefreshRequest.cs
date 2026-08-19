using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Auth
{
    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
