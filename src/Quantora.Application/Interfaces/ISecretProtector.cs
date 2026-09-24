using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Interfaces
{
    public interface ISecretProtector
    {
        string Protect(string value);

        string Unprotect(string protectedValue);
    }
}
