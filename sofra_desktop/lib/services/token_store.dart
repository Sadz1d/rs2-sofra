import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../models/current_user.dart';

/// Drzi tokene i trenutnog korisnika u memoriji i perzistira ih u secure storage-u.
/// Ne zna nista o HTTP-u ili navigaciji - to je posao ApiClient-a i AuthProvider-a.
class TokenStore {
  TokenStore() : _storage = const FlutterSecureStorage();

  final FlutterSecureStorage _storage;

  static const _keyAccessToken = 'sofra_access_token';
  static const _keyRefreshToken = 'sofra_refresh_token';
  static const _keyExpiresAt = 'sofra_access_expires_at';
  static const _keyUser = 'sofra_current_user';

  String? accessToken;
  String? refreshToken;
  DateTime? accessTokenExpiresAtUtc;
  CurrentUser? currentUser;

  Future<void> save({
    required String accessToken,
    required String refreshToken,
    required DateTime accessTokenExpiresAtUtc,
    required CurrentUser user,
  }) async {
    this.accessToken = accessToken;
    this.refreshToken = refreshToken;
    this.accessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
    currentUser = user;

    await Future.wait([
      _storage.write(key: _keyAccessToken, value: accessToken),
      _storage.write(key: _keyRefreshToken, value: refreshToken),
      _storage.write(key: _keyExpiresAt, value: accessTokenExpiresAtUtc.toIso8601String()),
      _storage.write(key: _keyUser, value: jsonEncode(user.toJson())),
    ]);
  }

  /// Ucitava perzistirane tokene sa diska u memoriju. Vraca true ako je nesto pronadjeno.
  Future<bool> loadFromStorage() async {
    final values = await Future.wait([
      _storage.read(key: _keyAccessToken),
      _storage.read(key: _keyRefreshToken),
      _storage.read(key: _keyExpiresAt),
      _storage.read(key: _keyUser),
    ]);

    final access = values[0];
    final refresh = values[1];
    final expiresAt = values[2];
    final userJson = values[3];

    if (access == null || refresh == null || userJson == null) {
      return false;
    }

    accessToken = access;
    refreshToken = refresh;
    accessTokenExpiresAtUtc = expiresAt != null ? DateTime.parse(expiresAt) : null;
    currentUser = CurrentUser.fromJson(jsonDecode(userJson) as Map<String, dynamic>);
    return true;
  }

  Future<void> clear() async {
    accessToken = null;
    refreshToken = null;
    accessTokenExpiresAtUtc = null;
    currentUser = null;

    await Future.wait([
      _storage.delete(key: _keyAccessToken),
      _storage.delete(key: _keyRefreshToken),
      _storage.delete(key: _keyExpiresAt),
      _storage.delete(key: _keyUser),
    ]);
  }
}
