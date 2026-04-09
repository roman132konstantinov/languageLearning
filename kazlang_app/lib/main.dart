import 'package:flutter/widgets.dart';
import 'package:kazlang_app/src/app.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(KazLangApp(controller: AppController()));
}
