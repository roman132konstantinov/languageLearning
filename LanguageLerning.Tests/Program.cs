using System.Security.Cryptography;
using System.Text;
using Application.Common.Exceptions;
using Application.Common.Security;
using Application.DTOs.Auth;
using Application.DTOs.Category;
using Application.DTOs.ExerciseOption;
using Application.DTOs.Lessons;
using Application.DTOs.Progress;
using Application.Interfaces;
using Application.Options;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

var tests = new (string Name, Func<Task> Run)[]
{
    ("Password hasher verifies legacy SHA256 and requests rehash", PasswordHasherSupportsLegacySha256Async),
    ("First registration bootstraps an admin session", RegisterBootstrapsAdminAsync),
    ("Login failures trigger lockout", LoginFailuresTriggerLockoutAsync),
    ("Refresh token rotates and revokes the old token", RefreshTokenRotatesAsync),
    ("Password change revokes active refresh tokens", ChangePasswordRevokesRefreshTokensAsync),
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
        Console.WriteLine(ex);
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("Test failures:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(failure);
    }

    Environment.Exit(1);
}

Console.WriteLine($"All tests passed: {tests.Length}");

static Task PasswordHasherSupportsLegacySha256Async()
{
    var authOptions = CreateAuthOptions();
    var hasher = new PasswordHasher(authOptions);
    using var sha256 = SHA256.Create();
    var legacyHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes("Password1")));

    var result = hasher.VerifyPassword(legacyHash, "Password1");
    Assert(result == PasswordVerificationStatus.SuccessRehashNeeded, "Legacy SHA256 password should request rehash.");

    var modernHash = hasher.HashPassword("Password1");
    Assert(hasher.VerifyPassword(modernHash, "Password1") == PasswordVerificationStatus.Success, "PBKDF2 password should verify successfully.");
    return Task.CompletedTask;
}

static async Task RegisterBootstrapsAdminAsync()
{
    var repository = new FakeAuthRepository();
    var authService = CreateAuthService(repository);

    var response = await authService.RegisterAsync(new RegisterRequestDto
    {
        Email = "admin@example.com",
        Password = "Password1",
        UserName = "Admin",
        Level = LanguageLevel.A1
    }, CreateMetadata());

    Assert(response.User.Role == UserRole.Admin, "The first registered user should become Admin.");
    Assert(!string.IsNullOrWhiteSpace(response.AccessToken), "Access token should be returned.");
    Assert(!string.IsNullOrWhiteSpace(response.RefreshToken), "Refresh token should be returned.");
    Assert(repository.Users.Count == 1, "A user should be persisted.");
    Assert(repository.RefreshTokens.Count == 1, "A refresh token should be persisted.");
}

static async Task LoginFailuresTriggerLockoutAsync()
{
    var repository = new FakeAuthRepository();
    var authOptions = CreateAuthOptions();
    authOptions.MaxFailedAccessAttempts = 2;
    authOptions.LockoutMinutes = 30;
    var authService = CreateAuthService(repository, authOptions);
    var passwordHasher = new PasswordHasher(authOptions);

    repository.Users.Add(new User
    {
        Id = 1,
        Email = "student@example.com",
        PasswordHash = passwordHasher.HashPassword("Password1"),
        UserName = "Student",
        Role = UserRole.User,
        Level = LanguageLevel.A1,
        CreatedAt = DateTime.UtcNow,
        IsActive = true,
        SecurityStamp = Guid.NewGuid().ToString("N")
    });

    await AssertThrowsAsync<UnauthorizedException>(() => authService.LoginAsync(new LoginRequestDto
    {
        Email = "student@example.com",
        Password = "WrongPass1"
    }, CreateMetadata()));

    await AssertThrowsAsync<UnauthorizedException>(() => authService.LoginAsync(new LoginRequestDto
    {
        Email = "student@example.com",
        Password = "WrongPass1"
    }, CreateMetadata()));

    Assert(repository.Users[0].LockoutEndUtc.HasValue, "Lockout should be set after repeated failures.");

    await AssertThrowsAsync<UnauthorizedException>(() => authService.LoginAsync(new LoginRequestDto
    {
        Email = "student@example.com",
        Password = "Password1"
    }, CreateMetadata()));
}

static async Task RefreshTokenRotatesAsync()
{
    var repository = new FakeAuthRepository();
    var authService = CreateAuthService(repository);

    var registerResponse = await authService.RegisterAsync(new RegisterRequestDto
    {
        Email = "refresh@example.com",
        Password = "Password1",
        UserName = "Refresh User",
        Level = LanguageLevel.A1
    }, CreateMetadata());

    var refreshResponse = await authService.RefreshTokenAsync(new RefreshTokenRequestDto
    {
        RefreshToken = registerResponse.RefreshToken
    }, CreateMetadata());

    Assert(refreshResponse.RefreshToken != registerResponse.RefreshToken, "Refresh token should rotate.");
    Assert(repository.RefreshTokens.Count == 2, "A rotated token should add a new persisted refresh token.");
    Assert(repository.RefreshTokens.Count(x => x.RevokedAt.HasValue) == 1, "The old refresh token should be revoked.");
}

static async Task ChangePasswordRevokesRefreshTokensAsync()
{
    var repository = new FakeAuthRepository();
    var authService = CreateAuthService(repository);

    var registerResponse = await authService.RegisterAsync(new RegisterRequestDto
    {
        Email = "changepassword@example.com",
        Password = "Password1",
        UserName = "Change Password",
        Level = LanguageLevel.A1
    }, CreateMetadata());

    var userId = repository.Users.Single().Id;
    var originalSecurityStamp = repository.Users.Single().SecurityStamp;

    await authService.ChangePasswordAsync(userId, new ChangePasswordDto
    {
        CurrentPassword = "Password1",
        NewPassword = "Password2"
    }, CreateMetadata());

    Assert(repository.Users.Single().SecurityStamp != originalSecurityStamp, "Security stamp should rotate after password change.");
    Assert(repository.RefreshTokens.All(x => x.RevokedAt.HasValue), "All active refresh tokens should be revoked after password change.");

    await AssertThrowsAsync<UnauthorizedException>(() => authService.RefreshTokenAsync(new RefreshTokenRequestDto
    {
        RefreshToken = registerResponse.RefreshToken
    }, CreateMetadata()));
}

