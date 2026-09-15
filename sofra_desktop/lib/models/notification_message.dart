/// Payload dogadjaja "notificationReceived" sa NotificationHub-a.
/// [type] je sirova vrijednost enuma NotificationType (backend ne koristi string konverziju za enume).
class NotificationMessage {
  const NotificationMessage({
    required this.title,
    required this.text,
    required this.type,
    required this.createdAt,
    this.referenceId,
  });

  final String title;
  final String text;
  final int type;
  final int? referenceId;
  final DateTime createdAt;

  factory NotificationMessage.fromJson(Map<String, dynamic> json) => NotificationMessage(
        title: json['title'] as String,
        text: json['text'] as String,
        type: json['type'] as int,
        referenceId: json['referenceId'] as int?,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}
