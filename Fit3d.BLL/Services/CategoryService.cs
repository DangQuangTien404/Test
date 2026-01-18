using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;
using Fit3d.BLL.Interfaces;
using FIt3d.DAL.Common;
using FIt3d.DAL.Entities;
using FIt3d.DAL.Repositories.Interfaces;

namespace Fit3d.BLL.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IGenericRepository<Category> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(IGenericRepository<Category> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ICollection<CategoryDTO>> GetAllAsync()
        {
            var entities = await _repository.GetListAsync(x => !x.IsDeleted);
            return entities.Select(ToDTO).ToList();
        }

        public async Task<PagingResponse<CategoryDTO>> GetPagingAsync(int page, int size, string? search = null)
        {
            var result = await _repository.GetPagingListSelectedAsync(
                selector: x => ToDTO(x),
                predicate: x => !x.IsDeleted && (string.IsNullOrEmpty(search) || x.Name.Contains(search)),
                orderBy: x => x.OrderBy(c => c.DisplayOrder),
                page: page,
                size: size
            );
            return result;
        }

        public async Task<CategoryDTO?> GetByIdAsync(Guid id)
        {
            var entity = await _repository.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return entity == null ? null : ToDTO(entity);
        }

        public async Task<CategoryDTO> CreateAsync(CreateCategoryDTO createDto)
        {
            var entity = new Category
            {
                Id = Guid.NewGuid(),
                Name = createDto.Name,
                Description = createDto.Description,
                ImageUrl = createDto.ImageUrl,
                DisplayOrder = createDto.DisplayOrder,
                IsActive = createDto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.InsertAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return ToDTO(entity);
        }

        public async Task<CategoryDTO?> UpdateAsync(Guid id, UpdateCategoryDTO updateDto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted) return null;

            entity.Name = updateDto.Name;
            entity.Description = updateDto.Description;
            entity.ImageUrl = updateDto.ImageUrl;
            entity.DisplayOrder = updateDto.DisplayOrder;
            entity.IsActive = updateDto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return ToDTO(entity);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted) return false;

            // Soft delete
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            _repository.UpdateAsync(entity);

            // Hard delete option: _repository.DeleteAsync(entity);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static CategoryDTO ToDTO(Category entity)
        {
            return new CategoryDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                ImageUrl = entity.ImageUrl,
                DisplayOrder = entity.DisplayOrder,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
