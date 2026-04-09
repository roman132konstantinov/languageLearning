import 'dart:async';
import 'dart:convert';

import 'package:audioplayers/audioplayers.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_tts/flutter_tts.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

String buildDefaultApiBaseUrl() {
  if (kIsWeb) {
    return 'http://localhost:5174';
  }

  return defaultTargetPlatform == TargetPlatform.android
      ? 'http://10.0.2.2:5174'
      : 'http://localhost:5174';
}

dynamic _jsonValue(Map<String, dynamic> json, List<String> keys) {
  for (final key in keys) {
    if (json.containsKey(key)) {
      return json[key];
    }
  }

  return null;
}

Map<String, dynamic> _asJsonMap(dynamic value) {
  if (value is Map<String, dynamic>) {
    return value;
  }

  if (value is Map) {
    return value.map((key, item) => MapEntry('$key', item));
  }

  return <String, dynamic>{};
}

List<Map<String, dynamic>> _asJsonList(dynamic value) {
  if (value is! List) {
    return const [];
  }

  return value.map((item) => _asJsonMap(item)).toList();
}

String _asString(dynamic value) => value?.toString() ?? '';

int _asInt(dynamic value) {
  if (value is int) {
    return value;
  }

  if (value is double) {
    return value.round();
  }

  if (value is String) {
    return int.tryParse(value.trim()) ?? 0;
  }

  return 0;
}

double _asDouble(dynamic value) {
  if (value is double) {
    return value;
  }

  if (value is int) {
    return value.toDouble();
  }

  if (value is String) {
    return double.tryParse(value.trim()) ?? 0;
  }

  return 0;
}

bool _asBool(dynamic value) {
  if (value is bool) {
    return value;
  }

  if (value is num) {
    return value != 0;
  }

  if (value is String) {
    final normalized = value.trim().toLowerCase();
    return normalized == 'true' || normalized == '1';
  }

  return false;
}

DateTime? _asDateTime(dynamic value) {
  if (value is String && value.trim().isNotEmpty) {
    return DateTime.tryParse(value)?.toLocal();
  }

  return null;
}

String? _nullIfBlank(String value) {
  final trimmed = value.trim();
  return trimmed.isEmpty ? null : trimmed;
}

bool _isLoopbackHost(String value) {
  final normalized = value.trim().toLowerCase();
  return normalized == 'localhost' ||
      normalized == '127.0.0.1' ||
      normalized == '::1';
}

String? _resolveAudioUrl(String? value, String baseUrl) {
  final normalized = _nullIfBlank(value ?? '');
  if (normalized == null) {
    return null;
  }

  final rawUri = Uri.tryParse(normalized);
  final baseUri = Uri.tryParse(baseUrl);

  if (rawUri != null && rawUri.hasScheme) {
    if (baseUri != null &&
        baseUri.hasScheme &&
        _isLoopbackHost(rawUri.host) &&
        !_isLoopbackHost(baseUri.host)) {
      return rawUri
          .replace(
            scheme: baseUri.scheme,
            host: baseUri.host,
            port: baseUri.hasPort ? baseUri.port : rawUri.port,
          )
          .toString();
    }

    return rawUri.toString();
  }

  if (baseUri != null && baseUri.hasScheme) {
    return baseUri.resolve(normalized).toString();
  }

  return normalized;
}

String _twoDigits(int value) => value.toString().padLeft(2, '0');

String _formatDateTimeShort(DateTime value) {
  final local = value.toLocal();
  return '${_twoDigits(local.day)}.${_twoDigits(local.month)} ${_twoDigits(local.hour)}:${_twoDigits(local.minute)}';
}

enum LanguageLevel { beginner, a1, a2, b1 }

extension LanguageLevelX on LanguageLevel {
  String get label => switch (this) {
    LanguageLevel.beginner => 'Начальный',
    LanguageLevel.a1 => 'A1',
    LanguageLevel.a2 => 'A2',
    LanguageLevel.b1 => 'B1',
  };

  int get apiValue => switch (this) {
    LanguageLevel.beginner => 0,
    LanguageLevel.a1 => 1,
    LanguageLevel.a2 => 2,
    LanguageLevel.b1 => 3,
  };
}

LanguageLevel languageLevelFromJson(dynamic value) {
  final normalized = _asInt(value);
  return switch (normalized) {
    1 => LanguageLevel.a1,
    2 => LanguageLevel.a2,
    3 => LanguageLevel.b1,
    _ => LanguageLevel.beginner,
  };
}

enum UserRole { user, editor, admin }

extension UserRoleX on UserRole {
  String get label => switch (this) {
    UserRole.user => 'Ученик',
    UserRole.editor => 'Редактор',
    UserRole.admin => 'Администратор',
  };

  int get apiValue => switch (this) {
    UserRole.user => 0,
    UserRole.editor => 1,
    UserRole.admin => 2,
  };
}

UserRole userRoleFromJson(dynamic value) {
  final normalized = _asInt(value);
  return switch (normalized) {
    1 => UserRole.editor,
    2 => UserRole.admin,
    _ => UserRole.user,
  };
}

enum ExerciseType {
  translateWord,
  chooseAnswer,
  matchPair,
  fillInTheBlank,
  listening,
}

extension ExerciseTypeX on ExerciseType {
  String get label => switch (this) {
    ExerciseType.translateWord => 'Перевод слова',
    ExerciseType.chooseAnswer => 'Выбор ответа',
    ExerciseType.matchPair => 'Сопоставление',
    ExerciseType.fillInTheBlank => 'Заполнить пропуск',
    ExerciseType.listening => 'Аудирование',
  };

  int get apiValue => switch (this) {
    ExerciseType.translateWord => 0,
    ExerciseType.chooseAnswer => 1,
    ExerciseType.matchPair => 2,
    ExerciseType.fillInTheBlank => 3,
    ExerciseType.listening => 4,
  };
}

ExerciseType exerciseTypeFromJson(dynamic value) {
  final normalized = _asInt(value);
  return switch (normalized) {
    1 => ExerciseType.chooseAnswer,
    2 => ExerciseType.matchPair,
    3 => ExerciseType.fillInTheBlank,
    4 => ExerciseType.listening,
    _ => ExerciseType.translateWord,
  };
}

enum WordProgressStatus { fresh, learning, review, mastered }

extension WordProgressStatusX on WordProgressStatus {
  String get label => switch (this) {
    WordProgressStatus.fresh => 'Новое',
    WordProgressStatus.learning => 'Изучается',
    WordProgressStatus.review => 'На повторении',
    WordProgressStatus.mastered => 'Освоено',
  };
}

enum _WordProgressFilter { all, fresh, learning, dueReview, mastered }

extension _WordProgressFilterX on _WordProgressFilter {
  String get label => switch (this) {
    _WordProgressFilter.all => 'Все',
    _WordProgressFilter.fresh => 'Новые',
    _WordProgressFilter.learning => 'Изучаются',
    _WordProgressFilter.dueReview => 'Повторить',
    _WordProgressFilter.mastered => 'Освоенные',
  };
}

WordProgressStatus wordProgressStatusFromJson(dynamic value) {
  final normalized = _asInt(value);
  return switch (normalized) {
    1 => WordProgressStatus.learning,
    2 => WordProgressStatus.review,
    3 => WordProgressStatus.mastered,
    _ => WordProgressStatus.fresh,
  };
}

class PagedResponse<T> {
  const PagedResponse({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
  });

  final List<T> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final int totalPages;

  factory PagedResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic> json) parser,
  ) {
    final itemList = _asJsonList(_jsonValue(json, ['items', 'Items']));
    return PagedResponse<T>(
      items: itemList.map(parser).toList(),
      page: _asInt(_jsonValue(json, ['page', 'Page'])),
      pageSize: _asInt(_jsonValue(json, ['pageSize', 'PageSize'])),
      totalCount: _asInt(_jsonValue(json, ['totalCount', 'TotalCount'])),
      totalPages: _asInt(_jsonValue(json, ['totalPages', 'TotalPages'])),
    );
  }
}

class UserProfile {
  const UserProfile({
    required this.id,
    required this.email,
    required this.userName,
    required this.role,
    required this.level,
    required this.createdAt,
    required this.lastLoginAt,
    required this.emailConfirmed,
    required this.isActive,
  });

  final int id;
  final String email;
  final String userName;
  final UserRole role;
  final LanguageLevel level;
  final DateTime? createdAt;
  final DateTime? lastLoginAt;
  final bool emailConfirmed;
  final bool isActive;

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    return UserProfile(
      id: _asInt(_jsonValue(json, ['id', 'Id'])),
      email: _asString(_jsonValue(json, ['email', 'Email'])),
      userName: _asString(_jsonValue(json, ['userName', 'UserName'])),
      role: userRoleFromJson(_jsonValue(json, ['role', 'Role'])),
      level: languageLevelFromJson(_jsonValue(json, ['level', 'Level'])),
      createdAt: _asDateTime(_jsonValue(json, ['createdAt', 'CreatedAt'])),
      lastLoginAt: _asDateTime(
        _jsonValue(json, ['lastLoginAt', 'LastLoginAt']),
      ),
      emailConfirmed: _asBool(
        _jsonValue(json, ['emailConfirmed', 'EmailConfirmed']),
      ),
      isActive: _asBool(_jsonValue(json, ['isActive', 'IsActive'])),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'email': email,
      'userName': userName,
      'role': role.apiValue,
      'level': level.apiValue,
      'createdAt': createdAt?.toIso8601String(),
      'lastLoginAt': lastLoginAt?.toIso8601String(),
      'emailConfirmed': emailConfirmed,
      'isActive': isActive,
    };
  }
}

class AuthSession {
  const AuthSession({
    required this.accessToken,
    required this.accessTokenExpiresAt,
    required this.refreshToken,
    required this.refreshTokenExpiresAt,
    required this.user,
  });

  final String accessToken;
  final DateTime? accessTokenExpiresAt;
  final String refreshToken;
  final DateTime? refreshTokenExpiresAt;
  final UserProfile user;

  factory AuthSession.fromJson(Map<String, dynamic> json) {
    return AuthSession(
      accessToken: _asString(_jsonValue(json, ['accessToken', 'AccessToken'])),
      accessTokenExpiresAt: _asDateTime(
        _jsonValue(json, ['accessTokenExpiresAt', 'AccessTokenExpiresAt']),
      ),
      refreshToken: _asString(
        _jsonValue(json, ['refreshToken', 'RefreshToken']),
      ),
      refreshTokenExpiresAt: _asDateTime(
        _jsonValue(json, ['refreshTokenExpiresAt', 'RefreshTokenExpiresAt']),
      ),
      user: UserProfile.fromJson(
        _asJsonMap(_jsonValue(json, ['user', 'User'])),
      ),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'accessToken': accessToken,
      'accessTokenExpiresAt': accessTokenExpiresAt?.toIso8601String(),
      'refreshToken': refreshToken,
      'refreshTokenExpiresAt': refreshTokenExpiresAt?.toIso8601String(),
      'user': user.toJson(),
    };
  }

  AuthSession copyWith({
    String? accessToken,
    DateTime? accessTokenExpiresAt,
    String? refreshToken,
    DateTime? refreshTokenExpiresAt,
    UserProfile? user,
  }) {
    return AuthSession(
      accessToken: accessToken ?? this.accessToken,
      accessTokenExpiresAt: accessTokenExpiresAt ?? this.accessTokenExpiresAt,
      refreshToken: refreshToken ?? this.refreshToken,
      refreshTokenExpiresAt:
          refreshTokenExpiresAt ?? this.refreshTokenExpiresAt,
      user: user ?? this.user,
    );
  }
}

class Category {
  const Category({
    required this.id,
    required this.name,
    required this.description,
  });

  final int id;
  final String name;
  final String? description;

  factory Category.fromJson(Map<String, dynamic> json) {
    return Category(
      id: _asInt(_jsonValue(json, ['id', 'Id'])),
      name: _asString(_jsonValue(json, ['name', 'Name'])),
      description: _nullIfBlank(
        _asString(_jsonValue(json, ['description', 'Description'])),
      ),
    );
  }
}

class Lesson {
  const Lesson({
    required this.id,
    required this.title,
    required this.description,
    required this.audioUrl,
    required this.level,
    required this.order,
    required this.isPublished,
  });

  final int id;
  final String title;
  final String? description;
  final String? audioUrl;
  final LanguageLevel level;
  final int order;
  final bool isPublished;

  factory Lesson.fromJson(Map<String, dynamic> json) {
    return Lesson(
      id: _asInt(_jsonValue(json, ['id', 'Id'])),
      title: _asString(_jsonValue(json, ['title', 'Title'])),
      description: _nullIfBlank(
        _asString(_jsonValue(json, ['description', 'Description'])),
      ),
      audioUrl: _nullIfBlank(
        _asString(_jsonValue(json, ['audioUrl', 'AudioUrl'])),
      ),
      level: languageLevelFromJson(_jsonValue(json, ['level', 'Level'])),
      order: _asInt(_jsonValue(json, ['order', 'Order'])),
      isPublished: _asBool(_jsonValue(json, ['isPublished', 'IsPublished'])),
    );
  }
}

class LessonWord {
  const LessonWord({
    required this.wordId,
    required this.order,
    required this.kazakhText,
    required this.russianTranslation,
    required this.pronunciation,
    required this.example,
    required this.audioUrl,
    required this.imageUrl,
    required this.categoryId,
  });

  final int wordId;
  final int order;
  final String kazakhText;
  final String russianTranslation;
  final String? pronunciation;
  final String? example;
  final String? audioUrl;
  final String? imageUrl;
  final int categoryId;

  factory LessonWord.fromJson(Map<String, dynamic> json) {
    return LessonWord(
      wordId: _asInt(_jsonValue(json, ['wordId', 'WordId'])),
      order: _asInt(_jsonValue(json, ['order', 'Order'])),
      kazakhText: _asString(_jsonValue(json, ['kazakhText', 'KazakhText'])),
      russianTranslation: _asString(
        _jsonValue(json, ['russianTranslation', 'RussianTranslation']),
      ),
      pronunciation: _nullIfBlank(
        _asString(_jsonValue(json, ['pronunciation', 'Pronunciation'])),
      ),
      example: _nullIfBlank(
        _asString(_jsonValue(json, ['example', 'Example'])),
      ),
      audioUrl: _nullIfBlank(
        _asString(_jsonValue(json, ['audioUrl', 'AudioUrl'])),
      ),
      imageUrl: _nullIfBlank(
        _asString(_jsonValue(json, ['imageUrl', 'ImageUrl'])),
      ),
      categoryId: _asInt(_jsonValue(json, ['categoryId', 'CategoryId'])),
    );
  }
}

class WordEntry {
  const WordEntry({
    required this.id,
    required this.kazakhText,
    required this.russianTranslation,
    required this.pronunciation,
    required this.example,
    required this.audioUrl,
    required this.imageUrl,
    required this.level,
    required this.isActive,
    required this.categoryId,
  });

  final int id;
  final String kazakhText;
  final String russianTranslation;
  final String? pronunciation;
  final String? example;
  final String? audioUrl;
  final String? imageUrl;
  final LanguageLevel level;
  final bool isActive;
  final int categoryId;

  factory WordEntry.fromJson(Map<String, dynamic> json) {
    return WordEntry(
      id: _asInt(_jsonValue(json, ['id', 'Id'])),
      kazakhText: _asString(_jsonValue(json, ['kazakhText', 'KazakhText'])),
      russianTranslation: _asString(
        _jsonValue(json, ['russianTranslation', 'RussianTranslation']),
      ),
      pronunciation: _nullIfBlank(
        _asString(_jsonValue(json, ['pronunciation', 'Pronunciation'])),
      ),
      example: _nullIfBlank(
        _asString(_jsonValue(json, ['example', 'Example'])),
      ),
      audioUrl: _nullIfBlank(
        _asString(_jsonValue(json, ['audioUrl', 'AudioUrl'])),
      ),
      imageUrl: _nullIfBlank(
        _asString(_jsonValue(json, ['imageUrl', 'ImageUrl'])),
      ),
      level: languageLevelFromJson(_jsonValue(json, ['level', 'Level'])),
      isActive: _asBool(_jsonValue(json, ['isActive', 'IsActive'])),
      categoryId: _asInt(_jsonValue(json, ['categoryId', 'CategoryId'])),
    );
  }
}

class Exercise {
  const Exercise({
    required this.id,
    required this.lessonId,
    required this.question,
    required this.order,
    required this.type,
    required this.explanation,
    required this.wordId,
  });

  final int id;
  final int lessonId;
  final String question;
  final int order;
  final ExerciseType type;
  final String? explanation;
  final int? wordId;

  factory Exercise.fromJson(Map<String, dynamic> json) {
    final wordValue = _jsonValue(json, ['wordId', 'WordId']);
    return Exercise(
      id: _asInt(_jsonValue(json, ['id', 'Id'])),
      lessonId: _asInt(_jsonValue(json, ['lessonId', 'LessonId'])),
      question: _asString(_jsonValue(json, ['question', 'Question'])),
      order: _asInt(_jsonValue(json, ['order', 'Order'])),
      type: exerciseTypeFromJson(_jsonValue(json, ['type', 'Type'])),
      explanation: _nullIfBlank(
        _asString(_jsonValue(json, ['explanation', 'Explanation'])),
      ),
      wordId: wordValue == null ? null : _asInt(wordValue),
    );
  }
}

class ExerciseOption {
  const ExerciseOption({
    required this.id,
    required this.exerciseId,
    required this.text,
    required this.isCorrect,
  });

  final int id;
  final int exerciseId;
  final String text;
  final bool isCorrect;

