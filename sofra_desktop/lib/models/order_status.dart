/// Mora se poklapati sa Sofra.API/Enums/OrderStatus.cs - raspored/vrijednosti enuma nisu string
/// preko JSON-a (backend ne koristi JsonStringEnumConverter), pa vrijednosti stizu kao brojevi.
enum OrderStatus {
  pending(0, 'Na čekanju', 'Vrati na čekanje'),
  confirmed(1, 'Potvrđena', 'Potvrdi'),
  inPreparation(2, 'U pripremi', 'Započni pripremu'),
  ready(3, 'Spremna', 'Označi kao spremno'),
  delivered(4, 'Isporučena', 'Označi kao isporučeno'),
  completed(5, 'Završena', 'Završi narudžbu'),
  cancelled(6, 'Otkazana', 'Otkaži narudžbu');

  const OrderStatus(this.value, this.label, this.actionVerb);

  final int value;

  /// Naziv statusa - isti tekst koji salje i API u AllowedTransition.Label.
  final String label;

  /// Cisto prezentacioni glagol za dugme ("Potvrdi" umjesto "Potvrđena") - da li se prelaz
  /// SMIJE ponuditi dolazi iskljucivo iz OrderResponse.allowedTransitions, ovo je samo tekst.
  final String actionVerb;

  bool get isActive => this != OrderStatus.completed && this != OrderStatus.cancelled;

  static OrderStatus fromValue(int value) =>
      OrderStatus.values.firstWhere((e) => e.value == value, orElse: () => OrderStatus.pending);
}
