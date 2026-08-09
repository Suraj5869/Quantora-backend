using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Commands.Login
{
    public sealed class LoginCommandValidator
    : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}
