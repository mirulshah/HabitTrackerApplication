using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Tasks
{
    public class TaskOverviewResponse
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TasksAverageDailyCompletionCount { get; set; } = 0;
        public int TasksAverageWeeklyCompletionCount { get; set; } = 0;
    }
}
