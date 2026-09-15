import 'allowed_transition.dart';
import 'order_status.dart';
import 'order_type.dart';

class OrderListItem {
  const OrderListItem({
    required this.id,
    required this.number,
    required this.type,
    required this.status,
    required this.userId,
    required this.userName,
    required this.total,
    required this.isPaid,
    required this.createdAt,
    required this.itemCount,
    this.diningTableId,
    this.diningTableNumber,
  });

  final int id;
  final String number;
  final OrderType type;
  final OrderStatus status;
  final int userId;
  final String userName;
  final int? diningTableId;
  final int? diningTableNumber;
  final double total;
  final bool isPaid;
  final DateTime createdAt;
  final int itemCount;

  factory OrderListItem.fromJson(Map<String, dynamic> json) => OrderListItem(
        id: json['id'] as int,
        number: json['number'] as String,
        type: OrderType.fromValue(json['type'] as int),
        status: OrderStatus.fromValue(json['status'] as int),
        userId: json['userId'] as int,
        userName: json['userName'] as String,
        diningTableId: json['diningTableId'] as int?,
        diningTableNumber: json['diningTableNumber'] as int?,
        total: (json['total'] as num).toDouble(),
        isPaid: json['isPaid'] as bool,
        createdAt: DateTime.parse(json['createdAt'] as String),
        itemCount: json['itemCount'] as int,
      );
}

class OrderItem {
  const OrderItem({
    required this.id,
    required this.menuItemId,
    required this.menuItemName,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
    this.note,
  });

  final int id;
  final int menuItemId;
  final String menuItemName;
  final int quantity;
  final double unitPrice;
  final double lineTotal;
  final String? note;

  factory OrderItem.fromJson(Map<String, dynamic> json) => OrderItem(
        id: json['id'] as int,
        menuItemId: json['menuItemId'] as int,
        menuItemName: json['menuItemName'] as String,
        quantity: json['quantity'] as int,
        unitPrice: (json['unitPrice'] as num).toDouble(),
        lineTotal: (json['lineTotal'] as num).toDouble(),
        note: json['note'] as String?,
      );
}

class OrderDetail {
  const OrderDetail({
    required this.id,
    required this.number,
    required this.type,
    required this.status,
    required this.userId,
    required this.userName,
    required this.subtotal,
    required this.discount,
    required this.tax,
    required this.total,
    required this.isPaid,
    required this.createdAt,
    required this.items,
    required this.allowedTransitions,
    this.diningTableId,
    this.diningTableNumber,
    this.waiterId,
    this.waiterName,
    this.note,
    this.promotionId,
    this.promotionCode,
    this.paymentMethodName,
    this.confirmedAt,
    this.preparationStartedAt,
    this.readyAt,
    this.deliveredAt,
    this.completedAt,
    this.cancelledAt,
    this.cancelReason,
  });

  final int id;
  final String number;
  final OrderType type;
  final OrderStatus status;
  final int userId;
  final String userName;
  final int? diningTableId;
  final int? diningTableNumber;
  final int? waiterId;
  final String? waiterName;
  final String? note;
  final double subtotal;
  final double discount;
  final double tax;
  final double total;
  final int? promotionId;
  final String? promotionCode;
  final bool isPaid;
  final String? paymentMethodName;
  final DateTime createdAt;
  final DateTime? confirmedAt;
  final DateTime? preparationStartedAt;
  final DateTime? readyAt;
  final DateTime? deliveredAt;
  final DateTime? completedAt;
  final DateTime? cancelledAt;
  final String? cancelReason;
  final List<OrderItem> items;
  final List<AllowedTransition> allowedTransitions;

  /// Vrijeme na koje se oslanja "vrijeme cekanja" prikaz - zadnji poznati status timestamp, ili kreiranje.
  DateTime get lastStatusChangeAt =>
      cancelledAt ?? completedAt ?? deliveredAt ?? readyAt ?? preparationStartedAt ?? confirmedAt ?? createdAt;

  /// Napomene sa nivoa narudzbe i stavki spojene u jednu traku - mockup prikazuje jednu traku po kartici.
  List<String> get notes => [
        if (note != null && note!.trim().isNotEmpty) note!.trim(),
        for (final item in items)
          if (item.note != null && item.note!.trim().isNotEmpty) '${item.menuItemName}: ${item.note!.trim()}',
      ];

  factory OrderDetail.fromJson(Map<String, dynamic> json) => OrderDetail(
        id: json['id'] as int,
        number: json['number'] as String,
        type: OrderType.fromValue(json['type'] as int),
        status: OrderStatus.fromValue(json['status'] as int),
        userId: json['userId'] as int,
        userName: json['userName'] as String,
        diningTableId: json['diningTableId'] as int?,
        diningTableNumber: json['diningTableNumber'] as int?,
        waiterId: json['waiterId'] as int?,
        waiterName: json['waiterName'] as String?,
        note: json['note'] as String?,
        subtotal: (json['subtotal'] as num).toDouble(),
        discount: (json['discount'] as num).toDouble(),
        tax: (json['tax'] as num).toDouble(),
        total: (json['total'] as num).toDouble(),
        promotionId: json['promotionId'] as int?,
        promotionCode: json['promotionCode'] as String?,
        isPaid: json['isPaid'] as bool,
        paymentMethodName: json['paymentMethodName'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
        confirmedAt: _parseNullable(json['confirmedAt']),
        preparationStartedAt: _parseNullable(json['preparationStartedAt']),
        readyAt: _parseNullable(json['readyAt']),
        deliveredAt: _parseNullable(json['deliveredAt']),
        completedAt: _parseNullable(json['completedAt']),
        cancelledAt: _parseNullable(json['cancelledAt']),
        cancelReason: json['cancelReason'] as String?,
        items: (json['items'] as List<dynamic>)
            .map((e) => OrderItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        allowedTransitions: (json['allowedTransitions'] as List<dynamic>)
            .map((e) => AllowedTransition.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  static DateTime? _parseNullable(dynamic value) => value == null ? null : DateTime.parse(value as String);
}
