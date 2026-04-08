using Application.Common.Exceptions;
using Application.DTOs.Category;
using Application.DTOs.ExerciseOption;
using Application.DTOs.Lessons;
using Application.DTOs.Progress;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

var tests = new (string Name, Func<Task> Run)[]
{
    ("Category duplicate name validation", CategoryDuplicateNameValidationAsync),
    ("Lesson duplicate order validation", LessonDuplicateOrderValidationAsync),
    ("Exercise submit answer updates progress", ExerciseSubmitAnswerUpdatesProgressAsync)
};

var failures = new List<string>();

foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"[PASS] {test.Name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{test.Name}: {ex.Message}");
        Console.WriteLine($"[FAIL] {test.Name}");
        Console.WriteLine(ex.ToString());
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("Test failures:");
    foreach (var failure in failures)
        Console.Error.WriteLine(failure);

    Environment.Exit(1);
}

Console.WriteLine($"All tests passed: {tests.Length}");

static async Task CategoryDuplicateNameValidationAsync()
{
    var repository = new FakeCategoryRepository
    {
        ExistingCategories = new List<Category> { new() { Id = 1, Name = "Еда" } }
    };

    var service = new CategoryService(repository);

    await AssertThrowsAsync<ConflictException>(() =>
        service.CreateAsync(new CreateCategoryDto { Name = "  Еда  " }));
}

static async Task LessonDuplicateOrderValidationAsync()
{
    var repository = new FakeLessonRepository { DuplicateOrder = true };
    var service = new LessonService(repository);

    await AssertThrowsAsync<ConflictException>(() =>
        service.CreateAsync(new CreateLessonDto
        {
            Title = "Новый урок",
            Order = 1,
            Level = LanguageLevel.A1
        }));
}

static async Task ExerciseSubmitAnswerUpdatesProgressAsync()
{
    var repository = new FakeExerciseRepository();
    var wordProgressService = new FakeUserWordProgressService();
    var lessonProgressService = new FakeUserLessonProgressService();
    var service = new ExerciseService(repository, wordProgressService, lessonProgressService);

    var result = await service.SubmitAnswerAsync(1, new SubmitAnswerDto
    {
        OptionId = 2,
        UserId = 7
    });

    Assert(!result.IsCorrect, "Expected answer to be incorrect.");
    Assert(result.CorrectAnswer == "ана", "Expected the correct answer text to be returned.");
    Assert(result.IsLessonCompleted == true, "Expected lesson to be marked as completed.");
    Assert(result.LessonScore == 100, "Expected lesson score to be 100.");
    Assert(wordProgressService.LastUpdate == (7, 10, false), "Expected word progress to be updated.");
    Assert(lessonProgressService.LastRecalculation == (7, 5), "Expected lesson progress to be recalculated.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static async Task AssertThrowsAsync<TException>(Func<Task> action) where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception of type {typeof(TException).Name}.");
}

file sealed class FakeCategoryRepository : ICategoryRepository
{
    public List<Category> ExistingCategories { get; set; } = new();

    public Task AddAsync(Category category)
    {
        category.Id = ExistingCategories.Count + 1;
        ExistingCategories.Add(category);
        return Task.CompletedTask;
    }

    public void Delete(Category category) => ExistingCategories.Remove(category);

    public Task<bool> ExistsByNameAsync(string name, int? excludeCategoryId = null)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var exists = ExistingCategories.Any(x =>
            x.Name.ToLowerInvariant() == normalized &&
            (!excludeCategoryId.HasValue || x.Id != excludeCategoryId.Value));

        return Task.FromResult(exists);
    }

    public Task<List<Category>> GetAllAsync() => Task.FromResult(ExistingCategories.ToList());

    public Task<Category?> GetByIdAsync(int id) => Task.FromResult(ExistingCategories.FirstOrDefault(x => x.Id == id));

    public Task<bool> HasWordsAsync(int id) => Task.FromResult(false);

    public Task SaveChangesAsync() => Task.CompletedTask;

    public void Update(Category category)
    {
    }
}

