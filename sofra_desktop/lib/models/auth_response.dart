import 'current_user.dart';

class AuthResponse {
  const AuthResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.accessTokenExpiresAtUtc,
    required this.user,
  });

  final String accessToken;
  final String refreshToken;
  final DateTime accessTokenExpiresAtUtc;
  final CurrentUser user;

  factory AuthResponse.fromJson(Map<String, dynamic> json) => AuthResponse(
        accessToken: json['accessToken'] as String,
        refreshToken: json['refreshToken'] as String,
        accessTokenExpiresAtUtc: DateTime.parse(json['accessTokenExpiresAtUtc'] as String),
        user: CurrentUser.fromJson(json['user'] as Map<String, dynamic>),
      );
}
