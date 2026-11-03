import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/constants/api_constants.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/validators.dart';
import '../../../data/models/invitation_model.dart';
import '../../providers/data_providers.dart';

/// Davet rolleri ekranda böyle görünür.
const invitationRoleLabels = {'head_teacher': 'Hoca', 'assistant_teacher': 'Yardımcı hoca'};

enum _Step { loading, invalid, form, code, done }

/// Davet bağlantısıyla (…/katil/KOD) hoca veya yardımcı hoca olarak katılma.
/// Bilgiler → SMS kodu → istek kurs yöneticisinin onayına düşer; onaylanınca telefonla giriş yapılır.
class JoinScreen extends ConsumerStatefulWidget {
  const JoinScreen({super.key, required this.code});

  final String code;

  @override
  ConsumerState<JoinScreen> createState() => _JoinScreenState();
}

class _JoinScreenState extends ConsumerState<JoinScreen> {
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _phone = TextEditingController();
  final _email = TextEditingController();
  final _otp = TextEditingController();

  _Step _step = _Step.loading;
  InvitationPublicModel? _invitation;
  String? _phoneNormalized;
  bool _busy = false;
  String? _error;

  String get _code => normalizeInvitationCode(widget.code);

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    for (final c in [_firstName, _lastName, _phone, _email, _otp]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _step = _Step.loading;
      _error = null;
    });
    try {
      final invitation = await ref.read(invitationRepositoryProvider).getPublic(_code);
      if (!mounted) return;
      setState(() {
        _invitation = invitation;
        _step = _Step.form;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _step = _Step.invalid;
      });
    }
  }

  Future<void> _sendCode() async {
    final phone = normalizePhone(_phone.text);
    if (_firstName.text.trim().isEmpty || _lastName.text.trim().isEmpty) {
      setState(() => _error = 'Adınızı ve soyadınızı yazın.');
      return;
    }
    if (phone == null) {
      setState(() => _error = 'Cep telefonu numaranızı 05XX XXX XX XX şeklinde girin.');
      return;
    }
    final email = _email.text.trim();
    if (email.isNotEmpty && !isValidEmail(email)) {
      setState(() => _error = 'E-posta adresi geçersiz; boş da bırakabilirsiniz.');
      return;
    }
    await _run(() async {
      await ref.read(invitationRepositoryProvider).sendJoinCode(_code, phone);
      _phoneNormalized = phone;
      _otp.clear();
      _step = _Step.code;
    });
  }

  Future<void> _join() async {
    final otp = _otp.text.trim();
    if (!isValidCode(otp)) {
      setState(() => _error = '6 haneli kodu eksiksiz girin.');
      return;
    }
    final email = _email.text.trim();
    await _run(() async {
      await ref.read(invitationRepositoryProvider).join(
            _code,
            firstName: _firstName.text.trim(),
            lastName: _lastName.text.trim(),
            phone: _phoneNormalized!,
            email: email.isEmpty ? null : email,
            otp: otp,
          );
      _step = _Step.done;
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
      appBar: AppBar(
        title: const Text('Kursa katıl'),
        leading: IconButton(
          icon: const Icon(Icons.close),
          tooltip: 'Kapat',
          onPressed: () => context.canPop() ? context.pop() : context.go('/login'),
        ),
      ),
      body: SafeArea(
        child: switch (_step) {
          _Step.loading => const Center(child: CircularProgressIndicator()),
          _Step.invalid => _InvalidView(message: _error ?? '', onRetry: _load),
          _Step.done => _DoneView(phone: _phoneNormalized!),
          _ => ListView(
              padding: const EdgeInsets.all(AppSpacing.xl),
              children: [
                _InviteCard(invitation: _invitation!),
                const SizedBox(height: AppSpacing.xl),
                ...(_step == _Step.form ? _formFields() : _codeFields()),
              ],
            ),
        },
      ),
    );
  }

  List<Widget> _formFields() => [
        const Text('Bilgilerinizi yazın; telefonunuza bir doğrulama kodu göndereceğiz.',
            style: TextStyle(color: AppColors.muted, fontSize: 15)),
        const SizedBox(height: AppSpacing.lg),
        TextField(
          controller: _firstName,
          textCapitalization: TextCapitalization.words,
          textInputAction: TextInputAction.next,
          autofillHints: const [AutofillHints.givenName],
          decoration: const InputDecoration(labelText: 'Ad'),
        ),
        const SizedBox(height: AppSpacing.md),
        TextField(
          controller: _lastName,
          textCapitalization: TextCapitalization.words,
          textInputAction: TextInputAction.next,
          autofillHints: const [AutofillHints.familyName],
          decoration: const InputDecoration(labelText: 'Soyad'),
        ),
        const SizedBox(height: AppSpacing.md),
        TextField(
          controller: _phone,
          keyboardType: TextInputType.phone,
          autofillHints: const [AutofillHints.telephoneNumber],
          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9 +]')), LengthLimitingTextInputFormatter(17)],
          textInputAction: TextInputAction.next,
          style: const TextStyle(fontSize: 20, letterSpacing: 1),
          decoration: const InputDecoration(
            labelText: 'Cep telefonu',
            hintText: '05XX XXX XX XX',
            helperText: 'Uygulamaya bu numarayla giriş yapacaksınız.',
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        TextField(
          controller: _email,
          keyboardType: TextInputType.emailAddress,
          autocorrect: false,
          autofillHints: const [AutofillHints.email],
          textInputAction: TextInputAction.done,
          onSubmitted: (_) => _sendCode(),
          decoration: const InputDecoration(labelText: 'E-posta (isteğe bağlı)'),
        ),
        if (_error != null) _ErrorText(_error!),
        const SizedBox(height: AppSpacing.lg),
        FilledButton(
          onPressed: _busy ? null : _sendCode,
          child: Text(_busy ? 'Gönderiliyor...' : 'Doğrulama kodu gönder'),
        ),
      ];

  List<Widget> _codeFields() => [
        Text('${formatPhone(_phoneNormalized!)} numarasına SMS ile gelen 6 haneli kodu girin.',
            style: const TextStyle(color: AppColors.muted, fontSize: 15)),
        const SizedBox(height: AppSpacing.lg),
        TextField(
          controller: _otp,
          autofocus: true,
          keyboardType: TextInputType.number,
          textAlign: TextAlign.center,
          maxLength: 6,
          autofillHints: const [AutofillHints.oneTimeCode],
          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
          style: const TextStyle(fontSize: 28, letterSpacing: 10, fontWeight: FontWeight.w700),
          decoration: const InputDecoration(counterText: '', hintText: '••••••', labelText: 'Doğrulama kodu'),
          onChanged: (value) {
            if (value.length == 6 && !_busy) _join();
          },
        ),
        if (_error != null) _ErrorText(_error!),
        const SizedBox(height: AppSpacing.lg),
        FilledButton(onPressed: _busy ? null : _join, child: Text(_busy ? 'Gönderiliyor...' : 'Katılım isteği gönder')),
        const SizedBox(height: AppSpacing.sm),
        TextButton(onPressed: _busy ? null : _sendCode, child: const Text('Kodu tekrar gönder')),
        TextButton(
          onPressed: _busy ? null : () => setState(() {
            _step = _Step.form;
            _error = null;
          }),
          child: const Text('Bilgileri değiştir'),
        ),
        if (ApiConstants.showTestHints)
          const Text('Test sunucusu: kod 123456.',
              textAlign: TextAlign.center, style: TextStyle(fontSize: 12, color: AppColors.muted)),
      ];
}

class _InviteCard extends StatelessWidget {
  const _InviteCard({required this.invitation});

  final InvitationPublicModel invitation;

  @override
  Widget build(BuildContext context) {
    final role = invitationRoleLabels[invitation.role] ?? invitation.role;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: AppSpacing.xl),
      decoration: BoxDecoration(
        gradient: const LinearGradient(colors: [AppColors.green, Color(0xFF2E7D55)]),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        children: [
          const Text('DAVETLİSİNİZ',
              style: TextStyle(color: Colors.white70, fontSize: 12, fontWeight: FontWeight.w700, letterSpacing: 1.2)),
          const SizedBox(height: 6),
          Text(invitation.courseName,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.w800)),
          if (invitation.place.isNotEmpty) ...[
            const SizedBox(height: 2),
            Text(invitation.place, style: const TextStyle(color: Colors.white70)),
          ],
          const SizedBox(height: AppSpacing.md),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
            decoration: BoxDecoration(color: Colors.white24, borderRadius: BorderRadius.circular(999)),
            child: Text('$role olarak katılın',
                style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w700, fontSize: 15)),
          ),
        ],
      ),
    );
  }
}

