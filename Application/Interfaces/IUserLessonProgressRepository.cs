using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUserLessonProgressRepository
    {
        Task<bool> UserExistsAsync(int userId);
        Task<bool> LessonExistsAsync(int lessonId);
        Task<UserLessonProgress?> GetByUserAndLessonAsync(int userId, int lessonId);
        Task<List<UserLessonProgress>> GetByUserAsync(int userId);
        Task<int> GetLessonWordCountAsync(int lessonId);
        Task<int> GetLearnedLessonWordCountAsync(int userId, int lessonId);
        Task AddAsync(UserLessonProgress progress);
        void Update(UserLessonProgress progress);
        Task SaveChangesAsync();
    }
}
