class ApiConstants {
  ApiConstants._();

  /// Derlemede `--dart-define=API_BASE_URL=...` ile değiştirilebilir; varsayılan test sunucusunun API'si.
  static const baseUrl = String.fromEnvironment('API_BASE_URL', defaultValue: 'https://hafiztakip.api.yasirsharp.com.tr');

  /// Test sunucusunda e-posta gönderilmez; kod sabittir. Canlı derlemede `--dart-define=SHOW_TEST_HINTS=false`.
  static const showTestHints = bool.fromEnvironment('SHOW_TEST_HINTS', defaultValue: true);

  static const connectTimeout = Duration(seconds: 15);
  static const receiveTimeout = Duration(seconds: 20);
}
