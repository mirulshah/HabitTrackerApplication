using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Domain.Entities
{
    public class TaskItem : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; } = false;
        public int Priority { get; set; }
        public User User { get; set; } = null!;
    }
}
