using Application.Common.Exceptions;
using Application.DTOs.Progress;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

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
            ValidateIds(userId, wordId);

            if (!await _userWordProgressRepository.UserExistsAsync(userId))
                throw new NotFoundException("User not found.");

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
                progress.RepetitionCount++;
                progress.Status = progress.CorrectAnswers switch
                {
                    >= 5 => WordProgressStatus.Mastered,
                    >= 3 => WordProgressStatus.Review,
                    _ => WordProgressStatus.Learning
                };
            }
            else
            {
                progress.WrongAnswers++;
                progress.RepetitionCount = 0;

                if (progress.Status != WordProgressStatus.New)
                    progress.Status = WordProgressStatus.Learning;
            }

            progress.LastReviewedAt = DateTime.UtcNow;
            progress.NextReviewAt = progress.Status switch
            {
                WordProgressStatus.Mastered => DateTime.UtcNow.AddDays(7),
                WordProgressStatus.Review => DateTime.UtcNow.AddDays(3),
                WordProgressStatus.Learning => DateTime.UtcNow.AddDays(1),
                _ => null
            };

            await _userWordProgressRepository.SaveChangesAsync();
        }

        public async Task<List<UserWordProgressResponseDto>> GetByUserAsync(int userId)
        {
            if (userId <= 0)
                throw new ValidationException("UserId must be greater than 0.");

            if (!await _userWordProgressRepository.UserExistsAsync(userId))
                throw new NotFoundException("User not found.");

            var progressItems = await _userWordProgressRepository.GetByUserAsync(userId);
            return progressItems.Select(MapToResponse).ToList();
        }

        public async Task<UserWordProgressResponseDto?> GetByUserAndWordAsync(int userId, int wordId)
        {
            ValidateIds(userId, wordId);

            if (!await _userWordProgressRepository.UserExistsAsync(userId))
                throw new NotFoundException("User not found.");

            var progress = await _userWordProgressRepository.GetByUserAndWordAsync(userId, wordId);
            return progress is null ? null : MapToResponse(progress);
        }

        private static UserWordProgressResponseDto MapToResponse(UserWordProgress progress)
        {
            return new UserWordProgressResponseDto
            {
                WordId = progress.WordId,
                KazakhText = progress.Word.KazakhText,
                RussianTranslation = progress.Word.RussianTranslation,
                Status = progress.Status,
                CorrectAnswers = progress.CorrectAnswers,
                WrongAnswers = progress.WrongAnswers,
                LastReviewedAt = progress.LastReviewedAt,
                NextReviewAt = progress.NextReviewAt,
                EaseFactor = progress.EaseFactor,
                RepetitionCount = progress.RepetitionCount
            };
        }

        private static void ValidateIds(int userId, int wordId)
        {
            if (userId <= 0)
                throw new ValidationException("UserId must be greater than 0.");

            if (wordId <= 0)
                throw new ValidationException("WordId must be greater than 0.");
        }
    }
}
