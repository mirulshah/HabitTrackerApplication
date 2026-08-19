using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace HabitTracker.Application.Features.Tasks.Validators
{
    public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
    {
        public UpdateTaskStatusRequestValidator() 
        {
            RuleFor(x => x.IsCompleted)
                .NotNull()
                .WithMessage("IsCompleted field is required.");
        }
    }
}