  factory ExerciseOption.fromJson(Map<String, dynamic> json) {
    return ExerciseOption(
      id: _asInt(_jsonValue(json, ['id', 'Id'])),
      exerciseId: _asInt(_jsonValue(json, ['exerciseId', 'ExerciseId'])),
      text: _asString(_jsonValue(json, ['text', 'Text'])),
      isCorrect: _asBool(_jsonValue(json, ['isCorrect', 'IsCorrect'])),
    );
  }
}

class SubmitAnswerResult {
  const SubmitAnswerResult({
    required this.isCorrect,
    required this.correctAnswer,
    required this.explanation,
    required this.isLessonCompleted,
    required this.lessonScore,
  });

  final bool isCorrect;
  final String? correctAnswer;
  final String? explanation;
  final bool? isLessonCompleted;
  final int? lessonScore;

  factory SubmitAnswerResult.fromJson(Map<String, dynamic> json) {
    return SubmitAnswerResult(
      isCorrect: _asBool(_jsonValue(json, ['isCorrect', 'IsCorrect'])),
      correctAnswer: _nullIfBlank(
        _asString(_jsonValue(json, ['correctAnswer', 'CorrectAnswer'])),
      ),
      explanation: _nullIfBlank(
        _asString(_jsonValue(json, ['explanation', 'Explanation'])),
      ),
      isLessonCompleted:
          json.containsKey('isLessonCompleted') ||
              json.containsKey('IsLessonCompleted')
          ? _asBool(
              _jsonValue(json, ['isLessonCompleted', 'IsLessonCompleted']),
            )
          : null,
      lessonScore:
          json.containsKey('lessonScore') || json.containsKey('LessonScore')
          ? _asInt(_jsonValue(json, ['lessonScore', 'LessonScore']))
          : null,
    );
  }
}

class UserWordProgress {
  const UserWordProgress({
    required this.wordId,
    required this.kazakhText,
    required this.russianTranslation,
    required this.status,
    required this.correctAnswers,
    required this.wrongAnswers,
    required this.lastReviewedAt,
    required this.nextReviewAt,
    required this.easeFactor,
    required this.repetitionCount,
  });

  final int wordId;
  final String kazakhText;
  final String russianTranslation;
  final WordProgressStatus status;
  final int correctAnswers;
  final int wrongAnswers;
  final DateTime? lastReviewedAt;
  final DateTime? nextReviewAt;
  final double easeFactor;
  final int repetitionCount;

  factory UserWordProgress.fromJson(Map<String, dynamic> json) {
    return UserWordProgress(
      wordId: _asInt(_jsonValue(json, ['wordId', 'WordId'])),
      kazakhText: _asString(_jsonValue(json, ['kazakhText', 'KazakhText'])),
      russianTranslation: _asString(
        _jsonValue(json, ['russianTranslation', 'RussianTranslation']),
      ),
      status: wordProgressStatusFromJson(
        _jsonValue(json, ['status', 'Status']),
      ),
      correctAnswers: _asInt(
        _jsonValue(json, ['correctAnswers', 'CorrectAnswers']),
      ),
      wrongAnswers: _asInt(_jsonValue(json, ['wrongAnswers', 'WrongAnswers'])),
      lastReviewedAt: _asDateTime(
        _jsonValue(json, ['lastReviewedAt', 'LastReviewedAt']),
      ),
      nextReviewAt: _asDateTime(
        _jsonValue(json, ['nextReviewAt', 'NextReviewAt']),
      ),
      easeFactor: _asDouble(_jsonValue(json, ['easeFactor', 'EaseFactor'])),
      repetitionCount: _asInt(
        _jsonValue(json, ['repetitionCount', 'RepetitionCount']),
      ),
    );
  }
}

class UserLessonProgress {
  const UserLessonProgress({
    required this.lessonId,
    required this.lessonTitle,
    required this.isCompleted,
    required this.completedAt,
    required this.score,
    required this.learnedWords,
    required this.totalWords,
  });

  final int lessonId;
  final String lessonTitle;
  final bool isCompleted;
  final DateTime? completedAt;
  final int score;
  final int learnedWords;
  final int totalWords;

  factory UserLessonProgress.fromJson(Map<String, dynamic> json) {
    return UserLessonProgress(
      lessonId: _asInt(_jsonValue(json, ['lessonId', 'LessonId'])),
      lessonTitle: _asString(_jsonValue(json, ['lessonTitle', 'LessonTitle'])),
      isCompleted: _asBool(_jsonValue(json, ['isCompleted', 'IsCompleted'])),
      completedAt: _asDateTime(
        _jsonValue(json, ['completedAt', 'CompletedAt']),
      ),
      score: _asInt(_jsonValue(json, ['score', 'Score'])),
      learnedWords: _asInt(_jsonValue(json, ['learnedWords', 'LearnedWords'])),
      totalWords: _asInt(_jsonValue(json, ['totalWords', 'TotalWords'])),
    );
  }
}

class ExerciseBundle {
  const ExerciseBundle({required this.exercise, required this.options});

  final Exercise exercise;
  final List<ExerciseOption> options;
}

class StoredAppState {
  const StoredAppState({required this.baseUrl, required this.session});

  final String baseUrl;
  final AuthSession? session;
}

class SessionStore {
  static const _baseUrlKey = 'kazlang.baseUrl';
  static const _sessionKey = 'kazlang.session';

  Future<StoredAppState> load() async {
    final preferences = await SharedPreferences.getInstance();
    final baseUrl =
        preferences.getString(_baseUrlKey) ?? buildDefaultApiBaseUrl();
    final rawSession = preferences.getString(_sessionKey);

    AuthSession? session;
    if (rawSession != null && rawSession.isNotEmpty) {
      try {
        session = AuthSession.fromJson(_asJsonMap(jsonDecode(rawSession)));
      } catch (_) {
        session = null;
      }
    }

    return StoredAppState(baseUrl: baseUrl, session: session);
  }

  Future<void> saveBaseUrl(String baseUrl) async {
    final preferences = await SharedPreferences.getInstance();
    await preferences.setString(_baseUrlKey, baseUrl);
  }

  Future<void> saveSession(AuthSession? session) async {
    final preferences = await SharedPreferences.getInstance();
    if (session == null) {
      await preferences.remove(_sessionKey);
      return;
    }

    await preferences.setString(_sessionKey, jsonEncode(session.toJson()));
  }
}

class ApiException implements Exception {
  ApiException(this.statusCode, this.message);

  final int? statusCode;
  final String message;

  @override
  String toString() => 'ApiException($statusCode): $message';
}

class ApiClient {
  ApiClient({required this.baseUrl, http.Client? httpClient})
    : _httpClient = httpClient ?? http.Client();

  final http.Client _httpClient;
  String baseUrl;
  AuthSession? _session;
  Future<AuthSession?> Function()? refreshSession;

  void updateSession(AuthSession? session) {
    _session = session;
  }

  Future<dynamic> get(
    String path, {
    Map<String, dynamic>? query,
    bool authenticated = true,
    bool allowRefresh = true,
  }) {
    return _send(
      'GET',
      path,
      query: query,
      authenticated: authenticated,
      allowRefresh: allowRefresh,
    );
  }

  Future<dynamic> post(
    String path, {
    Map<String, dynamic>? body,
    Map<String, dynamic>? query,
    bool authenticated = true,
    bool allowRefresh = true,
  }) {
    return _send(
      'POST',
      path,
      body: body,
      query: query,
      authenticated: authenticated,
      allowRefresh: allowRefresh,
    );
  }

  Future<dynamic> put(
    String path, {
    Map<String, dynamic>? body,
    Map<String, dynamic>? query,
    bool authenticated = true,
    bool allowRefresh = true,
  }) {
    return _send(
      'PUT',
      path,
      body: body,
      query: query,
      authenticated: authenticated,
      allowRefresh: allowRefresh,
    );
  }

  Future<dynamic> delete(
    String path, {
    Map<String, dynamic>? query,
    bool authenticated = true,
    bool allowRefresh = true,
  }) {
    return _send(
      'DELETE',
      path,
      query: query,
      authenticated: authenticated,
      allowRefresh: allowRefresh,
    );
  }

  Future<dynamic> _send(
    String method,
    String path, {
    Map<String, dynamic>? body,
    Map<String, dynamic>? query,
    required bool authenticated,
    required bool allowRefresh,
  }) async {
    final uri = _buildUri(path, query);
    final request = http.Request(method, uri);

    request.headers['Accept'] = 'application/json';
    if (body != null) {
      request.headers['Content-Type'] = 'application/json';
      request.body = jsonEncode(body);
    }

    if (authenticated) {
      final token = _session?.accessToken;
      if (token == null || token.isEmpty) {
        throw ApiException(401, 'Сессия истекла. Войдите снова.');
      }

      request.headers['Authorization'] = 'Bearer $token';
    }

    http.StreamedResponse response;
    try {
      response = await _httpClient
          .send(request)
          .timeout(const Duration(seconds: 25));
    } on TimeoutException {
      throw ApiException(null, 'Сервер долго не отвечает. Попробуйте ещё раз.');
    } catch (_) {
      throw ApiException(
        null,
        'Не удалось подключиться к серверу. Проверьте адрес подключения и попробуйте снова.',
      );
    }

    final responseText = await response.stream.bytesToString();

    if (response.statusCode == 401 &&
        authenticated &&
        allowRefresh &&
        refreshSession != null) {
      final refreshedSession = await refreshSession!.call();
      if (refreshedSession != null) {
        return _send(
          method,
          path,
          body: body,
          query: query,
          authenticated: authenticated,
          allowRefresh: false,
        );
      }
    }

    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (responseText.trim().isEmpty) {
        return null;
      }

      return jsonDecode(responseText);
    }

    throw ApiException(
      response.statusCode,
      _extractErrorMessage(responseText, response.statusCode),
    );
  }

  Uri _buildUri(String path, Map<String, dynamic>? query) {
    final normalizedBase = baseUrl.endsWith('/')
        ? baseUrl.substring(0, baseUrl.length - 1)
        : baseUrl;
    final normalizedPath = path.startsWith('/') ? path : '/$path';

    final parsed = Uri.parse('$normalizedBase$normalizedPath');
    if (query == null || query.isEmpty) {
      return parsed;
    }

    final queryParameters = <String, String>{};
    for (final entry in query.entries) {
      final value = entry.value;
      if (value == null) {
        continue;
      }

      final text = value.toString();
      if (text.trim().isEmpty) {
        continue;
      }

      queryParameters[entry.key] = text;
    }

    return parsed.replace(queryParameters: queryParameters);
  }

  String _extractErrorMessage(String responseText, int statusCode) {
    if (responseText.trim().isEmpty) {
      return _friendlyErrorMessage(
        'Запрос завершился с ошибкой ($statusCode).',
        statusCode,
      );
    }

    try {
      final map = _asJsonMap(jsonDecode(responseText));
      final detail = _nullIfBlank(
        _asString(_jsonValue(map, ['detail', 'Detail'])),
      );
      if (detail != null) {
        return _friendlyErrorMessage(detail, statusCode);
      }

      final title = _nullIfBlank(
        _asString(_jsonValue(map, ['title', 'Title'])),
      );
      if (title != null) {
        return _friendlyErrorMessage(title, statusCode);
      }

      final errors = _jsonValue(map, ['errors', 'Errors']);
      if (errors is Map) {
        final flattened = errors.values
            .expand(
              (value) =>
                  value is List ? value.map((item) => '$item') : ['$value'],
            )
            .join('\n');

        if (flattened.isNotEmpty) {
          return _friendlyErrorMessage(flattened, statusCode);
        }
      }
    } catch (_) {
      return _friendlyErrorMessage(responseText, statusCode);
    }

    return _friendlyErrorMessage(responseText, statusCode);
  }

  String _friendlyErrorMessage(String message, int? statusCode) {
    final normalized = message.trim();
    if (normalized.isEmpty) {
      return 'Произошла ошибка. Попробуйте ещё раз.';
    }

    const exactMatches = <String, String>{
      'Invalid email or password.': 'Неверный email или пароль.',
      'Неверный email или пароль.': 'Неверный email или пароль.',
      'Authentication failed.': 'Не удалось выполнить вход.',
      'Request validation failed.': 'Проверьте введённые данные.',
      'Access denied.': 'Доступ запрещён.',
      'Resource not found.': 'Ресурс не найден.',
      'Request conflicts with current data.':
          'Запрос конфликтует с текущими данными.',
      'An unexpected error occurred.': 'Произошла непредвиденная ошибка.',
      'Password is required.': 'Введите пароль.',
      'Введите пароль.': 'Введите пароль.',
      'Password must contain at least one uppercase letter.':
          'Пароль должен содержать хотя бы одну заглавную букву.',
      'Password must contain at least one lowercase letter.':
          'Пароль должен содержать хотя бы одну строчную букву.',
      'Password must contain at least one digit.':
          'Пароль должен содержать хотя бы одну цифру.',
      'Password must contain at least one non-alphanumeric character.':
          'Пароль должен содержать хотя бы один специальный символ.',
      'You can only access your own profile and progress.':
          'Можно просматривать только свой профиль и свой прогресс.',
      'Можно просматривать только свой профиль и свой прогресс.':
          'Можно просматривать только свой профиль и свой прогресс.',
    };

    final exact = exactMatches[normalized];
    if (exact != null) {
      return exact;
    }

    if (normalized.startsWith('Password must be at least ') &&
        normalized.endsWith(' characters long.')) {
      final minLength = normalized
          .replaceFirst('Password must be at least ', '')
          .replaceFirst(' characters long.', '');
      return 'Пароль должен содержать минимум $minLength символов.';
    }

    if (statusCode == 401) {
      return 'Неверный email или пароль.';
    }

    return normalized;
  }
}

class AuthApi {
  const AuthApi(this._client);

  final ApiClient _client;

  Future<AuthSession> login({
    required String email,
    required String password,
  }) async {
    final result = await _client.post(
      '/api/auth/login',
      body: {'email': email, 'password': password},
      authenticated: false,
      allowRefresh: false,
    );

    return AuthSession.fromJson(_asJsonMap(result));
  }

  Future<AuthSession> register({
    required String email,
    required String password,
    required String userName,
    required LanguageLevel level,
  }) async {
    final result = await _client.post(
      '/api/auth/register',
      body: {
        'email': email,
        'password': password,
        'userName': userName,
        'level': level.apiValue,
      },
      authenticated: false,
      allowRefresh: false,
    );

    return AuthSession.fromJson(_asJsonMap(result));
  }

  Future<AuthSession> refresh(String refreshToken) async {
    final result = await _client.post(
      '/api/auth/refresh-token',
      body: {'refreshToken': refreshToken},
      authenticated: false,
      allowRefresh: false,
    );

    return AuthSession.fromJson(_asJsonMap(result));
  }

  Future<void> logout(String refreshToken) async {
    await _client.post(
      '/api/auth/logout',
      body: {'refreshToken': refreshToken},
      authenticated: true,
      allowRefresh: false,
    );
  }

  Future<UserProfile> me() async {
    final result = await _client.get('/api/auth/me');
    return UserProfile.fromJson(_asJsonMap(result));
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    await _client.post(
      '/api/auth/change-password',
      body: {'currentPassword': currentPassword, 'newPassword': newPassword},
    );
  }
}

class LearningApi {
  const LearningApi(this._client);

  final ApiClient _client;

  Future<PagedResponse<Category>> getCategories({
    int page = 1,
    int pageSize = 50,
    String? search,
    String? sortBy,
    String? sortOrder,
  }) async {
    final result = await _client.get(
      '/api/categories',
      query: {
        'page': page,
        'pageSize': pageSize,
        'search': search,
        'sortBy': sortBy,
        'sortOrder': sortOrder,
      },
    );

    return PagedResponse<Category>.fromJson(
      _asJsonMap(result),
      Category.fromJson,
    );
  }

  Future<Category> getCategory(int id) async {
    final result = await _client.get('/api/categories/$id');
    return Category.fromJson(_asJsonMap(result));
  }

  Future<Category> createCategory(Map<String, dynamic> payload) async {
    final result = await _client.post('/api/categories', body: payload);
    return Category.fromJson(_asJsonMap(result));
  }

  Future<Category> updateCategory(int id, Map<String, dynamic> payload) async {
    await _client.put('/api/categories/$id', body: payload);
    return getCategory(id);
  }

  Future<void> deleteCategory(int id) {
    return _client.delete('/api/categories/$id');
  }

  Future<PagedResponse<WordEntry>> getWords({
    int page = 1,
    int pageSize = 100,
    String? search,
    String? sortBy,
    String? sortOrder,
    LanguageLevel? level,
    int? categoryId,
    bool? isActive,
  }) async {
    final result = await _client.get(
      '/api/words',
      query: {
        'page': page,
        'pageSize': pageSize,
        'search': search,
        'sortBy': sortBy,
        'sortOrder': sortOrder,
        'level': level?.apiValue,
        'categoryId': categoryId,
        'isActive': isActive,
      },
    );

    return PagedResponse<WordEntry>.fromJson(
      _asJsonMap(result),
      WordEntry.fromJson,
    );
  }

  Future<WordEntry> getWord(int id) async {
    final result = await _client.get('/api/words/$id');
    return WordEntry.fromJson(_asJsonMap(result));
  }

  Future<WordEntry> createWord(Map<String, dynamic> payload) async {
    final result = await _client.post('/api/words', body: payload);
    return WordEntry.fromJson(_asJsonMap(result));
  }

  Future<WordEntry> updateWord(int id, Map<String, dynamic> payload) async {
    await _client.put('/api/words/$id', body: payload);
    return getWord(id);
  }

  Future<void> deleteWord(int id) => _client.delete('/api/words/$id');

