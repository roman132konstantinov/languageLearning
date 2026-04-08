using Application.DTOs.Users;

namespace Application.Interfaces
{
    public interface IUserService
    {
        Task<List<UserResponseDto>> GetAllAsync();
        Task<UserResponseDto?> GetByIdAsync(int id);
        Task<UserResponseDto> CreateAsync(CreateUserDto dto);
        Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
