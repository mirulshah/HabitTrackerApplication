using HabitTracker.Application.Features.Tasks;
using HabitTracker.Application.Enum;
using HabitTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using HabitTracker.Domain.Entities;
using HabitTracker.Application.Common;
using Asp.Versioning;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace HabitTracker.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/tasks")]
    [ApiController]
    
    public class TasksController : APIControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TasksController> _logger;

        public TasksController(AppDbContext db, ILogger<TasksController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: api/<Tasks>
        [HttpGet]
        public async Task<ActionResult<PagedResult<TaskResponse>>> Get([FromQuery] TaskQueryParameters parameters)
        {
            var userId = GetCurrentUserId();

            var tasksQuery = _db.Tasks.AsQueryable().Where(t => t.UserId == userId);

            tasksQuery = parameters.Status switch
            {
                TaskStatusFilter.Completed => tasksQuery.Where(t => t.IsCompleted),
                TaskStatusFilter.Pending => tasksQuery.Where(t => !t.IsCompleted && (t.DueDate == null || t.DueDate >= DateTime.UtcNow)),
                TaskStatusFilter.Overdue => tasksQuery.Where(t => !t.IsCompleted && t.DueDate < DateTime.UtcNow),
                _ => tasksQuery
            };

            if(parameters.Priority.HasValue)
            {
                tasksQuery = tasksQuery.Where(t => t.Priority == parameters.Priority.Value);
            }

            if(parameters.DueDate.HasValue)
                tasksQuery = tasksQuery.Where(t => t.DueDate.HasValue && t.DueDate <= parameters.DueDate);

            tasksQuery = ((parameters.SortBy.ToLower(), parameters.SortDir.ToLower()) switch
            {
                ("duedate", "asc") => tasksQuery.OrderBy(t => t.DueDate),
                ("duedate", "desc") => tasksQuery.OrderByDescending(t => t.DueDate),
                ("priority", "asc") => tasksQuery.OrderBy(t => t.Priority),
                ("priority", "desc") => tasksQuery.OrderByDescending(t => t.Priority),
                _ => tasksQuery.OrderBy(t => t.DueDate)
            });

            var totalCount = tasksQuery.Count();

            var items = await tasksQuery
                .Skip((parameters.Page - 1) * parameters.PageSize ?? 0)
                .Take(parameters.PageSize ?? 10)
                .Select(t => new TaskResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    DueDate = t.DueDate,
                    IsCompleted = t.IsCompleted,
                    Priority = t.Priority
                })
                .ToListAsync();

            return Ok(new PagedResult<TaskResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = parameters.Page ?? 1,
                PageSize = parameters.PageSize ?? 10
            });
        }

        // GET api/<Tasks>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskResponse>> Get(Guid id)
        {
            var userId = GetCurrentUserId();

            var task = await _db.Tasks
                .Where(t => t.Id == id && userId == t.UserId)
                .Select(t => new TaskResponse 
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    DueDate = t.DueDate,
                    IsCompleted = t.IsCompleted
                })
                .FirstOrDefaultAsync();

            if (task is null)
                return NotFound();

            return Ok(task);
        }

        // POST api/<Tasks>
        [HttpPost]
        public async Task<ActionResult<TaskResponse>> Post(CreateTaskRequest request, [FromServices] IValidator<CreateTaskRequest> createTaskValidator)
        {
            var validationResult = await createTaskValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
            }

            var userId = GetCurrentUserId();

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate,
                Priority = request.Priority,
                UserId = userId
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            var response = new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                IsCompleted = task.IsCompleted,
                Priority = task.Priority
            };

            return CreatedAtAction(nameof(Get), new { }, response);
        }

        // PUT api/<Tasks>/5
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskResponse>> Put(Guid id, UpdateTaskRequest request, [FromServices] IValidator<UpdateTaskRequest> updateTaskValidator)
        {
            var validationResult = await updateTaskValidator.ValidateAsync(request);

            if(!validationResult.IsValid) 
            { 
                return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
            }

            var userId = GetCurrentUserId();

            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null) 
            {
                _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}.", id, userId);
                return NotFound(); 
            }

            task.Title = request.Title;
            task.Description = request.Description;
            task.DueDate = request.DueDate;
            task.IsCompleted = request.IsCompleted;
            task.Priority = request.Priority;

            await _db.SaveChangesAsync();

            var response = new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                IsCompleted = task.IsCompleted,
                Priority = task.Priority
            };

            return Ok(response);

        }

        // PATCH api/<Tasks>/5
        [HttpPatch("{id}")]
        public async Task<ActionResult<TaskResponse>> Patch(Guid id, PatchItemTaskRequest request, [FromServices] IValidator<PatchItemTaskRequest> patchTaskValidator)
        {
            var validationResult = await patchTaskValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
            }
            var userId = GetCurrentUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}.", id, userId);
                return NotFound();
            }
            if (request.Title != null) task.Title = request.Title;
            if (request.Description != null) task.Description = request.Description;
            if (request.DueDate.HasValue) task.DueDate = request.DueDate;
            if (request.Priority.HasValue) task.Priority = request.Priority.Value;
            if (request.IsCompleted.HasValue) task.IsCompleted = request.IsCompleted.Value;
            await _db.SaveChangesAsync();
            var response = new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                IsCompleted = task.IsCompleted,
                Priority = task.Priority
            };
            return Ok(response);
        }

        // PATCH api/<Tasks>/5/status
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<TaskResponse>> UpdateStatus(Guid id, UpdateTaskStatusRequest request)
        {
            var userId = GetCurrentUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}.", id, userId);
                return NotFound();
            }
            task.IsCompleted = request.IsCompleted;
            await _db.SaveChangesAsync();
            var response = new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                IsCompleted = task.IsCompleted,
                Priority = task.Priority
            };
            return Ok(response);
        }

        // DELETE api/<Tasks>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for user {UserId}.", id, userId);
                return NotFound();
            }
            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        
    }
}
