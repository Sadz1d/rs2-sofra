/// Jedan status u koji prijavljeni korisnik smije upravo sada prevesti narudzbu/rezervaciju -
/// vec provjereno na backendu (uloga, vlasnistvo, placanje). UI crta dugmad iskljucivo iz ove
/// liste i ne racuna dozvole sam.
class AllowedTransition {
  const AllowedTransition({required this.status, required this.label});

  final int status;
  final String label;

  factory AllowedTransition.fromJson(Map<String, dynamic> json) => AllowedTransition(
        status: json['status'] as int,
        label: json['label'] as String,
      );
}

extension AllowedTransitionListX on List<AllowedTransition> {
  AllowedTransition? forStatus(int status) {
    for (final transition in this) {
      if (transition.status == status) return transition;
    }
    return null;
  }
}
