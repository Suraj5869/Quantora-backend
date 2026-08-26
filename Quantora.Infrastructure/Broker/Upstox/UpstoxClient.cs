using Microsoft.Extensions.Options;
using Quantora.Application.Configurations;
using Quantora.Application.Modules.Broker.DTOs;
using Quantora.Application.Modules.Broker.Interfaces;
using Quantora.Infrastructure.Broker.Upstox.Exceptions;
using Quantora.Infrastructure.Broker.Upstox.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Quantora.Infrastructure.Broker.Upstox
{
    public sealed class UpstoxClient : IUpstoxClient
    {
        private const string SandboxBaseUrl =
            "https://api-sandbox.upstox.com";

        private readonly HttpClient _httpClient;
        private readonly UpstoxSettings _settings;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public UpstoxClient(
            HttpClient httpClient,
            IOptions<UpstoxSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<UpstoxOrderResponse> PlaceSandboxOrderAsync(
            UpstoxPlaceOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.UseSandbox)
            {
                throw new InvalidOperationException(
                    "Sandbox order operation is disabled when UseSandbox is false.");
            }

            if (string.IsNullOrWhiteSpace(
                _settings.SandboxAccessToken))
            {
                throw new InvalidOperationException(
                    "Upstox Sandbox access token is not configured.");
            }

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{SandboxBaseUrl}/v3/order/place");

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _settings.SandboxAccessToken);

            httpRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            var payload = new
            {
                quantity = request.Quantity,
                product = request.Product,
                validity = request.Validity,
                price = request.Price,
                tag = request.Tag,
                instrument_token = request.InstrumentToken,
                order_type = request.OrderType,
                transaction_type = request.TransactionType,
                disclosed_quantity = request.DisclosedQuantity,
                trigger_price = request.TriggerPrice,
                is_amo = request.IsAmo,
                slice = request.Slice
            };

            httpRequest.Content = JsonContent.Create(payload);

            using var response = await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            UpstoxApiResponse<UpstoxPlaceOrderData>? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<
                        UpstoxApiResponse<UpstoxPlaceOrderData>>(
                        responseBody,
                        JsonOptions);
            }
            catch (JsonException)
            {
                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    "Invalid response received from Upstox.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage =
                    result?.Errors?
                        .FirstOrDefault()?
                        .Message
                    ?? "Upstox API request failed.";

                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    errorMessage);
            }

            if (result?.Data is null)
            {
                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    "Upstox returned an empty response.");
            }

            return new UpstoxOrderResponse
            {
                OrderId = result.Data.OrderIds[0],
                Message = "Sandbox order placed successfully.",
                Status = result.Status
            };
        }

        public async Task<UpstoxOrderDetailsResponse> GetOrderDetailsAsync(
    string orderId,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new ArgumentException(
                    "Order ID is required.",
                    nameof(orderId));
            }

            if (!_settings.UseSandbox)
            {
                throw new InvalidOperationException(
                    "Sandbox order operation is disabled when UseSandbox is false.");
            }

            if (string.IsNullOrWhiteSpace(
                _settings.SandboxAccessToken))
            {
                throw new InvalidOperationException(
                    "Upstox Sandbox access token is not configured.");
            }

            var url =
                $"{SandboxBaseUrl}/v2/order/details?order_id={Uri.EscapeDataString(orderId)}";

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Get,
                url);

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _settings.SandboxAccessToken);

            httpRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            using var response = await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            UpstoxApiResponse<UpstoxOrderDetailsItem>? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<
                        UpstoxApiResponse<UpstoxOrderDetailsItem>>(
                        responseBody,
                        JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    $"Invalid response received from Upstox. " +
                    $"Response: {responseBody}. " +
                    $"Error: {ex.Message}");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage =
                    result?.Errors?
                        .FirstOrDefault()?
                        .Message
                    ?? "Unable to retrieve order details from Upstox.";

                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    errorMessage);
            }

            var order = result?.Data;

            if (order is null)
            {
                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    "Upstox did not return order details.");
            }

            DateTimeOffset? orderTimestamp = null;

            if (DateTimeOffset.TryParse(
                order.OrderTimestamp,
                out var parsedTimestamp))
            {
                orderTimestamp = parsedTimestamp;
            }

            return new UpstoxOrderDetailsResponse
            {
                OrderId = order.OrderId,
                InstrumentToken = order.InstrumentToken,
                Exchange = order.Exchange,
                Product = order.Product,
                TransactionType = order.TransactionType,
                OrderType = order.OrderType,
                Validity = order.Validity,
                Quantity = order.Quantity,
                DisclosedQuantity = order.DisclosedQuantity,
                Price = order.Price,
                AveragePrice = order.AveragePrice,
                Status = order.Status,
                StatusMessage = order.StatusMessage,
                Tag = order.Tag,
                OrderTimestamp = orderTimestamp
            };
        }

        public async Task<UpstoxModifyOrderResponse> ModifySandboxOrderAsync(
    UpstoxModifyOrderRequest request,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.OrderId))
            {
                throw new ArgumentException(
                    "Order ID is required.",
                    nameof(request.OrderId));
            }

            if (request.Quantity <= 0)
            {
                throw new ArgumentException(
                    "Quantity must be greater than zero.",
                    nameof(request.Quantity));
            }

            if (request.Price < 0)
            {
                throw new ArgumentException(
                    "Price cannot be negative.",
                    nameof(request.Price));
            }

            if (!_settings.UseSandbox)
            {
                throw new InvalidOperationException(
                    "Sandbox order operation is disabled when UseSandbox is false.");
            }

            if (string.IsNullOrWhiteSpace(
                _settings.SandboxAccessToken))
            {
                throw new InvalidOperationException(
                    "Upstox Sandbox access token is not configured.");
            }

            var url =
                $"{SandboxBaseUrl}/v3/order/modify";

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Put,
                url);

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _settings.SandboxAccessToken);

            httpRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            var payload = new
            {
                quantity = request.Quantity,
                validity = request.Validity,
                price = request.Price,
                order_id = request.OrderId,
                order_type = request.OrderType,
                disclosed_quantity = request.DisclosedQuantity,
                trigger_price = request.TriggerPrice
            };

            httpRequest.Content = JsonContent.Create(payload);

            using var response = await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            UpstoxApiResponse<UpstoxModifyOrderData>? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<
                        UpstoxApiResponse<UpstoxModifyOrderData>>(
                        responseBody,
                        JsonOptions);
            }
            catch (JsonException)
            {
                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    $"Invalid response received from Upstox. " +
        $"Response: {responseBody}");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage =
                    result?.Errors?
                        .FirstOrDefault()?
                        .Message
                    ?? "Unable to modify Upstox order.";

                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    errorMessage);
            }

            if (result?.Data is null)
            {
                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    "Upstox returned an empty modify-order response.");
            }

            return new UpstoxModifyOrderResponse
            {
                OrderId = result.Data.OrderId,
                Status = result.Status,
                Message = "Sandbox order modified successfully."
            };
        }

        public async Task<UpstoxCancelOrderResponse> CancelSandboxOrderAsync(
    string orderId,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new ArgumentException(
                    "Order ID is required.",
                    nameof(orderId));
            }

            if (!_settings.UseSandbox)
            {
                throw new InvalidOperationException(
                    "Sandbox order operation is disabled when UseSandbox is false.");
            }

            if (string.IsNullOrWhiteSpace(_settings.SandboxAccessToken))
            {
                throw new InvalidOperationException(
                    "Upstox Sandbox access token is not configured.");
            }

            var url =
                $"{SandboxBaseUrl}/v3/order/cancel" +
                $"?order_id={Uri.EscapeDataString(orderId)}";

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Delete,
                url);

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _settings.SandboxAccessToken);

            httpRequest.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            using var response = await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage;

                try
                {
                    var errorResponse =
                        JsonSerializer.Deserialize<
                            UpstoxApiResponse<UpstoxCancelOrderData>>(
                            responseBody,
                            JsonOptions);

                    errorMessage =
                        errorResponse?.Errors?
                            .FirstOrDefault()?
                            .Message
                        ?? responseBody;
                }
                catch (JsonException)
                {
                    errorMessage = responseBody;
                }

                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    $"Upstox cancel order failed: {errorMessage}");
            }

            UpstoxApiResponse<UpstoxCancelOrderData>? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<
                        UpstoxApiResponse<UpstoxCancelOrderData>>(
                        responseBody,
                        JsonOptions);
            }
            catch (JsonException)
            {
                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    $"Invalid response received from Upstox: {responseBody}");
            }

            if (result?.Data is null)
            {
                throw new UpstoxApiException(
                    (int)response.StatusCode,
                    "Upstox returned an empty cancel-order response.");
            }

            return new UpstoxCancelOrderResponse
            {
                OrderId = result.Data.OrderId,
                Status = result.Status,
                Message = "Sandbox order cancelled successfully."
            };
        }
    }
}
