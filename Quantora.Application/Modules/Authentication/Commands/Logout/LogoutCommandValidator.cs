using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Commands.Logout
{
    public sealed class LogoutCommandValidator
     : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .MaximumLength(500);
        }
    }
}
