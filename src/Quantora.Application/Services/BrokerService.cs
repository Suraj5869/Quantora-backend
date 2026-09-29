using Microsoft.Extensions.Options;
using Quantora.Application.Common.Interfaces;
using Quantora.Application.Configurations;
using Quantora.Application.DTOs.Broker;
using Quantora.Application.Interfaces;
using Quantora.Domain.Entities;
using System.Security.Cryptography;

namespace Quantora.Application.Services;

public sealed class BrokerService : IBrokerService
{
    private readonly IBrokerProvider _brokerProvider;
    private readonly IUpstoxClient _upstoxClient;
    private readonly IBrokerConnectionRepository _connectionRepository;
    private readonly IBrokerOAuthStateRepository _oauthStateRepository;
    private readonly ISecretProtector _secretProtector;
    private readonly ICurrentUserService _currentUserService;
    private readonly UpstoxSettings _settings;

    public BrokerService(
    IUpstoxClient upstoxClient,
    IBrokerConnectionRepository connectionRepository,
    IBrokerOAuthStateRepository oauthStateRepository,
    ISecretProtector secretProtector,
    ICurrentUserService currentUserService,
    IOptions<UpstoxSettings> options)
    {
        _upstoxClient = upstoxClient;
        _connectionRepository = connectionRepository;
        _oauthStateRepository = oauthStateRepository;
        _secretProtector = secretProtector;
        _currentUserService = currentUserService;
        _settings = options.Value;
    }

    public async Task<BrokerConnectionResponse> GetConnectionAsync(
    CancellationToken cancellationToken = default)
    {
        var connection =
            await _connectionRepository.GetAsync(
                _currentUserService.UserId,
                "Upstox",
                cancellationToken);

        return new BrokerConnectionResponse
        {
            Broker = "Upstox",
            IsConnected =
                connection is not null &&
                connection.IsActive,
            IsSandbox = _settings.UseSandbox,
            Environment = _settings.UseSandbox
                ? "Sandbox"
                : "Production",
            CheckedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<string> GetAuthorizationUrlAsync(
     CancellationToken cancellationToken = default)
    {
        var state =
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32));

        var stateHash =
            Convert.ToHexString(
                SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(state)));

        await _oauthStateRepository.CreateAsync(
            _currentUserService.UserId,
            "Upstox",
            stateHash,
            DateTimeOffset.UtcNow.AddMinutes(10),
            cancellationToken);

        var query = string.Join(
            "&",
            new Dictionary<string, string>
            {
                ["client_id"] = _settings.ClientId,
                ["redirect_uri"] = _settings.RedirectUri,
                ["response_type"] = "code",
                ["state"] = state
            }
            .Select(x =>
                $"{Uri.EscapeDataString(x.Key)}=" +
                $"{Uri.EscapeDataString(x.Value)}"));

        return
            $"https://api.upstox.com/v2/login/authorization/dialog?{query}";
    }

    public async Task DisconnectUpstoxAsync(
    CancellationToken cancellationToken = default)
    {
        await _connectionRepository.DisconnectAsync(
            _currentUserService.UserId,
            "Upstox",
            cancellationToken);
    }

    public Task<UpstoxOrderResponse> PlaceSandboxOrderAsync(
        UpstoxPlaceOrderRequest request,
        CancellationToken cancellationToken = default) =>
        _upstoxClient.PlaceSandboxOrderAsync(
            request,
            cancellationToken);

    public Task<UpstoxOrderDetailsResponse> GetOrderDetailsAsync(
        string orderId,
        CancellationToken cancellationToken = default) =>
        _upstoxClient.GetOrderDetailsAsync(
            orderId,
            cancellationToken);

    public Task<UpstoxModifyOrderResponse> ModifySandboxOrderAsync(
        UpstoxModifyOrderRequest request,
        CancellationToken cancellationToken = default) =>
        _upstoxClient.ModifySandboxOrderAsync(
            request,
            cancellationToken);

    public Task<UpstoxCancelOrderResponse> CancelSandboxOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default) =>
        _upstoxClient.CancelSandboxOrderAsync(
            orderId,
            cancellationToken);

    public async Task<UpstoxOAuthCallbackResponse>
    HandleOAuthCallbackAsync(
        string code,
        string state,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException(
                "Authorization code is required.");

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException(
                "OAuth state is required.");

        var stateHash =
            Convert.ToHexString(
                SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(state)));

        var userId =
            await _oauthStateRepository.ConsumeAsync(
                stateHash,
                cancellationToken);

        if (userId is null)
            throw new UnauthorizedAccessException(
                "Invalid or expired OAuth state.");

        var token =
            await _upstoxClient.ExchangeAuthorizationCodeAsync(
                code,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var connection = new BrokerConnection
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Broker = "Upstox",
            BrokerUserId = token.UserId,
            AccessTokenEncrypted =
                _secretProtector.Protect(
                    token.AccessToken),
            ExtendedTokenEncrypted =
                string.IsNullOrWhiteSpace(token.ExtendedToken)
                    ? null
                    : _secretProtector.Protect(
                        token.ExtendedToken),
            IsActive = token.IsActive,
            ConnectedAt = now,
            UpdatedAt = now
        };

        await _connectionRepository.UpsertAsync(
            connection,
            cancellationToken);

        return new UpstoxOAuthCallbackResponse
        {
            Success = true,
            Broker = "Upstox",
            UpstoxUserId = token.UserId,
            UserName = token.UserName,
            Message = "Upstox account connected successfully."
        };
    }
}
