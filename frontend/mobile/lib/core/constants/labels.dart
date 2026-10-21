import 'package:flutter/material.dart';

import 'app_colors.dart';

/// Backend kodları ↔ ekranda görünen Türkçe adlar (backend: HafizTakip.Core/Enums).
class PerformanceLevel {
  const PerformanceLevel(this.code, this.label, this.color);

  final String code;
  final String label;
  final Color color;

  static const all = [
    PerformanceLevel('mukemmel', 'Mükemmel', AppColors.green),
    PerformanceLevel('cok_iyi', 'Çok iyi', AppColors.blue),
    PerformanceLevel('iyi', 'İyi', AppColors.purple),
    PerformanceLevel('orta', 'Orta', AppColors.gold),
    PerformanceLevel('zayif', 'Zayıf', AppColors.red),
  ];

  static PerformanceLevel? of(String code) {
    for (final level in all) {
      if (level.code == code) return level;
    }
    return null;
  }
}

class HomeworkStatus {
  HomeworkStatus._();

  static const labels = {
    'yapildi': 'Yapıldı',
    'eksik': 'Eksik',
    'yapilmadi': 'Yapılmadı',
    'mazeretli': 'Mazeretli',
  };
}

class AttendanceStatus {
  const AttendanceStatus(this.code, this.label, this.color, this.pale);

  final String code;
  final String label;
  final Color color;
  final Color pale;

  static const present = AttendanceStatus('present', 'Geldi', AppColors.green, AppColors.greenPale);
  static const absent = AttendanceStatus('absent', 'Gelmedi', AppColors.red, AppColors.redPale);
  static const late = AttendanceStatus('late', 'Geç', AppColors.gold, AppColors.goldPale);
  static const excused = AttendanceStatus('excused', 'İzinli', AppColors.blue, AppColors.bluePale);

  /// Dokundukça bu sırayla değişir.
  static const cycle = [present, absent, late, excused];

  static AttendanceStatus of(String code) =>
      cycle.firstWhere((s) => s.code == code, orElse: () => present);

  AttendanceStatus get next => cycle[(cycle.indexOf(this) + 1) % cycle.length];
}

class RoleLabels {
  RoleLabels._();

  static const labels = {
    'tenant_admin': 'Kurs müdürü',
    'head_teacher': 'Başhoca',
    'assistant_teacher': 'Yardımcı hoca',
    'parent': 'Veli',
    'student': 'Öğrenci',
  };

  /// Mobil uygulama şimdilik kurs personeli içindir.
  static const staffRoles = {'tenant_admin', 'head_teacher', 'assistant_teacher'};
}
