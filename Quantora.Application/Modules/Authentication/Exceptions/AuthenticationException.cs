using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Exceptions
{
    public sealed class AuthenticationException : Exception
    {
        public AuthenticationException(string message)
            : base(message)
        {
        }
    }
}
