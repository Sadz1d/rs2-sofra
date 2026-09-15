/// Mora se poklapati sa Sofra.API/Enums/OrderStatus.cs - raspored/vrijednosti enuma nisu string
/// preko JSON-a (backend ne koristi JsonStringEnumConverter), pa vrijednosti stizu kao brojevi.
enum OrderStatus {
  pending(0, 'Na čekanju'),
  confirmed(1, 'Potvrđena'),
  inPreparation(2, 'U pripremi'),
  ready(3, 'Spremna'),
  delivered(4, 'Isporučena'),
  completed(5, 'Završena'),
  cancelled(6, 'Otkazana');

  const OrderStatus(this.value, this.label);

  final int value;
  final String label;

  bool get isActive => this != OrderStatus.completed && this != OrderStatus.cancelled;

  static OrderStatus fromValue(int value) =>
      OrderStatus.values.firstWhere((e) => e.value == value, orElse: () => OrderStatus.pending);
}
