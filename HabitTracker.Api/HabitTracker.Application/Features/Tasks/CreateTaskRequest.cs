using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Tasks
{
    public class CreateTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public int Priority { get; set; } = 0;
    }
}
