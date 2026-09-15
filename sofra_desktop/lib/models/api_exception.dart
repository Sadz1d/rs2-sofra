/// Standardizovana greska iz API-ja - poruka za korisnika i, ako je validacijska (422),
/// greske po poljima da ih forme mogu prikazati ispod odgovarajuce kontrole.
class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode, this.fieldErrors});

  final String message;
  final int? statusCode;
  final Map<String, List<String>>? fieldErrors;

  List<String>? errorsFor(String field) => fieldErrors?[field];

  @override
  String toString() => message;
}
