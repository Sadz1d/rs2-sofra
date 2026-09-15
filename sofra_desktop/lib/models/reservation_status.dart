/// Mora se poklapati sa Sofra.API/Enums/ReservationStatus.cs.
enum ReservationStatus {
  pending(0, 'Na čekanju'),
  confirmed(1, 'Potvrđena'),
  rejected(2, 'Odbijena'),
  cancelled(3, 'Otkazana'),
  completed(4, 'Završena'),
  noShow(5, 'Nedolazak');

  const ReservationStatus(this.value, this.label);

  final int value;
  final String label;

  static ReservationStatus fromValue(int value) =>
      ReservationStatus.values.firstWhere((e) => e.value == value, orElse: () => ReservationStatus.pending);
}