static async Task CategoryDuplicateNameValidationAsync()
{
    var repository = new FakeCategoryRepository
    {
        ExistingCategories = new List<Category> { new() { Id = 1, Name = "Food" } }
    };

    var service = new CategoryService(repository);

    await AssertThrowsAsync<ConflictException>(() =>
        service.CreateAsync(new CreateCategoryDto { Name = "  Food  " }));
}

static async Task LessonDuplicateOrderValidationAsync()
{
    var repository = new FakeLessonRepository { DuplicateOrder = true };
    var service = new LessonService(repository);

    await AssertThrowsAsync<ConflictException>(() =>
        service.CreateAsync(new CreateLessonDto
        {
            Title = "New lesson",
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
    Assert(result.CorrectAnswer == "ana", "Expected the correct answer text to be returned.");
    Assert(result.IsLessonCompleted == true, "Expected lesson to be marked as completed.");
    Assert(result.LessonScore == 100, "Expected lesson score to be 100.");
    Assert(wordProgressService.LastUpdate == (7, 10, false), "Expected word progress to be updated.");
    Assert(lessonProgressService.LastRecalculation == (7, 5), "Expected lesson progress to be recalculated.");
}

static AuthService CreateAuthService(FakeAuthRepository repository, AuthOptions? authOptions = null)
{
    var resolvedAuthOptions = authOptions ?? CreateAuthOptions();
    var jwtOptions = new JwtOptions
    {
        Issuer = "tests",
        Audience = "tests-clients",
        SigningKey = "super-secret-signing-key-for-tests-123456",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    return new AuthService(
        repository,
        new PasswordHasher(resolvedAuthOptions),
        new TokenService(jwtOptions, TimeProvider.System),
        resolvedAuthOptions,
        TimeProvider.System);
}

static AuthOptions CreateAuthOptions()
{
    return new AuthOptions
    {
        MaxFailedAccessAttempts = 5,
        LockoutMinutes = 15,
        MinPasswordLength = 8,
        RequireDigit = true,
        RequireUppercase = true,
        RequireLowercase = true,
        RequireNonAlphanumeric = false,
        PasswordHashIterations = 100000,
        BootstrapFirstUserAsAdmin = true
    };
}

static AuthRequestMetadata CreateMetadata()
{
    return new AuthRequestMetadata
    {
        IpAddress = "127.0.0.1",
        UserAgent = "tests"
    };
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
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

file sealed class FakeAuthRepository : IAuthRepository
{
    public List<User> Users { get; } = new();
    public List<RefreshToken> RefreshTokens { get; } = new();
    public List<AuthAuditLog> AuditLogs { get; } = new();
    private int _userId = 1;
    private int _refreshTokenId = 1;
    private int _auditId = 1;

    public Task<bool> AnyUsersAsync() => Task.FromResult(Users.Count > 0);

    public Task<User?> GetUserByIdAsync(int userId)
        => Task.FromResult(Users.FirstOrDefault(x => x.Id == userId));

    public Task<User?> GetUserByEmailAsync(string email)
        => Task.FromResult(Users.FirstOrDefault(x => x.Email == email));

    public Task<User?> GetUserByIdWithRefreshTokensAsync(int userId)
    {
        var user = Users.FirstOrDefault(x => x.Id == userId);
        if (user is not null)
        {
            user.RefreshTokens = RefreshTokens.Where(x => x.UserId == userId).ToList();
        }

        return Task.FromResult(user);
    }

    public Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash)
        => Task.FromResult(RefreshTokens.FirstOrDefault(x => x.TokenHash == tokenHash));

    public Task<RefreshToken?> GetRefreshTokenByHashWithUserAsync(string tokenHash)
    {
        var token = RefreshTokens.FirstOrDefault(x => x.TokenHash == tokenHash);
        if (token is not null)
        {
            token.User = Users.Single(x => x.Id == token.UserId);
        }

        return Task.FromResult(token);
    }

    public Task AddUserAsync(User user)
    {
        user.Id = _userId++;
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        refreshToken.Id = _refreshTokenId++;
        if (refreshToken.UserId == 0 && refreshToken.User is not null)
        {
            refreshToken.UserId = refreshToken.User.Id;
        }

        if (string.IsNullOrWhiteSpace(refreshToken.SessionId))
        {
            refreshToken.SessionId = Guid.NewGuid().ToString("N");
        }

        RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task AddAuditLogAsync(AuthAuditLog auditLog)
    {
        auditLog.Id = _auditId++;
        AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    public void UpdateUser(User user)
    {
        var index = Users.FindIndex(x => x.Id == user.Id);
        if (index >= 0)
        {
            Users[index] = user;
        }
    }

    public Task SaveChangesAsync() => Task.CompletedTask;
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
            Question = "How do you say mother?",
            Explanation = "Choose the correct core family word.",
            Options = new List<ExerciseOption>
            {
                new() { Id = 1, Text = "ana", IsCorrect = true },
                new() { Id = 2, Text = "ake", IsCorrect = false }
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
            LessonTitle = "Family",
            IsCompleted = true,
            Score = 100,
            LearnedWords = 4,
            TotalWords = 4
        });
    }
}
