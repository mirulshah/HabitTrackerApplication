using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Tasks.Validators
{
    public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
    {
        public UpdateTaskRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(100);
            RuleFor(x => x.Description)
                .MaximumLength(500);
            RuleFor(x => x.DueDate)
                .Must(date => date is null || date >= DateTime.UtcNow)
                .WithMessage("Due date cannot be in the past.");
            RuleFor(x => x.Priority)
                .InclusiveBetween(0, 5)
                .WithMessage("Priority must be between 0 and 5.");
        }
    }
}
