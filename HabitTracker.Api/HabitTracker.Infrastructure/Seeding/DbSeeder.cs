using System;
using System.Collections.Generic;
using System.Text;
using HabitTracker.Domain.Entities;
using HabitTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

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
        }
    }
}
