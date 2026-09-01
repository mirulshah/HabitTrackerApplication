using FluentValidation;
using HabitTracker.Application.Features.Habit;
using HabitTracker.Infrastructure.Persistence;
using HabitTracker.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using HabitTracker.Application.Common;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;
using HabitTracker.Application.Features.HabitLog;


namespace HabitTracker.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/habits")]
    [ApiController]
    public class HabitController : APIControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<HabitController> _logger;

        public HabitController(AppDbContext db, ILogger<HabitController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // POST: api/habit
        [HttpPost]
        public async Task<ActionResult<HabitResponse>> Post(CreateHabitRequest request, [FromServices] IValidator<CreateHabitRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
            }

            var userId = GetCurrentUserId();

            var habit = new Habit
            {
                UserId = userId,
                Title = request.Title,
                Frequency = request.Frequency
            };

            _db.Habits.Add(habit);
            await _db.SaveChangesAsync();

            var response = new HabitResponse
            {
                Id = habit.Id,
                Title = habit.Title,
                Frequency = habit.Frequency,
                CreatedAt = habit.CreatedAt,
                UpdatedAt = habit.UpdatedAt
            };

            return Ok(response);
        }

        // GET: api/habit
        [HttpGet]
        public async Task<ActionResult<PagedResult<HabitResponse>>> Get([FromQuery] HabitQueryParameter query)
        {
            var userId = GetCurrentUserId();

            var today = DateTime.UtcNow.Date;

            var habitsQuery = _db.Habits
                .Where(h => h.UserId == userId);

            if (!string.IsNullOrEmpty(query.Search))
            {
                habitsQuery = habitsQuery.Where(h => h.Title.Contains(query.Search));
            }

            // Apply frequency filter
            if (query.Frequency.HasValue)
            {
                habitsQuery = habitsQuery.Where(h => h.Frequency == query.Frequency.Value);
            }

            // Apply completed today filter
            if (query.CompletedToday.HasValue)
            {
                if (query.CompletedToday.Value)
                {
                    habitsQuery = habitsQuery.Where(h => h.Logs.Any(l => l.CompletedDate == today));
                }
                else
                {
                    habitsQuery = habitsQuery.Where(h => !h.Logs.Any(l => l.CompletedDate == today));
                }
            }

            habitsQuery = (query.SortBy.ToLower(), query.SortDir.ToLower()) switch
            {
                ("title", "desc") => habitsQuery.OrderByDescending(h => h.Title),
                ("title", _) => habitsQuery.OrderBy(h => h.Title),
                ("createdat", "desc") => habitsQuery.OrderByDescending(h => h.CreatedAt),
                ("createdat", _) => habitsQuery.OrderBy(h => h.CreatedAt),
            };

            var totalCount = await habitsQuery.CountAsync();

            var items = await habitsQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(h => new HabitResponse
                {
                    Id = h.Id,
                    Title = h.Title,
                    Frequency = h.Frequency,
                    CreatedAt = h.CreatedAt,
                    UpdatedAt = h.UpdatedAt,
                    IsCompletedToday = h.Logs.Any(l => l.CompletedDate == today)
                })
                .ToListAsync();

            return Ok(new PagedResult<HabitResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            });
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<HabitResponse>> GetById(Guid id)
        {
            var userId = GetCurrentUserId();
            var habit = await _db.Habits
                .Include(h => h.Logs)
                .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);
            if (habit == null)
            {
                return NotFound();
            }
            var today = DateTime.UtcNow.Date;
            var response = new HabitResponse
            {
                Id = habit.Id,
                Title = habit.Title,
                Frequency = habit.Frequency,
                CreatedAt = habit.CreatedAt,
                UpdatedAt = habit.UpdatedAt,
                IsCompletedToday = habit.Logs.Any(l => l.CompletedDate == today)
            };
            return Ok(response);
        }

        [HttpPatch("{id}/log")]
        public async Task<ActionResult<HabitResponse>> MarkComplete(Guid id)
        {
            var today = DateTime.UtcNow.Date;
            var userId = GetCurrentUserId();
            var habit = await _db.Habits
                .Include(h => h.Logs)
                .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

            if (habit == null)
            {
                return NotFound();
            }

            if (habit.Logs.Any(l => l.HabitId == habit.Id && l.CompletedDate == today))
            {
                _logger.LogWarning("Habit already marked as completed on date {date}", today);
                return Conflict("Habit already marked as completed today.");
            }

            var log = new HabitLog
            {
                HabitId = habit.Id,
                CompletedDate = today
            };

            _db.HabitLogs.Add(log);

            await _db.SaveChangesAsync();

            var response = new HabitResponse
            {
                Id = habit.Id,
                Title = habit.Title,
                Frequency = habit.Frequency,
                CreatedAt = habit.CreatedAt,
                UpdatedAt = habit.UpdatedAt,
                IsCompletedToday = true
            };

            return Ok(response);
        }

        [HttpDelete("{id}/log/today")]

        public async Task<IActionResult> Incomplete(Guid id)
        {
            var today = DateTime.UtcNow.Date;

            var userId = GetCurrentUserId();

            var result = await _db.Habits.Where(h => h.Id == id && h.UserId == userId).Select(h => new
            {
                Habit = h,
                TodayLog = h.Logs.FirstOrDefault(l => l.CompletedDate == today)
            }).FirstOrDefaultAsync();
                            

            //var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

            if(result is null)
            {
                return NotFound();
            }

            //var log = await _db.HabitLogs.FirstOrDefaultAsync(l => l.HabitId == habit.Id && l.CompletedDate == today);

            if (result.TodayLog is null)
            {
                _logger.LogWarning("Habit is still incompleted today");
                return Conflict("Habit is still incompleted today");
            }

            _db.HabitLogs.Remove(result.TodayLog);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id}/log")]
        public async Task<ActionResult<HabitLogResponse>> GetLog(Guid id, [FromQuery] HabitLogQueryParameters query) 
        {
            var userId = GetCurrentUserId();

            var habit = await _db.Habits.AnyAsync(h => h.Id.Equals(id) && h.UserId == userId);

            if (!habit)
            {
                _logger.LogWarning("The habit {habit} does not exist",id);
                return NoContent();
            }

            var log = _db.HabitLogs.Where(l => l.HabitId == id);

            if(query.Year.HasValue)
            {
               log = log.Where(l => l.CompletedDate.Year == query.Year.Value);
            }

            if (query.Month.HasValue) 
            {
                log = log.Where(l => l.CompletedDate.Month == query.Month.Value);
            }

            log = (query.SortBy.ToLower(), query.SortDir.ToLower()) switch
            {
                ("createdat", "desc") => log.OrderByDescending(l => l.CreatedAt),
                ("createdat", _) => log.OrderBy(l => l.CreatedAt)
            };

            var totalCount = await log.CountAsync();

            var items = await log
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(l => new HabitLogResponse
                { 
                    Id = l.Id,
                    CompletedDate = l.CompletedDate
                }).ToListAsync();

            return Ok(new PagedResult<HabitLogResponse>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            });


        }


    }
}
