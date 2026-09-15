import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../models/allowed_transition.dart';
import '../../../models/reservation.dart';
import '../../../models/reservation_status.dart';
import '../../../providers/reservations_provider.dart';
import '../../../providers/tables_provider.dart';
import '../../../theme/app_theme.dart';
import '../../../utils/formatting.dart';
import '../../../widgets/app_toast.dart';
import '../../../widgets/confirm_dialog.dart';
import '../../../widgets/error_view.dart';
import '../../../widgets/loading_view.dart';
import 'reject_reservation_dialog.dart';

class ReservationDetailPanel extends StatelessWidget {
  const ReservationDetailPanel({super.key});

  @override
  Widget build(BuildContext context) {
    final reservations = context.watch<ReservationsProvider>();

    return Container(
      width: 380,
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(left: BorderSide(color: Color(0x1A000000))),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 20, 12, 12),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    reservations.selectedReservation?.userName ?? 'Rezervacija',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => context.read<ReservationsProvider>().clearSelection(),
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(child: _Body(reservations: reservations)),
        ],
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.reservations});

  final ReservationsProvider reservations;

  @override
  Widget build(BuildContext context) {
    if (reservations.selectedLoading) {
      return const LoadingView();
    }
    if (reservations.selectedError != null) {
      return ErrorView(
        message: reservations.selectedError!,
        onRetry: () => reservations.selectReservation(reservations.selectedReservationId!),
      );
    }
    final reservation = reservations.selectedReservation;
    if (reservation == null) {
      return const Center(child: Text('Odaberite rezervaciju.'));
    }

    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _Pill(label: reservation.status.label, color: _statusColor(reservation.status)),
          const SizedBox(height: 20),
          _InfoRow(label: 'Termin', value: formatDateTime(reservation.reservationAt)),
          _InfoRow(label: 'Trajanje', value: '${reservation.durationMinutes} min'),
          _InfoRow(label: 'Broj gostiju', value: '${reservation.guests}'),
          _InfoRow(label: 'Zona', value: reservation.zoneName),
          _InfoRow(label: 'Gost', value: reservation.userName),
          if (reservation.note != null && reservation.note!.isNotEmpty)
            _InfoRow(label: 'Napomena', value: reservation.note!),
          if (reservation.rejectReason != null) _InfoRow(label: 'Razlog odbijanja', value: reservation.rejectReason!),
          if (reservation.alternativeAt != null)
            _InfoRow(label: 'Predloženi termin', value: formatDateTime(reservation.alternativeAt!)),
          const SizedBox(height: 20),
          Text('Sto', style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 8),
          _TableAssignment(reservation: reservation),
          const SizedBox(height: 24),
          Text('Promjena statusa', style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 12),
          for (final option in reservation.allowedTransitions)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: _TransitionButton(reservation: reservation, option: option),
            ),
          if (reservation.allowedTransitions.isEmpty)
            Text(
              'Nema dostupnih akcija za vašu ulogu na ovom statusu.',
              style: Theme.of(context).textTheme.bodySmall,
            ),
          const SizedBox(height: 20),
          Text(_historyLine(reservation), style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    );
  }

  String _historyLine(ReservationDetail reservation) {
    final parts = <String>['Kreirana ${formatDateTime(reservation.createdAt)}'];
    if (reservation.processedByName != null && reservation.processedAt != null) {
      parts.add('Obradio ${reservation.processedByName} ${formatDateTime(reservation.processedAt!)}');
    }
    if (reservation.cancelledByName != null && reservation.cancelledAt != null) {
      parts.add('Otkazao ${reservation.cancelledByName} ${formatDateTime(reservation.cancelledAt!)}');
    }
    return 'Historija: ${parts.join(' · ')}';
  }

  Color _statusColor(ReservationStatus status) => switch (status) {
        ReservationStatus.pending => Colors.amber.shade800,
        ReservationStatus.confirmed => AppColors.success,
        ReservationStatus.rejected || ReservationStatus.cancelled || ReservationStatus.noShow => AppColors.danger,
        ReservationStatus.completed => AppColors.dark,
      };
}

class _TableAssignment extends StatelessWidget {
  const _TableAssignment({required this.reservation});

  final ReservationDetail reservation;

