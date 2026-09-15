class Zone {
  const Zone({required this.id, required this.name, required this.capacity});

  final int id;
  final String name;
  final int capacity;

  factory Zone.fromJson(Map<String, dynamic> json) => Zone(
        id: json['id'] as int,
        name: json['name'] as String,
        capacity: json['capacity'] as int,
      );
}
