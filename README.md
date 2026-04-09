# LanguageLearning

Учебный проект для изучения казахского языка.

Сейчас это ранняя стадия разработки: базовая архитектура, API, база данных и мобильный клиент уже есть
## Что есть сейчас

Проект состоит из двух основных частей:

- ASP.NET Core backend
- Flutter-клиент `kazlang_app`

## Текущий функционал

### Backend

Серверная часть уже умеет:

- регистрировать пользователей и выполнять вход
- выдавать `access token` и `refresh token`
- поддерживать роли `Admin`, `Editor`, `User`
- отдавать список категорий, слов, уроков и упражнений
- создавать и редактировать учебный контент
- хранить прогресс пользователя по словам и урокам
- работать с пагинацией, фильтрацией и сортировкой в основных списках
- применять EF Core migrations для базы данных

Также в проекте уже есть:

- health endpoints
- базовая валидация
- ограничение запросов
- аудит аутентификации

### Flutter-приложение

Мобильный клиент уже умеет:

- авторизацию и регистрацию
- просмотр уроков, слов и прогресса
- работу с контентом для ролей с правами редактирования
- прохождение части учебных упражнений
- воспроизведение аудио урока и слова
- fallback-озвучку слова через TTS, если отдельный `audioUrl` не задан

## Что ещё не доделано

На текущий момент проект всё ещё в разработке. Например:

- не завершены все пользовательские сценарии обучения
- нужна дальнейшая доработка UI/UX
- не доведены до конца production-сценарии деплоя
- не реализованы все вспомогательные возможности вокруг аккаунта и контента
- README и внутренняя документация ещё будут уточняться по мере развития проекта

## Стек

- `.NET`
- `ASP.NET Core`
- `Entity Framework Core`
- `SQL Server`
- `Flutter`
- `Dart`

## Структура проекта

- `API` — web API и конфигурация приложения
- `Application` — сервисы, DTO и бизнес-логика
- `Domain` — доменные сущности
- `Infrastructure` — работа с БД, репозитории, migrations
- `LanguageLerning.Tests` — тестовый проект
- `kazlang_app` — Flutter-клиент

## Как запустить backend локально

Нужны значения конфигурации:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey`

Пример локального запуска:

```powershell
dotnet restore LanguageLerning.slnx
dotnet build LanguageLerning.slnx
dotnet run --project API/API.csproj
```

В development-режиме приложение применяет миграции и может заполнить базу демо-данными.

## Как запустить Flutter-клиент

```powershell
cd kazlang_app
flutter pub get
flutter run
```

Если backend запущен не на адресе по умолчанию, базовый URL можно изменить в самом приложении.

## База данных и миграции

Создать новую миграцию:

```powershell
$env:ConnectionStrings__DefaultConnection='Server=(localdb)\\mssqllocaldb;Database=LanguageLearningDesignTime;Trusted_Connection=True;TrustServerCertificate=True;'
dotnet ef migrations add <MigrationName> --project Infrastructure/Infrastructure.csproj --startup-project API/API.csproj --context LanguageLearningDbContext
```

Применить миграции:

```powershell
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project API/API.csproj --context LanguageLearningDbContext
```

## Проверка

```powershell
dotnet build LanguageLerning.slnx -p:UseSharedCompilation=false
dotnet run --project LanguageLerning.Tests/LanguageLerning.Tests.csproj
```

Для Flutter:

```powershell
cd kazlang_app
flutter analyze
```
