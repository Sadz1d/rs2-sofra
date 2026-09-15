import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../models/api_exception.dart';
import '../../../models/dining_table.dart';
import '../../../providers/tables_provider.dart';
import '../../../widgets/app_toast.dart';
import '../../../widgets/form_error_text.dart';

/// Forma za kreiranje/izmjenu stola (Admin) - broj, kapacitet, zona, tip, konobar, QR kod.
/// Status se namjerno ne uredjuje ovdje - mijenja se iskljucivo posljedicno kroz narudzbe/rezervacije.
class TableFormDialog extends StatefulWidget {
  const TableFormDialog({super.key, this.existing});

  final DiningTable? existing;

  @override
  State<TableFormDialog> createState() => _TableFormDialogState();
}

class _TableFormDialogState extends State<TableFormDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _numberController;
  late final TextEditingController _capacityController;
  late final TextEditingController _qrController;
  int? _zoneId;
  int? _tableTypeId;
  int? _waiterId;
  bool _saving = false;
  Map<String, List<String>>? _fieldErrors;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    _numberController = TextEditingController(text: existing?.number.toString() ?? '');
    _capacityController = TextEditingController(text: existing?.capacity.toString() ?? '');
    _qrController = TextEditingController(text: existing?.qrCode ?? '');
    _zoneId = existing?.zoneId;
    _tableTypeId = existing?.tableTypeId;
    _waiterId = existing?.waiterId;
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<TablesProvider>().loadKonobarOptions();
    });
  }

  @override
  void dispose() {
    _numberController.dispose();
    _capacityController.dispose();
    _qrController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final tablesProvider = context.watch<TablesProvider>();
    final isEdit = widget.existing != null;

    return AlertDialog(
      title: Text(isEdit ? 'Uredi sto ${widget.existing!.number}' : 'Novi sto'),
      content: SizedBox(
        width: 380,
        child: Form(
          key: _formKey,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                TextFormField(
                  controller: _numberController,
                  decoration: const InputDecoration(labelText: 'Broj stola'),
                  keyboardType: TextInputType.number,
                  validator: (value) =>
                      (value == null || int.tryParse(value) == null || int.parse(value) <= 0)
                          ? 'Unesite validan broj stola.'
                          : null,
                ),
                FormErrorText(errors: _fieldErrors?['Number']),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _capacityController,
                  decoration: const InputDecoration(labelText: 'Kapacitet (broj osoba)'),
                  keyboardType: TextInputType.number,
                  validator: (value) =>
                      (value == null || int.tryParse(value) == null || int.parse(value) <= 0)
                          ? 'Unesite validan kapacitet.'
                          : null,
                ),
                FormErrorText(errors: _fieldErrors?['Capacity']),
                const SizedBox(height: 12),
                DropdownButtonFormField<int>(
                  initialValue: _zoneId,
                  decoration: const InputDecoration(labelText: 'Zona'),
                  items: [
                    for (final zone in tablesProvider.zones) DropdownMenuItem(value: zone.id, child: Text(zone.name)),
                  ],
                  onChanged: (value) => setState(() => _zoneId = value),
                  validator: (value) => value == null ? 'Zona je obavezna.' : null,
                ),
                FormErrorText(errors: _fieldErrors?['ZoneId']),
                const SizedBox(height: 12),
                DropdownButtonFormField<int>(
                  initialValue: _tableTypeId,
                  decoration: const InputDecoration(labelText: 'Tip stola'),
                  items: [
                    for (final type in tablesProvider.tableTypes) DropdownMenuItem(value: type.id, child: Text(type.name)),
                  ],
                  onChanged: (value) => setState(() => _tableTypeId = value),
                  validator: (value) => value == null ? 'Tip stola je obavezan.' : null,
                ),
                FormErrorText(errors: _fieldErrors?['TableTypeId']),
                const SizedBox(height: 12),
                DropdownButtonFormField<int?>(
                  initialValue: _waiterId,
                  decoration: const InputDecoration(labelText: 'Dodijeljeni konobar'),
                  items: [
                    const DropdownMenuItem(value: null, child: Text('Bez konobara')),
                    for (final waiter in tablesProvider.konobarOptions)
                      DropdownMenuItem(value: waiter.id, child: Text(waiter.fullName)),
                  ],
                  onChanged: (value) => setState(() => _waiterId = value),
                ),
                FormErrorText(errors: _fieldErrors?['WaiterId']),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _qrController,
                  decoration: const InputDecoration(labelText: 'QR kod (opcionalno - generiše se automatski)'),
                ),
                FormErrorText(errors: _fieldErrors?['QrCode']),
              ],
            ),
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _saving ? null : () => Navigator.of(context).pop(),
          child: const Text('Odustani'),
        ),
        FilledButton(
          onPressed: _saving ? null : _submit,
          child: _saving
              ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
              : Text(isEdit ? 'Sačuvaj' : 'Kreiraj'),
        ),
      ],
    );
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _saving = true;
      _fieldErrors = null;
    });

    final tablesProvider = context.read<TablesProvider>();
    final number = int.parse(_numberController.text);
    final capacity = int.parse(_capacityController.text);
    final qrCode = _qrController.text.trim().isEmpty ? null : _qrController.text.trim();

    try {
      if (widget.existing != null) {
        await tablesProvider.updateTable(
          widget.existing!.id,
          number: number,
          capacity: capacity,
          zoneId: _zoneId!,
          tableTypeId: _tableTypeId!,
          waiterId: _waiterId,
          qrCode: qrCode,
        );
      } else {
        await tablesProvider.createTable(
          number: number,
          capacity: capacity,
          zoneId: _zoneId!,
          tableTypeId: _tableTypeId!,
          waiterId: _waiterId,
          qrCode: qrCode,
        );
      }
      if (mounted) {
        Navigator.of(context).pop();
        showAppToast(context, widget.existing != null ? 'Sto je sačuvan.' : 'Sto je kreiran.');
      }
    } on ApiException catch (e) {
      setState(() {
        _fieldErrors = e.fieldErrors;
        _saving = false;
      });
      if (e.fieldErrors == null && mounted) {
        showAppToast(context, e.message, isError: true);
      }
    } catch (e) {
      setState(() => _saving = false);
      if (mounted) showAppToast(context, e.toString(), isError: true);
    }
  }
}
