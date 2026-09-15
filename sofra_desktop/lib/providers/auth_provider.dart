import 'package:flutter/foundation.dart';

import '../constants/roles.dart';
import '../models/api_exception.dart';
import '../models/current_user.dart';
import '../services/auth_service.dart';
import '../services/signalr_service.dart';
import '../services/token_store.dart';

enum AuthStatus { unknown, authenticating, authenticated, unauthenticated }

/// Drzi trenutnog korisnika i status prijave; ekrani ovo posmatraju preko provider-a
/// i ne zovu AuthService direktno.
class AuthProvider extends ChangeNotifier {
  AuthProvider({
    required this.authService,
    required this.tokenStore,
    required this.signalRService,
  });

  final AuthService authService;
  final TokenStore tokenStore;
  final SignalRService signalRService;

  AuthStatus status = AuthStatus.unknown;
  String? lastError;

  CurrentUser? get currentUser => tokenStore.currentUser;
  bool get isAuthenticated => status == AuthStatus.authenticated;

  /// Poziva se jednom pri pokretanju aplikacije (iz main.dart) - pokusava obnoviti
  /// prijavu iz perzistiranih tokena.
  Future<void> bootstrap() async {
    final restored = await authService.tryAutoLogin();
    if (restored) {
      status = AuthStatus.authenticated;
      notifyListeners();
      await signalRService.connect();
    } else {
      status = AuthStatus.unauthenticated;
      notifyListeners();
    }
  }

  Future<bool> login({required String username, required String password}) async {
    status = AuthStatus.authenticating;
    lastError = null;
    notifyListeners();

    try {
      final auth = await authService.login(username: username, password: password);

      final isStaff = auth.user.roles.any((role) => role != Roles.gost);
      if (!isStaff) {
        await authService.logout();
        status = AuthStatus.unauthenticated;
        lastError = 'Gost korisnici se ne mogu prijaviti u desktop aplikaciju - '
            'ona je namijenjena osoblju restorana.';
        notifyListeners();
        return false;
      }

      status = AuthStatus.authenticated;
      notifyListeners();
      await signalRService.connect();
      return true;
    } on ApiException catch (e) {
      status = AuthStatus.unauthenticated;
      lastError = e.message;
      notifyListeners();
      return false;
    }
  }

  Future<void> logout() async {
    await signalRService.disconnect();
    await authService.logout();
    status = AuthStatus.unauthenticated;
    notifyListeners();
  }

  /// Poziva ga ApiClient kad ni osvjezavanje tokena ne uspije usred zahtjeva -
  /// forsira odjavu i povratak na login ekran. Tokeni su vec obrisani u TokenStore-u.
  void handleSessionExpired() {
    signalRService.disconnect();
    status = AuthStatus.unauthenticated;
    lastError = 'Sesija je istekla. Prijavite se ponovo.';
    notifyListeners();
  }
}
