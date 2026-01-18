using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;
using FIt3d.DAL.Common;

namespace Fit3d.BLL.Interfaces
{
    public interface ICategoryService
    {
        Task<ICollection<CategoryDTO>> GetAllAsync();
        Task<PagingResponse<CategoryDTO>> GetPagingAsync(int page, int size, string? search = null);
        Task<CategoryDTO?> GetByIdAsync(Guid id);
        Task<CategoryDTO> CreateAsync(CreateCategoryDTO createDto);
        Task<CategoryDTO?> UpdateAsync(Guid id, UpdateCategoryDTO updateDto);
        Task<bool> DeleteAsync(Guid id);
    }
}
