using FluentAssertions;
using HabitTracker.Api.Controllers;
using HabitTracker.Application.Features.Tasks;
using HabitTracker.Application.Features.Tasks.Validators;
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
    public class TaskControllerTests
    {
        public static TasksController CreateController(Guid userId, out HabitTracker.Infrastructure.Persistence.AppDbContext db)
        {
            db = TestDbContextFactory.Create();
            var logger = Mock.Of<ILogger<TasksController>>();

            var controller = new TasksController(db, logger);

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
        public async Task CreateTask_ShouldReturnCreatedTask()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var controller = CreateController(userId, out var db);

            var request = new CreateTaskRequest
            {
                Title = "Test Task",
                Description = "This is a test task.",
                Priority = 1,
                DueDate = DateTime.UtcNow.AddDays(1)
            };

            var validator = new CreateTasksRequestValidator();
            var result = await controller.Post(request, validator);

            //Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var response = createdResult.Value.Should().BeAssignableTo<TaskResponse>().Subject;

            response.Title.Should().Be("Test Task");
            response.Description.Should().Be("This is a test task.");
            response.Priority.Should().Be(1);
            response.IsCompleted.Should().BeFalse();

            // Verify that the task was actually saved in the database
            var savedTask = await db.Tasks.FindAsync(response.Id);
            savedTask.Should().NotBeNull();
            savedTask.UserId.Should().Be(userId);
        }

        [Fact]
        public async Task CreateTask_WithEmptyTitle_ReturnsValidationProblem()
        {
            //Arrange
            var userId = Guid.NewGuid();
            var controller = CreateController(userId, out _);
            var request = new CreateTaskRequest
            {
                Title = "", // Invalid title
                Description = "This is a test task.",
                Priority = 1,
                DueDate = DateTime.UtcNow.AddDays(1)
            };
            var validator = new CreateTasksRequestValidator();
            var result = await controller.Post(request, validator);
            //Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
        }
        [Fact]
        public async Task GetTaskById_WhenTaskExists_ReturnsTask()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var controller = CreateController(userId, out var db);
            // Seed a task for the user
            var seededTask = await TestDbContextFactory.SeedTaskAsync(db, userId, "Finish Report");
            // Act
            var result = await controller.Get(seededTask.Id);
            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<TaskResponse>().Subject;
            response.Id.Should().Be(seededTask.Id);
            response.Title.Should().Be("Finish Report");
        }

        [Fact]
        public async Task GetTaskById_WhenTaskDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var controller = CreateController(userId, out _);
            var nonExistentTaskId = Guid.NewGuid();
            // Act
            var result = await controller.Get(nonExistentTaskId);
            // Assert
            result.Result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetTaskById_WhenTaskBelongsToDifferentUser_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var controller = CreateController(userId, out var db);
            var otherUserId = Guid.NewGuid();
            var seededTask = await TestDbContextFactory.SeedTaskAsync(db, otherUserId, "Finish Report");
            // Act
            var result = await controller.Get(seededTask.Id);
            // Assert
            result.Result.Should().BeOfType<NotFoundResult>().Which.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task PutTaskById_WithValidRequest_UpdateAllFields() 
        {
            // Arrange
            var userId = Guid.NewGuid();
            var controller = CreateController(userId, out var db);
            var seededTask = await TestDbContextFactory.SeedTaskAsync(db, userId, "Initial Title");

            var request = new UpdateTaskRequest
            {
                Title = "Updated Title",
                Description = "Updated Description",
                Priority = 2,
                DueDate = DateTime.UtcNow.AddDays(5),
                IsCompleted = true
            };

            var validator = new UpdateTaskRequestValidator();

            var result = await controller.Put(seededTask.Id,request, validator);

            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<TaskResponse>().Subject;

            response.Title.Should().Be("Updated Title");
            response.Description.Should().Be("Updated Description"); 
            response.Priority.Should().Be(2);
            response.IsCompleted.Should().BeTrue();

            var savedTask = await db.Tasks.FindAsync(response.Id);
            savedTask!.Title.Should().Be("Updated Title");
        }
    }
}
