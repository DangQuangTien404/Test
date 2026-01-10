using DataLabeling.Core.DTOs;
using DataLabeling.Core.Entities;
using DataLabeling.Core.Enums;
using DataLabeling.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataLabeling.BLL.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityLogService _logService;
        private readonly INotificationService _notificationService; 

        public ProjectService(IUnitOfWork unitOfWork, IActivityLogService logService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
            _notificationService = notificationService;
        }

        public async Task<ProjectViewDto> CreateProjectAsync(CreateProjectDto dto)
        {
            var manager = await _unitOfWork.Repository<User>().GetByIdAsync(dto.ManagerId);
            if (manager == null) throw new Exception("Người quản lý (ManagerId) không tồn tại.");

            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                Instruction = dto.Instruction,
                LabelConfig = dto.LabelConfig,
                ManagerId = dto.ManagerId,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.Repository<Project>().AddAsync(project);
            await _unitOfWork.CompleteAsync();
            await _logService.LogAsync(dto.ManagerId, "Create", "Project", project.Id.ToString(), $"Created project {project.Name}");
            await _notificationService.NotifyProjectListUpdateAsync();

            return new ProjectViewDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                ManagerName = manager.FullName,
                TotalImages = 0
            };
        }

        public async Task<int> AddDataItemsAsync(AddDataItemDto dto)
        {
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(dto.ProjectId);
            if (project == null) throw new Exception("Dự án không tồn tại.");
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var dataItems = new List<DataItem>();

                foreach (var url in dto.ImageUrls)
                {
                    var item = new DataItem
                    {
                        DataUrl = url,
                        FileName = System.IO.Path.GetFileName(url),
                        ProjectId = dto.ProjectId
                    };
                    dataItems.Add(item);
                    await _unitOfWork.Repository<DataItem>().AddAsync(item);
                }
                await _unitOfWork.CompleteAsync();
                foreach (var item in dataItems)
                {
                    var task = new LabelTask
                    {
                        DataItemId = item.Id,
                        Status = ProjectTaskStatus.New,
                        AnnotatorId = null,
                        LabelData = null 
                    };
                    await _unitOfWork.Repository<LabelTask>().AddAsync(task);
                }
                await _unitOfWork.CompleteAsync();

                await _unitOfWork.CommitTransactionAsync();

                await _notificationService.NotifyProjectUpdateAsync(dto.ProjectId);

                return dataItems.Count;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ProjectViewDto>> GetProjectsByManagerAsync(int managerId)
        {
            return await _unitOfWork.Repository<Project>()
                .AsQueryable()
                .Include(p => p.Manager)
                .Include(p => p.DataItems)
                .Where(p => p.ManagerId == managerId)
                .Select(p => new ProjectViewDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    ManagerName = p.Manager.FullName,
                    TotalImages = p.DataItems.Count()
                })
                .ToListAsync();
        }

        public async Task<ProjectProgressDto> GetProjectProgressAsync(int projectId)
        {
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(projectId);
            if (project == null) throw new Exception("Dự án không tồn tại.");
            var dataItems = await _unitOfWork.Repository<DataItem>()
                                          .AsQueryable()
                                          .Include(d => d.LabelTask)
                                          .Where(d => d.ProjectId == projectId)
                                          .ToListAsync();

            var stats = new ProjectProgressDto
            {
                ProjectId = projectId,
                TotalItems = dataItems.Count,
                NewItems = dataItems.Count(d => d.LabelTask == null || d.LabelTask.Status == ProjectTaskStatus.New),
                InProgressItems = dataItems.Count(d => d.LabelTask != null && d.LabelTask.Status == ProjectTaskStatus.InProgress),
                SubmittedItems = dataItems.Count(d => d.LabelTask != null && d.LabelTask.Status == ProjectTaskStatus.Submitted),
                RejectedItems = dataItems.Count(d => d.LabelTask != null && d.LabelTask.Status == ProjectTaskStatus.Rejected),
                ApprovedItems = dataItems.Count(d => d.LabelTask != null && d.LabelTask.Status == ProjectTaskStatus.Approved)
            };

            if (stats.TotalItems > 0)
            {
                stats.PercentComplete = Math.Round((double)stats.ApprovedItems / stats.TotalItems * 100, 2);
            }

            return stats;
        }

        public async Task<IEnumerable<ExportDataItemDto>> ExportApprovedDataAsync(int projectId)
        {
            var query = _unitOfWork.Repository<DataItem>()
                        .AsQueryable()
                        .Include(d => d.LabelTask)
                            .ThenInclude(t => t.Annotator)
                        .Where(d => d.ProjectId == projectId && d.LabelTask.Status == ProjectTaskStatus.Approved);

            var result = await query.Select(d => new ExportDataItemDto
            {
                Id = d.Id,
                FileName = d.FileName,
                Url = d.DataUrl,
                LabelData = d.LabelTask.LabelData,
                AnnotatorName = d.LabelTask.Annotator != null ? d.LabelTask.Annotator.FullName : "Unknown",
                ReviewerComment = d.LabelTask.ReviewerComment
            }).ToListAsync();

            return result;
        }
    }
}