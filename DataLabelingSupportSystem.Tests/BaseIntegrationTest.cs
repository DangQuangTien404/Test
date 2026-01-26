using DTOs.Requests;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DataLabelingSupportSystem.Tests
{
    public class BaseIntegrationTest : IClassFixture<CustomWebApplicationFactory>
    {
        protected readonly HttpClient _client;
        protected readonly CustomWebApplicationFactory _factory;
        protected readonly JsonSerializerOptions _jsonOptions;

        public BaseIntegrationTest(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        protected async Task<string> AuthenticateAsync(string email, string password)
        {
            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = password
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
            return result?.AccessToken ?? string.Empty;
        }

        protected async Task RegisterUserAsync(string fullName, string email, string password, string role)
        {
            var request = new RegisterRequest
            {
                FullName = fullName,
                Email = email,
                Password = password,
                Role = role
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/register", request);
            // We don't throw here to allow tests to assert on failure,
            // but helper is mostly for setup so usually we expect success.
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to register user {email}: {error}");
            }
        }

        protected void AuthenticateClient(string token)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        protected class LoginResponse
        {
            public string Message { get; set; } = string.Empty;
            public string AccessToken { get; set; } = string.Empty;
            public string TokenType { get; set; } = string.Empty;
        }
    }
}
