using Application.DTOs.Progress;

namespace Application.Interfaces
{
    public interface IUserLessonProgressService
    {
        Task<List<UserLessonProgressResponseDto>> GetByUserAsync(int userId);
        Task<UserLessonProgressResponseDto?> GetByUserAndLessonAsync(int userId, int lessonId);
        Task<UserLessonProgressResponseDto> RecalculateAsync(int userId, int lessonId);
    }
}
