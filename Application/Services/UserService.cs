using System.Security.Cryptography;
using System.Text;
using Application.Common.Exceptions;
using Application.DTOs.Users;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<UserResponseDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(MapToResponse).ToList();
        }

        public async Task<UserResponseDto?> GetByIdAsync(int id)
        {
            ValidateUserId(id);

            var user = await _userRepository.GetByIdAsync(id);
            return user is null ? null : MapToResponse(user);
        }

        public async Task<UserResponseDto> CreateAsync(CreateUserDto dto)
        {
            ValidateCreateDto(dto);

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            if (await _userRepository.GetByEmailAsync(normalizedEmail) is not null)
                throw new ConflictException("User with this email already exists.");

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = HashPassword(dto.Password),
                UserName = dto.UserName.Trim(),
                Level = dto.Level,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return MapToResponse(user);
        }

        public async Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto)
        {
            ValidateUserId(id);
            ValidateUpdateDto(dto);

            var user = await _userRepository.GetByIdAsync(id);
            if (user is null)
                return null;

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (existingUser is not null && existingUser.Id != id)
                throw new ConflictException("User with this email already exists.");

            user.Email = normalizedEmail;
            user.UserName = dto.UserName.Trim();
            user.Level = dto.Level;
            user.IsActive = dto.IsActive;

            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.PasswordHash = HashPassword(dto.Password);

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return MapToResponse(user);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            ValidateUserId(id);

            var user = await _userRepository.GetByIdAsync(id);
            if (user is null)
                return false;

            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        private static UserResponseDto MapToResponse(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                Level = user.Level,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive
            };
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(hashBytes);
        }

        private static void ValidateUserId(int id)
        {
            if (id <= 0)
                throw new ValidationException("UserId must be greater than 0.");
        }

        private static void ValidateCreateDto(CreateUserDto dto)
        {
            if (dto is null)
                throw new ValidationException("User payload is required.");

            ValidateUserFields(dto.Email, dto.Password, dto.UserName, dto.Level, requirePassword: true);
        }

        private static void ValidateUpdateDto(UpdateUserDto dto)
        {
            if (dto is null)
                throw new ValidationException("User payload is required.");

            ValidateUserFields(dto.Email, dto.Password, dto.UserName, dto.Level, requirePassword: false);
        }

        private static void ValidateUserFields(string email, string? password, string userName, LanguageLevel level, bool requirePassword)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email is required.");

            if (email.Trim().Length > 150)
                throw new ValidationException("Email must not exceed 150 characters.");

            if (!email.Contains('@'))
                throw new ValidationException("Email format is invalid.");

            if (string.IsNullOrWhiteSpace(userName))
                throw new ValidationException("UserName is required.");

            if (userName.Trim().Length > 100)
                throw new ValidationException("UserName must not exceed 100 characters.");

            if (requirePassword && string.IsNullOrWhiteSpace(password))
                throw new ValidationException("Password is required.");

            if (!string.IsNullOrWhiteSpace(password) && password.Trim().Length < 6)
                throw new ValidationException("Password must be at least 6 characters long.");

            if (!Enum.IsDefined(typeof(LanguageLevel), level))
                throw new ValidationException("Invalid user level.");
        }
    }
}
