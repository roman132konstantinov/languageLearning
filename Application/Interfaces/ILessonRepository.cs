using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ILessonRepository
    {
        Task<List<Lesson>> GetAllAsync();
        Task<Lesson?> GetByIdAsync(int id);
        Task AddAsync(Lesson lesson);
        void Update(Lesson lesson);
        void Delete(Lesson lesson);
        Task SaveChangesAsync();
    }
}
