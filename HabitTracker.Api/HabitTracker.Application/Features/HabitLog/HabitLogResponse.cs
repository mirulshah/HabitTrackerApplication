using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.HabitLog
{
    public class HabitLogResponse
    {
        public Guid Id { get; set; }
        public DateTime CompletedDate { get; set; }
    }
}
