class CurrentUser {
  const CurrentUser({
    required this.id,
    required this.username,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.roles,
    this.phone,
    this.imageUrl,
  });

  final int id;
  final String username;
  final String email;
  final String firstName;
  final String lastName;
  final String? phone;
  final String? imageUrl;
  final List<String> roles;

  String get fullName => '$firstName $lastName';

  bool hasRole(String role) => roles.contains(role);

  factory CurrentUser.fromJson(Map<String, dynamic> json) => CurrentUser(
        id: json['id'] as int,
        username: json['username'] as String,
        email: json['email'] as String,
        firstName: json['firstName'] as String,
        lastName: json['lastName'] as String,
        phone: json['phone'] as String?,
        imageUrl: json['imageUrl'] as String?,
        roles: (json['roles'] as List<dynamic>).map((e) => e as String).toList(),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'username': username,
        'email': email,
        'firstName': firstName,
        'lastName': lastName,
        'phone': phone,
        'imageUrl': imageUrl,
        'roles': roles,
      };
}