file sealed class FakeLessonRepository : ILessonRepository
{
    public bool DuplicateOrder { get; set; }
    private readonly List<Lesson> _lessons = new();

    public Task AddAsync(Lesson lesson)
    {
        lesson.Id = _lessons.Count + 1;
        _lessons.Add(lesson);
        return Task.CompletedTask;
    }

    public void Delete(Lesson lesson) => _lessons.Remove(lesson);

    public Task<bool> ExistsWithOrderAsync(int order, int? excludeLessonId = null)
        => Task.FromResult(DuplicateOrder);

    public Task<List<Lesson>> GetAllAsync() => Task.FromResult(_lessons.ToList());

    public Task<Lesson?> GetByIdAsync(int id) => Task.FromResult(_lessons.FirstOrDefault(x => x.Id == id));

    public Task SaveChangesAsync() => Task.CompletedTask;

    public void Update(Lesson lesson)
    {
    }
}

file sealed class FakeExerciseRepository : IExerciseRepository
{
    public Task AddAsync(Exercise exercise) => Task.CompletedTask;

    public void Delete(Exercise exercise)
    {
    }

    public Task<bool> ExistsWithOrderAsync(int lessonId, int order) => Task.FromResult(false);

    public Task<bool> ExistsWithOrderAsync(int lessonId, int order, int excludeExerciseId) => Task.FromResult(false);

    public Task<Exercise?> GetByIdAsync(int id)
    {
        var exercise = new Exercise
        {
            Id = 1,
            LessonId = 5,
            WordId = 10,
            Type = ExerciseType.ChooseAnsver,
            Question = "Как по-казахски мама?",
            Explanation = "Нужно выбрать базовое слово семьи.",
            Options = new List<ExerciseOption>
            {
                new() { Id = 1, Text = "ана", IsCorrect = true },
                new() { Id = 2, Text = "әке", IsCorrect = false }
            }
        };

        return Task.FromResult<Exercise?>(exercise);
    }

    public Task<List<Exercise>> GetByLessonIdAsync(int lessonId) => Task.FromResult(new List<Exercise>());

    public Task<Exercise?> GetByIdWithLessonAsync(int id) => Task.FromResult<Exercise?>(null);

    public Task<bool> LessonExistsAsync(int lessonId) => Task.FromResult(true);

    public Task SaveChangesAsync() => Task.CompletedTask;

    public void Update(Exercise exercise)
    {
    }

    public Task<bool> WordExistsAsync(int wordId) => Task.FromResult(true);
}

file sealed class FakeUserWordProgressService : IUserWordProgressService
{
    public (int userId, int wordId, bool isCorrect)? LastUpdate { get; private set; }

    public Task<List<UserWordProgressResponseDto>> GetByUserAsync(int userId) => Task.FromResult(new List<UserWordProgressResponseDto>());

    public Task<UserWordProgressResponseDto?> GetByUserAndWordAsync(int userId, int wordId)
        => Task.FromResult<UserWordProgressResponseDto?>(null);

    public Task UpdateAsync(int userId, int wordId, bool isCorrect)
    {
        LastUpdate = (userId, wordId, isCorrect);
        return Task.CompletedTask;
    }
}

file sealed class FakeUserLessonProgressService : IUserLessonProgressService
{
    public (int userId, int lessonId)? LastRecalculation { get; private set; }

    public Task<List<UserLessonProgressResponseDto>> GetByUserAsync(int userId)
        => Task.FromResult(new List<UserLessonProgressResponseDto>());

    public Task<UserLessonProgressResponseDto?> GetByUserAndLessonAsync(int userId, int lessonId)
        => Task.FromResult<UserLessonProgressResponseDto?>(null);

    public Task<UserLessonProgressResponseDto> RecalculateAsync(int userId, int lessonId)
    {
        LastRecalculation = (userId, lessonId);

        return Task.FromResult(new UserLessonProgressResponseDto
        {
            LessonId = lessonId,
            LessonTitle = "Семья",
            IsCompleted = true,
            Score = 100,
            LearnedWords = 4,
            TotalWords = 4
        });
    }
}
