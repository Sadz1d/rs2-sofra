import '../models/auth_response.dart';
import 'api_client.dart';
import 'token_store.dart';

/// Poziva /api/auth/* i cuva rezultat u TokenStore. AuthProvider ovo omotava u
/// stanje koje UI moze da posmatra (ChangeNotifier); ovaj servis ne zna za widgete.
class AuthService {
  AuthService({required this.apiClient, required this.tokenStore});

  final ApiClient apiClient;
  final TokenStore tokenStore;

  Future<AuthResponse> login({required String username, required String password}) async {
    final json = await apiClient.post('/api/auth/login', body: {
      'username': username,
      'password': password,
    }) as JsonMap;

    final auth = AuthResponse.fromJson(json);
    await tokenStore.save(
      accessToken: auth.accessToken,
      refreshToken: auth.refreshToken,
      accessTokenExpiresAtUtc: auth.accessTokenExpiresAtUtc,
      user: auth.user,
    );
    return auth;
  }

  Future<void> logout() async {
    final refreshToken = tokenStore.refreshToken;
    try {
      await apiClient.post('/api/auth/logout', body: {'refreshToken': refreshToken});
    } catch (_) {
      // Sesija je vec nevazeca na serveru - lokalna odjava se svakako izvrsava ispod.
    }
    await tokenStore.clear();
  }

  /// Pokusaj automatske prijave pri pokretanju - ucita perzistirane tokene i potvrdi
  /// ih osvjezavanjem (refresh token moze biti istekao ili opozvan u medjuvremenu).
  Future<bool> tryAutoLogin() async {
    final hasStoredTokens = await tokenStore.loadFromStorage();
    if (!hasStoredTokens) {
      return false;
    }

    try {
      final json = await apiClient.post('/api/auth/refresh', body: {
        'refreshToken': tokenStore.refreshToken,
      }) as JsonMap;

      final auth = AuthResponse.fromJson(json);
      await tokenStore.save(
        accessToken: auth.accessToken,
        refreshToken: auth.refreshToken,
        accessTokenExpiresAtUtc: auth.accessTokenExpiresAtUtc,
        user: auth.user,
      );
      return true;
    } catch (_) {
      await tokenStore.clear();
      return false;
    }
  }
}
