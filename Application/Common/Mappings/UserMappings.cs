using Application.DTOs.Users;
using Domain.Entities;

namespace Application.Common.Mappings
{
    public static class UserMappings
    {
        public static UserResponseDto ToResponse(this User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                Role = user.Role,
                Level = user.Level,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                EmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive
            };
        }
    }
}
