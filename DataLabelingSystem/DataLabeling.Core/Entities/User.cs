using DataLabeling.Core.Enums;
using System; // Nhớ thêm dòng này
using System.Collections.Generic;

namespace DataLabeling.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public ICollection<Project> ManagedProjects { get; set; }
        public ICollection<LabelTask> AssignedTasks { get; set; }
    }
}