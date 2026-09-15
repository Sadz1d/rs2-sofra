import 'table_status.dart';

class DiningTable {
  const DiningTable({
    required this.id,
    required this.number,
    required this.capacity,
    required this.zoneId,
    required this.zoneName,
    required this.tableTypeId,
    required this.tableTypeName,
    required this.status,
    required this.qrCode,
    required this.isActive,
    this.waiterId,
    this.waiterName,
  });

  final int id;
  final int number;
  final int capacity;
  final int zoneId;
  final String zoneName;
  final int tableTypeId;
  final String tableTypeName;
  final TableStatus status;
  final int? waiterId;
  final String? waiterName;
  final String qrCode;
  final bool isActive;

  factory DiningTable.fromJson(Map<String, dynamic> json) => DiningTable(
        id: json['id'] as int,
        number: json['number'] as int,
        capacity: json['capacity'] as int,
        zoneId: json['zoneId'] as int,
        zoneName: json['zoneName'] as String,
        tableTypeId: json['tableTypeId'] as int,
        tableTypeName: json['tableTypeName'] as String,
        status: TableStatus.fromValue(json['status'] as int),
        waiterId: json['waiterId'] as int?,
        waiterName: json['waiterName'] as String?,
        qrCode: json['qrCode'] as String,
        isActive: json['isActive'] as bool,
      );
}
