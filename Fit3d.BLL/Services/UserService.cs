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
    public class UserService : IUserService
    {
        private readonly IGenericRepository<User> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IGenericRepository<User> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ICollection<UserDTO>> GetAllAsync()
        {
            var entities = await _repository.GetListAsync(x => !x.IsDeleted);
            return entities.Select(ToDTO).ToList();
        }

        public async Task<PagingResponse<UserDTO>> GetPagingAsync(int page, int size, string? search = null)
        {
            var result = await _repository.GetPagingListSelectedAsync(
                selector: x => ToDTO(x),
                predicate: x => !x.IsDeleted &&
                                (string.IsNullOrEmpty(search) || x.FullName.Contains(search) || x.Email.Contains(search)),
                orderBy: x => x.OrderBy(u => u.FullName),
                page: page,
                size: size
            );
            return result;
        }

        public async Task<UserDTO?> GetByIdAsync(Guid id)
        {
            var entity = await _repository.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return entity == null ? null : ToDTO(entity);
        }

        public async Task<UserDTO> CreateAsync(CreateUserDTO createDto)
        {
            // Check if email exists
            var existing = await _repository.SingleOrDefaultAsync(x => x.Email == createDto.Email && !x.IsDeleted);
            if (existing != null)
            {
                throw new ArgumentException("Email already exists.");
            }

            var entity = new User
            {
                Id = Guid.NewGuid(),
                FullName = createDto.FullName,
                Email = createDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password),
                Phone = createDto.Phone,
                Address = createDto.Address,
                Role = createDto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.InsertAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return ToDTO(entity);
        }

        public async Task<UserDTO?> UpdateAsync(Guid id, UpdateUserDTO updateDto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted) return null;

            // Check email uniqueness if changed
            if (entity.Email != updateDto.Email)
            {
                var existing = await _repository.SingleOrDefaultAsync(x => x.Email == updateDto.Email && x.Id != id && !x.IsDeleted);
                if (existing != null)
                {
                    throw new ArgumentException("Email already exists.");
                }
            }

            entity.FullName = updateDto.FullName;
            entity.Email = updateDto.Email;
            entity.Phone = updateDto.Phone;
            entity.Address = updateDto.Address;
            entity.Role = updateDto.Role;
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

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid id, ChangePasswordDTO changePasswordDto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted) return false;

            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.OldPassword, entity.PasswordHash))
            {
                return false;
            }

            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            entity.UpdatedAt = DateTime.UtcNow;

            _repository.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static UserDTO ToDTO(User entity)
        {
            return new UserDTO
            {
                Id = entity.Id,
                FullName = entity.FullName,
                Email = entity.Email,
                Phone = entity.Phone,
                Address = entity.Address,
                Role = entity.Role,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
