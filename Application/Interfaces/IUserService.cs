using Application.DTOs.Common;
using Application.DTOs.Users;

namespace Application.Interfaces
{
    public interface IUserService
    {
        Task<PagedResponseDto<UserResponseDto>> GetAllAsync(UserQueryDto query);
        Task<UserResponseDto?> GetByIdAsync(int id);
        Task<UserResponseDto> CreateAsync(CreateUserDto dto);
        Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
