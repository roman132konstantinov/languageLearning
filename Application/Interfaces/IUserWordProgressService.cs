using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IUserWordProgressService
    {
        Task UpdateAsync(int userId, int wordId, bool isCorrect);
    }
}
