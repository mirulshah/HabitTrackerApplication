using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace HabitTracker.Application.Features.Habit.Validators
{
    public class CreateHabitRequestValidator : AbstractValidator<CreateHabitRequest>
    {
        public CreateHabitRequestValidator() 
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
