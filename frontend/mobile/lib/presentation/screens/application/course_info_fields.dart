import 'package:flutter/material.dart';

import '../../../core/constants/app_spacing.dart';
import '../../../core/utils/validators.dart';
import '../../../data/models/course_application_model.dart';

/// Kurs bilgileri alanları (başvuru ve reddedilen başvuruyu düzeltme).
class CourseInfoControllers {
  CourseInfoControllers({CourseApplicationStatus? initial})
      : courseName = TextEditingController(text: initial?.courseName),
        city = TextEditingController(text: initial?.city),
        district = TextEditingController(text: initial?.district),
        address = TextEditingController(text: initial?.address),
        email = TextEditingController(text: initial?.contactEmail);

  final TextEditingController courseName;
  final TextEditingController city;
  final TextEditingController district;
  final TextEditingController address;
  final TextEditingController email;

  /// Eksik/hatalı alan varsa mesajı, yoksa null.
  String? validate() {
    if (courseName.text.trim().length < 3) return 'Kurs adını yazın.';
    if (city.text.trim().isEmpty || district.text.trim().isEmpty) return 'İl ve ilçeyi yazın.';
    final mail = email.text.trim();
    if (mail.isNotEmpty && !isValidEmail(mail)) return 'E-posta adresi geçersiz; boş da bırakabilirsiniz.';
    return null;
  }

  CourseInfo toInfo() {
    String? clean(TextEditingController c) => c.text.trim().isEmpty ? null : c.text.trim();
    return CourseInfo(
      courseName: courseName.text.trim(),
      city: city.text.trim(),
      district: district.text.trim(),
      address: clean(address),
      contactEmail: clean(email),
    );
  }

  void dispose() {
    for (final c in [courseName, city, district, address, email]) {
      c.dispose();
    }
  }
}

class CourseInfoFields extends StatelessWidget {
  const CourseInfoFields({super.key, required this.controllers});

  final CourseInfoControllers controllers;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextField(
          controller: controllers.courseName,
          textCapitalization: TextCapitalization.words,
          textInputAction: TextInputAction.next,
          decoration: const InputDecoration(labelText: 'Kurs adı', hintText: "Ör. Merkez Kur'an Kursu"),
        ),
        const SizedBox(height: AppSpacing.md),
        Row(
          children: [
            Expanded(
              child: TextField(
                controller: controllers.city,
                textCapitalization: TextCapitalization.words,
                textInputAction: TextInputAction.next,
                decoration: const InputDecoration(labelText: 'İl'),
              ),
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: TextField(
                controller: controllers.district,
                textCapitalization: TextCapitalization.words,
                textInputAction: TextInputAction.next,
                decoration: const InputDecoration(labelText: 'İlçe'),
              ),
            ),
          ],
        ),
        const SizedBox(height: AppSpacing.md),
        TextField(
          controller: controllers.address,
          textCapitalization: TextCapitalization.sentences,
          textInputAction: TextInputAction.next,
          decoration: const InputDecoration(labelText: 'Adres (isteğe bağlı)'),
        ),
        const SizedBox(height: AppSpacing.md),
        TextField(
          controller: controllers.email,
          keyboardType: TextInputType.emailAddress,
          autocorrect: false,
          textInputAction: TextInputAction.next,
          decoration: const InputDecoration(
            labelText: 'E-posta (isteğe bağlı)',
            helperText: 'Onay sonucu bu adrese gelir; e-postayla da giriş yapabilirsiniz.',
          ),
        ),
      ],
    );
  }
}
