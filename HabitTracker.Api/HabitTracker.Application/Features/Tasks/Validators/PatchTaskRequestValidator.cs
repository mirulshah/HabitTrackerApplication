using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace HabitTracker.Application.Features.Tasks.Validators
{
    public class PatchTaskRequestValidator : AbstractValidator<PatchItemTaskRequest>
    {
        public PatchTaskRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200)
                .When(x => x.Title != null);

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => x.Description != null);

            RuleFor(x => x.DueDate)
                .LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(365))
                .When(x => x.DueDate.HasValue);

            RuleFor(x => x.Priority)
                .InclusiveBetween(0, 5)
                .When(x => x.Priority.HasValue);

        }
    }
}
