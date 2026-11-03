import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/constants/api_constants.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/validators.dart';
import '../../providers/auth_provider.dart';
import '../../providers/data_providers.dart';
import '../../widgets/brand_header.dart';
import 'course_info_fields.dart';

/// "Kursumu kaydet" (kurs müdürü): telefon doğrulama → kurs ve müdür bilgileri.
/// Başvuru kursu "onay bekliyor" açar ve oturum başlatır; yönlendirme başvuru durumu ekranına yapılır.
class ApplyScreen extends ConsumerStatefulWidget {
  const ApplyScreen({super.key});

  @override
  ConsumerState<ApplyScreen> createState() => _ApplyScreenState();
}

class _ApplyScreenState extends ConsumerState<ApplyScreen> {
  final _phone = TextEditingController();
  final _code = TextEditingController();
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _course = CourseInfoControllers();

  bool _codeSent = false;
  String? _phoneNormalized;
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    for (final c in [_phone, _code, _firstName, _lastName]) {
      c.dispose();
    }
    _course.dispose();
    super.dispose();
  }

  Future<void> _sendCode() async {
    final phone = normalizePhone(_phone.text);
    if (phone == null) {
      setState(() => _error = 'Cep telefonu numaranızı 05XX XXX XX XX şeklinde girin.');
      return;
    }
    await _run(() async {
      await ref.read(courseApplicationRepositoryProvider).sendCode(phone);
      _phoneNormalized = phone;
      _codeSent = true;
    });
  }

  Future<void> _submit() async {
    String? problem;
    if (!isValidCode(_code.text.trim())) {
      problem = 'Telefonunuza gelen 6 haneli kodu girin.';
    } else if (_firstName.text.trim().isEmpty || _lastName.text.trim().isEmpty) {
      problem = 'Adınızı ve soyadınızı yazın.';
    } else {
      problem = _course.validate();
    }
    if (problem != null) {
      setState(() => _error = problem);
      return;
    }
    await _run(() async {
      final session = await ref.read(courseApplicationRepositoryProvider).submit(
            phone: _phoneNormalized!,
            code: _code.text.trim(),
            info: _course.toInfo(),
            firstName: _firstName.text.trim(),
            lastName: _lastName.text.trim(),
          );
      // Kurs "onay bekliyor" açıldı; yönlendirici başvuru durumu ekranına götürür
      await ref.read(authProvider.notifier).signIn(session);
    });
  }

  Future<void> _run(Future<void> Function() action) async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await action();
    } on ApiException catch (e) {
      _error = e.message;
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Kursumu kaydet')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.xl),
          children: _codeSent ? _formStep() : _phoneStep(),
        ),
      ),
    );
  }

  List<Widget> _phoneStep() => [
        const BrandHeader(subtitle: 'Kursunuzu kaydedin; platform yöneticisi onaylayınca hocalarınızı davet edebilirsiniz.'),
        const SizedBox(height: 28),
        TextField(
          controller: _phone,
          keyboardType: TextInputType.phone,
          autofillHints: const [AutofillHints.telephoneNumber],
          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9 +]')), LengthLimitingTextInputFormatter(17)],
          textInputAction: TextInputAction.send,
          onSubmitted: (_) => _sendCode(),
          style: const TextStyle(fontSize: 20, letterSpacing: 1),
          decoration: InputDecoration(
            labelText: 'Cep telefonunuz (kurs müdürü)',
            hintText: '05XX XXX XX XX',
            errorText: _error,
            errorMaxLines: 3,
          ),
        ),
        const SizedBox(height: AppSpacing.lg),
        FilledButton(onPressed: _busy ? null : _sendCode, child: Text(_busy ? 'Gönderiliyor...' : 'Doğrulama kodu gönder')),
      ];

  List<Widget> _formStep() => [
        Text('${formatPhone(_phoneNormalized!)} numarasına SMS ile gelen kodu ve kurs bilgilerinizi girin.',
            style: const TextStyle(color: AppColors.muted, fontSize: 15)),
        const SizedBox(height: AppSpacing.lg),
        TextField(
          controller: _code,
          keyboardType: TextInputType.number,
          maxLength: 6,
          autofillHints: const [AutofillHints.oneTimeCode],
          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
          style: const TextStyle(fontSize: 24, letterSpacing: 8, fontWeight: FontWeight.w700),
          decoration: const InputDecoration(labelText: 'Doğrulama kodu', counterText: ''),
        ),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton(
            onPressed: _busy ? null : _sendCode,
            style: TextButton.styleFrom(minimumSize: const Size(0, 40)),
            child: const Text('Kodu tekrar gönder'),
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        const _Heading('Kurs'),
        CourseInfoFields(controllers: _course),
        const SizedBox(height: AppSpacing.lg),
        const _Heading('Kurs müdürü (siz)'),
        Row(
          children: [
            Expanded(
              child: TextField(
                controller: _firstName,
                textCapitalization: TextCapitalization.words,
                decoration: const InputDecoration(labelText: 'Ad'),
              ),
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: TextField(
                controller: _lastName,
                textCapitalization: TextCapitalization.words,
                decoration: const InputDecoration(labelText: 'Soyad'),
              ),
            ),
          ],
        ),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.only(top: AppSpacing.md),
            child: Text(_error!, style: const TextStyle(color: AppColors.red)),
          ),
        const SizedBox(height: AppSpacing.lg),
        FilledButton(onPressed: _busy ? null : _submit, child: Text(_busy ? 'Gönderiliyor...' : 'Başvuruyu gönder')),
        TextButton(
          onPressed: _busy ? null : () => setState(() {
            _codeSent = false;
            _error = null;
          }),
          child: const Text('Numarayı değiştir'),
        ),
        if (ApiConstants.showTestHints)
          const Text('Test sunucusu: kod 123456.',
              textAlign: TextAlign.center, style: TextStyle(fontSize: 12, color: AppColors.muted)),
      ];
}

class _Heading extends StatelessWidget {
  const _Heading(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: AppSpacing.sm),
        child: Text(text, style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w800, color: AppColors.green)),
      );
}
