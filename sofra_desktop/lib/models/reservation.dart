import 'allowed_transition.dart';
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

class ReservationDetail {
  const ReservationDetail({
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
    required this.allowedTransitions,
    this.diningTableId,
    this.diningTableNumber,
    this.note,
    this.rejectReason,
    this.alternativeAt,
    this.processedById,
    this.processedByName,
    this.processedAt,
    this.cancelledById,
    this.cancelledByName,
    this.cancelledAt,
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
  final String? note;
  final ReservationStatus status;
  final String? rejectReason;
  final DateTime? alternativeAt;
  final int? processedById;
  final String? processedByName;
  final DateTime? processedAt;
  final int? cancelledById;
  final String? cancelledByName;
  final DateTime? cancelledAt;
  final DateTime createdAt;
  final List<AllowedTransition> allowedTransitions;

  factory ReservationDetail.fromJson(Map<String, dynamic> json) => ReservationDetail(
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
        note: json['note'] as String?,
        status: ReservationStatus.fromValue(json['status'] as int),
        rejectReason: json['rejectReason'] as String?,
        alternativeAt: json['alternativeAt'] == null ? null : DateTime.parse(json['alternativeAt'] as String),
        processedById: json['processedById'] as int?,
        processedByName: json['processedByName'] as String?,
        processedAt: json['processedAt'] == null ? null : DateTime.parse(json['processedAt'] as String),
        cancelledById: json['cancelledById'] as int?,
        cancelledByName: json['cancelledByName'] as String?,
        cancelledAt: json['cancelledAt'] == null ? null : DateTime.parse(json['cancelledAt'] as String),
        createdAt: DateTime.parse(json['createdAt'] as String),
        allowedTransitions: (json['allowedTransitions'] as List<dynamic>)
            .map((e) => AllowedTransition.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
