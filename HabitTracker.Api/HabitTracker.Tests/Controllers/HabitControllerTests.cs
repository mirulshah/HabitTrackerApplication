using FluentAssertions;
using HabitTracker.Api.Controllers;
using HabitTracker.Infrastructure.Persistence;
using HabitTracker.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace HabitTracker.Tests.Controllers
{
    public class HabitControllerTests
    {
        public static HabitController CreateController(Guid userId,out AppDbContext db)
        {
            db = TestDbContextFactory.Create();
            var logger = Mock.Of<ILogger<HabitController>>();

            var controller = new HabitController(db, logger);

            // Simulate an authenticated user by setting the User property of the controller's HttpContext
            // Since GetCurrentUserId() retrieves the user ID from the claims, we need to add a claim for the user ID
            var claims = new List<Claim> { new("sub", userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        [Fact]

        public async Task Imcomplete_WhenHabitCompletedToday_RemovesLogAndReturnsNoContent()
        {
            var userId = Guid.NewGuid();
            var controller = CreateController(userId, out var db);

            var (habit,log) = await TestDbContextFactory.SeedHabitWithTodayLogAsync(db, userId);

            var result = await controller.Incomplete(habit.Id);

            result.Should().BeOfType<NoContentResult>();

            var remainingLog = await db.HabitLogs.FindAsync(log.Id);
            remainingLog.Should().BeNull();
        }
    }
}