  Future<PagedResponse<Lesson>> getLessons({
    int page = 1,
    int pageSize = 100,
    String? search,
    String? sortBy,
    String? sortOrder,
  }) async {
    final result = await _client.get(
      '/api/lessons',
      query: {
        'page': page,
        'pageSize': pageSize,
        'search': search,
        'sortBy': sortBy,
        'sortOrder': sortOrder,
      },
    );

    return PagedResponse<Lesson>.fromJson(_asJsonMap(result), Lesson.fromJson);
  }

  Future<Lesson> getLesson(int id) async {
    final result = await _client.get('/api/lessons/$id');
    return Lesson.fromJson(_asJsonMap(result));
  }

  Future<Lesson> createLesson(Map<String, dynamic> payload) async {
    final result = await _client.post('/api/lessons', body: payload);
    return Lesson.fromJson(_asJsonMap(result));
  }

  Future<Lesson> updateLesson(int id, Map<String, dynamic> payload) async {
    final result = await _client.put('/api/lessons/$id', body: payload);
    return Lesson.fromJson(_asJsonMap(result));
  }

  Future<void> deleteLesson(int id) => _client.delete('/api/lessons/$id');

  Future<List<LessonWord>> getLessonWords(int lessonId) async {
    final result = await _client.get('/api/lessons/$lessonId/words');
    return _asJsonList(result).map(LessonWord.fromJson).toList();
  }

  Future<void> addWordToLesson({
    required int lessonId,
    required int wordId,
    required int order,
  }) {
    return _client.post(
      '/api/lessons/$lessonId/words',
      body: {'wordId': wordId, 'order': order},
    );
  }

  Future<void> removeWordFromLesson({
    required int lessonId,
    required int wordId,
  }) {
    return _client.delete('/api/lessons/$lessonId/words/$wordId');
  }

  Future<List<Exercise>> getExercises(int lessonId) async {
    final result = await _client.get('/api/lessons/$lessonId/exercises');
    return _asJsonList(result).map(Exercise.fromJson).toList();
  }

  Future<Exercise> getExercise(int id) async {
    final result = await _client.get('/api/exercises/$id');
    return Exercise.fromJson(_asJsonMap(result));
  }

  Future<Exercise> createExercise({
    required int lessonId,
    required Map<String, dynamic> payload,
  }) async {
    final result = await _client.post(
      '/api/lessons/$lessonId/exercises',
      body: payload,
    );
    return Exercise.fromJson(_asJsonMap(result));
  }

  Future<Exercise> updateExercise(int id, Map<String, dynamic> payload) async {
    final result = await _client.put('/api/exercises/$id', body: payload);
    return Exercise.fromJson(_asJsonMap(result));
  }

  Future<void> deleteExercise(int id) => _client.delete('/api/exercises/$id');

  Future<List<ExerciseOption>> getExerciseOptions(int exerciseId) async {
    final result = await _client.get('/api/exercises/$exerciseId/options');
    return _asJsonList(result).map(ExerciseOption.fromJson).toList();
  }

  Future<ExerciseOption> createExerciseOption({
    required int exerciseId,
    required Map<String, dynamic> payload,
  }) async {
    final result = await _client.post(
      '/api/exercises/$exerciseId/options',
      body: payload,
    );
    return ExerciseOption.fromJson(_asJsonMap(result));
  }

  Future<ExerciseOption> updateExerciseOption({
    required int exerciseId,
    required int optionId,
    required Map<String, dynamic> payload,
  }) async {
    await _client.put(
      '/api/exercises/$exerciseId/options/$optionId',
      body: payload,
    );

    final options = await getExerciseOptions(exerciseId);
    return options.firstWhere((item) => item.id == optionId);
  }

  Future<void> deleteExerciseOption({
    required int exerciseId,
    required int optionId,
  }) {
    return _client.delete('/api/exercises/$exerciseId/options/$optionId');
  }

  Future<SubmitAnswerResult> submitAnswer({
    required int exerciseId,
    required int optionId,
  }) async {
    final result = await _client.post(
      '/api/exercises/$exerciseId/submit',
      body: {'optionId': optionId},
    );
    return SubmitAnswerResult.fromJson(_asJsonMap(result));
  }
}

class UserApi {
  const UserApi(this._client);

  final ApiClient _client;

  Future<PagedResponse<UserProfile>> getUsers({
    int page = 1,
    int pageSize = 100,
    String? search,
    String? sortBy,
    String? sortOrder,
  }) async {
    final result = await _client.get(
      '/api/users',
      query: {
        'page': page,
        'pageSize': pageSize,
        'search': search,
        'sortBy': sortBy,
        'sortOrder': sortOrder,
      },
    );

    return PagedResponse<UserProfile>.fromJson(
      _asJsonMap(result),
      UserProfile.fromJson,
    );
  }

  Future<UserProfile> getUser(int id) async {
    final result = await _client.get('/api/users/$id');
    return UserProfile.fromJson(_asJsonMap(result));
  }

  Future<UserProfile> createUser(Map<String, dynamic> payload) async {
    final result = await _client.post('/api/users', body: payload);
    return UserProfile.fromJson(_asJsonMap(result));
  }

  Future<UserProfile> updateUser(int id, Map<String, dynamic> payload) async {
    final result = await _client.put('/api/users/$id', body: payload);
    return UserProfile.fromJson(_asJsonMap(result));
  }

  Future<void> deleteUser(int id) => _client.delete('/api/users/$id');

  Future<List<UserWordProgress>> getWordProgress(int userId) async {
    final result = await _client.get('/api/users/$userId/word-progress');
    return _asJsonList(result).map(UserWordProgress.fromJson).toList();
  }

  Future<List<UserLessonProgress>> getLessonProgress(int userId) async {
    final result = await _client.get('/api/users/$userId/lesson-progress');
    return _asJsonList(result).map(UserLessonProgress.fromJson).toList();
  }
}

class AudioPlaybackController extends ChangeNotifier {
  AudioPlaybackController() {
    _playerStateSubscription = _player.onPlayerStateChanged.listen((state) {
      _playerState = state;
      if (_activeMode != _AudioPlaybackMode.remoteFile) {
        return;
      }

      if (state == PlayerState.completed) {
        _reset();
      } else if (state == PlayerState.stopped && !_isLoading) {
        _reset();
      } else if (state == PlayerState.playing) {
        _isLoading = false;
      }
      notifyListeners();
    });

    _tts.setStartHandler(() {
      _activeMode = _AudioPlaybackMode.textToSpeech;
      _isLoading = false;
      _isSpeaking = true;
      notifyListeners();
    });
    _tts.setCompletionHandler(() {
      _reset();
      notifyListeners();
    });
    _tts.setCancelHandler(() {
      _reset();
      notifyListeners();
    });
    _tts.setErrorHandler((_) {
      _reset();
      notifyListeners();
    });

    unawaited(_initializeTts());
  }

  final AudioPlayer _player = AudioPlayer();
  final FlutterTts _tts = FlutterTts();
  late final StreamSubscription<PlayerState> _playerStateSubscription;

  String? _activePlaybackId;
  PlayerState _playerState = PlayerState.stopped;
  _AudioPlaybackMode? _activeMode;
  bool _isLoading = false;
  bool _isSpeaking = false;

  bool isPlaying(String? playbackId) {
    if (playbackId == null || _activePlaybackId != playbackId) {
      return false;
    }

    return switch (_activeMode) {
      _AudioPlaybackMode.remoteFile => _playerState == PlayerState.playing,
      _AudioPlaybackMode.textToSpeech => _isSpeaking,
      null => false,
    };
  }

  bool isLoading(String? playbackId) =>
      playbackId != null && _activePlaybackId == playbackId && _isLoading;

  Future<String?> toggle({
    required String playbackId,
    required String baseUrl,
    String? audioUrl,
    String? fallbackText,
  }) async {
    final resolvedAudioUrl = _resolveAudioUrl(audioUrl, baseUrl);
    final normalizedFallbackText = _nullIfBlank(fallbackText ?? '');
    if (resolvedAudioUrl == null && normalizedFallbackText == null) {
      return 'Аудио для этого материала ещё не добавлено.';
    }

    if (_activePlaybackId == playbackId &&
        (isPlaying(playbackId) || isLoading(playbackId))) {
      await stop();
      return null;
    }

    try {
      await stop();

      _activePlaybackId = playbackId;
      _isLoading = true;
      _isSpeaking = false;
      notifyListeners();

      if (resolvedAudioUrl != null) {
        _activeMode = _AudioPlaybackMode.remoteFile;
        await _player.play(UrlSource(resolvedAudioUrl));
        return null;
      }

      _activeMode = _AudioPlaybackMode.textToSpeech;
      await _configureTtsLanguage();
      final result = await _tts.speak(normalizedFallbackText!);
      if (result != 1) {
        _reset();
        return 'Не удалось озвучить слово на устройстве.';
      }

      return null;
    } catch (_) {
      _reset();
      return 'Не удалось воспроизвести аудио. Проверьте ссылку.';
    } finally {
      if (_activeMode != _AudioPlaybackMode.textToSpeech) {
        _isLoading = false;
        notifyListeners();
      }
    }
  }

  Future<void> stop() async {
    final activeMode = _activeMode;
    if (activeMode == null) {
      return;
    }

    if (activeMode == _AudioPlaybackMode.remoteFile) {
      await _player.stop();
    } else {
      await _tts.stop();
    }

    _reset();
    notifyListeners();
  }

  Future<void> _initializeTts() async {
    try {
      await _tts.awaitSpeakCompletion(true);
      await _tts.setSpeechRate(0.45);
      await _tts.setPitch(1.0);
      await _tts.setVolume(1.0);
      await _tts.setAudioAttributesForNavigation();
    } catch (_) {
      // Best-effort initialization: fallback still works with platform defaults.
    }
  }

  Future<void> _configureTtsLanguage() async {
    for (final candidate in const ['kk-KZ', 'kk_KZ', 'ru-RU']) {
      try {
        final availability = await _tts.isLanguageAvailable(candidate);
        final isAvailable = availability == true || availability == 1;
        if (isAvailable) {
          await _tts.setLanguage(candidate);
          return;
        }
      } catch (_) {
        // Try the next locale or let the engine keep its default language.
      }
    }
  }

  void _reset() {
    _activePlaybackId = null;
    _activeMode = null;
    _isLoading = false;
    _isSpeaking = false;
  }

  @override
  void dispose() {
    _playerStateSubscription.cancel();
    unawaited(_tts.stop());
    unawaited(_player.dispose());
    super.dispose();
  }
}

enum _AudioPlaybackMode { remoteFile, textToSpeech }

class AppController extends ChangeNotifier {
  AppController({SessionStore? sessionStore, ApiClient? apiClient})
    : _sessionStore = sessionStore ?? SessionStore(),
      _apiClient = apiClient ?? ApiClient(baseUrl: buildDefaultApiBaseUrl()),
      _baseUrl =
          (apiClient ?? ApiClient(baseUrl: buildDefaultApiBaseUrl())).baseUrl {
    _authApi = AuthApi(_apiClient);
    _learningApi = LearningApi(_apiClient);
    _userApi = UserApi(_apiClient);
    _apiClient.refreshSession = _refreshSession;
  }

  final SessionStore _sessionStore;
  final ApiClient _apiClient;
  late final AuthApi _authApi;
  late final LearningApi _learningApi;
  late final UserApi _userApi;
  final AudioPlaybackController _audioPlayback = AudioPlaybackController();

  bool _isReady = false;
  bool _isBusy = false;
  String _baseUrl;
  AuthSession? _session;

  bool get isReady => _isReady;
  bool get isBusy => _isBusy;
  String get baseUrl => _baseUrl;
  AuthSession? get session => _session;
  UserProfile? get currentUser => _session?.user;
  bool get isAuthenticated => _session != null;
  bool get canEditContent =>
      currentUser?.role == UserRole.admin ||
      currentUser?.role == UserRole.editor;
  bool get canManageUsers => currentUser?.role == UserRole.admin;
  AuthApi get authApi => _authApi;
  LearningApi get learningApi => _learningApi;
  UserApi get userApi => _userApi;
  AudioPlaybackController get audioPlayback => _audioPlayback;

  Future<void> initialize() async {
    if (_isReady) {
      return;
    }

    final state = await _sessionStore.load();
    _baseUrl = state.baseUrl;
    _apiClient.baseUrl = _baseUrl;

    if (state.session != null) {
      _session = state.session;
      _apiClient.updateSession(_session);

      try {
        final me = await _authApi.me();
        _session = _session!.copyWith(user: me);
        await _sessionStore.saveSession(_session);
      } catch (_) {
        final refreshed = await _refreshSession();
        if (refreshed == null) {
          await _clearSession(notify: false);
        }
      }
    }

    _isReady = true;
    notifyListeners();
  }

  Future<void> updateBaseUrl(String value) async {
    final normalized = value.trim();
    if (normalized.isEmpty) {
      throw ApiException(null, 'Укажите адрес сервера.');
    }

    _baseUrl = normalized;
    _apiClient.baseUrl = normalized;
    await _sessionStore.saveBaseUrl(normalized);
    notifyListeners();
  }

  Future<void> signIn({required String email, required String password}) async {
    await _runBusy(() async {
      final authSession = await _authApi.login(
        email: email,
        password: password,
      );
      await _setSession(authSession);
    });
  }

  Future<void> register({
    required String email,
    required String password,
    required String userName,
    required LanguageLevel level,
  }) async {
    await _runBusy(() async {
      final authSession = await _authApi.register(
        email: email,
        password: password,
        userName: userName,
        level: level,
      );
      await _setSession(authSession);
    });
  }

  Future<void> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    await _runBusy(() async {
      await _authApi.changePassword(
        currentPassword: currentPassword,
        newPassword: newPassword,
      );
    });
  }

  Future<UserProfile> refreshCurrentUser() async {
    final refreshedUser = await _authApi.me();
    if (_session != null) {
      _session = _session!.copyWith(user: refreshedUser);
      _apiClient.updateSession(_session);
      await _sessionStore.saveSession(_session);
      notifyListeners();
    }

    return refreshedUser;
  }

  Future<void> logout() async {
    final refreshToken = _session?.refreshToken;
    if (refreshToken != null && refreshToken.isNotEmpty) {
      try {
        await _authApi.logout(refreshToken);
      } catch (_) {}
    }

    await _clearSession();
  }

  Future<void> _setSession(AuthSession authSession) async {
    _session = authSession;
    _apiClient.updateSession(authSession);
    await _sessionStore.saveSession(authSession);
    notifyListeners();
  }

  Future<AuthSession?> _refreshSession() async {
    final refreshToken = _session?.refreshToken;
    if (refreshToken == null || refreshToken.isEmpty) {
      return null;
    }

    try {
      final refreshed = await _authApi.refresh(refreshToken);
      _session = refreshed;
      _apiClient.updateSession(refreshed);
      await _sessionStore.saveSession(refreshed);
      notifyListeners();
      return refreshed;
    } catch (_) {
      await _clearSession();
      return null;
    }
  }

  Future<void> _clearSession({bool notify = true}) async {
    _session = null;
    _apiClient.updateSession(null);
    await _sessionStore.saveSession(null);
    if (notify) {
      notifyListeners();
    }
  }

  Future<void> _runBusy(Future<void> Function() action) async {
    _isBusy = true;
    notifyListeners();
    try {
      await action();
    } finally {
      _isBusy = false;
      notifyListeners();
    }
  }

  @override
  void dispose() {
    _audioPlayback.dispose();
    super.dispose();
  }
}

class KazLangApp extends StatefulWidget {
  const KazLangApp({super.key, required this.controller});

  final AppController controller;

  @override
  State<KazLangApp> createState() => _KazLangAppState();
}

class _KazLangAppState extends State<KazLangApp> {
  @override
  void initState() {
    super.initState();
    unawaited(widget.controller.initialize());
  }

  @override
  Widget build(BuildContext context) {
    const canvas = Color(0xFFF6F1E8);
    const ink = Color(0xFF17323B);
    const accent = Color(0xFFDE8A57);
    const mint = Color(0xFF5A9185);

    return AnimatedBuilder(
      animation: widget.controller,
      builder: (context, _) {
        return MaterialApp(
          debugShowCheckedModeBanner: false,
          title: 'KazLang',
          theme: ThemeData(
            useMaterial3: true,
            scaffoldBackgroundColor: canvas,
            colorScheme: const ColorScheme.light(
              primary: ink,
              secondary: accent,
              tertiary: mint,
              surface: Colors.white,
            ),
            textTheme: const TextTheme(
              displayLarge: TextStyle(
                fontSize: 36,
                fontWeight: FontWeight.w800,
                height: 1.05,
                letterSpacing: -1.2,
                color: ink,
              ),
              headlineMedium: TextStyle(
                fontSize: 26,
                fontWeight: FontWeight.w800,
                color: ink,
              ),
              titleLarge: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w700,
                color: ink,
              ),
              bodyLarge: TextStyle(
                fontSize: 16,
                height: 1.45,
                color: Color(0xFF33545D),
              ),
              bodyMedium: TextStyle(
                fontSize: 14,
                height: 1.45,
                color: Color(0xFF5F777D),
              ),
            ),
          ),
          home: !widget.controller.isReady
              ? const _SplashScreen()
              : widget.controller.isAuthenticated
              ? KazLangShell(controller: widget.controller)
              : AuthScreen(controller: widget.controller),
        );
      },
    );
  }
}

class _SplashScreen extends StatelessWidget {
  const _SplashScreen();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFFF6F1E8), Color(0xFFE8F0EE)],
          ),
        ),
        child: const Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              CircleAvatar(
                radius: 34,
                backgroundColor: Color(0xFF17323B),
                child: Text(
                  'QZ',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 24,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
              SizedBox(height: 18),
              Text(
                'KazLang',
                style: TextStyle(
                  color: Color(0xFF17323B),
                  fontSize: 28,
                  fontWeight: FontWeight.w800,
                ),
              ),
              SizedBox(height: 10),
              Text(
                'Готовим приложение и восстанавливаем сессию...',
                style: TextStyle(color: Color(0xFF5F777D)),
              ),
              SizedBox(height: 24),
              CircularProgressIndicator(),
            ],
          ),
        ),
      ),
    );
  }
}

