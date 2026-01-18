using System;
using System.ComponentModel.DataAnnotations;
using FIt3d.DAL.Entities;

namespace Fit3d.BLL.DTOs
{
    public class UserDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateUserDTO
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public UserRole Role { get; set; } = UserRole.Customer;
    }

    public class UpdateUserDTO
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public UserRole Role { get; set; }

        public bool IsActive { get; set; }
    }

    public class ChangePasswordDTO
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
