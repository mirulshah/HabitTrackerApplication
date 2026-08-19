using HabitTracker.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Habit
{
    public class HabitResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public HabitFrequency Frequency { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsCompletedToday { get; set; } = false;
    }
}
