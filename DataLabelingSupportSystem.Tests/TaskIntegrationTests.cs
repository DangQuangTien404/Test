using DTOs.Requests;
using DTOs.Responses;
using FluentAssertions;
using System.Net.Http.Json;
using Xunit;

namespace DataLabelingSupportSystem.Tests
{
    public class TaskIntegrationTests : BaseIntegrationTest
    {
        public TaskIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task FullTaskWorkflow_ShouldSucceed()
        {
            // 1. Setup Users
            var managerEmail = $"mgr_task_{Guid.NewGuid()}@test.com";
            var annotatorEmail = $"anno_task_{Guid.NewGuid()}@test.com";
            var reviewerEmail = $"rev_task_{Guid.NewGuid()}@test.com";
            var password = "Password123!";

            await RegisterUserAsync("Manager Task", managerEmail, password, "Manager");
            await RegisterUserAsync("Annotator Task", annotatorEmail, password, "Annotator");
            await RegisterUserAsync("Reviewer Task", reviewerEmail, password, "Reviewer");

            // Get Annotator ID
            var annotatorToken = await AuthenticateAsync(annotatorEmail, password);
            AuthenticateClient(annotatorToken);
            var annotatorProfile = await _client.GetFromJsonAsync<UserProfileResponse>("/api/User/profile");
            var annotatorId = annotatorProfile!.Id;

            // 2. Manager Creates Project & Imports Data
            var managerToken = await AuthenticateAsync(managerEmail, password);
            AuthenticateClient(managerToken);

            var createProjectRequest = new CreateProjectRequest
            {
                Name = "Task Workflow Project",
                PricePerLabel = 1.0m,
                TotalBudget = 500,
                Deadline = DateTime.UtcNow.AddMonths(1)
            };
            var createProjResp = await _client.PostAsJsonAsync("/api/Project", createProjectRequest);
            createProjResp.EnsureSuccessStatusCode();
            var projectData = await createProjResp.Content.ReadFromJsonAsync<CreateProjectResponse>(_jsonOptions);
            var projectId = projectData!.ProjectId;

            var importRequest = new ImportDataRequest
            {
                StorageUrls = new List<string> { "http://img1.jpg", "http://img2.jpg" }
            };
            await _client.PostAsJsonAsync($"/api/Project/{projectId}/import-data", importRequest);

            // 3. Manager Assigns Task to Annotator
            var assignRequest = new AssignTaskRequest
            {
                ProjectId = projectId,
                AnnotatorId = annotatorId,
                Quantity = 1
            };
            var assignResp = await _client.PostAsJsonAsync("/api/Task/assign", assignRequest);
            assignResp.EnsureSuccessStatusCode();

            // 4. Annotator Gets Tasks
            AuthenticateClient(annotatorToken);
            var tasksResp = await _client.GetFromJsonAsync<List<AssignmentResponse>>($"/api/Task/project/{projectId}/images");
            tasksResp.Should().NotBeNullOrEmpty();
            var assignment = tasksResp!.First();
            var assignmentId = assignment.Id;
            assignment.Status.Should().Be("Assigned");

            // 5. Annotator Saves Draft
            var draftRequest = new SubmitAnnotationRequest
            {
                AssignmentId = assignmentId,
                DataJSON = "{\"draft\": true}"
            };
            var draftResp = await _client.PostAsJsonAsync("/api/Task/save-draft", draftRequest);
            draftResp.EnsureSuccessStatusCode();

            // Verify status is InProgress (Check via GetTasks again)
            tasksResp = await _client.GetFromJsonAsync<List<AssignmentResponse>>($"/api/Task/project/{projectId}/images");
            tasksResp!.First(t => t.Id == assignmentId).Status.Should().Be("InProgress");

            // 6. Annotator Submits
            var submitRequest = new SubmitAnnotationRequest
            {
                AssignmentId = assignmentId,
                DataJSON = "{\"final\": true}"
            };
            var submitResp = await _client.PostAsJsonAsync("/api/Task/submit", submitRequest);
            submitResp.EnsureSuccessStatusCode();

            // Verify status is Submitted
            tasksResp = await _client.GetFromJsonAsync<List<AssignmentResponse>>($"/api/Task/project/{projectId}/images");
            tasksResp!.First(t => t.Id == assignmentId).Status.Should().Be("Submitted");

            // 7. Reviewer Approves
            var reviewerToken = await AuthenticateAsync(reviewerEmail, password);
            AuthenticateClient(reviewerToken);

            var reviewRequest = new ReviewRequest
            {
                AssignmentId = assignmentId,
                IsApproved = true,
                Comment = "Good job"
            };
            var reviewResp = await _client.PostAsJsonAsync("/api/Review", reviewRequest);
            reviewResp.EnsureSuccessStatusCode();

            // 8. Verify Stats (Manager)
            AuthenticateClient(managerToken);
            var statsResp = await _client.GetFromJsonAsync<ProjectStatisticsResponse>($"/api/Project/{projectId}/stats");
            statsResp!.CompletedItems.Should().Be(1);
            statsResp.ApprovedAssignments.Should().Be(1);
        }

        private class UserProfileResponse
        {
            public string Id { get; set; } = string.Empty;
        }

        private class CreateProjectResponse
        {
            public int ProjectId { get; set; }
        }
    }
}
