import 'package:flutter/material.dart';

class AppColors {
  const AppColors._();

  static const primary = Color(0xFFD4572B);
  static const dark = Color(0xFF221F1B);
  static const background = Color(0xFFF6F3EE);
  static const danger = Color(0xFFB3261E);
  static const success = Color(0xFF2E7D32);
}

/// Poppins za naslove (display/headline/title), Inter za tekst (body/label) -
/// oba su lokalni asset fontovi u assets/fonts/, bez mreznog fetch-a pri pokretanju.
class AppFonts {
  const AppFonts._();

  static const heading = 'Poppins';
  static const body = 'Inter';
}

/// Jedan ThemeData za citavu aplikaciju - ekrani ne definisu vlastite boje ni stilove.
class AppTheme {
  const AppTheme._();

  static ThemeData get light {
    final colorScheme = ColorScheme.fromSeed(
      seedColor: AppColors.primary,
      brightness: Brightness.light,
    ).copyWith(
      primary: AppColors.primary,
      onPrimary: Colors.white,
      surface: Colors.white,
      error: AppColors.danger,
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: colorScheme,
      scaffoldBackgroundColor: AppColors.background,
      fontFamily: AppFonts.body,
      textTheme: _textTheme(AppColors.dark),
      appBarTheme: const AppBarTheme(
        backgroundColor: Colors.white,
        foregroundColor: AppColors.dark,
        elevation: 0,
        scrolledUnderElevation: 0,
      ),
      cardTheme: CardThemeData(
        color: Colors.white,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: const BorderSide(color: Color(0x1A000000)),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          disabledBackgroundColor: AppColors.primary.withValues(alpha: 0.4),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.dark,
          side: const BorderSide(color: Color(0x33221F1B)),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(foregroundColor: AppColors.primary),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: Color(0x33221F1B)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: Color(0x33221F1B)),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.danger),
        ),
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      ),
      dividerTheme: const DividerThemeData(color: Color(0x1A221F1B), thickness: 1),
      snackBarTheme: SnackBarThemeData(
        backgroundColor: AppColors.dark,
        contentTextStyle: const TextStyle(color: Colors.white),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
    );
  }

  static TextTheme _textTheme(Color bodyColor) {
    final base = Typography.blackMountainView
        .apply(bodyColor: bodyColor, displayColor: bodyColor)
        .apply(fontFamily: AppFonts.body);

    TextStyle? heading(TextStyle? style, FontWeight weight) =>
        style?.copyWith(fontFamily: AppFonts.heading, fontWeight: weight);

    return base.copyWith(
      displayLarge: heading(base.displayLarge, FontWeight.w600),
      displayMedium: heading(base.displayMedium, FontWeight.w600),
      displaySmall: heading(base.displaySmall, FontWeight.w600),
      headlineLarge: heading(base.headlineLarge, FontWeight.w600),
      headlineMedium: heading(base.headlineMedium, FontWeight.w600),
      headlineSmall: heading(base.headlineSmall, FontWeight.w600),
      titleLarge: heading(base.titleLarge, FontWeight.w600),
      titleMedium: heading(base.titleMedium, FontWeight.w500),
      titleSmall: heading(base.titleSmall, FontWeight.w500),
    );
  }
}
