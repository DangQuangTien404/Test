using BLL.Services;
using DAL.Interfaces;
using DTOs.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace DataLabelingSupportSystem.Tests
{
    public class ProjectServiceTests
    {
        private readonly Mock<IProjectRepository> _projectRepoMock;
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IRepository<UserProjectStat>> _statsRepoMock;
        private readonly Mock<IRepository<Invoice>> _invoiceRepoMock;
        private readonly ProjectService _service;

        public ProjectServiceTests()
        {
            _projectRepoMock = new Mock<IProjectRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _statsRepoMock = new Mock<IRepository<UserProjectStat>>();
            _invoiceRepoMock = new Mock<IRepository<Invoice>>();

            _service = new ProjectService(
                _projectRepoMock.Object,
                _userRepoMock.Object,
                _statsRepoMock.Object,
                _invoiceRepoMock.Object
            );
        }

        [Fact]
        public async Task GetProjectStatisticsAsync_ShouldCalculateProgressCorrectly()
        {
            // Arrange
            var projectId = 1;
            var project = new Project
            {
                Id = projectId,
                Name = "Test Stats",
                DataItems = new List<DataItem>
                {
                    new DataItem { Status = "Completed" },
                    new DataItem { Status = "Done" },
                    new DataItem { Status = "Assigned" },
                    new DataItem { Status = "New" }
                }
            };

            _projectRepoMock.Setup(r => r.GetProjectWithStatsDataAsync(projectId))
                .ReturnsAsync(project);

            _statsRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<UserProjectStat>().AsQueryable());

            // Act
            var result = await _service.GetProjectStatisticsAsync(projectId);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(4);
            result.CompletedItems.Should().Be(2); // "Completed" and "Done"
            result.ProgressPercentage.Should().Be(50.0m); // 2/4 = 50%
        }
    }
}
