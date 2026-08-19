using HabitTracker.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Habit
{
    public class HabitQueryParameter
    {
        public string? Search { get; set; }
        public HabitFrequency? Frequency { get; set; }
        public bool? CompletedToday { get; set; }
        public string SortBy { get; set; } = "createdAt";
        public string SortDir { get; set; } = "desc";
        public int PageSize { get; set; } = 20;
        public int Page { get; set; } = 1;

    }
}
