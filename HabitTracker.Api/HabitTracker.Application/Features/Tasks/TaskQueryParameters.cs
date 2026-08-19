using HabitTracker.Application.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Tasks
{
    public class TaskQueryParameters
    {
        public TaskStatusFilter Status { get; set; } = TaskStatusFilter.All;
        public int? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public string SortBy { get; set; } = "dueDate";
        public string SortDir { get; set; } = "asc";
        public int? PageSize { get; set; }
        public int? Page { get; set; }

    }
}