  @override
  Widget build(BuildContext context) {
    final tablesProvider = context.watch<TablesProvider>();
    final canAssign = reservation.status == ReservationStatus.pending || reservation.status == ReservationStatus.confirmed;
    final zoneTables = tablesProvider.tables.where((t) => t.zoneId == reservation.zoneId).toList();

    if (!canAssign) {
      return Text(
        reservation.diningTableNumber != null ? 'Sto ${reservation.diningTableNumber}' : 'Nije dodijeljen sto.',
      );
    }

    return DropdownButtonFormField<int?>(
      initialValue: reservation.diningTableId,
      isDense: true,
      decoration: const InputDecoration(labelText: 'Dodijeljeni sto'),
      items: [
        const DropdownMenuItem(value: null, child: Text('Bez dodijeljenog stola')),
        for (final table in zoneTables)
          DropdownMenuItem(value: table.id, child: Text('Sto ${table.number} (${table.capacity} os.)')),
      ],
      onChanged: (tableId) async {
        if (tableId == null) return;
        try {
          await context.read<ReservationsProvider>().assignTable(reservation.id, tableId);
          if (context.mounted) showAppToast(context, 'Sto je dodijeljen rezervaciji.');
        } catch (e) {
          if (context.mounted) showAppToast(context, e.toString(), isError: true);
        }
      },
    );
  }
}

class _TransitionButton extends StatelessWidget {
  const _TransitionButton({required this.reservation, required this.option});

  final ReservationDetail reservation;
  final AllowedTransition option;

  @override
  Widget build(BuildContext context) {
    final targetStatus = ReservationStatus.fromValue(option.status);
    final isDestructive = targetStatus == ReservationStatus.rejected ||
        targetStatus == ReservationStatus.cancelled ||
        targetStatus == ReservationStatus.noShow;

    return SizedBox(
      width: double.infinity,
      child: isDestructive
          ? OutlinedButton(
              style: OutlinedButton.styleFrom(foregroundColor: AppColors.danger),
              onPressed: () => _handle(context, targetStatus),
              child: Text(targetStatus.actionVerb),
            )
          : FilledButton(
              style: targetStatus == ReservationStatus.confirmed
                  ? FilledButton.styleFrom(backgroundColor: AppColors.success)
                  : null,
              onPressed: () => _handle(context, targetStatus),
              child: Text(targetStatus.actionVerb),
            ),
    );
  }

  Future<void> _handle(BuildContext context, ReservationStatus targetStatus) async {
    final reservationsProvider = context.read<ReservationsProvider>();

    if (targetStatus == ReservationStatus.rejected) {
      final result = await showRejectReservationDialog(
        context,
        guestName: reservation.userName,
        reservationAt: reservation.reservationAt,
        guests: reservation.guests,
        zoneName: reservation.zoneName,
      );
      if (result == null || !context.mounted) return;
      try {
        await reservationsProvider.transition(
          reservation.id,
          targetStatus,
          rejectReason: result.reason,
          alternativeAt: result.alternativeAt,
        );
        if (context.mounted) showAppToast(context, 'Rezervacija je odbijena i gost je obaviješten.');
      } catch (e) {
        if (context.mounted) showAppToast(context, e.toString(), isError: true);
      }
      return;
    }

    final confirmed = await showConfirmDialog(
      context,
      title: '${targetStatus.actionVerb} rezervaciju',
      message: '${targetStatus.actionVerb} rezervaciju za ${reservation.userName}?',
      confirmLabel: targetStatus.actionVerb,
      danger: targetStatus == ReservationStatus.cancelled || targetStatus == ReservationStatus.noShow,
    );
    if (!confirmed || !context.mounted) return;
    try {
      await reservationsProvider.transition(reservation.id, targetStatus);
      if (context.mounted) showAppToast(context, 'Status rezervacije je ažuriran.');
    } catch (e) {
      if (context.mounted) showAppToast(context, e.toString(), isError: true);
    }
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(width: 110, child: Text(label, style: Theme.of(context).textTheme.bodySmall)),
          Expanded(child: Text(value, style: const TextStyle(fontWeight: FontWeight.w500))),
        ],
      ),
    );
  }
}

class _Pill extends StatelessWidget {
  const _Pill({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
      child: Text(label, style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w600)),
    );
  }
}
