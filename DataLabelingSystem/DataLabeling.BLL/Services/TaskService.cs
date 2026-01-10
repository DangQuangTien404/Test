using DataLabeling.Core.DTOs;
using DataLabeling.Core.Entities;
using DataLabeling.Core.Enums;
using DataLabeling.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataLabeling.BLL.Services
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityLogService _logService;
        private readonly INotificationService _notificationService; 

        public TaskService(IUnitOfWork unitOfWork, IActivityLogService logService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
            _notificationService = notificationService;
        }

        public async Task AssignTasksAsync(AssignTaskDto dto)
        {
            var annotator = await _unitOfWork.Repository<User>().GetByIdAsync(dto.AnnotatorId);
            if (annotator == null) throw new Exception("Annotator không tồn tại");

            foreach (var taskId in dto.TaskIds)
            {
                var task = await _unitOfWork.Repository<LabelTask>().GetByIdAsync(taskId);
                if (task != null)
                {
                    if (task.Status == ProjectTaskStatus.Approved)
                    {
                        throw new Exception($"Task ID {taskId} đã hoàn thành, không thể phân công lại.");
                    }

                    task.AnnotatorId = dto.AnnotatorId;
                    task.Status = ProjectTaskStatus.New;
                    _unitOfWork.Repository<LabelTask>().Update(task);
                }
            }
            await _unitOfWork.CompleteAsync();

            await _notificationService.NotifyUserAsync(dto.AnnotatorId.ToString(), $"Bạn vừa được phân công {dto.TaskIds.Count} nhiệm vụ mới.");
        }

        public async Task SubmitLabelAsync(SubmitLabelDto dto)
        {
            var task = await _unitOfWork.Repository<LabelTask>().GetByIdAsync(dto.TaskId);
            if (task == null) throw new Exception("Nhiệm vụ không tồn tại");

            var dataItem = await _unitOfWork.Repository<DataItem>().GetByIdAsync(task.DataItemId);

            if (task.Status == ProjectTaskStatus.Approved)
                throw new Exception("Nhiệm vụ này đã được duyệt, không thể sửa đổi.");

            task.LabelData = dto.LabelData;
            task.Status = ProjectTaskStatus.Submitted;
            task.LastUpdated = DateTime.Now;

            _unitOfWork.Repository<LabelTask>().Update(task);
            await _unitOfWork.CompleteAsync();

            if (dataItem != null)
            {
                await _notificationService.NotifyProjectUpdateAsync(dataItem.ProjectId);
            }
        }

        public async Task ReviewTaskAsync(ReviewTaskDto dto)
        {
            var task = await _unitOfWork.Repository<LabelTask>().GetByIdAsync(dto.TaskId);
            if (task == null) throw new Exception("Task không tồn tại");

            var dataItem = await _unitOfWork.Repository<DataItem>().GetByIdAsync(task.DataItemId);

            if (task.Status == ProjectTaskStatus.New || task.Status == ProjectTaskStatus.InProgress)
                throw new Exception("Annotator chưa nộp bài, không thể chấm điểm.");

            string notificationMsg = "";
            if (dto.IsApproved)
            {
                task.Status = ProjectTaskStatus.Approved;
                task.ReviewerComment = string.IsNullOrEmpty(dto.Comment) ? "Đã duyệt" : dto.Comment;
                task.ErrorType = ErrorType.None;
                notificationMsg = $"Task {task.Id} của bạn đã được duyệt.";
            }
            else
            {
                task.Status = ProjectTaskStatus.Rejected;
                task.ReviewerComment = dto.Comment;
                task.ErrorType = dto.ErrorType ?? ErrorType.Other;

                notificationMsg = $"Task {task.Id} bị từ chối. Lý do: {dto.Comment}";
            }

            task.LastUpdated = DateTime.Now;
            _unitOfWork.Repository<LabelTask>().Update(task);
            await _unitOfWork.CompleteAsync();

            string action = dto.IsApproved ? "Approve" : "Reject";
            await _logService.LogAsync(null, action, "Task", task.Id.ToString(), $"Reviewed task {task.Id}");

            if (task.AnnotatorId.HasValue)
            {
                await _notificationService.NotifyUserAsync(task.AnnotatorId.Value.ToString(), notificationMsg);
            }

            if (dataItem != null)
            {
                await _notificationService.NotifyProjectUpdateAsync(dataItem.ProjectId);
            }
        }

        public async Task<IEnumerable<TaskViewDto>> GetTasksByAnnotatorAsync(int annotatorId)
        {
            var tasks = await _unitOfWork.Repository<LabelTask>()
               .FindAsync(t => t.AnnotatorId == annotatorId);

            var result = new List<TaskViewDto>();

            foreach (var t in tasks)
            {
                var dataItem = await _unitOfWork.Repository<DataItem>().GetByIdAsync(t.DataItemId);
                if (dataItem != null)
                {
                    var project = await _unitOfWork.Repository<Project>().GetByIdAsync(dataItem.ProjectId);

                    result.Add(new TaskViewDto
                    {
                        Id = t.Id,
                        DataUrl = dataItem.DataUrl,
                        Status = t.Status.ToString(),
                        LabelData = t.LabelData,
                        ProjectName = project?.Name ?? $"Project {dataItem.ProjectId}",
                        Instruction = project?.Instruction ?? "Không có hướng dẫn",
                        LabelConfig = project?.LabelConfig ?? "",
                        ReviewerComment = t.ReviewerComment,
                        ErrorType = t.ErrorType.HasValue ? t.ErrorType.Value.ToString() : ""
                    });
                }
            }
            return result;
        }

        public async Task<IEnumerable<TaskViewDto>> GetSubmittedTasksAsync()
        {
            var tasks = await _unitOfWork.Repository<LabelTask>()
                .FindAsync(t => t.Status == ProjectTaskStatus.Submitted);

            var result = new List<TaskViewDto>();
            foreach (var t in tasks)
            {
                var dataItem = await _unitOfWork.Repository<DataItem>().GetByIdAsync(t.DataItemId);
                result.Add(new TaskViewDto
                {
                    Id = t.Id,
                    DataUrl = dataItem?.DataUrl ?? "N/A",
                    ProjectName = "Project " + dataItem?.ProjectId,
                    Status = t.Status.ToString(),
                    LabelData = t.LabelData
                });
            }
            return result;
        }
    }
}