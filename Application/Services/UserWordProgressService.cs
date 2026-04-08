using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class UserWordProgressService : IUserWordProgressService
    {
        private readonly IUserWordProgressRepository _userWordProgressRepository;

        public UserWordProgressService(IUserWordProgressRepository userWordProgressRepository)
        {
            _userWordProgressRepository = userWordProgressRepository;
        }

        public async Task UpdateAsync(int userId, int wordId, bool isCorrect)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId must be greater than 0.");

            if (wordId <= 0)
                throw new ArgumentException("WordId must be greater than 0.");

            var userExists = await _userWordProgressRepository.UserExistsAsync(userId);
            if (!userExists)
                throw new ArgumentException("User not found.");

            var progress = await _userWordProgressRepository.GetByUserAndWordAsync(userId, wordId);

            if (progress == null)
            {
                progress = new UserWordProgress
                {
                    UserId = userId,
                    WordId = wordId,
                    CorrectAnswers = 0,
                    WrongAnswers = 0,
                    LastReviewedAt = DateTime.UtcNow,
                    Status = WordProgressStatus.New
                };

                await _userWordProgressRepository.AddAsync(progress);
            }

            if (isCorrect)
            {
                progress.CorrectAnswers++;

                if (progress.Status == WordProgressStatus.New)
                    progress.Status = WordProgressStatus.Learning;
            }
            else
            {
                progress.WrongAnswers++;
            }

            progress.LastReviewedAt = DateTime.UtcNow;

            await _userWordProgressRepository.SaveChangesAsync();
        }
    }
}
