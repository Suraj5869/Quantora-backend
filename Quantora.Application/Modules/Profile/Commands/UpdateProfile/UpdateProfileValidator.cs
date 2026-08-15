using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Profile.Commands.UpdateProfile
{
    public sealed class UpdateProfileValidator
    : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Full name is required.")
                .MaximumLength(150)
                .WithMessage("Full name cannot exceed 150 characters.")
                .Must(name => name.Trim().Length >= 2)
                .WithMessage("Full name must contain at least 2 characters.");
        }
    }
}
