/// Mora se poklapati sa Sofra.API/Enums/TableStatus.cs.
enum TableStatus {
  free(0, 'Slobodan'),
  occupied(1, 'Zauzet'),
  reserved(2, 'Rezervisan');

  const TableStatus(this.value, this.label);

  final int value;
  final String label;

  static TableStatus fromValue(int value) =>
      TableStatus.values.firstWhere((e) => e.value == value, orElse: () => TableStatus.free);
}
