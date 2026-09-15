import 'package:intl/intl.dart';

/// Koristi 'de_DE' obrazac (tačka za hiljade, zarez za decimale) jer se poklapa sa
/// bosanskim zapisom novca (npr. "2.480,00 KM") bez potrebe za inicijalizacijom
/// dodatnih lokalizacionih podataka za 'bs' koji intl ne garantuje unaprijed.
final _decimalFormat = NumberFormat('#,##0.00', 'de_DE');
final _integerFormat = NumberFormat('#,##0', 'de_DE');
final _dateFormat = DateFormat('dd.MM.yyyy.');
final _timeFormat = DateFormat('HH:mm');
final _dateTimeFormat = DateFormat('dd.MM.yyyy. HH:mm');

String formatMoney(num value) => '${_decimalFormat.format(value)} KM';

String formatInt(num value) => _integerFormat.format(value);

String formatDate(DateTime value) => _dateFormat.format(value.toLocal());

String formatTime(DateTime value) => _timeFormat.format(value.toLocal());

String formatDateTime(DateTime value) => _dateTimeFormat.format(value.toLocal());

final _dateOnlyFormat = DateFormat('yyyy-MM-dd');

/// Format koji ocekuju DateOnly parametri na /api/statistics i /api/reports (bez vremena).
String apiDateOnly(DateTime value) => _dateOnlyFormat.format(value);

/// "čeka 8 min" / "čeka 1h 12min" stil trajanja od [since] do sada.
String formatWaitDuration(DateTime since) {
  final elapsed = DateTime.now().difference(since);
  if (elapsed.inMinutes < 1) return 'upravo sad';
  if (elapsed.inMinutes < 60) return '${elapsed.inMinutes} min';
  final hours = elapsed.inHours;
  final minutes = elapsed.inMinutes % 60;
  return '${hours}h ${minutes}min';
}
