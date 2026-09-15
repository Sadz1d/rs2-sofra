/// Mora se poklapati sa Sofra.API/Enums/OrderType.cs.
enum OrderType {
  dineIn(0, 'Za stolom'),
  takeaway(1, 'Za ponijeti');

  const OrderType(this.value, this.label);

  final int value;
  final String label;

  static OrderType fromValue(int value) =>
      OrderType.values.firstWhere((e) => e.value == value, orElse: () => OrderType.dineIn);
}
