using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Data
{
    public class LanguageLearningDbContext : DbContext
    {

        public LanguageLearningDbContext(DbContextOptions<LanguageLearningDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users => Set<User>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Word> Words => Set<Word>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<LessonWord> LessonWords => Set<LessonWord>();
        public DbSet<Exercise> Exercises => Set<Exercise>();
        public DbSet<ExerciseOption> ExerciseOptions => Set<ExerciseOption>();
        public DbSet<UserWordProgress> UserWordProgresses => Set<UserWordProgress>();
        public DbSet<UserLessonProgress> UserLessonProgresses => Set<UserLessonProgress>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LanguageLearningDbContext).Assembly);
        }
    }
}
