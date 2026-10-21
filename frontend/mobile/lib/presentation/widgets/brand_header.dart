import 'package:flutter/material.dart';

import '../../core/constants/app_colors.dart';

/// Giriş ekranlarının üstündeki logo ve başlık.
class BrandHeader extends StatelessWidget {
  const BrandHeader({super.key, required this.subtitle});

  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Container(
          width: 72,
          height: 72,
          decoration: BoxDecoration(color: AppColors.green, borderRadius: BorderRadius.circular(20)),
          alignment: Alignment.center,
          child: const Text('☪', style: TextStyle(fontSize: 34, color: Colors.white)),
        ),
        const SizedBox(height: 14),
        const Text(
          'Hafız Takip',
          style: TextStyle(fontSize: 26, fontWeight: FontWeight.w800, color: AppColors.green),
        ),
        const SizedBox(height: 4),
        Text(subtitle, textAlign: TextAlign.center, style: const TextStyle(color: AppColors.muted)),
      ],
    );
  }
}
