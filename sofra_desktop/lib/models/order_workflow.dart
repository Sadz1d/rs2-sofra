import '../constants/roles.dart';
import 'order_status.dart';

class OrderTransitionOption {
  const OrderTransitionOption({required this.to, required this.label, required this.allowed, this.disabledReason});

  final OrderStatus to;
  final String label;
  final bool allowed;
  final String? disabledReason;
}

/// Ogledalo Sofra.API/Services/OrderStateMachine.cs (Graph) - samo za odluku koja se dugmad
/// prikazuju u UI-ju. Backend ostaje jedini stvarni izvor istine i ponovo provjerava sve ovo;
/// ovo postoji da se nedozvoljen prelaz nikad i ne ponudi korisniku.
class OrderWorkflow {
  const OrderWorkflow._();

  static const Map<OrderStatus, List<_Transition>> _graph = {
    OrderStatus.pending: [
      _Transition(OrderStatus.confirmed, [Roles.konobar, Roles.admin]),
      _Transition(OrderStatus.cancelled, [Roles.konobar, Roles.admin]),
    ],
    OrderStatus.confirmed: [
      _Transition(OrderStatus.inPreparation, [Roles.kuhar, Roles.admin]),
      _Transition(OrderStatus.cancelled, [Roles.konobar, Roles.admin]),
    ],
    OrderStatus.inPreparation: [
      _Transition(OrderStatus.ready, [Roles.kuhar, Roles.admin]),
    ],
    OrderStatus.ready: [
      _Transition(OrderStatus.delivered, [Roles.konobar, Roles.admin]),
    ],
    OrderStatus.delivered: [
      _Transition(OrderStatus.completed, [Roles.konobar, Roles.admin]),
    ],
  };

  /// Sve dozvoljene sljedece statuse iz [current] za korisnika sa [roles], sa razlogom
  /// zasto je otkazivanje (ako je ponudjeno) onemoguceno kad je narudzba vec placena.
  static List<OrderTransitionOption> nextOptions(
    OrderStatus current,
    List<String> roles, {
    required bool isPaid,
  }) {
    final transitions = _graph[current] ?? const [];
    return [
      for (final t in transitions)
        OrderTransitionOption(
          to: t.to,
          label: _actionLabel(t.to),
          allowed: t.roles.any(roles.contains) && !(t.to == OrderStatus.cancelled && isPaid),
          disabledReason: t.to == OrderStatus.cancelled && isPaid
              ? 'Plaćena narudžba se ne može otkazati bez povrata sredstava.'
              : null,
        ),
    ];
  }

  /// Jedna "glavna" akcija za kompaktnu karticu (kanban/KDS) - prva dozvoljena koja NIJE otkazivanje.
  static OrderTransitionOption? primaryOption(OrderStatus current, List<String> roles, {required bool isPaid}) {
    final options = nextOptions(current, roles, isPaid: isPaid);
    for (final option in options) {
      if (option.allowed && option.to != OrderStatus.cancelled) {
        return option;
      }
    }
    return null;
  }

  static bool canCancel(OrderStatus current, List<String> roles, {required bool isPaid}) {
    final transitions = _graph[current] ?? const [];
    return transitions.any((t) => t.to == OrderStatus.cancelled && t.roles.any(roles.contains));
  }

  static String _actionLabel(OrderStatus to) => switch (to) {
        OrderStatus.confirmed => 'Potvrdi',
        OrderStatus.inPreparation => 'Započni pripremu',
        OrderStatus.ready => 'Označi kao spremno',
        OrderStatus.delivered => 'Označi kao isporučeno',
        OrderStatus.completed => 'Završi narudžbu',
        OrderStatus.cancelled => 'Otkaži narudžbu',
        OrderStatus.pending => 'Vrati na čekanje',
      };
}

class _Transition {
  const _Transition(this.to, this.roles);

  final OrderStatus to;
  final List<String> roles;
}
