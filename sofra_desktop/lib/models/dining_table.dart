import 'table_status.dart';

class DiningTable {
  const DiningTable({
    required this.id,
    required this.number,
    required this.capacity,
    required this.zoneId,
    required this.zoneName,
    required this.status,
    required this.isActive,
  });

  final int id;
  final int number;
  final int capacity;
  final int zoneId;
  final String zoneName;
  final TableStatus status;
  final bool isActive;

  factory DiningTable.fromJson(Map<String, dynamic> json) => DiningTable(
        id: json['id'] as int,
        number: json['number'] as int,
        capacity: json['capacity'] as int,
        zoneId: json['zoneId'] as int,
        zoneName: json['zoneName'] as String,
        status: TableStatus.fromValue(json['status'] as int),
        isActive: json['isActive'] as bool,
      );
}
