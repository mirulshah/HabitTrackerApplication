using HabitTracker.Domain.Entities;
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
    }
}
