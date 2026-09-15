class TableType {
  const TableType({required this.id, required this.name});

  final int id;
  final String name;

  factory TableType.fromJson(Map<String, dynamic> json) => TableType(
        id: json['id'] as int,
        name: json['name'] as String,
      );
}
