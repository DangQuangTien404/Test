using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fit3d.BLL.DTOs;
using FIt3d.DAL.Common;

namespace Fit3d.BLL.Interfaces
{
    public interface IUserService
    {
        Task<ICollection<UserDTO>> GetAllAsync();
        Task<PagingResponse<UserDTO>> GetPagingAsync(int page, int size, string? search = null);
        Task<UserDTO?> GetByIdAsync(Guid id);
        Task<UserDTO> CreateAsync(CreateUserDTO createDto);
        Task<UserDTO?> UpdateAsync(Guid id, UpdateUserDTO updateDto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDTO changePasswordDto);
    }
}
