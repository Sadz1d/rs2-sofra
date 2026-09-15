class InventoryItem {
  const InventoryItem({
    required this.id,
    required this.name,
    required this.unitOfMeasureAbbreviation,
    required this.quantity,
    required this.minQuantity,
    required this.isLowStock,
  });

  final int id;
  final String name;
  final String unitOfMeasureAbbreviation;
  final double quantity;
  final double minQuantity;
  final bool isLowStock;

  factory InventoryItem.fromJson(Map<String, dynamic> json) => InventoryItem(
        id: json['id'] as int,
        name: json['name'] as String,
        unitOfMeasureAbbreviation: json['unitOfMeasureAbbreviation'] as String,
        quantity: (json['quantity'] as num).toDouble(),
        minQuantity: (json['minQuantity'] as num).toDouble(),
        isLowStock: json['isLowStock'] as bool,
      );
}
