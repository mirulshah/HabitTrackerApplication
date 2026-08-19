using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace HabitTracker.Application.Features.Habit.Validators
{
    public class CreateTaskRequestValidator : AbstractValidator<CreateHabitRequest>
    {
        public CreateTaskRequestValidator() 
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Frequency)
                .NotEmpty()
                .IsInEnum()
                .WithMessage("Frequency must be a valid enum value.");
        }
    }
}
