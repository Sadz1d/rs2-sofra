/// Mora se poklapati sa Sofra.API/Enums/ReservationStatus.cs.
enum ReservationStatus {
  pending(0, 'Na čekanju', 'Vrati na čekanje'),
  confirmed(1, 'Potvrđena', 'Potvrdi'),
  rejected(2, 'Odbijena', 'Odbij'),
  cancelled(3, 'Otkazana', 'Otkaži'),
  completed(4, 'Završena', 'Završi'),
  noShow(5, 'Nedolazak', 'Označi kao nedolazak');

  const ReservationStatus(this.value, this.label, this.actionVerb);

  final int value;

  /// Naziv statusa - isti tekst koji salje i API u AllowedTransition.Label.
  final String label;

  /// Cisto prezentacioni glagol za dugme - da li se prelaz SMIJE ponuditi dolazi
  /// iskljucivo iz ReservationResponse.allowedTransitions, ovo je samo tekst.
  final String actionVerb;

  static ReservationStatus fromValue(int value) =>
      ReservationStatus.values.firstWhere((e) => e.value == value, orElse: () => ReservationStatus.pending);
}