class AuthScreen extends StatefulWidget {
  const AuthScreen({super.key, required this.controller});

  final AppController controller;

  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabController;
  final _loginEmailController = TextEditingController();
  final _loginPasswordController = TextEditingController();
  final _registerEmailController = TextEditingController();
  final _registerPasswordController = TextEditingController();
  final _registerNameController = TextEditingController();
  final _baseUrlController = TextEditingController();
  LanguageLevel _registerLevel = LanguageLevel.a1;
  bool _serverSettingsExpanded = false;
  String? _authErrorMessage;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _baseUrlController.text = widget.controller.baseUrl;
    _loginEmailController.text = '';
    _loginPasswordController.text = '';
  }

  @override
  void dispose() {
    _tabController.dispose();
    _loginEmailController.dispose();
    _loginPasswordController.dispose();
    _registerEmailController.dispose();
    _registerPasswordController.dispose();
    _registerNameController.dispose();
    _baseUrlController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [Color(0xFFF6F1E8), Color(0xFFF1E4D7), Color(0xFFE8F0EE)],
          ),
        ),
        child: SafeArea(
          child: LayoutBuilder(
            builder: (context, constraints) {
              final isWide = constraints.maxWidth >= 900;
              return Center(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.all(20),
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 1120),
                    child: isWide
                        ? Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(child: _buildIntro(theme)),
                              const SizedBox(width: 24),
                              SizedBox(
                                width: 420,
                                child: _buildAuthCard(context),
                              ),
                            ],
                          )
                        : Column(
                            children: [
                              _buildIntro(theme),
                              const SizedBox(height: 24),
                              _buildAuthCard(context),
                            ],
                          ),
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }

  Widget _buildIntro(ThemeData theme) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.85),
            borderRadius: BorderRadius.circular(999),
          ),
          child: const Text(
            'КАЗАХСКИЙ ДЛЯ РУССКОГОВОРЯЩИХ',
            style: TextStyle(
              color: Color(0xFF3D6057),
              fontWeight: FontWeight.w700,
              letterSpacing: 1.1,
            ),
          ),
        ),
        const SizedBox(height: 18),
        Text(
          'Приложение, которое говорит с пользователем по-русски, а учит по-казахски.',
          style: theme.textTheme.displayLarge,
        ),
        const SizedBox(height: 16),
        Text(
          'Здесь собраны уроки, слова, упражнения и весь ваш прогресс по изучению казахского языка.',
          style: theme.textTheme.bodyLarge,
        ),
        const SizedBox(height: 24),
        const _FeatureStrip(
          icon: Icons.school_rounded,
          title: 'Уроки и упражнения',
          subtitle:
              'Список уроков, прохождение, проверка ответов и фиксация прогресса.',
        ),
        const SizedBox(height: 14),
        const _FeatureStrip(
          icon: Icons.auto_stories_rounded,
          title: 'Слова и категории',
          subtitle:
              'Фильтрация, словарь, наполнение уроков и связь с контентом.',
        ),
        const SizedBox(height: 14),
        const _FeatureStrip(
          icon: Icons.admin_panel_settings_rounded,
          title: 'Роли и управление',
          subtitle:
              'Для преподавателей доступны инструменты управления материалами прямо в приложении.',
        ),
      ],
    );
  }

  Widget _buildAuthCard(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(22),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.9),
        borderRadius: BorderRadius.circular(30),
        boxShadow: const [
          BoxShadow(
            color: Color(0x183C5660),
            blurRadius: 28,
            offset: Offset(0, 16),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Вход',
            style: TextStyle(
              color: Color(0xFF17323B),
              fontSize: 24,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 10),
          const Text(
            'Войдите в аккаунт или создайте новый, чтобы продолжить обучение.',
            style: TextStyle(color: Color(0xFF5F777D)),
          ),
          const SizedBox(height: 18),
          Theme(
            data: Theme.of(context).copyWith(
              dividerColor: Colors.transparent,
              splashColor: Colors.transparent,
              highlightColor: Colors.transparent,
            ),
            child: ExpansionTile(
              tilePadding: EdgeInsets.zero,
              childrenPadding: const EdgeInsets.only(bottom: 8),
              initiallyExpanded: _serverSettingsExpanded,
              onExpansionChanged: (value) {
                setState(() => _serverSettingsExpanded = value);
              },
              title: const Text(
                'Настройки сервера',
                style: TextStyle(
                  color: Color(0xFF17323B),
                  fontWeight: FontWeight.w700,
                ),
              ),
              subtitle: const Text(
                'Нужно только при первом подключении или если адрес сервера изменился.',
                style: TextStyle(color: Color(0xFF7A7066), fontSize: 12),
              ),
              children: const [SizedBox(height: 8)],
            ),
          ),
          if (_serverSettingsExpanded) ...[
            TextField(
              controller: _baseUrlController,
              decoration: const InputDecoration(
                labelText: 'Адрес сервера',
                hintText: 'http://10.0.2.2:5174',
                prefixIcon: Icon(Icons.cloud_outlined),
              ),
            ),
            const SizedBox(height: 8),
            const Text(
              'Для Android-эмулятора обычно подходит http://10.0.2.2:5174',
              style: TextStyle(color: Color(0xFF7A7066), fontSize: 12),
            ),
          ],
          const SizedBox(height: 20),
          if (_authErrorMessage != null) ...[
            _InlineNotice(message: _authErrorMessage!, isError: true),
            const SizedBox(height: 16),
          ],
          TabBar(
            controller: _tabController,
            labelColor: const Color(0xFF17323B),
            indicatorColor: const Color(0xFFDE8A57),
            tabs: const [
              Tab(text: 'Вход'),
              Tab(text: 'Регистрация'),
            ],
          ),
          const SizedBox(height: 18),
          SizedBox(
            height: 360,
            child: TabBarView(
              controller: _tabController,
              children: [_buildLoginForm(context), _buildRegisterForm(context)],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildLoginForm(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        TextField(
          controller: _loginEmailController,
          decoration: const InputDecoration(
            labelText: 'Email',
            prefixIcon: Icon(Icons.mail_outline),
          ),
        ),
        const SizedBox(height: 14),
        TextField(
          controller: _loginPasswordController,
          obscureText: true,
          decoration: const InputDecoration(
            labelText: 'Пароль',
            prefixIcon: Icon(Icons.lock_outline),
          ),
        ),
        const Spacer(),
        SizedBox(
          width: double.infinity,
          child: FilledButton(
            onPressed: widget.controller.isBusy ? null : _submitLogin,
            style: FilledButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 18),
              backgroundColor: const Color(0xFF17323B),
            ),
            child: widget.controller.isBusy
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Text('Войти'),
          ),
        ),
      ],
    );
  }

  Widget _buildRegisterForm(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        children: [
          TextField(
            controller: _registerNameController,
            decoration: const InputDecoration(
              labelText: 'Имя пользователя',
              prefixIcon: Icon(Icons.person_outline),
            ),
          ),
          const SizedBox(height: 14),
          TextField(
            controller: _registerEmailController,
            decoration: const InputDecoration(
              labelText: 'Email',
              prefixIcon: Icon(Icons.mail_outline),
            ),
          ),
          const SizedBox(height: 14),
          TextField(
            controller: _registerPasswordController,
            obscureText: true,
            decoration: const InputDecoration(
              labelText: 'Пароль',
              prefixIcon: Icon(Icons.lock_outline),
            ),
          ),
          const SizedBox(height: 14),
          DropdownButtonFormField<LanguageLevel>(
            initialValue: _registerLevel,
            items: LanguageLevel.values
                .map(
                  (level) =>
                      DropdownMenuItem(value: level, child: Text(level.label)),
                )
                .toList(),
            onChanged: (value) {
              if (value != null) {
                setState(() => _registerLevel = value);
              }
            },
            decoration: const InputDecoration(
              labelText: 'Уровень',
              prefixIcon: Icon(Icons.flag_outlined),
            ),
          ),
          const SizedBox(height: 18),
          SizedBox(
            width: double.infinity,
            child: FilledButton(
              onPressed: widget.controller.isBusy ? null : _submitRegister,
              style: FilledButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 18),
                backgroundColor: const Color(0xFF17323B),
              ),
              child: widget.controller.isBusy
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Создать аккаунт'),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _submitLogin() async {
    try {
      setState(() => _authErrorMessage = null);
      await widget.controller.updateBaseUrl(_baseUrlController.text);
      await widget.controller.signIn(
        email: _loginEmailController.text.trim(),
        password: _loginPasswordController.text,
      );
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Вход выполнен.');
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      setState(() => _authErrorMessage = error.message);
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _submitRegister() async {
    try {
      setState(() => _authErrorMessage = null);
      await widget.controller.updateBaseUrl(_baseUrlController.text);
      await widget.controller.register(
        email: _registerEmailController.text.trim(),
        password: _registerPasswordController.text,
        userName: _registerNameController.text.trim(),
        level: _registerLevel,
      );
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Аккаунт создан и сессия открыта.');
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      setState(() => _authErrorMessage = error.message);
      _showSnack(context, error.message, isError: true);
    }
  }
}

class KazLangShell extends StatefulWidget {
  const KazLangShell({super.key, required this.controller});

  final AppController controller;

  @override
  State<KazLangShell> createState() => _KazLangShellState();
}

class _KazLangShellState extends State<KazLangShell> {
  int _selectedIndex = 0;

  @override
  Widget build(BuildContext context) {
    final pages = <Widget>[
      DashboardPage(controller: widget.controller),
      LessonsPage(controller: widget.controller),
      WordsPage(controller: widget.controller),
      ProfilePage(controller: widget.controller),
      if (widget.controller.canEditContent)
        ManagementPage(controller: widget.controller),
    ];

    final destinations = <NavigationDestination>[
      const NavigationDestination(
        icon: Icon(Icons.home_outlined),
        selectedIcon: Icon(Icons.home_rounded),
        label: 'Главная',
      ),
      const NavigationDestination(
        icon: Icon(Icons.school_outlined),
        selectedIcon: Icon(Icons.school_rounded),
        label: 'Уроки',
      ),
      const NavigationDestination(
        icon: Icon(Icons.auto_stories_outlined),
        selectedIcon: Icon(Icons.auto_stories_rounded),
        label: 'Слова',
      ),
      const NavigationDestination(
        icon: Icon(Icons.person_outline_rounded),
        selectedIcon: Icon(Icons.person_rounded),
        label: 'Профиль',
      ),
      if (widget.controller.canEditContent)
        const NavigationDestination(
          icon: Icon(Icons.tune_outlined),
          selectedIcon: Icon(Icons.tune_rounded),
          label: 'Управление',
        ),
    ];

    return Scaffold(
      body: IndexedStack(index: _selectedIndex, children: pages),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedIndex,
        onDestinationSelected: (value) =>
            setState(() => _selectedIndex = value),
        destinations: destinations,
      ),
    );
  }
}

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key, required this.controller});

  final AppController controller;

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  bool _loading = true;
  List<UserLessonProgress> _lessonProgress = const [];
  List<UserWordProgress> _wordProgress = const [];
  List<Lesson> _recommendedLessons = const [];

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final userId = widget.controller.currentUser!.id;
      final results = await Future.wait<dynamic>([
        widget.controller.userApi.getLessonProgress(userId),
        widget.controller.userApi.getWordProgress(userId),
        widget.controller.learningApi.getLessons(pageSize: 3, sortBy: 'order'),
      ]);

      setState(() {
        _lessonProgress = results[0] as List<UserLessonProgress>;
        _wordProgress = results[1] as List<UserWordProgress>;
        _recommendedLessons = (results[2] as PagedResponse<Lesson>).items;
      });
    } on ApiException catch (error) {
      if (mounted) {
        _showSnack(context, error.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = widget.controller.currentUser!;
    bool isReadyForReview(UserWordProgress item) {
      if (item.nextReviewAt != null) {
        return !item.nextReviewAt!.isAfter(DateTime.now());
      }

      return item.status == WordProgressStatus.review;
    }

    final masteredCount = _wordProgress
        .where((item) => item.status == WordProgressStatus.mastered)
        .length;
    final reviewCount = _wordProgress.where(isReadyForReview).length;
    final completedLessons = _lessonProgress
        .where((item) => item.isCompleted)
        .length;
    final activeWords = List<UserWordProgress>.of(_wordProgress)
      ..sort((left, right) {
        final leftDue = isReadyForReview(left);
        final rightDue = isReadyForReview(right);
        if (leftDue != rightDue) {
          return leftDue ? -1 : 1;
        }

        final leftDate =
            left.nextReviewAt ?? DateTime.fromMillisecondsSinceEpoch(0);
        final rightDate =
            right.nextReviewAt ?? DateTime.fromMillisecondsSinceEpoch(0);
        return leftDate.compareTo(rightDate);
      });

    return _GradientPage(
      title: 'Сәлем, ${user.userName}',
      subtitle:
          'Здесь собран ваш учебный маршрут, прогресс по словам и ближайшие уроки.',
      onRefresh: _load,
      child: _loading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                Row(
                  children: [
                    Expanded(
                      child: _MetricCard(
                        title: 'Пройдено уроков',
                        value: '$completedLessons',
                        caption: 'из ${_lessonProgress.length}',
                        color: const Color(0xFF17323B),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _MetricCard(
                        title: 'Освоено слов',
                        value: '$masteredCount',
                        caption: 'на долгой памяти',
                        color: const Color(0xFF5A9185),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                _MetricCard(
                  title: 'Нужно повторить',
                  value: '$reviewCount',
                  caption: 'слов уже готовы к повторению сегодня',
                  color: const Color(0xFFDE8A57),
                ),
                const SizedBox(height: 18),
                _SectionPanel(
                  title: 'Следующие уроки',
                  subtitle: 'То, что стоит открыть в первую очередь.',
                  child: Column(
                    children: _recommendedLessons
                        .map(
                          (lesson) => ListTile(
                            contentPadding: EdgeInsets.zero,
                            title: Text(lesson.title),
                            subtitle: Text(
                              lesson.description ??
                                  'Описание пока не добавлено.',
                            ),
                            trailing: _LevelBadge(label: lesson.level.label),
                          ),
                        )
                        .toList(),
                  ),
                ),
                const SizedBox(height: 18),
                _SectionPanel(
                  title: 'Слова в работе',
                  subtitle:
                      'Сначала показываются слова, которые уже пора повторить.',
                  child: Column(
                    children: activeWords.take(5).map((item) {
                      return ListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(item.kazakhText),
                        subtitle: Text(item.russianTranslation),
                        trailing: Text(
                          item.status.label,
                          style: const TextStyle(
                            color: Color(0xFF5F777D),
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      );
                    }).toList(),
                  ),
                ),
              ],
            ),
    );
  }
}

class LessonsPage extends StatefulWidget {
  const LessonsPage({super.key, required this.controller});

  final AppController controller;

  @override
  State<LessonsPage> createState() => _LessonsPageState();
}

class _LessonsPageState extends State<LessonsPage> {
  bool _loading = true;
  final _searchController = TextEditingController();
  List<Lesson> _lessons = const [];

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final response = await widget.controller.learningApi.getLessons(
        search: _nullIfBlank(_searchController.text),
        sortBy: 'order',
      );
      setState(() => _lessons = response.items);
    } on ApiException catch (error) {
      if (mounted) {
        _showSnack(context, error.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _createLesson() async {
    final payload = await showLessonDialog(context);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.createLesson(payload);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Урок создан.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _editLesson(Lesson lesson) async {
    final payload = await showLessonDialog(context, initial: lesson);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.updateLesson(lesson.id, payload);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Урок обновлён.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _deleteLesson(Lesson lesson) async {
    final confirmed = await showConfirmDialog(
      context,
      title: 'Удалить урок?',
      message: 'Урок "${lesson.title}" будет удалён.',
    );
    if (!confirmed) {
      return;
    }

    try {
      await widget.controller.learningApi.deleteLesson(lesson.id);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Урок удалён.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return _GradientPage(
      title: 'Уроки',
      subtitle:
          'Здесь пользователь видит список уроков, а редактор может управлять контентом.',
      action: widget.controller.canEditContent
          ? IconButton(
              onPressed: _createLesson,
              icon: const Icon(Icons.add_circle_outline_rounded),
            )
          : null,
      onRefresh: _load,
      child: Column(
        children: [
          _SearchInput(
            controller: _searchController,
            hintText: 'Поиск по названию или описанию',
            onSubmitted: (_) => _load(),
            onRefresh: _load,
          ),
          const SizedBox(height: 18),
          if (_loading)
            const Padding(
              padding: EdgeInsets.only(top: 48),
              child: CircularProgressIndicator(),
            )
          else if (_lessons.isEmpty)
            const _EmptyState(
              title: 'Уроков пока нет',
              subtitle: 'Когда уроки появятся, они будут показаны здесь.',
            )
          else
            Column(
              children: _lessons
                  .map(
                    (lesson) => _ListCard(
                      title: lesson.title,
                      subtitle:
                          lesson.description ??
                          'Описание для урока ещё не задано.',
                      chips: [
                        lesson.level.label,
                        if (widget.controller.canEditContent)
                          'Порядок ${lesson.order}',
                        if (widget.controller.canEditContent)
                          lesson.isPublished ? 'Опубликован' : 'Черновик',
                      ],
                      onTap: () async {
                        await Navigator.of(context).push(
                          MaterialPageRoute(
                            builder: (_) => LessonDetailPage(
                              controller: widget.controller,
                              lessonId: lesson.id,
                            ),
                          ),
                        );
                        if (mounted) {
                          await _load();
                        }
                      },
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          if (_nullIfBlank(lesson.audioUrl ?? '') != null)
                            _AudioActionButton(
                              controller: widget.controller.audioPlayback,
                              playbackId: 'lesson:${lesson.id}',
                              baseUrl: widget.controller.baseUrl,
                              audioUrl: lesson.audioUrl,
                              tooltip: 'Слушать урок',
                              compact: true,
                            ),
                          if (widget.controller.canEditContent)
                            PopupMenuButton<String>(
                              onSelected: (value) {
                                if (value == 'edit') {
                                  _editLesson(lesson);
                                } else if (value == 'delete') {
                                  _deleteLesson(lesson);
                                }
                              },
                              itemBuilder: (context) => const [
                                PopupMenuItem(
                                  value: 'edit',
                                  child: Text('Редактировать'),
                                ),
                                PopupMenuItem(
                                  value: 'delete',
                                  child: Text('Удалить'),
                                ),
                              ],
                            ),
                        ],
                      ),
                    ),
                  )
                  .toList(),
            ),
        ],
      ),
    );
  }
}

class LessonDetailPage extends StatefulWidget {
  const LessonDetailPage({
    super.key,
    required this.controller,
    required this.lessonId,
  });

  final AppController controller;
  final int lessonId;

  @override
  State<LessonDetailPage> createState() => _LessonDetailPageState();
}

class _LessonDetailPageState extends State<LessonDetailPage> {
  bool _loading = true;
  Lesson? _lesson;
  List<LessonWord> _words = const [];
  List<ExerciseBundle> _exerciseBundles = const [];
  final Map<int, SubmitAnswerResult> _results = <int, SubmitAnswerResult>{};

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final lesson = await widget.controller.learningApi.getLesson(
        widget.lessonId,
      );
      final words = await widget.controller.learningApi.getLessonWords(
        widget.lessonId,
      );
      final exercises = await widget.controller.learningApi.getExercises(
        widget.lessonId,
      );
      final bundles = <ExerciseBundle>[];
      for (final exercise in exercises) {
        final options = await widget.controller.learningApi.getExerciseOptions(
          exercise.id,
        );
        final shuffledOptions = List<ExerciseOption>.of(options)..shuffle();
        bundles.add(
          ExerciseBundle(exercise: exercise, options: shuffledOptions),
        );
      }

      setState(() {
        _lesson = lesson;
        _words = words;
        _exerciseBundles = bundles;
      });
    } on ApiException catch (error) {
      if (mounted) {
        _showSnack(context, error.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _editLesson() async {
    if (_lesson == null) {
      return;
    }

    final payload = await showLessonDialog(context, initial: _lesson);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.updateLesson(_lesson!.id, payload);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Данные урока обновлены.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _addWordToLesson() async {
    final wordsResponse = await widget.controller.learningApi.getWords(
      pageSize: 200,
    );
    if (!mounted) {
      return;
    }

    if (wordsResponse.items.isEmpty) {
      _showSnack(
        context,
        'Сначала создайте слова в словаре, затем их можно будет привязать к уроку.',
        isError: true,
      );
      return;
    }

    final payload = await showAddWordToLessonDialog(
      context,
      words: wordsResponse.items,
      initialOrder: _words.length + 1,
    );
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.addWordToLesson(
        lessonId: widget.lessonId,
        wordId: payload['wordId'] as int,
        order: payload['order'] as int,
      );
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Слово добавлено в урок.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _removeWordFromLesson(LessonWord word) async {
    final confirmed = await showConfirmDialog(
      context,
      title: 'Убрать слово из урока?',
      message: 'Слово "${word.kazakhText}" будет отвязано от этого урока.',
    );
    if (!confirmed) {
      return;
    }

    try {
      await widget.controller.learningApi.removeWordFromLesson(
        lessonId: widget.lessonId,
        wordId: word.wordId,
      );
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Слово убрано.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _createExercise() async {
    final payload = await showExerciseDialog(context);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.createExercise(
        lessonId: widget.lessonId,
        payload: payload,
      );
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Упражнение создано.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _editExercise(Exercise exercise) async {
    final payload = await showExerciseDialog(context, initial: exercise);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.updateExercise(exercise.id, payload);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Упражнение обновлено.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _deleteExercise(Exercise exercise) async {
    final confirmed = await showConfirmDialog(
      context,
      title: 'Удалить упражнение?',
      message: 'Упражнение "${exercise.question}" будет удалено.',
    );
    if (!confirmed) {
      return;
    }

    try {
      await widget.controller.learningApi.deleteExercise(exercise.id);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Упражнение удалено.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _manageOptions(Exercise exercise) async {
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (context) {
        return ExerciseOptionsSheet(
          controller: widget.controller,
          exercise: exercise,
          onChanged: _load,
        );
      },
    );
  }

  Future<void> _submitAnswer({
    required int exerciseId,
    required int optionId,
  }) async {
    try {
      final result = await widget.controller.learningApi.submitAnswer(
        exerciseId: exerciseId,
        optionId: optionId,
      );
      setState(() => _results[exerciseId] = result);
      if (!mounted) {
        return;
      }
      _showSnack(
        context,
        result.isCorrect
            ? 'Ответ верный.'
            : 'Ответ неверный. Проверь подсказку ниже.',
        isError: !result.isCorrect,
      );
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    final lesson = _lesson;
    return Scaffold(
      appBar: AppBar(
        title: Text(lesson?.title ?? 'Урок'),
        actions: widget.controller.canEditContent && lesson != null
            ? [
                IconButton(
                  onPressed: _editLesson,
                  icon: const Icon(Icons.edit_outlined),
                ),
              ]
            : null,
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : lesson == null
          ? const _EmptyState(
              title: 'Урок не найден',
              subtitle: 'Не получилось загрузить данные урока.',
            )
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  _SectionPanel(
                    title: lesson.title,
                    subtitle:
                        lesson.description ?? 'Описание урока пока не задано.',
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Wrap(
                          spacing: 10,
                          runSpacing: 10,
                          children: [
                            _InfoPill(label: lesson.level.label),
                            if (widget.controller.canEditContent)
                              _InfoPill(label: 'Порядок ${lesson.order}'),
                            if (widget.controller.canEditContent)
                              _InfoPill(
                                label: lesson.isPublished
                                    ? 'Опубликован'
                                    : 'Черновик',
                              ),
                          ],
                        ),
                        if (_nullIfBlank(lesson.audioUrl ?? '') != null) ...[
                          const SizedBox(height: 16),
                          _AudioActionButton(
                            controller: widget.controller.audioPlayback,
                            playbackId: 'lesson:${lesson.id}',
                            baseUrl: widget.controller.baseUrl,
                            audioUrl: lesson.audioUrl,
                            label: 'Слушать урок',
                            tooltip: 'Воспроизвести аудио урока',
                          ),
                        ],
                      ],
                    ),
                  ),
                  const SizedBox(height: 18),
                  _SectionPanel(
                    title: 'Слова урока',
                    subtitle:
                        'Словарь, который пользователь изучает внутри урока.',
                    action: widget.controller.canEditContent
                        ? IconButton(
                            onPressed: _addWordToLesson,
                            icon: const Icon(Icons.add_circle_outline),
                          )
                        : null,
                    child: _words.isEmpty
                        ? const Text('Слова ещё не добавлены.')
                        : Column(
                            children: _words.map((word) {
                              return ListTile(
                                contentPadding: EdgeInsets.zero,
                                title: Text(word.kazakhText),
                                subtitle: Text(
                                  '${word.russianTranslation}'
                                  '${word.pronunciation != null ? ' - ${word.pronunciation}' : ''}',
                                ),
                                trailing: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    _AudioActionButton(
                                      controller:
                                          widget.controller.audioPlayback,
                                      playbackId: 'lesson-word:${word.wordId}',
                                      baseUrl: widget.controller.baseUrl,
                                      audioUrl: word.audioUrl,
                                      fallbackText:
                                          word.pronunciation ?? word.kazakhText,
                                      tooltip: 'Слушать слово',
                                      compact: true,
                                    ),
                                    if (widget.controller.canEditContent)
                                      IconButton(
                                        onPressed: () =>
                                            _removeWordFromLesson(word),
                                        icon: const Icon(Icons.close_rounded),
                                      )
                                    else
                                      Text('#${word.order}'),
                                  ],
                                ),
                              );
                            }).toList(),
                          ),
                  ),
                  const SizedBox(height: 18),
                  _SectionPanel(
                    title: 'Упражнения',
                    subtitle:
                        'Пользователь проходит задания здесь же, без выхода в другой экран.',
                    action: widget.controller.canEditContent
                        ? IconButton(
                            onPressed: _createExercise,
                            icon: const Icon(Icons.add_circle_outline),
                          )
                        : null,
                    child: _exerciseBundles.isEmpty
                        ? const Text('Упражнения ещё не созданы.')
                        : Column(
                            children: _exerciseBundles.map((bundle) {
                              final result = _results[bundle.exercise.id];
                              return Container(
                                margin: const EdgeInsets.only(bottom: 14),
                                padding: const EdgeInsets.all(16),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFF8F4EF),
                                  borderRadius: BorderRadius.circular(22),
                                ),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Row(
                                      children: [
                                        Expanded(
                                          child: Text(
                                            '${bundle.exercise.order}. ${bundle.exercise.question}',
                                            style: const TextStyle(
                                              color: Color(0xFF17323B),
                                              fontSize: 17,
                                              fontWeight: FontWeight.w700,
                                            ),
                                          ),
                                        ),
                                        if (widget.controller.canEditContent)
                                          PopupMenuButton<String>(
                                            onSelected: (value) {
                                              if (value == 'edit') {
                                                _editExercise(bundle.exercise);
                                              } else if (value == 'delete') {
                                                _deleteExercise(
                                                  bundle.exercise,
                                                );
                                              } else if (value == 'options') {
                                                _manageOptions(bundle.exercise);
                                              }
                                            },
                                            itemBuilder: (_) => const [
                                              PopupMenuItem(
                                                value: 'edit',
                                                child: Text('Редактировать'),
                                              ),
                                              PopupMenuItem(
                                                value: 'options',
                                                child: Text('Варианты ответов'),
                                              ),
                                              PopupMenuItem(
                                                value: 'delete',
                                                child: Text('Удалить'),
                                              ),
                                            ],
                                          ),
                                      ],
                                    ),
                                    const SizedBox(height: 8),
                                    Text(
                                      bundle.exercise.type.label,
                                      style: const TextStyle(
                                        color: Color(0xFF5F777D),
                                        fontWeight: FontWeight.w600,
                                      ),
                                    ),
                                    const SizedBox(height: 14),
                                    Wrap(
                                      spacing: 10,
                                      runSpacing: 10,
                                      children: bundle.options.map((option) {
                                        return FilledButton.tonal(
                                          onPressed: () => _submitAnswer(
                                            exerciseId: bundle.exercise.id,
                                            optionId: option.id,
                                          ),
                                          child: Text(option.text),
                                        );
                                      }).toList(),
                                    ),
                                    if (result != null) ...[
                                      const SizedBox(height: 14),
                                      Container(
                                        width: double.infinity,
                                        padding: const EdgeInsets.all(14),
                                        decoration: BoxDecoration(
                                          color: result.isCorrect
                                              ? const Color(0xFFE4F1ED)
                                              : const Color(0xFFF8E3DA),
                                          borderRadius: BorderRadius.circular(
                                            18,
                                          ),
                                        ),
                                        child: Column(
                                          crossAxisAlignment:
                                              CrossAxisAlignment.start,
                                          children: [
                                            Text(
                                              result.isCorrect
                                                  ? 'Верно'
                                                  : 'Нужно ещё раз',
                                              style: TextStyle(
                                                color: result.isCorrect
                                                    ? const Color(0xFF35655B)
                                                    : const Color(0xFF8B4D28),
                                                fontWeight: FontWeight.w700,
                                              ),
                                            ),
                                            if (result.correctAnswer !=
                                                null) ...[
                                              const SizedBox(height: 6),
                                              Text(
                                                'Правильный ответ: ${result.correctAnswer}',
                                              ),
                                            ],
                                            if (result.explanation != null) ...[
                                              const SizedBox(height: 6),
                                              Text(result.explanation!),
                                            ],
                                            if (result.lessonScore != null) ...[
                                              const SizedBox(height: 6),
                                              Text(
                                                'Текущий score урока: ${result.lessonScore}',
                                              ),
                                            ],
                                          ],
                                        ),
                                      ),
                                    ],
                                  ],
                                ),
                              );
                            }).toList(),
                          ),
                  ),
                ],
              ),
            ),
    );
  }
}

class WordsPage extends StatefulWidget {
  const WordsPage({super.key, required this.controller});

  final AppController controller;

  @override
  State<WordsPage> createState() => _WordsPageState();
}

class _WordsPageState extends State<WordsPage> {
  bool _loading = true;
  final _searchController = TextEditingController();
  List<WordEntry> _words = const [];
  List<Category> _categories = const [];
  List<UserWordProgress> _wordProgress = const [];
  LanguageLevel? _selectedLevel;
  int? _selectedCategoryId;
  bool? _selectedIsActive;
  _WordProgressFilter _selectedProgressFilter = _WordProgressFilter.all;

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final userId = widget.controller.currentUser!.id;
      final results = await Future.wait<dynamic>([
        widget.controller.learningApi.getCategories(pageSize: 200),
        widget.controller.learningApi.getWords(
          search: _nullIfBlank(_searchController.text),
          level: _selectedLevel,
          categoryId: _selectedCategoryId,
          isActive: _selectedIsActive,
          sortBy: 'kazakhText',
        ),
        widget.controller.userApi.getWordProgress(userId),
      ]);

      setState(() {
        _categories = (results[0] as PagedResponse<Category>).items;
        _words = (results[1] as PagedResponse<WordEntry>).items;
        _wordProgress = results[2] as List<UserWordProgress>;
      });
    } on ApiException catch (error) {
      if (mounted) {
        _showSnack(context, error.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _createWord() async {
    if (_categories.isEmpty) {
      _showSnack(
        context,
        'Сначала создайте хотя бы одну категорию.',
        isError: true,
      );
      return;
    }

    final payload = await showWordDialog(context, categories: _categories);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.createWord(payload);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Слово добавлено.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _editWord(WordEntry word) async {
    final payload = await showWordDialog(
      context,
      categories: _categories,
      initial: word,
    );
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.updateWord(word.id, payload);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Слово обновлено.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _deleteWord(WordEntry word) async {
    final confirmed = await showConfirmDialog(
      context,
      title: 'Удалить слово?',
      message: 'Слово "${word.kazakhText}" будет удалено.',
    );
    if (!confirmed) {
      return;
    }

    try {
      await widget.controller.learningApi.deleteWord(word.id);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Слово удалено.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  bool _isReadyForReview(UserWordProgress progress) {
    final nextReviewAt = progress.nextReviewAt;
    if (nextReviewAt != null) {
      return !nextReviewAt.isAfter(DateTime.now());
    }

    return progress.status == WordProgressStatus.review;
  }

  bool _matchesProgressFilter(UserWordProgress? progress) {
    return switch (_selectedProgressFilter) {
      _WordProgressFilter.all => true,
      _WordProgressFilter.fresh =>
        progress == null || progress.status == WordProgressStatus.fresh,
      _WordProgressFilter.learning =>
        progress != null && progress.status == WordProgressStatus.learning,
      _WordProgressFilter.dueReview =>
        progress != null && _isReadyForReview(progress),
      _WordProgressFilter.mastered =>
        progress != null && progress.status == WordProgressStatus.mastered,
    };
  }

  String _buildWordDescription(WordEntry word, UserWordProgress? progress) {
    final parts = <String>[];
    if (_nullIfBlank(word.pronunciation ?? '') != null) {
      parts.add('Произношение: ${word.pronunciation!.trim()}');
    }

    if (_nullIfBlank(word.example ?? '') != null) {
      parts.add(word.example!.trim());
    }

    if (progress != null) {
      if (_isReadyForReview(progress)) {
        parts.add('Пора повторить это слово сейчас.');
      } else if (progress.nextReviewAt != null) {
        parts.add(
          'Следующее повторение: ${_formatDateTimeShort(progress.nextReviewAt!)}',
        );
      }
    }

    return parts.join('\n');
  }

  Future<void> _startReview(List<UserWordProgress> reviewWords) async {
    if (reviewWords.isEmpty) {
      _showSnack(context, 'Пока нет слов для повторения.');
      return;
    }

    await showDialog<void>(
      context: context,
      builder: (context) => _WordReviewDialog(words: reviewWords),
    );
  }

  @override
  Widget build(BuildContext context) {
    final categoryMap = {
      for (final category in _categories) category.id: category.name,
    };
    final progressByWordId = {
      for (final progress in _wordProgress) progress.wordId: progress,
    };
    final reviewWords = _wordProgress.where(_isReadyForReview).toList()
      ..sort((left, right) {
        final leftValue =
            left.nextReviewAt ?? DateTime.fromMillisecondsSinceEpoch(0);
        final rightValue =
            right.nextReviewAt ?? DateTime.fromMillisecondsSinceEpoch(0);
        return leftValue.compareTo(rightValue);
      });
    final visibleWords = _words.where((word) {
      return _matchesProgressFilter(progressByWordId[word.id]);
    }).toList();

    return _GradientPage(
      title: 'Слова',
      subtitle:
          'Здесь видно, какие слова уже освоены, какие изучаются и какие пора повторить.',
      action: widget.controller.canEditContent
          ? IconButton(
              onPressed: _createWord,
              icon: const Icon(Icons.add_circle_outline),
            )
          : null,
      onRefresh: _load,
      child: Column(
        children: [
          _SearchInput(
            controller: _searchController,
            hintText: 'Найти слово или перевод',
            onSubmitted: (_) => _load(),
            onRefresh: _load,
          ),
          const SizedBox(height: 14),
          if (reviewWords.isNotEmpty) ...[
            _SectionPanel(
              title: 'Повторение сегодня',
              subtitle:
                  'Эти слова уже пора освежить в памяти. Карточки открываются по-русски, чтобы тренировать перевод на казахский.',
              action: FilledButton.tonal(
                onPressed: () => _startReview(reviewWords),
                child: const Text('Начать'),
              ),
              child: Column(
                children: reviewWords.take(3).map((item) {
                  final nextReviewText = item.nextReviewAt == null
                      ? 'Можно повторять уже сейчас'
                      : 'С ${_formatDateTimeShort(item.nextReviewAt!)}';
                  return ListTile(
                    contentPadding: EdgeInsets.zero,
                    title: Text(item.russianTranslation),
                    subtitle: Text(item.kazakhText),
                    trailing: Text(
                      nextReviewText,
                      style: const TextStyle(
                        color: Color(0xFF8B4D28),
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  );
                }).toList(),
              ),
            ),
            const SizedBox(height: 14),
          ],
          Align(
            alignment: Alignment.centerLeft,
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              children: _WordProgressFilter.values.map((filter) {
                return ChoiceChip(
                  label: Text(filter.label),
                  selected: _selectedProgressFilter == filter,
                  onSelected: (_) {
                    setState(() => _selectedProgressFilter = filter);
                  },
                );
              }).toList(),
            ),
          ),
          const SizedBox(height: 14),
          Wrap(
            spacing: 12,
            runSpacing: 12,
            children: [
              SizedBox(
                width: 160,
                child: DropdownButtonFormField<LanguageLevel?>(
                  initialValue: _selectedLevel,
                  items: [
                    const DropdownMenuItem<LanguageLevel?>(
                      value: null,
                      child: Text('Все уровни'),
                    ),
                    ...LanguageLevel.values.map(
                      (level) => DropdownMenuItem<LanguageLevel?>(
                        value: level,
                        child: Text(level.label),
                      ),
                    ),
                  ],
                  onChanged: (value) => setState(() => _selectedLevel = value),
                  decoration: const InputDecoration(labelText: 'Уровень'),
                ),
              ),
              SizedBox(
                width: 190,
                child: DropdownButtonFormField<int?>(
                  initialValue: _selectedCategoryId,
                  items: [
                    const DropdownMenuItem<int?>(
                      value: null,
                      child: Text('Все категории'),
                    ),
                    ..._categories.map(
                      (category) => DropdownMenuItem<int?>(
                        value: category.id,
                        child: Text(category.name),
                      ),
                    ),
                  ],
                  onChanged: (value) =>
                      setState(() => _selectedCategoryId = value),
                  decoration: const InputDecoration(labelText: 'Категория'),
                ),
              ),
              SizedBox(
                width: 170,
                child: DropdownButtonFormField<bool?>(
                  initialValue: _selectedIsActive,
                  items: const [
                    DropdownMenuItem<bool?>(
                      value: null,
                      child: Text('Любой статус'),
                    ),
                    DropdownMenuItem<bool?>(
                      value: true,
                      child: Text('Только активные'),
                    ),
                    DropdownMenuItem<bool?>(
                      value: false,
                      child: Text('Только скрытые'),
                    ),
                  ],
                  onChanged: (value) =>
                      setState(() => _selectedIsActive = value),
                  decoration: const InputDecoration(labelText: 'Статус'),
                ),
              ),
              FilledButton.tonal(
                onPressed: _load,
                child: const Text('Применить'),
              ),
            ],
          ),
          const SizedBox(height: 18),
          if (_loading)
            const Padding(
              padding: EdgeInsets.only(top: 48),
              child: CircularProgressIndicator(),
            )
          else if (visibleWords.isEmpty)
            const _EmptyState(
              title: 'Ничего не найдено',
              subtitle: 'Попробуйте изменить фильтры или ввести другой запрос.',
            )
          else
            Column(
              children: visibleWords.map((word) {
                final progress = progressByWordId[word.id];
                final description = _buildWordDescription(word, progress);
                return _ListCard(
                  title: word.kazakhText,
                  subtitle: word.russianTranslation,
                  description: description.isEmpty ? null : description,
                  chips: [
                    progress?.status.label ?? 'Ещё не изучалось',
                    if (progress != null && _isReadyForReview(progress))
                      'Пора повторить',
                    word.level.label,
                    categoryMap[word.categoryId] ??
                        'Категория #${word.categoryId}',
                    word.isActive ? 'Активно' : 'Скрыто',
                  ],
                  trailing: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      _AudioActionButton(
                        controller: widget.controller.audioPlayback,
                        playbackId: 'word:${word.id}',
                        baseUrl: widget.controller.baseUrl,
                        audioUrl: word.audioUrl,
                        fallbackText: word.pronunciation ?? word.kazakhText,
                        tooltip: 'Слушать слово',
                        compact: true,
                      ),
                      if (widget.controller.canEditContent)
                        PopupMenuButton<String>(
                          onSelected: (value) {
                            if (value == 'edit') {
                              _editWord(word);
                            } else if (value == 'delete') {
                              _deleteWord(word);
                            }
                          },
                          itemBuilder: (_) => const [
                            PopupMenuItem(
                              value: 'edit',
                              child: Text('Редактировать'),
                            ),
                            PopupMenuItem(
                              value: 'delete',
                              child: Text('Удалить'),
                            ),
                          ],
                        ),
                    ],
                  ),
                );
              }).toList(),
            ),
        ],
      ),
    );
  }
}

class ProfilePage extends StatefulWidget {
  const ProfilePage({super.key, required this.controller});

  final AppController controller;

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  bool _loading = true;
  List<UserLessonProgress> _lessonProgress = const [];
  List<UserWordProgress> _wordProgress = const [];

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final user = await widget.controller.refreshCurrentUser();
      final results = await Future.wait<dynamic>([
        widget.controller.userApi.getLessonProgress(user.id),
        widget.controller.userApi.getWordProgress(user.id),
      ]);

      setState(() {
        _lessonProgress = results[0] as List<UserLessonProgress>;
        _wordProgress = results[1] as List<UserWordProgress>;
      });
    } on ApiException catch (error) {
      if (mounted) {
        _showSnack(context, error.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _changePassword() async {
    final payload = await showChangePasswordDialog(context);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.changePassword(
        currentPassword: payload['currentPassword'] as String,
        newPassword: payload['newPassword'] as String,
      );
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Пароль обновлён. При необходимости войдите снова.');
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _logout() async {
    await widget.controller.logout();
    if (!mounted) {
      return;
    }
    _showSnack(context, 'Сессия завершена.');
  }

  @override
  Widget build(BuildContext context) {
    final user = widget.controller.currentUser!;
    return _GradientPage(
      title: 'Профиль',
      subtitle:
          'Личные данные, смена пароля и индивидуальный учебный прогресс.',
      onRefresh: _load,
      child: _loading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                _SectionPanel(
                  title: user.userName,
                  subtitle: user.email,
                  child: Wrap(
                    spacing: 10,
                    runSpacing: 10,
                    children: [
                      _InfoPill(label: user.role.label),
                      _InfoPill(label: user.level.label),
                      _InfoPill(
                        label: user.emailConfirmed
                            ? 'Email подтверждён'
                            : 'Email не подтверждён',
                      ),
                      _InfoPill(
                        label: user.isActive
                            ? 'Аккаунт активен'
                            : 'Аккаунт отключён',
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 18),
                Row(
                  children: [
                    Expanded(
                      child: FilledButton.tonal(
                        onPressed: _changePassword,
                        child: const Text('Сменить пароль'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: OutlinedButton(
                        onPressed: _logout,
                        child: const Text('Выйти'),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 18),
                _SectionPanel(
                  title: 'Прогресс по урокам',
                  subtitle: 'История завершения и текущий score.',
                  child: _lessonProgress.isEmpty
                      ? const Text('Прогресс по урокам пока пуст.')
                      : Column(
                          children: _lessonProgress.map((item) {
                            return ListTile(
                              contentPadding: EdgeInsets.zero,
                              title: Text(item.lessonTitle),
                              subtitle: Text(
                                '${item.learnedWords}/${item.totalWords} слов - score ${item.score}',
                              ),
                              trailing: Icon(
                                item.isCompleted
                                    ? Icons.check_circle_rounded
                                    : Icons.radio_button_unchecked_rounded,
                                color: item.isCompleted
                                    ? const Color(0xFF5A9185)
                                    : const Color(0xFFB8B2AB),
                              ),
                            );
                          }).toList(),
                        ),
                ),
                const SizedBox(height: 18),
                _SectionPanel(
                  title: 'Прогресс по словам',
                  subtitle: 'То, как движется ваш словарь по стадиям обучения.',
                  child: _wordProgress.isEmpty
                      ? const Text('Прогресс по словам пока пуст.')
                      : Column(
                          children: _wordProgress.take(10).map((item) {
                            return ListTile(
                              contentPadding: EdgeInsets.zero,
                              title: Text(item.kazakhText),
                              subtitle: Text(item.russianTranslation),
                              trailing: Text(
                                item.status.label,
                                style: const TextStyle(
                                  color: Color(0xFF5F777D),
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            );
                          }).toList(),
                        ),
                ),
              ],
            ),
    );
  }
}

class ManagementPage extends StatefulWidget {
  const ManagementPage({super.key, required this.controller});

  final AppController controller;

  @override
  State<ManagementPage> createState() => _ManagementPageState();
}

class _ManagementPageState extends State<ManagementPage>
    with SingleTickerProviderStateMixin {
  late final TabController _tabController;

  @override
  void initState() {
    super.initState();
    final tabCount = widget.controller.canManageUsers ? 2 : 1;
    _tabController = TabController(length: tabCount, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return _GradientPage(
      title: 'Управление',
      subtitle:
          'Здесь можно управлять материалами и пользователями, не выходя из приложения.',
      child: Column(
        children: [
          TabBar(
            controller: _tabController,
            tabs: [
              const Tab(text: 'Категории'),
              if (widget.controller.canManageUsers)
                const Tab(text: 'Пользователи'),
            ],
          ),
          const SizedBox(height: 18),
          SizedBox(
            height: 900,
            child: TabBarView(
              controller: _tabController,
              children: [
                CategoriesManager(controller: widget.controller),
                if (widget.controller.canManageUsers)
                  UsersManager(controller: widget.controller),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class CategoriesManager extends StatefulWidget {
  const CategoriesManager({super.key, required this.controller});

  final AppController controller;

  @override
  State<CategoriesManager> createState() => _CategoriesManagerState();
}

class _CategoriesManagerState extends State<CategoriesManager> {
  bool _loading = true;
  List<Category> _categories = const [];
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final response = await widget.controller.learningApi.getCategories(
        search: _nullIfBlank(_searchController.text),
        pageSize: 200,
      );
      setState(() => _categories = response.items);
    } on ApiException catch (error) {
      if (mounted) {
        _showSnack(context, error.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _createCategory() async {
    final payload = await showCategoryDialog(context);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.createCategory(payload);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Категория создана.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _editCategory(Category category) async {
    final payload = await showCategoryDialog(context, initial: category);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.updateCategory(category.id, payload);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Категория обновлена.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _deleteCategory(Category category) async {
    final confirmed = await showConfirmDialog(
      context,
      title: 'Удалить категорию?',
      message: 'Категория "${category.name}" будет удалена.',
    );
    if (!confirmed) {
      return;
    }

    try {
      await widget.controller.learningApi.deleteCategory(category.id);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Категория удалена.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: EdgeInsets.zero,
      children: [
        Row(
          children: [
            Expanded(
              child: _SearchInput(
                controller: _searchController,
                hintText: 'Поиск категории',
                onSubmitted: (_) => _load(),
                onRefresh: _load,
              ),
            ),
            const SizedBox(width: 12),
            FilledButton(
              onPressed: _createCategory,
              child: const Text('Добавить'),
            ),
          ],
        ),
        const SizedBox(height: 18),
        if (_loading)
          const Center(child: CircularProgressIndicator())
        else if (_categories.isEmpty)
          const _EmptyState(
            title: 'Категории не найдены',
            subtitle: 'Создайте первую категорию, чтобы наполнять словарь.',
          )
        else
          ..._categories.map(
            (category) => _ListCard(
              title: category.name,
              subtitle: category.description ?? 'Описание не заполнено.',
              trailing: PopupMenuButton<String>(
                onSelected: (value) {
                  if (value == 'edit') {
                    _editCategory(category);
                  } else if (value == 'delete') {
                    _deleteCategory(category);
                  }
                },
                itemBuilder: (_) => const [
                  PopupMenuItem(value: 'edit', child: Text('Редактировать')),
                  PopupMenuItem(value: 'delete', child: Text('Удалить')),
                ],
              ),
            ),
          ),
      ],
    );
  }
}

class UsersManager extends StatefulWidget {
  const UsersManager({super.key, required this.controller});

  final AppController controller;

  @override
  State<UsersManager> createState() => _UsersManagerState();
}

class _UsersManagerState extends State<UsersManager> {
  bool _loading = true;
  List<UserProfile> _users = const [];
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final response = await widget.controller.userApi.getUsers(
        pageSize: 200,
        search: _nullIfBlank(_searchController.text),
        sortBy: 'createdAt',
        sortOrder: 'desc',
      );
      setState(() => _users = response.items);
    } on ApiException catch (error) {
      if (mounted) {
        _showSnack(context, error.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _createUser() async {
    final payload = await showUserDialog(context);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.userApi.createUser(payload);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Пользователь создан.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _editUser(UserProfile user) async {
    final payload = await showUserDialog(context, initial: user);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.userApi.updateUser(user.id, payload);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Пользователь обновлён.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _deleteUser(UserProfile user) async {
    final confirmed = await showConfirmDialog(
      context,
      title: 'Удалить пользователя?',
      message: 'Пользователь "${user.userName}" будет удалён.',
    );
    if (!confirmed) {
      return;
    }

    try {
      await widget.controller.userApi.deleteUser(user.id);
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Пользователь удалён.');
      await _load();
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: EdgeInsets.zero,
      children: [
        Row(
          children: [
            Expanded(
              child: _SearchInput(
                controller: _searchController,
                hintText: 'Поиск пользователя',
                onSubmitted: (_) => _load(),
                onRefresh: _load,
              ),
            ),
            const SizedBox(width: 12),
            FilledButton(onPressed: _createUser, child: const Text('Добавить')),
          ],
        ),
        const SizedBox(height: 18),
        if (_loading)
          const Center(child: CircularProgressIndicator())
        else if (_users.isEmpty)
          const _EmptyState(
            title: 'Пользователи не найдены',
            subtitle: 'Когда список появится, он будет показан здесь.',
          )
        else
          ..._users.map(
            (user) => _ListCard(
              title: user.userName,
              subtitle: user.email,
              chips: [
                user.role.label,
                user.level.label,
                user.isActive ? 'Активен' : 'Отключён',
              ],
              onTap: () => Navigator.of(context).push(
                MaterialPageRoute(
                  builder: (_) => UserDetailPage(
                    controller: widget.controller,
                    userId: user.id,
                  ),
                ),
              ),
              trailing: PopupMenuButton<String>(
                onSelected: (value) {
                  if (value == 'edit') {
                    _editUser(user);
                  } else if (value == 'delete') {
                    _deleteUser(user);
                  }
                },
                itemBuilder: (_) => const [
                  PopupMenuItem(value: 'edit', child: Text('Редактировать')),
                  PopupMenuItem(value: 'delete', child: Text('Удалить')),
                ],
              ),
            ),
          ),
      ],
    );
  }
}

class UserDetailPage extends StatefulWidget {
  const UserDetailPage({
    super.key,
    required this.controller,
    required this.userId,
  });

  final AppController controller;
  final int userId;

  @override
  State<UserDetailPage> createState() => _UserDetailPageState();
}

class _UserDetailPageState extends State<UserDetailPage> {
  bool _loading = true;
  UserProfile? _user;
  List<UserLessonProgress> _lessonProgress = const [];
  List<UserWordProgress> _wordProgress = const [];

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final results = await Future.wait<dynamic>([
        widget.controller.userApi.getUser(widget.userId),
        widget.controller.userApi.getLessonProgress(widget.userId),
        widget.controller.userApi.getWordProgress(widget.userId),
      ]);
      setState(() {
        _user = results[0] as UserProfile;
        _lessonProgress = results[1] as List<UserLessonProgress>;
        _wordProgress = results[2] as List<UserWordProgress>;
      });
    } on ApiException catch (error) {
      if (mounted) {
        _showSnack(context, error.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = _user;
    return Scaffold(
      appBar: AppBar(title: const Text('Пользователь')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : user == null
          ? const _EmptyState(
              title: 'Пользователь не найден',
              subtitle: 'Не удалось получить карточку пользователя.',
            )
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  _SectionPanel(
                    title: user.userName,
                    subtitle: user.email,
                    child: Wrap(
                      spacing: 10,
                      runSpacing: 10,
                      children: [
                        _InfoPill(label: user.role.label),
                        _InfoPill(label: user.level.label),
                        _InfoPill(
                          label: user.emailConfirmed
                              ? 'Email подтверждён'
                              : 'Email не подтверждён',
                        ),
                        _InfoPill(
                          label: user.isActive ? 'Активен' : 'Неактивен',
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 18),
                  _SectionPanel(
                    title: 'Прогресс по урокам',
                    child: _lessonProgress.isEmpty
                        ? const Text('Прогресс отсутствует.')
                        : Column(
                            children: _lessonProgress.map((item) {
                              return ListTile(
                                contentPadding: EdgeInsets.zero,
                                title: Text(item.lessonTitle),
                                subtitle: Text(
                                  'Слов: ${item.learnedWords}/${item.totalWords}',
                                ),
                                trailing: Text('${item.score}%'),
                              );
                            }).toList(),
                          ),
                  ),
                  const SizedBox(height: 18),
                  _SectionPanel(
                    title: 'Прогресс по словам',
                    child: _wordProgress.isEmpty
                        ? const Text('Прогресс отсутствует.')
                        : Column(
                            children: _wordProgress.take(12).map((item) {
                              return ListTile(
                                contentPadding: EdgeInsets.zero,
                                title: Text(item.kazakhText),
                                subtitle: Text(item.russianTranslation),
                                trailing: Text(item.status.label),
                              );
                            }).toList(),
                          ),
                  ),
                ],
              ),
            ),
    );
  }
}

class ExerciseOptionsSheet extends StatefulWidget {
  const ExerciseOptionsSheet({
    super.key,
    required this.controller,
    required this.exercise,
    required this.onChanged,
  });

  final AppController controller;
  final Exercise exercise;
  final Future<void> Function() onChanged;

  @override
  State<ExerciseOptionsSheet> createState() => _ExerciseOptionsSheetState();
}

class _ExerciseOptionsSheetState extends State<ExerciseOptionsSheet> {
  bool _loading = true;
  List<ExerciseOption> _options = const [];

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final options = await widget.controller.learningApi.getExerciseOptions(
        widget.exercise.id,
      );
      setState(() => _options = options);
    } on ApiException catch (error) {
      if (mounted) {
        _showSnack(context, error.message, isError: true);
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _createOption() async {
    final payload = await showExerciseOptionDialog(context);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.createExerciseOption(
        exerciseId: widget.exercise.id,
        payload: payload,
      );
      if (!mounted) {
        return;
      }
      await _load();
      if (!mounted) {
        return;
      }
      await widget.onChanged();
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Вариант ответа создан.');
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _editOption(ExerciseOption option) async {
    final payload = await showExerciseOptionDialog(context, initial: option);
    if (payload == null) {
      return;
    }

    try {
      await widget.controller.learningApi.updateExerciseOption(
        exerciseId: widget.exercise.id,
        optionId: option.id,
        payload: payload,
      );
      if (!mounted) {
        return;
      }
      await _load();
      if (!mounted) {
        return;
      }
      await widget.onChanged();
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Вариант обновлён.');
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  Future<void> _deleteOption(ExerciseOption option) async {
    final confirmed = await showConfirmDialog(
      context,
      title: 'Удалить вариант?',
      message: 'Вариант "${option.text}" будет удалён.',
    );
    if (!confirmed) {
      return;
    }

    try {
      await widget.controller.learningApi.deleteExerciseOption(
        exerciseId: widget.exercise.id,
        optionId: option.id,
      );
      if (!mounted) {
        return;
      }
      await _load();
      if (!mounted) {
        return;
      }
      await widget.onChanged();
      if (!mounted) {
        return;
      }
      _showSnack(context, 'Вариант удалён.');
    } on ApiException catch (error) {
      if (!mounted) {
        return;
      }
      _showSnack(context, error.message, isError: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: EdgeInsets.only(
          left: 20,
          right: 20,
          top: 18,
          bottom: MediaQuery.of(context).viewInsets.bottom + 20,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Варианты ответа',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
                IconButton(
                  onPressed: _createOption,
                  icon: const Icon(Icons.add_circle_outline),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(widget.exercise.question),
            const SizedBox(height: 18),
            if (_loading)
              const Center(child: CircularProgressIndicator())
            else if (_options.isEmpty)
              const _EmptyState(
                title: 'Варианты ещё не заданы',
                subtitle: 'Добавьте хотя бы один вариант ответа.',
              )
            else
              Flexible(
                child: ListView(
                  shrinkWrap: true,
                  children: _options.map((option) {
                    return ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(option.text),
                      subtitle: Text(
                        option.isCorrect
                            ? 'Правильный ответ'
                            : 'Обычный вариант',
                      ),
                      trailing: PopupMenuButton<String>(
                        onSelected: (value) {
                          if (value == 'edit') {
                            _editOption(option);
                          } else if (value == 'delete') {
                            _deleteOption(option);
                          }
                        },
                        itemBuilder: (_) => const [
                          PopupMenuItem(
                            value: 'edit',
                            child: Text('Редактировать'),
                          ),
                          PopupMenuItem(
                            value: 'delete',
                            child: Text('Удалить'),
                          ),
                        ],
                      ),
                    );
                  }).toList(),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _GradientPage extends StatelessWidget {
  const _GradientPage({
    required this.title,
    required this.subtitle,
    required this.child,
    this.action,
    this.onRefresh,
  });

  final String title;
  final String subtitle;
  final Widget child;
  final Widget? action;
  final Future<void> Function()? onRefresh;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [Color(0xFFF6F1E8), Color(0xFFF1E4D7), Color(0xFFE8F0EE)],
        ),
      ),
      child: SafeArea(
        child: RefreshIndicator(
          onRefresh: onRefresh ?? () async {},
          child: ListView(
            padding: const EdgeInsets.all(20),
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          title,
                          style: Theme.of(context).textTheme.headlineMedium,
                        ),
                        const SizedBox(height: 8),
                        Text(subtitle),
                      ],
                    ),
                  ),
                  action ?? const SizedBox.shrink(),
                ],
              ),
              const SizedBox(height: 20),
              child,
            ],
          ),
        ),
      ),
    );
  }
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({
    required this.title,
    required this.value,
    required this.caption,
    required this.color,
  });

  final String title;
  final String value;
  final String caption;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(26),
        boxShadow: const [
          BoxShadow(
            color: Color(0x173C5660),
            blurRadius: 24,
            offset: Offset(0, 12),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              color: Colors.white70,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            value,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 30,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 6),
          Text(caption, style: const TextStyle(color: Colors.white70)),
        ],
      ),
    );
  }
}

class _SectionPanel extends StatelessWidget {
  const _SectionPanel({
    required this.title,
    required this.child,
    this.subtitle,
    this.action,
  });

  final String title;
  final String? subtitle;
  final Widget child;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.88),
        borderRadius: BorderRadius.circular(28),
        boxShadow: const [
          BoxShadow(
            color: Color(0x143C5660),
            blurRadius: 26,
            offset: Offset(0, 12),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        color: Color(0xFF17323B),
                        fontSize: 20,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    if (subtitle != null) ...[
                      const SizedBox(height: 6),
                      Text(subtitle!),
                    ],
                  ],
                ),
              ),
              action ?? const SizedBox.shrink(),
            ],
          ),
          const SizedBox(height: 18),
          child,
        ],
      ),
    );
  }
}

class _FeatureStrip extends StatelessWidget {
  const _FeatureStrip({
    required this.icon,
    required this.title,
    required this.subtitle,
  });

  final IconData icon;
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.78),
        borderRadius: BorderRadius.circular(24),
      ),
      child: Row(
        children: [
          CircleAvatar(
            radius: 24,
            backgroundColor: const Color(0xFFE7F1ED),
            child: Icon(icon, color: const Color(0xFF35655B)),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    color: Color(0xFF17323B),
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 4),
                Text(subtitle),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ListCard extends StatelessWidget {
  const _ListCard({
    required this.title,
    required this.subtitle,
    this.description,
    this.chips = const [],
    this.onTap,
    this.trailing,
  });

  final String title;
  final String subtitle;
  final String? description;
  final List<String> chips;
  final VoidCallback? onTap;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.9),
        borderRadius: BorderRadius.circular(24),
      ),
      child: ListTile(
        onTap: onTap,
        contentPadding: const EdgeInsets.all(18),
        title: Text(
          title,
          style: const TextStyle(
            color: Color(0xFF17323B),
            fontWeight: FontWeight.w700,
          ),
        ),
        subtitle: Padding(
          padding: const EdgeInsets.only(top: 10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(subtitle),
              if (description != null) ...[
                const SizedBox(height: 6),
                Text(description!),
              ],
              if (chips.isNotEmpty) ...[
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: chips
                      .map((chip) => _InfoPill(label: chip))
                      .toList(),
                ),
              ],
            ],
          ),
        ),
        trailing: trailing,
      ),
    );
  }
}

class _AudioActionButton extends StatelessWidget {
  const _AudioActionButton({
    required this.controller,
    required this.playbackId,
    required this.baseUrl,
    required this.tooltip,
    this.audioUrl,
    this.fallbackText,
    this.label,
    this.compact = false,
  });

  final AudioPlaybackController controller;
  final String playbackId;
  final String baseUrl;
  final String tooltip;
  final String? audioUrl;
  final String? fallbackText;
  final String? label;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        final isLoading = controller.isLoading(playbackId);
        final isPlaying = controller.isPlaying(playbackId);
        final iconWidget = isLoading
            ? const SizedBox(
                width: 18,
                height: 18,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : Icon(
                isPlaying
                    ? Icons.stop_circle_outlined
                    : Icons.volume_up_rounded,
              );

        Future<void> handlePressed() async {
          final error = await controller.toggle(
            playbackId: playbackId,
            baseUrl: baseUrl,
            audioUrl: audioUrl,
            fallbackText: fallbackText,
          );
          if (error != null && context.mounted) {
            _showSnack(context, error, isError: true);
          }
        }

        if (compact) {
          return IconButton(
            tooltip: tooltip,
            onPressed: isLoading ? null : handlePressed,
            icon: iconWidget,
          );
        }

        return FilledButton.tonalIcon(
          onPressed: isLoading ? null : handlePressed,
          icon: iconWidget,
          label: Text(isPlaying ? 'Остановить' : (label ?? 'Слушать аудио')),
        );
      },
    );
  }
}

class _LevelBadge extends StatelessWidget {
  const _LevelBadge({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: const Color(0xFFF7E7D6),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        label,
        style: const TextStyle(
          color: Color(0xFF8B4D28),
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _InfoPill extends StatelessWidget {
  const _InfoPill({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: const Color(0xFFF3EEE8),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        label,
        style: const TextStyle(
          color: Color(0xFF5F777D),
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _SearchInput extends StatelessWidget {
  const _SearchInput({
    required this.controller,
    required this.hintText,
    required this.onSubmitted,
    required this.onRefresh,
  });

  final TextEditingController controller;
  final String hintText;
  final ValueChanged<String> onSubmitted;
  final VoidCallback onRefresh;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<TextEditingValue>(
      valueListenable: controller,
      builder: (context, value, _) {
        return TextField(
          controller: controller,
          textInputAction: TextInputAction.search,
          onSubmitted: onSubmitted,
          decoration: InputDecoration(
            hintText: hintText,
            prefixIcon: const Icon(Icons.search_rounded),
            suffixIconConstraints: BoxConstraints(
              minWidth: value.text.isNotEmpty ? 96 : 48,
            ),
            suffixIcon: SizedBox(
              width: value.text.isNotEmpty ? 96 : 48,
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (value.text.isNotEmpty)
                    IconButton(
                      onPressed: () {
                        controller.clear();
                        onRefresh();
                      },
                      icon: const Icon(Icons.close_rounded),
                    ),
                  IconButton(
                    onPressed: onRefresh,
                    icon: const Icon(Icons.refresh_rounded),
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}

class _WordReviewDialog extends StatefulWidget {
  const _WordReviewDialog({required this.words});

  final List<UserWordProgress> words;

  @override
  State<_WordReviewDialog> createState() => _WordReviewDialogState();
}

class _WordReviewDialogState extends State<_WordReviewDialog> {
  late final List<UserWordProgress> _words;
  int _currentIndex = 0;
  bool _revealed = false;

  @override
  void initState() {
    super.initState();
    _words = List<UserWordProgress>.of(widget.words)..shuffle();
  }

  @override
  Widget build(BuildContext context) {
    final item = _words[_currentIndex];
    final nextReviewText = item.nextReviewAt == null
        ? 'Можно повторять в любой момент.'
        : 'Следующее системное повторение: ${_formatDateTimeShort(item.nextReviewAt!)}';

    return Dialog(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Повторение слов',
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 6),
            Text('Карточка ${_currentIndex + 1} из ${_words.length}'),
            const SizedBox(height: 18),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: const Color(0xFFF6F1E8),
                borderRadius: BorderRadius.circular(24),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Русский',
                    style: TextStyle(
                      color: Color(0xFF8B4D28),
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    item.russianTranslation,
                    style: const TextStyle(
                      color: Color(0xFF17323B),
                      fontSize: 24,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  if (_revealed) ...[
                    const SizedBox(height: 18),
                    const Text(
                      'Казахский',
                      style: TextStyle(
                        color: Color(0xFF2C6A59),
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      item.kazakhText,
                      style: const TextStyle(
                        color: Color(0xFF17323B),
                        fontSize: 22,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 14),
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: [
                        _InfoPill(label: item.status.label),
                        _InfoPill(label: 'Повторений: ${item.repetitionCount}'),
                        _InfoPill(
                          label:
                              'Верно: ${item.correctAnswers}, ошибок: ${item.wrongAnswers}',
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Text(
                      nextReviewText,
                      style: const TextStyle(color: Color(0xFF5F777D)),
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(height: 18),
            const Text(
              'Статус слова обновляется после ответов в упражнениях уроков.',
              style: TextStyle(color: Color(0xFF5F777D)),
            ),
            const SizedBox(height: 20),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: () => Navigator.of(context).pop(),
                    child: const Text('Закрыть'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: FilledButton(
                    onPressed: () {
                      if (!_revealed) {
                        setState(() => _revealed = true);
                        return;
                      }

                      if (_currentIndex >= _words.length - 1) {
                        Navigator.of(context).pop();
                        return;
                      }

                      setState(() {
                        _currentIndex++;
                        _revealed = false;
                      });
                    },
                    child: Text(
                      !_revealed
                          ? 'Показать ответ'
                          : _currentIndex >= _words.length - 1
                          ? 'Готово'
                          : 'Дальше',
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  const _EmptyState({required this.title, required this.subtitle});

  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        children: [
          const Icon(Icons.inbox_outlined, size: 42, color: Color(0xFF8F999D)),
          const SizedBox(height: 14),
          Text(
            title,
            style: const TextStyle(
              color: Color(0xFF17323B),
              fontSize: 18,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            subtitle,
            textAlign: TextAlign.center,
            style: const TextStyle(color: Color(0xFF5F777D)),
          ),
        ],
      ),
    );
  }
}

Future<bool> showConfirmDialog(
  BuildContext context, {
  required String title,
  required String message,
}) async {
  return await showDialog<bool>(
        context: context,
        builder: (context) {
          return AlertDialog(
            title: Text(title),
            content: Text(message),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(false),
                child: const Text('Отмена'),
              ),
              FilledButton(
                onPressed: () => Navigator.of(context).pop(true),
                child: const Text('Подтвердить'),
              ),
            ],
          );
        },
      ) ??
      false;
}

Future<Map<String, dynamic>?> showCategoryDialog(
  BuildContext context, {
  Category? initial,
}) async {
  final nameController = TextEditingController(text: initial?.name ?? '');
  final descriptionController = TextEditingController(
    text: initial?.description ?? '',
  );
  final formKey = GlobalKey<FormState>();

  final result = await showDialog<Map<String, dynamic>>(
    context: context,
    builder: (context) {
      return AlertDialog(
        title: Text(
          initial == null ? 'Новая категория' : 'Редактировать категорию',
        ),
        content: Form(
          key: formKey,
          child: SizedBox(
            width: 420,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: nameController,
                  decoration: const InputDecoration(labelText: 'Название'),
                  validator: (value) => (value == null || value.trim().isEmpty)
                      ? 'Введите название'
                      : null,
                ),
                const SizedBox(height: 14),
                TextFormField(
                  controller: descriptionController,
                  maxLines: 3,
                  decoration: const InputDecoration(labelText: 'Описание'),
                ),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Отмена'),
          ),
          FilledButton(
            onPressed: () {
              if (!formKey.currentState!.validate()) {
                return;
              }

              Navigator.of(context).pop({
                'name': nameController.text.trim(),
                'description': _nullIfBlank(descriptionController.text),
              });
            },
            child: const Text('Сохранить'),
          ),
        ],
      );
    },
  );

  nameController.dispose();
  descriptionController.dispose();
  return result;
}

Future<Map<String, dynamic>?> showLessonDialog(
  BuildContext context, {
  Lesson? initial,
}) async {
  final titleController = TextEditingController(text: initial?.title ?? '');
  final descriptionController = TextEditingController(
    text: initial?.description ?? '',
  );
  final audioController = TextEditingController(text: initial?.audioUrl ?? '');
  final orderController = TextEditingController(text: '${initial?.order ?? 1}');
  final formKey = GlobalKey<FormState>();
  var level = initial?.level ?? LanguageLevel.a1;
  var isPublished = initial?.isPublished ?? true;

  final result = await showDialog<Map<String, dynamic>>(
    context: context,
    builder: (context) {
      return StatefulBuilder(
        builder: (context, setState) {
          return AlertDialog(
            title: Text(initial == null ? 'Новый урок' : 'Редактировать урок'),
            content: Form(
              key: formKey,
              child: SizedBox(
                width: 440,
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      TextFormField(
                        controller: titleController,
                        decoration: const InputDecoration(
                          labelText: 'Название',
                        ),
                        validator: (value) =>
                            (value == null || value.trim().isEmpty)
                            ? 'Введите название'
                            : null,
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: descriptionController,
                        maxLines: 3,
                        decoration: const InputDecoration(
                          labelText: 'Описание',
                        ),
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: audioController,
                        decoration: const InputDecoration(
                          labelText: 'Audio URL',
                          hintText: 'https://.../lesson-audio.mp3',
                        ),
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: orderController,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(labelText: 'Порядок'),
                        validator: (value) => int.tryParse(value ?? '') == null
                            ? 'Введите число'
                            : null,
                      ),
                      const SizedBox(height: 14),
                      DropdownButtonFormField<LanguageLevel>(
                        initialValue: level,
                        items: LanguageLevel.values
                            .map(
                              (item) => DropdownMenuItem(
                                value: item,
                                child: Text(item.label),
                              ),
                            )
                            .toList(),
                        onChanged: (value) {
                          if (value != null) {
                            setState(() => level = value);
                          }
                        },
                        decoration: const InputDecoration(labelText: 'Уровень'),
                      ),
                      const SizedBox(height: 8),
                      SwitchListTile(
                        value: isPublished,
                        title: const Text('Опубликован'),
                        onChanged: (value) =>
                            setState(() => isPublished = value),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('Отмена'),
              ),
              FilledButton(
                onPressed: () {
                  if (!formKey.currentState!.validate()) {
                    return;
                  }

                  Navigator.of(context).pop({
                    'title': titleController.text.trim(),
                    'description': _nullIfBlank(descriptionController.text),
                    'audioUrl': _nullIfBlank(audioController.text),
                    'level': level.apiValue,
                    'order': int.parse(orderController.text.trim()),
                    'isPublished': isPublished,
                  });
                },
                child: const Text('Сохранить'),
              ),
            ],
          );
        },
      );
    },
  );

  titleController.dispose();
  descriptionController.dispose();
  audioController.dispose();
  orderController.dispose();
  return result;
}

Future<Map<String, dynamic>?> showWordDialog(
  BuildContext context, {
  required List<Category> categories,
  WordEntry? initial,
}) async {
  final kazakhController = TextEditingController(
    text: initial?.kazakhText ?? '',
  );
  final russianController = TextEditingController(
    text: initial?.russianTranslation ?? '',
  );
  final pronunciationController = TextEditingController(
    text: initial?.pronunciation ?? '',
  );
  final exampleController = TextEditingController(text: initial?.example ?? '');
  final audioController = TextEditingController(text: initial?.audioUrl ?? '');
  final imageController = TextEditingController(text: initial?.imageUrl ?? '');
  final formKey = GlobalKey<FormState>();
  var level = initial?.level ?? LanguageLevel.a1;
  var isActive = initial?.isActive ?? true;
  var categoryId =
      initial?.categoryId ?? (categories.isNotEmpty ? categories.first.id : 0);

  final result = await showDialog<Map<String, dynamic>>(
    context: context,
    builder: (context) {
      return StatefulBuilder(
        builder: (context, setState) {
          return AlertDialog(
            title: Text(
              initial == null ? 'Новое слово' : 'Редактировать слово',
            ),
            content: Form(
              key: formKey,
              child: SizedBox(
                width: 460,
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      TextFormField(
                        controller: kazakhController,
                        decoration: const InputDecoration(
                          labelText: 'Казахский текст',
                        ),
                        validator: (value) =>
                            (value == null || value.trim().isEmpty)
                            ? 'Введите слово'
                            : null,
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: russianController,
                        decoration: const InputDecoration(
                          labelText: 'Перевод на русский',
                        ),
                        validator: (value) =>
                            (value == null || value.trim().isEmpty)
                            ? 'Введите перевод'
                            : null,
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: pronunciationController,
                        decoration: const InputDecoration(
                          labelText: 'Произношение',
                        ),
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: exampleController,
                        maxLines: 3,
                        decoration: const InputDecoration(labelText: 'Пример'),
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: audioController,
                        decoration: const InputDecoration(
                          labelText: 'Audio URL',
                        ),
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: imageController,
                        decoration: const InputDecoration(
                          labelText: 'Image URL',
                        ),
                      ),
                      const SizedBox(height: 14),
                      DropdownButtonFormField<int>(
                        initialValue: categoryId,
                        items: categories
                            .map(
                              (category) => DropdownMenuItem(
                                value: category.id,
                                child: Text(category.name),
                              ),
                            )
                            .toList(),
                        onChanged: (value) {
                          if (value != null) {
                            setState(() => categoryId = value);
                          }
                        },
                        decoration: const InputDecoration(
                          labelText: 'Категория',
                        ),
                      ),
                      const SizedBox(height: 14),
                      DropdownButtonFormField<LanguageLevel>(
                        initialValue: level,
                        items: LanguageLevel.values
                            .map(
                              (item) => DropdownMenuItem(
                                value: item,
                                child: Text(item.label),
                              ),
                            )
                            .toList(),
                        onChanged: (value) {
                          if (value != null) {
                            setState(() => level = value);
                          }
                        },
                        decoration: const InputDecoration(labelText: 'Уровень'),
                      ),
                      const SizedBox(height: 8),
                      SwitchListTile(
                        value: isActive,
                        title: const Text('Активное слово'),
                        onChanged: (value) => setState(() => isActive = value),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('Отмена'),
              ),
              FilledButton(
                onPressed: () {
                  if (!formKey.currentState!.validate()) {
                    return;
                  }

                  Navigator.of(context).pop({
                    'kazakhText': kazakhController.text.trim(),
                    'russianTranslation': russianController.text.trim(),
                    'pronunciation': _nullIfBlank(pronunciationController.text),
                    'example': _nullIfBlank(exampleController.text),
                    'audioUrl': _nullIfBlank(audioController.text),
                    'imageUrl': _nullIfBlank(imageController.text),
                    'level': level.apiValue,
                    'isActive': isActive,
                    'categoryId': categoryId,
                  });
                },
                child: const Text('Сохранить'),
              ),
            ],
          );
        },
      );
    },
  );

  kazakhController.dispose();
  russianController.dispose();
  pronunciationController.dispose();
  exampleController.dispose();
  audioController.dispose();
  imageController.dispose();
  return result;
}

Future<Map<String, dynamic>?> showExerciseDialog(
  BuildContext context, {
  Exercise? initial,
}) async {
  final questionController = TextEditingController(
    text: initial?.question ?? '',
  );
  final orderController = TextEditingController(text: '${initial?.order ?? 1}');
  final explanationController = TextEditingController(
    text: initial?.explanation ?? '',
  );
  final wordIdController = TextEditingController(
    text: initial?.wordId?.toString() ?? '',
  );
  final formKey = GlobalKey<FormState>();
  var type = initial?.type ?? ExerciseType.chooseAnswer;

  final result = await showDialog<Map<String, dynamic>>(
    context: context,
    builder: (context) {
      return StatefulBuilder(
        builder: (context, setState) {
          return AlertDialog(
            title: Text(
              initial == null ? 'Новое упражнение' : 'Редактировать упражнение',
            ),
            content: Form(
              key: formKey,
              child: SizedBox(
                width: 440,
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      TextFormField(
                        controller: questionController,
                        maxLines: 3,
                        decoration: const InputDecoration(labelText: 'Вопрос'),
                        validator: (value) =>
                            (value == null || value.trim().isEmpty)
                            ? 'Введите вопрос'
                            : null,
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: orderController,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(labelText: 'Порядок'),
                        validator: (value) => int.tryParse(value ?? '') == null
                            ? 'Введите число'
                            : null,
                      ),
                      const SizedBox(height: 14),
                      DropdownButtonFormField<ExerciseType>(
                        initialValue: type,
                        items: ExerciseType.values
                            .map(
                              (item) => DropdownMenuItem(
                                value: item,
                                child: Text(item.label),
                              ),
                            )
                            .toList(),
                        onChanged: (value) {
                          if (value != null) {
                            setState(() => type = value);
                          }
                        },
                        decoration: const InputDecoration(
                          labelText: 'Тип упражнения',
                        ),
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: explanationController,
                        maxLines: 3,
                        decoration: const InputDecoration(
                          labelText: 'Пояснение',
                        ),
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: wordIdController,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(
                          labelText: 'WordId (необязательно)',
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('Отмена'),
              ),
              FilledButton(
                onPressed: () {
                  if (!formKey.currentState!.validate()) {
                    return;
                  }

                  Navigator.of(context).pop({
                    'question': questionController.text.trim(),
                    'order': int.parse(orderController.text.trim()),
                    'type': type.apiValue,
                    'explanation': _nullIfBlank(explanationController.text),
                    'wordId': int.tryParse(wordIdController.text.trim()),
                  });
                },
                child: const Text('Сохранить'),
              ),
            ],
          );
        },
      );
    },
  );

  questionController.dispose();
  orderController.dispose();
  explanationController.dispose();
  wordIdController.dispose();
  return result;
}

Future<Map<String, dynamic>?> showExerciseOptionDialog(
  BuildContext context, {
  ExerciseOption? initial,
}) async {
  final textController = TextEditingController(text: initial?.text ?? '');
  final formKey = GlobalKey<FormState>();
  var isCorrect = initial?.isCorrect ?? false;

  final result = await showDialog<Map<String, dynamic>>(
    context: context,
    builder: (context) {
      return StatefulBuilder(
        builder: (context, setState) {
          return AlertDialog(
            title: Text(
              initial == null
                  ? 'Новый вариант ответа'
                  : 'Редактировать вариант',
            ),
            content: Form(
              key: formKey,
              child: SizedBox(
                width: 380,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    TextFormField(
                      controller: textController,
                      decoration: const InputDecoration(
                        labelText: 'Текст ответа',
                      ),
                      validator: (value) =>
                          (value == null || value.trim().isEmpty)
                          ? 'Введите текст ответа'
                          : null,
                    ),
                    const SizedBox(height: 8),
                    SwitchListTile(
                      value: isCorrect,
                      title: const Text('Правильный ответ'),
                      onChanged: (value) => setState(() => isCorrect = value),
                    ),
                  ],
                ),
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('Отмена'),
              ),
              FilledButton(
                onPressed: () {
                  if (!formKey.currentState!.validate()) {
                    return;
                  }

                  Navigator.of(context).pop({
                    'text': textController.text.trim(),
                    'isCorrect': isCorrect,
                  });
                },
                child: const Text('Сохранить'),
              ),
            ],
          );
        },
      );
    },
  );

  textController.dispose();
  return result;
}

Future<Map<String, dynamic>?> showUserDialog(
  BuildContext context, {
  UserProfile? initial,
}) async {
  final nameController = TextEditingController(text: initial?.userName ?? '');
  final emailController = TextEditingController(text: initial?.email ?? '');
  final passwordController = TextEditingController();
  final formKey = GlobalKey<FormState>();
  var level = initial?.level ?? LanguageLevel.a1;
  var role = initial?.role ?? UserRole.user;
  var emailConfirmed = initial?.emailConfirmed ?? false;
  var isActive = initial?.isActive ?? true;

  final result = await showDialog<Map<String, dynamic>>(
    context: context,
    builder: (context) {
      return StatefulBuilder(
        builder: (context, setState) {
          return AlertDialog(
            title: Text(
              initial == null
                  ? 'Новый пользователь'
                  : 'Редактировать пользователя',
            ),
            content: Form(
              key: formKey,
              child: SizedBox(
                width: 460,
                child: SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      TextFormField(
                        controller: nameController,
                        decoration: const InputDecoration(
                          labelText: 'Имя пользователя',
                        ),
                        validator: (value) =>
                            (value == null || value.trim().isEmpty)
                            ? 'Введите имя'
                            : null,
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: emailController,
                        decoration: const InputDecoration(labelText: 'Email'),
                        validator: (value) =>
                            (value == null || value.trim().isEmpty)
                            ? 'Введите email'
                            : null,
                      ),
                      const SizedBox(height: 14),
                      TextFormField(
                        controller: passwordController,
                        obscureText: true,
                        decoration: InputDecoration(
                          labelText: initial == null
                              ? 'Пароль'
                              : 'Новый пароль (необязательно)',
                        ),
                        validator: (value) {
                          if (initial == null &&
                              (value == null || value.isEmpty)) {
                            return 'Введите пароль';
                          }

                          return null;
                        },
                      ),
                      const SizedBox(height: 14),
                      DropdownButtonFormField<UserRole>(
                        initialValue: role,
                        items: UserRole.values
                            .map(
                              (item) => DropdownMenuItem(
                                value: item,
                                child: Text(item.label),
                              ),
                            )
                            .toList(),
                        onChanged: (value) {
                          if (value != null) {
                            setState(() => role = value);
                          }
                        },
                        decoration: const InputDecoration(labelText: 'Роль'),
                      ),
                      const SizedBox(height: 14),
                      DropdownButtonFormField<LanguageLevel>(
                        initialValue: level,
                        items: LanguageLevel.values
                            .map(
                              (item) => DropdownMenuItem(
                                value: item,
                                child: Text(item.label),
                              ),
                            )
                            .toList(),
                        onChanged: (value) {
                          if (value != null) {
                            setState(() => level = value);
                          }
                        },
                        decoration: const InputDecoration(labelText: 'Уровень'),
                      ),
                      const SizedBox(height: 8),
                      SwitchListTile(
                        value: emailConfirmed,
                        title: const Text('Email подтверждён'),
                        onChanged: (value) =>
                            setState(() => emailConfirmed = value),
                      ),
                      SwitchListTile(
                        value: isActive,
                        title: const Text('Аккаунт активен'),
                        onChanged: (value) => setState(() => isActive = value),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('Отмена'),
              ),
              FilledButton(
                onPressed: () {
                  if (!formKey.currentState!.validate()) {
                    return;
                  }

                  Navigator.of(context).pop({
                    'email': emailController.text.trim(),
                    'password': _nullIfBlank(passwordController.text),
                    'userName': nameController.text.trim(),
                    'level': level.apiValue,
                    'role': role.apiValue,
                    'emailConfirmed': emailConfirmed,
                    'isActive': isActive,
                  });
                },
                child: const Text('Сохранить'),
              ),
            ],
          );
        },
      );
    },
  );

  nameController.dispose();
  emailController.dispose();
  passwordController.dispose();
  return result;
}

Future<Map<String, dynamic>?> showChangePasswordDialog(
  BuildContext context,
) async {
  final currentController = TextEditingController();
  final newController = TextEditingController();
  final formKey = GlobalKey<FormState>();

  final result = await showDialog<Map<String, dynamic>>(
    context: context,
    builder: (context) {
      return AlertDialog(
        title: const Text('Смена пароля'),
        content: Form(
          key: formKey,
          child: SizedBox(
            width: 360,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextFormField(
                  controller: currentController,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'Текущий пароль',
                  ),
                  validator: (value) => (value == null || value.isEmpty)
                      ? 'Введите текущий пароль'
                      : null,
                ),
                const SizedBox(height: 14),
                TextFormField(
                  controller: newController,
                  obscureText: true,
                  decoration: const InputDecoration(labelText: 'Новый пароль'),
                  validator: (value) => (value == null || value.isEmpty)
                      ? 'Введите новый пароль'
                      : null,
                ),
              ],
            ),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Отмена'),
          ),
          FilledButton(
            onPressed: () {
              if (!formKey.currentState!.validate()) {
                return;
              }

              Navigator.of(context).pop({
                'currentPassword': currentController.text,
                'newPassword': newController.text,
              });
            },
            child: const Text('Изменить'),
          ),
        ],
      );
    },
  );

  currentController.dispose();
  newController.dispose();
  return result;
}

Future<Map<String, dynamic>?> showAddWordToLessonDialog(
  BuildContext context, {
  required List<WordEntry> words,
  required int initialOrder,
}) async {
  final orderController = TextEditingController(text: '$initialOrder');
  final formKey = GlobalKey<FormState>();
  int? selectedWordId = words.isNotEmpty ? words.first.id : null;

  final result = await showDialog<Map<String, dynamic>>(
    context: context,
    builder: (context) {
      return StatefulBuilder(
        builder: (context, setState) {
          return AlertDialog(
            title: const Text('Добавить слово в урок'),
            content: Form(
              key: formKey,
              child: SizedBox(
                width: 420,
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    DropdownButtonFormField<int>(
                      initialValue: selectedWordId,
                      items: words
                          .map(
                            (word) => DropdownMenuItem(
                              value: word.id,
                              child: Text(
                                '${word.kazakhText} - ${word.russianTranslation}',
                              ),
                            ),
                          )
                          .toList(),
                      onChanged: (value) =>
                          setState(() => selectedWordId = value),
                      decoration: const InputDecoration(labelText: 'Слово'),
                    ),
                    const SizedBox(height: 14),
                    TextFormField(
                      controller: orderController,
                      keyboardType: TextInputType.number,
                      decoration: const InputDecoration(labelText: 'Порядок'),
                      validator: (value) => int.tryParse(value ?? '') == null
                          ? 'Введите число'
                          : null,
                    ),
                  ],
                ),
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(),
                child: const Text('Отмена'),
              ),
              FilledButton(
                onPressed: () {
                  if (!formKey.currentState!.validate() ||
                      selectedWordId == null) {
                    return;
                  }

                  Navigator.of(context).pop({
                    'wordId': selectedWordId!,
                    'order': int.parse(orderController.text.trim()),
                  });
                },
                child: const Text('Добавить'),
              ),
            ],
          );
        },
      );
    },
  );

  orderController.dispose();
  return result;
}

void _showSnack(BuildContext context, String message, {bool isError = false}) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      backgroundColor: isError
          ? const Color(0xFF8B4D28)
          : const Color(0xFF17323B),
      content: Text(message),
    ),
  );
}

class _InlineNotice extends StatelessWidget {
  const _InlineNotice({required this.message, this.isError = false});

  final String message;
  final bool isError;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(
        color: isError ? const Color(0xFFF6E4DA) : const Color(0xFFE3F0EC),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(
          color: isError ? const Color(0xFFE2B39B) : const Color(0xFFB8D4C9),
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            isError ? Icons.error_outline_rounded : Icons.check_circle_outline,
            color: isError ? const Color(0xFF8B4D28) : const Color(0xFF2C6A59),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              message,
              style: TextStyle(
                color: isError
                    ? const Color(0xFF6F3717)
                    : const Color(0xFF234B41),
                height: 1.35,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
