using Application.Common.Exceptions;
using Application.DTOs.Progress;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class UserLessonProgressService : IUserLessonProgressService
    {
        private readonly IUserLessonProgressRepository _userLessonProgressRepository;

        public UserLessonProgressService(IUserLessonProgressRepository userLessonProgressRepository)
        {
            _userLessonProgressRepository = userLessonProgressRepository;
        }

        public async Task<List<UserLessonProgressResponseDto>> GetByUserAsync(int userId)
        {
            ValidateUserId(userId);

            if (!await _userLessonProgressRepository.UserExistsAsync(userId))
                throw new NotFoundException("User not found.");

            var progressItems = await _userLessonProgressRepository.GetByUserAsync(userId);
            var result = new List<UserLessonProgressResponseDto>(progressItems.Count);

            foreach (var progress in progressItems)
            {
                var totalWords = await _userLessonProgressRepository.GetLessonWordCountAsync(progress.LessonId);
                var learnedWords = await _userLessonProgressRepository.GetLearnedLessonWordCountAsync(userId, progress.LessonId);
                result.Add(MapToResponse(progress, totalWords, learnedWords));
            }

            return result;
        }

        public async Task<UserLessonProgressResponseDto?> GetByUserAndLessonAsync(int userId, int lessonId)
        {
            ValidateUserId(userId);
            ValidateLessonId(lessonId);

            if (!await _userLessonProgressRepository.UserExistsAsync(userId))
                throw new NotFoundException("User not found.");

            var progress = await _userLessonProgressRepository.GetByUserAndLessonAsync(userId, lessonId);
            if (progress is null)
                return null;

            var totalWords = await _userLessonProgressRepository.GetLessonWordCountAsync(lessonId);
            var learnedWords = await _userLessonProgressRepository.GetLearnedLessonWordCountAsync(userId, lessonId);

            return MapToResponse(progress, totalWords, learnedWords);
        }

        public async Task<UserLessonProgressResponseDto> RecalculateAsync(int userId, int lessonId)
        {
            ValidateUserId(userId);
            ValidateLessonId(lessonId);

            if (!await _userLessonProgressRepository.UserExistsAsync(userId))
                throw new NotFoundException("User not found.");

            if (!await _userLessonProgressRepository.LessonExistsAsync(lessonId))
                throw new NotFoundException("Lesson not found.");

            var totalWords = await _userLessonProgressRepository.GetLessonWordCountAsync(lessonId);
            var learnedWords = await _userLessonProgressRepository.GetLearnedLessonWordCountAsync(userId, lessonId);
            var score = totalWords == 0 ? 0 : (int)Math.Round((double)learnedWords / totalWords * 100);
            var isCompleted = totalWords > 0 && learnedWords >= totalWords;

            var progress = await _userLessonProgressRepository.GetByUserAndLessonAsync(userId, lessonId);
            if (progress == null)
            {
                progress = new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = lessonId,
                    IsCompleted = isCompleted,
                    CompletedAt = isCompleted ? DateTime.UtcNow : null,
                    Score = score
                };

                await _userLessonProgressRepository.AddAsync(progress);
            }
            else
            {
                progress.Score = score;

                if (isCompleted && !progress.IsCompleted)
                    progress.CompletedAt = DateTime.UtcNow;

                if (!isCompleted)
                    progress.CompletedAt = null;

                progress.IsCompleted = isCompleted;
                _userLessonProgressRepository.Update(progress);
            }

            await _userLessonProgressRepository.SaveChangesAsync();

            progress = await _userLessonProgressRepository.GetByUserAndLessonAsync(userId, lessonId)
                ?? progress;

            return MapToResponse(progress, totalWords, learnedWords);
        }

        private static UserLessonProgressResponseDto MapToResponse(UserLessonProgress progress, int? totalWords, int? learnedWords)
        {
            return new UserLessonProgressResponseDto
            {
                LessonId = progress.LessonId,
                LessonTitle = progress.Lesson.Title,
                IsCompleted = progress.IsCompleted,
                CompletedAt = progress.CompletedAt,
                Score = progress.Score,
                LearnedWords = learnedWords ?? 0,
                TotalWords = totalWords ?? 0
            };
        }

        private static void ValidateUserId(int userId)
        {
            if (userId <= 0)
                throw new ValidationException("UserId must be greater than 0.");
        }

        private static void ValidateLessonId(int lessonId)
        {
            if (lessonId <= 0)
                throw new ValidationException("LessonId must be greater than 0.");
        }
    }
}
