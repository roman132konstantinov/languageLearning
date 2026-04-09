using Application.Common.Exceptions;
using Application.Common.Mappings;
using Application.Common.Pagination;
using Application.Common.Security;
using Application.DTOs.Common;
using Application.DTOs.Users;
using Application.Interfaces;
using Application.Options;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly AuthOptions _authOptions;

        public UserService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            AuthOptions authOptions)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _authOptions = authOptions;
        }

        public async Task<PagedResponseDto<UserResponseDto>> GetAllAsync(UserQueryDto query)
        {
            query ??= new UserQueryDto();

            PagedQueryNormalizer.Normalize(query);

            var users = await _userRepository.GetAllAsync(query);

            return new PagedResponseDto<UserResponseDto>
            {
                Items = users.Items.Select(x => x.ToResponse()).ToList(),
                Page = users.Page,
                PageSize = users.PageSize,
                TotalCount = users.TotalCount,
                TotalPages = users.TotalPages
            };
        }

        public async Task<UserResponseDto?> GetByIdAsync(int id)
        {
            ValidateUserId(id);

            var user = await _userRepository.GetByIdAsync(id);
            return user is null ? null : user.ToResponse();
        }

        public async Task<UserResponseDto> CreateAsync(CreateUserDto dto)
        {
            ValidateCreateDto(dto, _authOptions);

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            if (await _userRepository.GetByEmailAsync(normalizedEmail) is not null)
            {
                throw new ConflictException("User with this email already exists.");
            }

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                UserName = dto.UserName.Trim(),
                Role = dto.Role,
                Level = dto.Level,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = dto.EmailConfirmed,
                IsActive = dto.IsActive,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return user.ToResponse();
        }

        public async Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto)
        {
            ValidateUserId(id);
            ValidateUpdateDto(dto, _authOptions);

            var user = await _userRepository.GetByIdAsync(id);
            if (user is null)
            {
                return null;
            }

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (existingUser is not null && existingUser.Id != id)
            {
                throw new ConflictException("User with this email already exists.");
            }

            var passwordChanged = false;
            var roleChanged = user.Role != dto.Role;
            var activationChanged = user.IsActive != dto.IsActive;

            user.Email = normalizedEmail;
            user.UserName = dto.UserName.Trim();
            user.Level = dto.Level;
            user.Role = dto.Role;
            user.EmailConfirmed = dto.EmailConfirmed;
            user.IsActive = dto.IsActive;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                PasswordPolicyValidator.Validate(dto.Password, _authOptions);
                user.PasswordHash = _passwordHasher.HashPassword(dto.Password);
                passwordChanged = true;
            }

            if (passwordChanged || roleChanged || activationChanged)
            {
                user.SecurityStamp = Guid.NewGuid().ToString("N");
            }

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return user.ToResponse();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            ValidateUserId(id);

            var user = await _userRepository.GetByIdAsync(id);
            if (user is null)
            {
                return false;
            }

            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        private static void ValidateUserId(int id)
        {
            if (id <= 0)
            {
                throw new ValidationException("UserId must be greater than 0.");
            }
        }

        private static void ValidateCreateDto(CreateUserDto dto, AuthOptions authOptions)
        {
            if (dto is null)
            {
                throw new ValidationException("User payload is required.");
            }

            ValidateUserFields(dto.Email, dto.Password, dto.UserName, dto.Level, dto.Role, authOptions, requirePassword: true);
        }

        private static void ValidateUpdateDto(UpdateUserDto dto, AuthOptions authOptions)
        {
            if (dto is null)
            {
                throw new ValidationException("User payload is required.");
            }

            ValidateUserFields(dto.Email, dto.Password, dto.UserName, dto.Level, dto.Role, authOptions, requirePassword: false);
        }

        private static void ValidateUserFields(
            string email,
            string? password,
            string userName,
            LanguageLevel level,
            UserRole role,
            AuthOptions authOptions,
            bool requirePassword)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ValidationException("Email is required.");
            }

            if (email.Trim().Length > 150)
            {
                throw new ValidationException("Email must not exceed 150 characters.");
            }

            if (!email.Contains('@'))
            {
                throw new ValidationException("Email format is invalid.");
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ValidationException("UserName is required.");
            }

            if (userName.Trim().Length > 100)
            {
                throw new ValidationException("UserName must not exceed 100 characters.");
            }

            if (requirePassword)
            {
                PasswordPolicyValidator.Validate(password ?? string.Empty, authOptions);
            }
            else if (!string.IsNullOrWhiteSpace(password))
            {
                PasswordPolicyValidator.Validate(password, authOptions);
            }

            if (!Enum.IsDefined(typeof(LanguageLevel), level))
            {
                throw new ValidationException("Invalid user level.");
            }

            if (!Enum.IsDefined(typeof(UserRole), role))
            {
                throw new ValidationException("Invalid user role.");
            }
        }
    }
}
