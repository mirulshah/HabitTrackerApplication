using HabitTracker.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Domain.Entities
{
    public class Habit : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public HabitFrequency Frequency { get; set; }

        public User User { get; set; } = null!;
        public ICollection<HabitLog> Logs { get; set; } = new List<HabitLog>();
    }
}
