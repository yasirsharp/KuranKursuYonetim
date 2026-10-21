import 'package:dio/dio.dart';

/// Ekranda gösterilecek Türkçe mesajla API hatası.
class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode, this.fieldErrors = const {}});

  final String message;
  final int? statusCode;

  /// Doğrulama hataları: alan adı (camelCase) → mesaj.
  final Map<String, String> fieldErrors;

  bool get isUnauthorized => statusCode == 401;

  /// Backend yanıtı `{ success, message, errors? }` biçimindedir.
  factory ApiException.fromDio(DioException error) {
    final response = error.response;
    if (response == null) {
      return const ApiException('Sunucuya ulaşılamadı. İnternet bağlantınızı kontrol edin.');
    }

    final data = response.data;
    String? message;
    final fields = <String, String>{};
    if (data is Map) {
      message = data['message'] as String?;
      final errors = data['errors'];
      if (errors is Map) {
        errors.forEach((key, value) {
          final text = value is List && value.isNotEmpty ? value.first.toString() : value?.toString();
          if (text != null) fields[_camel(key.toString())] = text;
        });
      }
    }

    message ??= switch (response.statusCode) {
      401 => 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.',
      403 => 'Bu işlem için yetkiniz yok.',
      404 => 'Kayıt bulunamadı.',
      429 => 'Çok fazla istek gönderildi. Biraz bekleyip tekrar deneyin.',
      _ => 'Bir hata oluştu. Lütfen tekrar deneyin.',
    };
    if (message.isEmpty && fields.isNotEmpty) message = fields.values.first;

    return ApiException(message, statusCode: response.statusCode, fieldErrors: fields);
  }

  static String _camel(String key) => key.isEmpty ? key : key[0].toLowerCase() + key.substring(1);

  @override
  String toString() => message;
}
