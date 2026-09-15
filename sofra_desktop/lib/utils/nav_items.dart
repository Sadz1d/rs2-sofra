import 'package:flutter/material.dart';

import '../constants/roles.dart';

class NavItem {
  const NavItem({
    required this.label,
    required this.icon,
    required this.route,
    required this.allowedRoles,
  });

  final String label;
  final IconData icon;
  final String route;
  final List<String> allowedRoles;
}

/// Stavke sidebara - vidljivost svake se odredjuje prema ulozi prijavljenog korisnika.
/// Rute osim '/' za sada vode na privremeni "uskoro dostupno" ekran dok se moduli ne izgrade.
const List<NavItem> kNavItems = [
  NavItem(
    label: 'Početna',
    icon: Icons.dashboard_outlined,
    route: '/',
    allowedRoles: [Roles.admin, Roles.konobar, Roles.kuhar],
  ),
  NavItem(
    label: 'Narudžbe',
    icon: Icons.receipt_long_outlined,
    route: '/orders',
    allowedRoles: [Roles.admin, Roles.konobar, Roles.kuhar],
  ),
  NavItem(
    label: 'Kuhinja',
    icon: Icons.soup_kitchen_outlined,
    route: '/kitchen',
    allowedRoles: [Roles.admin, Roles.kuhar],
  ),
  NavItem(
    label: 'Stolovi',
    icon: Icons.table_bar_outlined,
    route: '/tables',
    allowedRoles: [Roles.admin, Roles.konobar],
  ),
  NavItem(
    label: 'Rezervacije',
    icon: Icons.event_available_outlined,
    route: '/reservations',
    allowedRoles: [Roles.admin, Roles.konobar],
  ),
  NavItem(
    label: 'Meni',
    icon: Icons.restaurant_menu_outlined,
    route: '/menu',
    allowedRoles: [Roles.admin],
  ),
  NavItem(
    label: 'Zalihe',
    icon: Icons.inventory_2_outlined,
    route: '/inventory',
    allowedRoles: [Roles.admin, Roles.kuhar],
  ),
  NavItem(
    label: 'Osoblje i smjene',
    icon: Icons.badge_outlined,
    route: '/staff',
    allowedRoles: [Roles.admin],
  ),
  NavItem(
    label: 'Recenzije',
    icon: Icons.star_outline,
    route: '/reviews',
    allowedRoles: [Roles.admin],
  ),
  NavItem(
    label: 'Promocije',
    icon: Icons.local_offer_outlined,
    route: '/promotions',
    allowedRoles: [Roles.admin],
  ),
  NavItem(
    label: 'Obavijesti',
    icon: Icons.campaign_outlined,
    route: '/news',
    allowedRoles: [Roles.admin],
  ),
  NavItem(
    label: 'Izvještaji',
    icon: Icons.picture_as_pdf_outlined,
    route: '/reports',
    allowedRoles: [Roles.admin],
  ),
  NavItem(
    label: 'Statistika',
    icon: Icons.bar_chart_outlined,
    route: '/statistics',
    allowedRoles: [Roles.admin],
  ),
  NavItem(
    label: 'Šifarnici',
    icon: Icons.list_alt_outlined,
    route: '/lookups',
    allowedRoles: [Roles.admin],
  ),
];
