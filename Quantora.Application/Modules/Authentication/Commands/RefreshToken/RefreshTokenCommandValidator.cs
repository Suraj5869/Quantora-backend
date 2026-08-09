using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Commands.RefreshToken
{
    public sealed class RefreshTokenCommandValidator
     : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .MaximumLength(500);
        }
    }
}
