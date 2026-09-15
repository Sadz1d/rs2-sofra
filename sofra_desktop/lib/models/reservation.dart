import 'reservation_status.dart';

class ReservationListItem {
  const ReservationListItem({
    required this.id,
    required this.userId,
    required this.userName,
    required this.reservationAt,
    required this.durationMinutes,
    required this.guests,
    required this.zoneId,
    required this.zoneName,
    required this.status,
    required this.createdAt,
    this.diningTableId,
    this.diningTableNumber,
  });

  final int id;
  final int userId;
  final String userName;
  final DateTime reservationAt;
  final int durationMinutes;
  final int guests;
  final int zoneId;
  final String zoneName;
  final int? diningTableId;
  final int? diningTableNumber;
  final ReservationStatus status;
  final DateTime createdAt;

  factory ReservationListItem.fromJson(Map<String, dynamic> json) => ReservationListItem(
        id: json['id'] as int,
        userId: json['userId'] as int,
        userName: json['userName'] as String,
        reservationAt: DateTime.parse(json['reservationAt'] as String),
        durationMinutes: json['durationMinutes'] as int,
        guests: json['guests'] as int,
        zoneId: json['zoneId'] as int,
        zoneName: json['zoneName'] as String,
        diningTableId: json['diningTableId'] as int?,
        diningTableNumber: json['diningTableNumber'] as int?,
        status: ReservationStatus.fromValue(json['status'] as int),
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}
