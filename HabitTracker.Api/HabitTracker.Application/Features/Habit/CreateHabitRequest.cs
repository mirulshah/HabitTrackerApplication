using HabitTracker.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Habit
{
    public class CreateHabitRequest
    {
        public string Title { get; set; } = string.Empty;
        public HabitFrequency Frequency { get; set; }
    }
}
