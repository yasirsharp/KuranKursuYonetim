import 'dart:async';

import 'package:dio/dio.dart';

import '../../data/models/session_model.dart';
import '../constants/api_constants.dart';
import 'api_exception.dart';

/// Oturum bilgisini API istemcisine veren ve yenilenen oturumu kaydeden taraf (AuthNotifier).
abstract class SessionHolder {
  SessionModel? get currentSession;
  Future<void> onSessionRefreshed(SessionModel session);
  Future<void> onSessionExpired();
}

/// Dio tabanlı istemci: her isteğe JWT ekler; 401 gelirse refresh token ile oturumu bir kez yeniler
/// (aynı anda gelen 401'ler tek yenileme isteğini paylaşır) ve isteği tekrarlar.
class ApiClient {
  ApiClient({required this._sessionHolder, Dio? dio})
      : _dio = dio ??
            Dio(BaseOptions(
              baseUrl: ApiConstants.baseUrl,
              connectTimeout: ApiConstants.connectTimeout,
              receiveTimeout: ApiConstants.receiveTimeout,
              headers: {'Content-Type': 'application/json', 'Accept': 'application/json'},
            )) {
    _dio.interceptors.add(InterceptorsWrapper(onRequest: _attachToken, onError: _refreshOnUnauthorized));
  }

  final Dio _dio;
  final SessionHolder _sessionHolder;
  Future<bool>? _refreshInFlight;

  static const _anonymousPrefix = '/api/auth/';

  Future<T> get<T>(String path, {Map<String, dynamic>? query, required T Function(dynamic data) parse}) =>
      _send(() => _dio.get(path, queryParameters: query), parse);

  Future<T> post<T>(String path, {Object? body, required T Function(dynamic data) parse}) =>
      _send(() => _dio.post(path, data: body), parse);

  Future<void> postVoid(String path, {Object? body}) => _send(() => _dio.post(path, data: body), (_) {});

  Future<void> delete(String path) => _send(() => _dio.delete(path), (_) {});

  /// Yanıtın `data` alanını ayrıştırır; hata durumunda Türkçe mesajlı ApiException fırlatır.
  Future<T> _send<T>(Future<Response<dynamic>> Function() request, T Function(dynamic data) parse) async {
    try {
      final response = await request();
      final body = response.data;
      return parse(body is Map ? body['data'] : body);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  void _attachToken(RequestOptions options, RequestInterceptorHandler handler) {
    final token = _sessionHolder.currentSession?.token;
    if (token != null && !options.path.startsWith(_anonymousPrefix)) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  Future<void> _refreshOnUnauthorized(DioException error, ErrorInterceptorHandler handler) async {
    final options = error.requestOptions;
    final isRetry = options.extra['retried'] == true;
    if (error.response?.statusCode != 401 || options.path.startsWith(_anonymousPrefix) || isRetry) {
      return handler.next(error);
    }

    final refreshed = await (_refreshInFlight ??= _refreshSession().whenComplete(() => _refreshInFlight = null));
    if (!refreshed) return handler.next(error);

    try {
      options.extra['retried'] = true;
      options.headers['Authorization'] = 'Bearer ${_sessionHolder.currentSession?.token}';
      handler.resolve(await _dio.fetch(options));
    } on DioException catch (e) {
      handler.next(e);
    }
  }

  Future<bool> _refreshSession() async {
    final session = _sessionHolder.currentSession;
    if (session == null) return false;
    try {
      final response = await _dio.post('/api/auth/refresh', data: {'refreshToken': session.refreshToken});
      final data = (response.data as Map)['data'] as Map<String, dynamic>;
      await _sessionHolder.onSessionRefreshed(session.withTokens(data['token'] as String, data['refreshToken'] as String));
      return true;
    } catch (_) {
      await _sessionHolder.onSessionExpired();
      return false;
    }
  }
}
