using DTOs.Requests;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace DataLabelingSupportSystem.Tests
{
    public class AuthIntegrationTests : BaseIntegrationTest
    {
        public AuthIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task Register_ShouldCreateUser_WhenDataIsValid()
        {
            // Arrange
            var email = $"user_{Guid.NewGuid()}@test.com";
            var password = "Password123!";
            var fullName = "Test User";

            // Act
            await RegisterUserAsync(fullName, email, password, "Annotator");

            // Assert
            var token = await AuthenticateAsync(email, password);
            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_ShouldFail_WhenCredentialsAreInvalid()
        {
            // Arrange
            var email = $"user_{Guid.NewGuid()}@test.com";
            var password = "Password123!";
            await RegisterUserAsync("Test User", email, password, "Annotator");

            // Act
            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = "WrongPassword!"
            };
            var response = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateProfile_ShouldUpdateName_WhenAuthorized()
        {
            // Arrange
            var email = $"manager_{Guid.NewGuid()}@test.com";
            var password = "Password123!";
            await RegisterUserAsync("Manager One", email, password, "Manager");

            var token = await AuthenticateAsync(email, password);
            AuthenticateClient(token);

            var updateRequest = new UpdateUserRequest
            {
                FullName = "Manager Updated"
            };

            // Act
            var response = await _client.PutAsJsonAsync("/api/User/profile", updateRequest);

            // Assert
            response.EnsureSuccessStatusCode();

            var profileResponse = await _client.GetFromJsonAsync<UserProfileResponse>("/api/User/profile");
            profileResponse.Should().NotBeNull();
            profileResponse!.FullName.Should().Be("Manager Updated");
        }

        private class UserProfileResponse
        {
            public string Id { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }
    }
}
