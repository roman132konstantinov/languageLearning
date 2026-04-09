using Application.Common.Pagination;
using Application.DTOs.Lessons;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface ILessonRepository
    {
        Task<PagedResult<Lesson>> GetAllAsync(LessonQueryDto query);
        Task<Lesson?> GetByIdAsync(int id);
        Task<bool> ExistsWithOrderAsync(int order, int? excludeLessonId = null);
        Task AddAsync(Lesson lesson);
        void Update(Lesson lesson);
        void Delete(Lesson lesson);
        Task SaveChangesAsync();
    }
}
