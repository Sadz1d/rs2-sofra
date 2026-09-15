/// Minimalan prikaz osoblja - samo za dropdown izbore (npr. dodjela konobara stolu).
class StaffUser {
  const StaffUser({required this.id, required this.fullName});

  final int id;
  final String fullName;

  factory StaffUser.fromJson(Map<String, dynamic> json) => StaffUser(
        id: json['id'] as int,
        fullName: '${json['firstName']} ${json['lastName']}',
      );
}
