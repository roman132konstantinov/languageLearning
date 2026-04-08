using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Exceptions;
using Application.Interfaces;
using Application.Services;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<LanguageLearningDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IWordRepository, WordRepository>();
builder.Services.AddScoped<IWordService, WordService>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();
builder.Services.AddScoped<ILessonService, LessonService>();
builder.Services.AddScoped<ILessonWordRepository, LessonWordRepository>();
builder.Services.AddScoped<ILessonWordService, LessonWordService>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IExerciseService, ExerciseService>();
builder.Services.AddScoped<IExerciseOptionRepository, ExerciseOptionRepository>();
builder.Services.AddScoped<IExerciseOptionService, ExerciseOptionService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserWordProgressRepository, UserWordProgressRepository>();
builder.Services.AddScoped<IUserWordProgressService, UserWordProgressService>();
builder.Services.AddScoped<IUserLessonProgressRepository, UserLessonProgressRepository>();
builder.Services.AddScoped<IUserLessonProgressService, UserLessonProgressService>();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var statusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            DbUpdateException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status400BadRequest => "Request validation failed.",
                StatusCodes.Status404NotFound => "Resource not found.",
                StatusCodes.Status409Conflict => "Request conflicts with current data.",
                _ => "An unexpected error occurred."
            },
            Detail = exception?.Message
        });
    });
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LanguageLearningDbContext>();

    try
    {
        await dbContext.Database.MigrateAsync();
        await DemoDataSeeder.SeedAsync(dbContext, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database migration or demo data seeding failed during startup.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
