import 'package:flutter_test/flutter_test.dart';
import 'package:kazlang_app/src/app.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  testWidgets('shows russian auth entry screen', (tester) async {
    SharedPreferences.setMockInitialValues({});

    await tester.pumpWidget(KazLangApp(controller: AppController()));
    await tester.pumpAndSettle();

    expect(find.text('Подключение и вход'), findsOneWidget);
    expect(find.text('КАЗАХСКИЙ ДЛЯ РУССКОГОВОРЯЩИХ'), findsOneWidget);
    expect(find.text('Вход'), findsOneWidget);
    expect(find.text('Регистрация'), findsOneWidget);
  });
}
