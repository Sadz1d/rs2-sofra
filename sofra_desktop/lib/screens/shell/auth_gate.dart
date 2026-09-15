import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/auth_provider.dart';
import '../auth/login_screen.dart';
import 'app_shell.dart';
import 'splash_view.dart';

/// Odlucuje sta se prikazuje za svaku imenovanu rutu na osnovu statusa prijave -
/// ekrani se ne pitaju sami da li je korisnik prijavljen.
class AuthGate extends StatelessWidget {
  const AuthGate({super.key, required this.route, required this.child});

  final String route;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final status = context.watch<AuthProvider>().status;

    return switch (status) {
      AuthStatus.unknown || AuthStatus.authenticating => const SplashView(),
      AuthStatus.unauthenticated => const LoginScreen(),
      AuthStatus.authenticated => AppShell(currentRoute: route, child: child),
    };
  }
}