class _InvalidView extends StatelessWidget {
  const _InvalidView({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(AppSpacing.xxl),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Icon(Icons.link_off, size: 48, color: AppColors.muted),
          const SizedBox(height: AppSpacing.md),
          const Text('Davet açılamadı',
              textAlign: TextAlign.center, style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800)),
          const SizedBox(height: AppSpacing.sm),
          Text(message, textAlign: TextAlign.center, style: const TextStyle(color: AppColors.muted, fontSize: 15)),
          const SizedBox(height: AppSpacing.xl),
          OutlinedButton.icon(onPressed: onRetry, icon: const Icon(Icons.refresh), label: const Text('Tekrar dene')),
          const SizedBox(height: AppSpacing.sm),
          TextButton(onPressed: () => context.go('/login'), child: const Text('Kursa kayıtlıysanız giriş yapın')),
        ],
      ),
    );
  }
}

class _DoneView extends StatelessWidget {
  const _DoneView({required this.phone});

  final String phone;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(AppSpacing.xxl),
      children: [
        const Icon(Icons.check_circle, size: 64, color: AppColors.green),
        const SizedBox(height: AppSpacing.md),
        const Text('İsteğiniz gönderildi',
            textAlign: TextAlign.center, style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
        const SizedBox(height: AppSpacing.md),
        Text(
          'Kurs yöneticiniz onaylayınca ${formatPhone(phone)} numaranızla giriş yapabilirsiniz.',
          textAlign: TextAlign.center,
          style: const TextStyle(color: AppColors.muted, fontSize: 16),
        ),
        const SizedBox(height: AppSpacing.xl),
        FilledButton(onPressed: () => context.go('/login'), child: const Text('Giriş ekranına git')),
      ],
    );
  }
}

class _ErrorText extends StatelessWidget {
  const _ErrorText(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(top: AppSpacing.md),
        child: Text(text, style: const TextStyle(color: AppColors.red, fontSize: 14)),
      );
}
