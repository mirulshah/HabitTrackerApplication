using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Domain.Entities
{
    public class HabitLog : BaseEntity
    {
        public Guid HabitId { get; set; }
        public DateTime CompletedDate { get; set; } = DateTime.Now;
        public Habit Habit { get; set; } = null!;
    }
}
