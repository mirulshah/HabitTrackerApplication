using HabitTracker.Domain.Entities;
using HabitTracker.Domain.Enum;
using HabitTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Tests.TestHelpers
{
    public static class TestDbContextFactory
    {
        public static AppDbContext Create() 
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        public static async Task<TaskItem> SeedTaskAsync(AppDbContext db, Guid userId, string title = "Sample Task")
        {
            var task = new TaskItem
            {
                UserId = userId,
                Title = title,
                Description = "Sample task description",
                Priority = 1,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            return task;
        }

        public static async Task<(Habit Habit, HabitLog Log)> SeedHabitWithTodayLogAsync(AppDbContext db, Guid userId, string title = "Sample Habit", HabitFrequency frequency = HabitFrequency.Daily)
        {
            var habit = new Habit
            {
                UserId = userId,
                Title = title,
                Frequency = frequency,
            };

            db.Habits.Add(habit);
            await db.SaveChangesAsync();

            var log = new HabitLog
            {
                HabitId = habit.Id,
                CompletedDate = DateTime.UtcNow.Date,
            };


            db.HabitLogs.Add(log);
            await db.SaveChangesAsync();

            return (habit, log);
        }
    }
}
