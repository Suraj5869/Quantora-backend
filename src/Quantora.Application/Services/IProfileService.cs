using Quantora.Application.DTOs.Profile;
namespace Quantora.Application.Services;
public interface IProfileService
{
    Task<ProfileResponse> GetAsync(CancellationToken cancellationToken = default);
    Task<ProfileResponse> UpdateAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
