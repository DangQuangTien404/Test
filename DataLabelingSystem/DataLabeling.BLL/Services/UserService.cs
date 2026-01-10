using DataLabeling.Core.DTOs;
using DataLabeling.Core.Entities;
using DataLabeling.Core.Enums;
using DataLabeling.Core.Interfaces;
using Microsoft.Extensions.Configuration; // Cần cái này để đọc appsettings
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DataLabeling.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityLogService _logService;
        private readonly IConfiguration _configuration; 
        public UserService(IUnitOfWork unitOfWork, IActivityLogService logService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _logService = logService;
            _configuration = configuration;
        }

        private string GenerateAccessToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["AccessTokenExpirationMinutes"])),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateLifetime = false 
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var users = await _unitOfWork.Repository<User>().FindAsync(u => u.Username == loginDto.Username);
            var user = users.FirstOrDefault();

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return null;

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken();
            var expiryDays = double.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(expiryDays);
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString()
                }
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
        {
            var principal = GetPrincipalFromExpiredToken(dto.AccessToken);
            if (principal == null) throw new Exception("Invalid access token or refresh token");

            var username = principal.Identity?.Name;
            var users = await _unitOfWork.Repository<User>().FindAsync(u => u.Username == username);
            var user = users.FirstOrDefault();

            if (user == null || user.RefreshToken != dto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new Exception("Invalid access token or refresh token");
            }

            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();
            var expiryDays = double.Parse(_configuration["JwtSettings:RefreshTokenExpirationDays"]);

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(expiryDays);

            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString()
                }
            };
        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            var existingUsers = await _unitOfWork.Repository<User>().FindAsync(u => u.Username == registerDto.Username);
            if (existingUsers.Any()) throw new Exception("Username đã tồn tại.");

            var newUser = new User
            {
                Username = registerDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                Role = registerDto.Role
            };

            await _unitOfWork.Repository<User>().AddAsync(newUser);
            await _unitOfWork.CompleteAsync();
            await _logService.LogAsync(newUser.Id, "Register", "User", newUser.Id.ToString(), $"Registered user {newUser.Username}");

            return new UserDto
            {
                Id = newUser.Id,
                Username = newUser.Username,
                FullName = newUser.FullName,
                Email = newUser.Email,
                Role = newUser.Role.ToString()
            };
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.Repository<User>().GetAllAsync();
            return users.Select(u => new UserDto { Id = u.Id, Username = u.Username, FullName = u.FullName, Email = u.Email, Role = u.Role.ToString() });
        }

        public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(UserRole role)
        {
            var users = await _unitOfWork.Repository<User>().FindAsync(u => u.Role == role);
            return users.Select(u => new UserDto { Id = u.Id, Username = u.Username, FullName = u.FullName, Email = u.Email, Role = u.Role.ToString() });
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null) return null;
            return new UserDto { Id = user.Id, Username = user.Username, FullName = user.FullName, Email = user.Email, Role = user.Role.ToString() };
        }

        public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null) throw new Exception("Người dùng không tồn tại.");

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.Role = dto.Role;
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.CompleteAsync();
            await _logService.LogAsync(user.Id, "Update", "User", user.Id.ToString(), "Updated profile");

            return new UserDto { Id = user.Id, Username = user.Username, FullName = user.FullName, Email = user.Email, Role = user.Role.ToString() };
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
            if (user == null) throw new Exception("Người dùng không tồn tại.");
            _unitOfWork.Repository<User>().Remove(user);
            await _unitOfWork.CompleteAsync();
            await _logService.LogAsync(null, "Delete", "User", id.ToString(), $"Deleted user {user.Username}");
        }
    }
}