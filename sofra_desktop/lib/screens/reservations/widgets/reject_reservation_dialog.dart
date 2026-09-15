import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';
import '../../../utils/formatting.dart';

class RejectReservationResult {
  const RejectReservationResult({required this.reason, this.alternativeAt});

  final String reason;
  final DateTime? alternativeAt;
}

/// Odbijanje rezervacije - obavezan razlog (min. 10 znakova, salje se gostu) i opcioni
/// predlog alternativnog termina.
Future<RejectReservationResult?> showRejectReservationDialog(
  BuildContext context, {
  required String guestName,
  required DateTime reservationAt,
  required int guests,
  required String zoneName,
}) {
  return showDialog<RejectReservationResult>(
    context: context,
    builder: (_) => _RejectReservationDialog(
      guestName: guestName,
      reservationAt: reservationAt,
      guests: guests,
      zoneName: zoneName,
    ),
  );
}

class _RejectReservationDialog extends StatefulWidget {
  const _RejectReservationDialog({
    required this.guestName,
    required this.reservationAt,
    required this.guests,
    required this.zoneName,
  });

  final String guestName;
  final DateTime reservationAt;
  final int guests;
  final String zoneName;

  @override
  State<_RejectReservationDialog> createState() => _RejectReservationDialogState();
}

class _RejectReservationDialogState extends State<_RejectReservationDialog> {
  final _formKey = GlobalKey<FormState>();
  final _reasonController = TextEditingController();
  DateTime? _altDate;
  TimeOfDay? _altTime;

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: widget.reservationAt,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null && mounted) {
      setState(() => _altDate = picked);
    }
  }

  Future<void> _pickTime() async {
    final picked = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(widget.reservationAt),
    );
    if (picked != null && mounted) {
      setState(() => _altTime = picked);
    }
  }

  void _submit() {
    if (!_formKey.currentState!.validate()) return;
    DateTime? alternativeAt;
    if (_altDate != null && _altTime != null) {
      alternativeAt = DateTime(_altDate!.year, _altDate!.month, _altDate!.day, _altTime!.hour, _altTime!.minute);
    }
    Navigator.of(context).pop(
      RejectReservationResult(reason: _reasonController.text.trim(), alternativeAt: alternativeAt),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Odbij rezervaciju'),
      content: SizedBox(
        width: 420,
        child: Form(
          key: _formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('${widget.guestName} · ${formatDateTime(widget.reservationAt)} · ${widget.guests} osoba · ${widget.zoneName}'),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _reasonController,
                  maxLines: 3,
                  autofocus: true,
                  decoration: const InputDecoration(labelText: 'Razlog odbijanja *'),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) return 'Razlog je obavezan.';
                    if (value.trim().length < 10) return 'Razlog mora imati najmanje 10 znakova.';
                    return null;
                  },
                ),
                Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Text(
                    'Razlog je obavezan i biće poslan gostu (najmanje 10 znakova).',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                ),
                const SizedBox(height: 16),
                Text('Predloži alternativni termin', style: Theme.of(context).textTheme.titleSmall),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        icon: const Icon(Icons.calendar_today_outlined, size: 16),
                        label: Text(_altDate != null ? formatDate(_altDate!) : 'Datum'),
                        onPressed: _pickDate,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: OutlinedButton.icon(
                        icon: const Icon(Icons.access_time, size: 16),
                        label: Text(_altTime != null ? _altTime!.format(context) : 'Vrijeme'),
                        onPressed: _pickTime,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: Colors.blue.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Icon(Icons.info_outline, size: 18, color: Colors.blue),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          'Gost će odmah dobiti notifikaciju u aplikaciji i e-mail. Akcija se bilježi (ko, kada, razlog).',
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Odustani'),
        ),
        FilledButton.icon(
          style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
          icon: const Icon(Icons.send_outlined, size: 18),
          label: const Text('Odbij i obavijesti gosta'),
          onPressed: _submit,
        ),
      ],
    );
  }
}
