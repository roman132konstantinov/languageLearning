using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data
{
    public static class DemoDataSeeder
    {
        public static async Task SeedAsync(LanguageLearningDbContext context, ILogger logger, CancellationToken cancellationToken = default)
        {
            if (await context.Categories.AnyAsync(cancellationToken))
            {
                logger.LogInformation("Demo data seed skipped because the database already contains categories.");
                return;
            }

            var family = new Category { Name = "Семья", Description = "Слова про семью и близких людей." };
            var food = new Category { Name = "Еда", Description = "Базовая еда и напитки на казахском языке." };
            var city = new Category { Name = "Город", Description = "Слова для повседневной жизни в городе." };
            var verbs = new Category { Name = "Глаголы", Description = "Часто используемые действия и команды." };

            await context.Categories.AddRangeAsync(new[] { family, food, city, verbs }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var words = new List<Word>
            {
                new() { CategoryId = family.Id, KazakhText = "ана", RussianTranslation = "мама", Pronunciation = "ана", Example = "Менің анам дәрігер.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = family.Id, KazakhText = "әке", RussianTranslation = "папа", Pronunciation = "аке", Example = "Әкем жұмыста.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = family.Id, KazakhText = "аға", RussianTranslation = "старший брат", Pronunciation = "ага", Example = "Ағам студент.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = family.Id, KazakhText = "әпке", RussianTranslation = "старшая сестра", Pronunciation = "апке", Example = "Әпкем мұғалім.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = family.Id, KazakhText = "дос", RussianTranslation = "друг", Pronunciation = "дос", Example = "Менің досым Алматыда тұрады.", Level = LanguageLevel.A1, IsActive = true },

                new() { CategoryId = food.Id, KazakhText = "нан", RussianTranslation = "хлеб", Pronunciation = "нан", Example = "Маған нан беріңіз.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = food.Id, KazakhText = "сүт", RussianTranslation = "молоко", Pronunciation = "сут", Example = "Бала сүт ішеді.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = food.Id, KazakhText = "шай", RussianTranslation = "чай", Pronunciation = "шай", Example = "Біз кешке шай ішеміз.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = food.Id, KazakhText = "су", RussianTranslation = "вода", Pronunciation = "су", Example = "Су ішіңіз.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = food.Id, KazakhText = "алма", RussianTranslation = "яблоко", Pronunciation = "алма", Example = "Алма тәтті.", Level = LanguageLevel.A1, IsActive = true },

                new() { CategoryId = city.Id, KazakhText = "мектеп", RussianTranslation = "школа", Pronunciation = "мектеп", Example = "Мектеп үлкен.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = city.Id, KazakhText = "дүкен", RussianTranslation = "магазин", Pronunciation = "дукен", Example = "Дүкен бүгін ашық.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = city.Id, KazakhText = "үй", RussianTranslation = "дом", Pronunciation = "уй", Example = "Менің үйім қалада.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = city.Id, KazakhText = "көше", RussianTranslation = "улица", Pronunciation = "коше", Example = "Көше тыныш.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = city.Id, KazakhText = "қала", RussianTranslation = "город", Pronunciation = "кала", Example = "Астана - әдемі қала.", Level = LanguageLevel.A1, IsActive = true },

                new() { CategoryId = verbs.Id, KazakhText = "бару", RussianTranslation = "идти", Pronunciation = "бару", Example = "Мен мектепке барамын.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = verbs.Id, KazakhText = "келу", RussianTranslation = "приходить", Pronunciation = "келу", Example = "Ол ертең келеді.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = verbs.Id, KazakhText = "отыру", RussianTranslation = "сидеть", Pronunciation = "отыру", Example = "Бала орындықта отыр.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = verbs.Id, KazakhText = "оқу", RussianTranslation = "читать / учиться", Pronunciation = "оку", Example = "Мен қазақша оқимын.", Level = LanguageLevel.A1, IsActive = true },
                new() { CategoryId = verbs.Id, KazakhText = "жазу", RussianTranslation = "писать", Pronunciation = "жазу", Example = "Ол хат жазады.", Level = LanguageLevel.A1, IsActive = true }
            };

            await context.Words.AddRangeAsync(words, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var lesson1 = new Lesson { Title = "Семья и знакомство", Description = "Первые слова о семье и друзьях.", Level = LanguageLevel.A1, Order = 1, IsPublished = true };
            var lesson2 = new Lesson { Title = "Еда и покупки", Description = "Еда, напитки и простые покупки.", Level = LanguageLevel.A1, Order = 2, IsPublished = true };
            var lesson3 = new Lesson { Title = "Город и действия", Description = "Городская лексика и основные глаголы.", Level = LanguageLevel.A1, Order = 3, IsPublished = true };

            await context.Lessons.AddRangeAsync(new[] { lesson1, lesson2, lesson3 }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var lessonWords = new List<LessonWord>
            {
                new() { LessonId = lesson1.Id, WordId = words[0].Id, Order = 1 },
                new() { LessonId = lesson1.Id, WordId = words[1].Id, Order = 2 },
                new() { LessonId = lesson1.Id, WordId = words[2].Id, Order = 3 },
                new() { LessonId = lesson1.Id, WordId = words[4].Id, Order = 4 },

                new() { LessonId = lesson2.Id, WordId = words[5].Id, Order = 1 },
                new() { LessonId = lesson2.Id, WordId = words[6].Id, Order = 2 },
                new() { LessonId = lesson2.Id, WordId = words[7].Id, Order = 3 },
                new() { LessonId = lesson2.Id, WordId = words[9].Id, Order = 4 },

                new() { LessonId = lesson3.Id, WordId = words[10].Id, Order = 1 },
                new() { LessonId = lesson3.Id, WordId = words[11].Id, Order = 2 },
                new() { LessonId = lesson3.Id, WordId = words[15].Id, Order = 3 },
                new() { LessonId = lesson3.Id, WordId = words[18].Id, Order = 4 }
            };

            await context.LessonWords.AddRangeAsync(lessonWords, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var exercises = new List<Exercise>
            {
                CreateExercise(lesson1.Id, words[0].Id, 1, "Как по-казахски 'мама'?", "Выбери правильный перевод."),
                CreateExercise(lesson1.Id, words[1].Id, 2, "Как по-казахски 'папа'?", "Это базовое слово семьи."),
                CreateExercise(lesson1.Id, words[2].Id, 3, "Как по-казахски 'старший брат'?", "Обрати внимание на букву 'ғ'."),
                CreateExercise(lesson1.Id, words[4].Id, 4, "Как по-казахски 'друг'?", "Это слово часто используется в диалогах."),

                CreateExercise(lesson2.Id, words[5].Id, 1, "Как по-казахски 'хлеб'?", "Полезно для бытовых диалогов."),
                CreateExercise(lesson2.Id, words[6].Id, 2, "Как по-казахски 'молоко'?", "Частое слово из повседневной жизни."),
                CreateExercise(lesson2.Id, words[7].Id, 3, "Как по-казахски 'чай'?", "Один из самых частых напитков."),
                CreateExercise(lesson2.Id, words[9].Id, 4, "Как по-казахски 'яблоко'?", "Фрукт для первых тем."),

                CreateExercise(lesson3.Id, words[10].Id, 1, "Как по-казахски 'школа'?", "Слово для темы города и учёбы."),
                CreateExercise(lesson3.Id, words[11].Id, 2, "Как по-казахски 'магазин'?", "Часто используется в городе."),
                CreateExercise(lesson3.Id, words[15].Id, 3, "Как по-казахски 'идти'?", "Полезный базовый глагол."),
                CreateExercise(lesson3.Id, words[18].Id, 4, "Как по-казахски 'читать / учиться'?", "Слово часто встречается в фразах о языке.")
            };

            await context.Exercises.AddRangeAsync(exercises, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var options = new List<ExerciseOption>();
            AddOptions(options, exercises[0].Id, "ана", "әке", "дос", "алма", 0);
            AddOptions(options, exercises[1].Id, "әке", "ана", "аға", "үй", 0);
            AddOptions(options, exercises[2].Id, "аға", "әпке", "нан", "су", 0);
            AddOptions(options, exercises[3].Id, "дос", "қала", "дүкен", "жазу", 0);

            AddOptions(options, exercises[4].Id, "нан", "сүт", "су", "үй", 0);
            AddOptions(options, exercises[5].Id, "сүт", "шай", "алма", "көше", 0);
            AddOptions(options, exercises[6].Id, "шай", "нан", "қала", "бару", 0);
            AddOptions(options, exercises[7].Id, "алма", "ана", "әке", "су", 0);

            AddOptions(options, exercises[8].Id, "мектеп", "дүкен", "көше", "қала", 0);
            AddOptions(options, exercises[9].Id, "дүкен", "мектеп", "үй", "бару", 0);
            AddOptions(options, exercises[10].Id, "бару", "келу", "отыру", "жазу", 0);
            AddOptions(options, exercises[11].Id, "оқу", "жазу", "отыру", "келу", 0);

            await context.ExerciseOptions.AddRangeAsync(options, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Demo data seeded successfully.");
        }

        private static Exercise CreateExercise(int lessonId, int wordId, int order, string question, string explanation)
        {
            return new Exercise
            {
                LessonId = lessonId,
                WordId = wordId,
                Order = order,
                Type = ExerciseType.ChooseAnsver,
                Question = question,
                Explanation = explanation
            };
        }

        private static void AddOptions(List<ExerciseOption> options, int exerciseId, string correct, string second, string third, string fourth, int correctIndex)
        {
            var values = new[] { correct, second, third, fourth };

            for (var i = 0; i < values.Length; i++)
            {
                options.Add(new ExerciseOption
                {
                    ExerciseId = exerciseId,
                    Text = values[i],
                    IsCorrect = i == correctIndex
                });
            }
        }
    }
}
