using DTOs.Requests;
using DTOs.Responses;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace DataLabelingSupportSystem.Tests
{
    public class ProjectIntegrationTests : BaseIntegrationTest
    {
        public ProjectIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateProject_ShouldSucceed_WhenUserIsManager()
        {
            // Arrange
            var email = $"manager_{Guid.NewGuid()}@test.com";
            await RegisterUserAsync("Manager Project", email, "Password123!", "Manager");
            var token = await AuthenticateAsync(email, "Password123!");
            AuthenticateClient(token);

            var createRequest = new CreateProjectRequest
            {
                Name = "Test Project",
                Description = "Integration Test",
                PricePerLabel = 0.5m,
                TotalBudget = 1000,
                Deadline = DateTime.UtcNow.AddMonths(1),
                LabelClasses = new List<LabelRequest>
                {
                    new LabelRequest { Name = "Car", Color = "#FF0000" }
                }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Project", createRequest);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<CreateProjectResponse>(_jsonOptions);
            result.Should().NotBeNull();
            result!.ProjectId.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CreateProject_ShouldFail_WhenUserIsAnnotator()
        {
            // Arrange
            var email = $"anno_{Guid.NewGuid()}@test.com";
            await RegisterUserAsync("Annotator User", email, "Password123!", "Annotator");
            var token = await AuthenticateAsync(email, "Password123!");
            AuthenticateClient(token);

            var createRequest = new CreateProjectRequest
            {
                Name = "Forbidden Project",
                PricePerLabel = 0.5m,
                TotalBudget = 1000,
                Deadline = DateTime.UtcNow.AddMonths(1)
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Project", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            error.Should().NotBeNull();
            error!.Message.Should().Contain("Only Manager or Admin");
        }

        private class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
        }

        [Fact]
        public async Task ImportData_ShouldIncreaseTotalItems()
        {
            // Arrange
            var email = $"manager_import_{Guid.NewGuid()}@test.com";
            await RegisterUserAsync("Manager Import", email, "Password123!", "Manager");
            var token = await AuthenticateAsync(email, "Password123!");
            AuthenticateClient(token);

            // Create Project
            var createRequest = new CreateProjectRequest
            {
                Name = "Import Test Project",
                PricePerLabel = 0.1m,
                TotalBudget = 100,
                Deadline = DateTime.UtcNow.AddDays(7)
            };
            var createResp = await _client.PostAsJsonAsync("/api/Project", createRequest);
            createResp.EnsureSuccessStatusCode();
            var projectData = await createResp.Content.ReadFromJsonAsync<CreateProjectResponse>(_jsonOptions);
            var projectId = projectData!.ProjectId;

            // Act
            var importRequest = new ImportDataRequest
            {
                StorageUrls = new List<string> { "http://img1.jpg", "http://img2.jpg" }
            };
            var importResp = await _client.PostAsJsonAsync($"/api/Project/{projectId}/import-data", importRequest);

            // Assert
            importResp.EnsureSuccessStatusCode();

            // Check Stats
            var statsResp = await _client.GetFromJsonAsync<ProjectStatisticsResponse>($"/api/Project/{projectId}/stats");
            statsResp.Should().NotBeNull();
            statsResp!.TotalItems.Should().Be(2);
        }

        private class CreateProjectResponse
        {
            public string Message { get; set; } = string.Empty;
            public int ProjectId { get; set; }
        }
    }
}
