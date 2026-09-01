using System;
using System.Collections.Generic;
using System.Text;
using HabitTracker.Domain.Entities;
using HabitTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HabitTracker.Domain.Enum;

namespace HabitTracker.Infrastructure.Seeding
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
        {
            if (await db.Users.AnyAsync())
            {
                return; // Database has already been seeded
            }

            var demoUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "demo@habittracker.com",
                CreatedAt = DateTime.UtcNow
            };

            demoUser.PasswordHash = passwordHasher.HashPassword(demoUser, "DemoPassword123!");

            var secondUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@habittracker.com",
                CreatedAt = DateTime.UtcNow
            };

            secondUser.PasswordHash = passwordHasher.HashPassword(secondUser, "TestPassword123!");

            db.Users.AddRange(demoUser, secondUser);

            // Seed initial data here if needed
            await db.SaveChangesAsync();

            await SeedHabitAsync(db, demoUser.Id);
        }

        public static async Task SeedHabitAsync(AppDbContext db, Guid userId)
        {
            var random = new Random(42);
            var frequencyValue = Enum.GetValues<HabitFrequency>();

            var habits = new List<Habit>();

            var title = HabitPool.OrderBy(_ => random.Next()).ToList();
            var habitCount = Math.Min(100, title.Count);

            for (var i = 0; i < habitCount; i++)
            {
                var frequency = frequencyValue[random.Next(frequencyValue.Length)];

                habits.Add(new Habit
                {
                    UserId = userId,
                    Title = title[i],
                    Frequency = frequency
                });
            }

            db.Habits.AddRange(habits);
            await db.SaveChangesAsync();

            var logs = new List<HabitLog>();

            foreach (var habit in habits)
            {
                for (var daysAgo = 30; daysAgo >= 0; daysAgo--)
                {
                    var wasCompleted = random.NextDouble() > 0.25;

                    if (wasCompleted)
                    {
                        logs.Add(new HabitLog
                        {
                            HabitId = habit.Id,
                            CompletedDate = DateTime.UtcNow.Date.AddDays(daysAgo)
                        });
                    }
                }
            }

            db.HabitLogs.AddRange(logs);
            await db.SaveChangesAsync();
        }

        public static readonly string[] HabitPool =
        {
            "Drink water", "Exercise", "Read", "Meditate", "Journal",
        "Stretch", "Walk outside", "Practice guitar", "Study Spanish",
        "No sugar", "Sleep by 11pm", "Cold shower", "Floss", "Gratitude list",
        "Plan tomorrow", "Review budget", "Call a friend", "Tidy desk",
        "Practice coding", "Draw", "Yoga", "Run", "Cycle", "Swim",
        "Eat vegetables", "No phone before bed", "Vitamins", "Skincare",
        "Learn a new word", "Write 500 words", "Declutter", "Deep breathing",
        "Listen to a podcast", "Cook at home", "Meal prep", "Practice piano",
        "Volunteer", "Read the news", "Check in with family", "Backup files"
        };
    }
}
