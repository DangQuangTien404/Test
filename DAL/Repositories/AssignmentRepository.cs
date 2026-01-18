using DAL.Interfaces;
using DTOs.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
    {
        public AssignmentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<List<Assignment>> GetAssignmentsByAnnotatorAsync(int projectId, string annotatorId)
        {
            return await _dbSet
                .Include(a => a.DataItem)
                .Where(a => a.ProjectId == projectId && a.AnnotatorId == annotatorId)
                .OrderByDescending(a => a.AssignedDate)
                .ToListAsync();
        }

        public async Task<List<Assignment>> GetAssignmentsForReviewerAsync(int projectId)
        {
            return await _dbSet
                .Include(a => a.DataItem)
                .Include(a => a.Annotator)
                .Where(a => a.ProjectId == projectId && a.Status == "Submitted")
                .ToListAsync();
        }

        public async Task<Assignment?> GetAssignmentWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(a => a.DataItem)
                .Include(a => a.Project)
                    .ThenInclude(p => p.LabelClasses)
                .Include(a => a.Annotations) 
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<DataItem>> GetUnassignedDataItemsAsync(int projectId, int quantity, int maxAssignments, string excludeAnnotatorId)
        {
            return await _context.DataItems
                .Include(d => d.Assignments)
                .Where(d => d.ProjectId == projectId
                            && d.Status != "Done"
                            && d.Assignments.Count < maxAssignments
                            && !d.Assignments.Any(a => a.AnnotatorId == excludeAnnotatorId))
                .Take(quantity)
                .ToListAsync();
        }

        public async Task<int> GetCompletedCountForDataItemAsync(int dataItemId)
        {
            return await _dbSet.CountAsync(a => a.DataItemId == dataItemId && a.Status == "Completed");
        }
    }
}