import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// Poruka o rezultatu akcije - uvijek konkretna ("Smjena je sačuvana."), nikad generičko "Success"/"Error".
void showAppToast(BuildContext context, String message, {bool isError = false}) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: isError ? AppColors.danger : null,
      ),
    );
}
