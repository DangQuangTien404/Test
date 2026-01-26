using BLL.Services;
using DAL.Interfaces;
using DTOs.Entities;
using DTOs.Requests;
using FluentAssertions;
using Moq;
using Xunit;

namespace DataLabelingSupportSystem.Tests
{
    public class TaskServiceTests
    {
        private readonly Mock<IAssignmentRepository> _assignmentRepoMock;
        private readonly Mock<IRepository<DataItem>> _dataItemRepoMock;
        private readonly Mock<IRepository<Annotation>> _annotationRepoMock;
        private readonly Mock<IRepository<UserProjectStat>> _statsRepoMock;
        private readonly TaskService _service;

        public TaskServiceTests()
        {
            _assignmentRepoMock = new Mock<IAssignmentRepository>();
            _dataItemRepoMock = new Mock<IRepository<DataItem>>();
            _annotationRepoMock = new Mock<IRepository<Annotation>>();
            _statsRepoMock = new Mock<IRepository<UserProjectStat>>();

            _service = new TaskService(
                _assignmentRepoMock.Object,
                _dataItemRepoMock.Object,
                _annotationRepoMock.Object,
                _statsRepoMock.Object
            );
        }

        [Fact]
        public async Task SubmitTaskAsync_ShouldThrow_WhenUserIsNotAnnotator()
        {
            // Arrange
            var assignmentId = 1;
            var userId = "user1";
            var annotatorId = "user2"; // Different user

            var assignment = new Assignment
            {
                Id = assignmentId,
                AnnotatorId = annotatorId,
                Status = "InProgress"
            };

            _assignmentRepoMock.Setup(r => r.GetAssignmentWithDetailsAsync(assignmentId))
                .ReturnsAsync(assignment);

            var request = new SubmitAnnotationRequest
            {
                AssignmentId = assignmentId,
                DataJSON = "{}"
            };

            // Act
            Func<Task> act = async () => await _service.SubmitTaskAsync(userId, request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task SubmitTaskAsync_ShouldUpdateStatusToSubmitted_WhenValid()
        {
            // Arrange
            var assignmentId = 1;
            var userId = "user1";
            var assignment = new Assignment
            {
                Id = assignmentId,
                AnnotatorId = userId,
                Status = "InProgress",
                Annotations = new List<Annotation>()
            };

            _assignmentRepoMock.Setup(r => r.GetAssignmentWithDetailsAsync(assignmentId))
                .ReturnsAsync(assignment);

            var request = new SubmitAnnotationRequest
            {
                AssignmentId = assignmentId,
                DataJSON = "{\"final\":true}"
            };

            // Act
            await _service.SubmitTaskAsync(userId, request);

            // Assert
            assignment.Status.Should().Be("Submitted");
            assignment.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            _assignmentRepoMock.Verify(r => r.Update(assignment), Times.Once);
            _assignmentRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _annotationRepoMock.Verify(r => r.AddAsync(It.IsAny<Annotation>()), Times.Once);
        }
    }
}
