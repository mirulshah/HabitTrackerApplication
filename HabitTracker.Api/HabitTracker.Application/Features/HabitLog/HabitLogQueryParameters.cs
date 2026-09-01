using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.HabitLog
{
    public class HabitLogQueryParameters
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public string SortBy { get; set; } = "createdAt";
        public string SortDir { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
