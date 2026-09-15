import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import '../models/api_exception.dart';
import '../models/auth_response.dart';
import '../models/paged_result.dart';
import 'token_store.dart';

typedef JsonMap = Map<String, dynamic>;

/// Jedini sloj u aplikaciji koji zna za HTTP. Dodaje Authorization header, parsira
/// `PagedResult<T>` i standardizovani ErrorResponse u tipizovanu ApiException, i sam
/// pokusava osvjezavanje tokena na 401 prije nego digne gresku dalje.
class ApiClient {
  ApiClient({required this.tokenStore});

  final TokenStore tokenStore;

  /// Postavlja AuthProvider nakon konstrukcije (izbjegava kruznu zavisnost izmedju
  /// ApiClient-a i AuthProvider-a) - poziva se kad ni osvjezavanje tokena ne uspije.
  void Function()? onSessionExpired;

  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5000',
  );

  final http.Client _client = http.Client();

  Future<dynamic> get(String path, {Map<String, dynamic>? query}) =>
      _send('GET', path, query: query);

  Future<dynamic> post(String path, {Object? body}) => _send('POST', path, body: body);

  Future<dynamic> put(String path, {Object? body}) => _send('PUT', path, body: body);

  Future<void> delete(String path) async => _send('DELETE', path);

  Future<PagedResult<T>> getPaged<T>(
    String path,
    T Function(JsonMap) fromJson, {
    Map<String, dynamic>? query,
  }) async {
    final json = await get(path, query: query) as JsonMap;
    return PagedResult.fromJson(json, fromJson);
  }

  Future<dynamic> postMultipart(
    String path, {
    required String fileField,
    required List<int> fileBytes,
    required String filename,
    Map<String, String>? fields,
  }) async {
    final request = http.MultipartRequest('POST', _uri(path));
    if (tokenStore.accessToken != null) {
      request.headers['Authorization'] = 'Bearer ${tokenStore.accessToken}';
    }
    if (fields != null) {
      request.fields.addAll(fields);
    }
    request.files.add(http.MultipartFile.fromBytes(fileField, fileBytes, filename: filename));

    final streamed = await request.send();
    final response = await http.Response.fromStream(streamed);
    return _parseResponse(response);
  }

  Uri _uri(String path, [Map<String, dynamic>? query]) {
    final normalized = path.startsWith('/') ? path : '/$path';
    Map<String, String>? stringQuery;
    if (query != null) {
      stringQuery = {
        for (final entry in query.entries)
          if (entry.value != null) entry.key: entry.value.toString(),
      };
    }
    return Uri.parse('$baseUrl$normalized').replace(
      queryParameters: (stringQuery == null || stringQuery.isEmpty) ? null : stringQuery,
    );
  }

  Future<dynamic> _send(
    String method,
    String path, {
    Map<String, dynamic>? query,
    Object? body,
    bool isRetry = false,
  }) async {
    final uri = _uri(path, query);
    final headers = <String, String>{'Content-Type': 'application/json'};
    if (tokenStore.accessToken != null) {
      headers['Authorization'] = 'Bearer ${tokenStore.accessToken}';
    }

    http.Response response;
    try {
      final encodedBody = body == null ? null : jsonEncode(body);
      switch (method) {
        case 'GET':
          response = await _client.get(uri, headers: headers);
        case 'POST':
          response = await _client.post(uri, headers: headers, body: encodedBody);
        case 'PUT':
          response = await _client.put(uri, headers: headers, body: encodedBody);
        case 'DELETE':
          response = await _client.delete(uri, headers: headers);
        default:
          throw ArgumentError('Nepodrzana HTTP metoda: $method');
      }
    } on SocketException {
      throw const ApiException('Nije moguće povezati se sa serverom. Provjerite konekciju.');
    }

    if (response.statusCode == 401 && !isRetry && tokenStore.refreshToken != null) {
      final refreshed = await _tryRefresh();
      if (refreshed) {
        return _send(method, path, query: query, body: body, isRetry: true);
      }
    }

    return _parseResponse(response);
  }

  Future<bool> _tryRefresh() async {
    try {
      final response = await _client.post(
        _uri('/api/auth/refresh'),
        headers: const {'Content-Type': 'application/json'},
        body: jsonEncode({'refreshToken': tokenStore.refreshToken}),
      );

      if (response.statusCode != 200) {
        await tokenStore.clear();
        onSessionExpired?.call();
        return false;
      }

      final auth = AuthResponse.fromJson(jsonDecode(utf8.decode(response.bodyBytes)) as JsonMap);
      await tokenStore.save(
        accessToken: auth.accessToken,
        refreshToken: auth.refreshToken,
        accessTokenExpiresAtUtc: auth.accessTokenExpiresAtUtc,
        user: auth.user,
      );
      return true;
    } catch (_) {
      await tokenStore.clear();
      onSessionExpired?.call();
      return false;
    }
  }

  dynamic _parseResponse(http.Response response) {
    final status = response.statusCode;
    final hasBody = response.bodyBytes.isNotEmpty;

    if (status >= 200 && status < 300) {
      return hasBody ? jsonDecode(utf8.decode(response.bodyBytes)) : null;
    }

    JsonMap? errorJson;
    if (hasBody) {
      try {
        errorJson = jsonDecode(utf8.decode(response.bodyBytes)) as JsonMap;
      } catch (_) {
        errorJson = null;
      }
    }

    final message = errorJson?['message'] as String? ?? _fallbackMessage(status);

    Map<String, List<String>>? fieldErrors;
    final errors = errorJson?['errors'] as JsonMap?;
    if (errors != null) {
      fieldErrors = {
        for (final entry in errors.entries)
          entry.key: (entry.value as List<dynamic>).map((e) => e.toString()).toList(),
      };
    }

    throw ApiException(message, statusCode: status, fieldErrors: fieldErrors);
  }

  String _fallbackMessage(int status) => switch (status) {
        400 => 'Zahtjev nije ispravan.',
        401 => 'Sesija je istekla. Prijavite se ponovo.',
        403 => 'Nemate dozvolu za ovu akciju.',
        404 => 'Traženi resurs ne postoji.',
        422 => 'Provjerite unesene podatke.',
        _ => 'Došlo je do greške na serveru. Pokušajte ponovo kasnije.',
      };
}
