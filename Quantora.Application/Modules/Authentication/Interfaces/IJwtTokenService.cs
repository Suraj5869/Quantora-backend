using System;
using System.Collections.Generic;
using System.Text;

namespace Quantora.Application.Modules.Authentication.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(Guid userId, string email);

        string GenerateRefreshToken();

        string HashRefreshToken(string refreshToken);
    }
}
