import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../auth/login_screen.dart';
import '../error/access_denied_screen.dart';
import 'app_shell.dart';
import 'splash_view.dart';

/// Odlucuje sta se prikazuje za svaku imenovanu rutu na osnovu statusa prijave i,
/// ako je [allowedRoles] zadan, uloge prijavljenog korisnika - ekrani se ne pitaju sami
/// da li je korisnik prijavljen niti da li smije vidjeti ovu rutu.
class AuthGate extends StatelessWidget {
  const AuthGate({super.key, required this.route, required this.child, this.allowedRoles});

  final String route;
  final Widget child;
  final List<String>? allowedRoles;

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    return switch (auth.status) {
      AuthStatus.unknown || AuthStatus.authenticating => const SplashView(),
      AuthStatus.unauthenticated => const LoginScreen(),
      AuthStatus.authenticated => AppShell(currentRoute: route, child: _resolveChild(auth)),
    };
  }

  Widget _resolveChild(AuthProvider auth) {
    if (allowedRoles == null) return child;
    final roles = auth.currentUser?.roles ?? const <String>[];
    if (!allowedRoles!.any(roles.contains)) return const AccessDeniedScreen();
    return child;
  }
}
