using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Tasks
{
    public class TaskResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; } = false;
        public int Priority { get; set; }
    }
}
