import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../providers/orders_provider.dart';
import 'widgets/order_detail_panel.dart';
import 'widgets/order_kanban_board.dart';
import 'widgets/orders_history_tab.dart';

class OrdersScreen extends StatefulWidget {
  const OrdersScreen({super.key});

  @override
  State<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends State<OrdersScreen> with SingleTickerProviderStateMixin {
  late final TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<OrdersProvider>().loadBoard();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final orders = context.watch<OrdersProvider>();

    return Row(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Material(
                color: Colors.white,
                child: TabBar(
                  controller: _tabController,
                  isScrollable: true,
                  tabs: const [
                    Tab(text: 'Aktivne'),
                    Tab(text: 'Historija'),
                  ],
                ),
              ),
              const Divider(height: 1),
              Expanded(
                child: TabBarView(
                  controller: _tabController,
                  children: [
                    OrderKanbanBoard(onSelect: (id) => context.read<OrdersProvider>().selectOrder(id)),
                    OrdersHistoryTab(onSelect: (id) => context.read<OrdersProvider>().selectOrder(id)),
                  ],
                ),
              ),
            ],
          ),
        ),
        if (orders.selectedOrderId != null) const OrderDetailPanel(),
      ],
    );
  }
}
