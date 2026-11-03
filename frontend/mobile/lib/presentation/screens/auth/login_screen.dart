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
import '../../../data/models/session_model.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/brand_header.dart';

/// 1. adım: kayıtlı cep telefonuna (SMS) veya e-posta adresine giriş kodu istenir.
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key, this.notice});

  /// Oturum düştüğünde vb. gösterilecek bilgi.
  final String? notice;

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _phoneController = TextEditingController();
  final _emailController = TextEditingController();
  bool _byPhone = true;
  bool _sending = false;
  String? _error;

  @override
  void dispose() {
    _phoneController.dispose();
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _sendCode() async {
    final LoginTarget target;
    if (_byPhone) {
      final phone = normalizePhone(_phoneController.text);
      if (phone == null) {
        setState(() => _error = 'Cep telefonu numaranızı 05XX XXX XX XX şeklinde girin.');
        return;
      }
      target = LoginTarget.phone(phone);
    } else {
      final email = _emailController.text.trim();
      if (!isValidEmail(email)) {
        setState(() => _error = 'Geçerli bir e-posta adresi girin.');
        return;
      }
      target = LoginTarget.email(email);
    }

    setState(() {
      _sending = true;
      _error = null;
    });
    try {
      await ref.read(authRepositoryProvider).sendCode(target);
      if (mounted) context.push('/login/code', extra: target);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }

  /// Bağlantı yerine kod paylaşıldıysa (ör. telefonda okunarak): …/katil/KOD ekranına gider.
  Future<void> _enterInvitationCode() async {
    final controller = TextEditingController();
    final code = await showDialog<String>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Davet kodu'),
        content: TextField(
          controller: controller,
          autofocus: true,
          textCapitalization: TextCapitalization.characters,
          decoration: const InputDecoration(hintText: 'Ör. AB7K2M9X', helperText: 'Kurs yöneticinizin gönderdiği 8 karakter'),
          onSubmitted: (v) => Navigator.pop(c, v),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c), child: const Text('Vazgeç')),
          TextButton(onPressed: () => Navigator.pop(c, controller.text), child: const Text('Devam')),
        ],
      ),
    );
    controller.dispose();
    final normalized = normalizeInvitationCode(_extractCode(code ?? ''));
    if (normalized.isNotEmpty && mounted) context.push('/katil/$normalized');
  }

  /// Bağlantının tamamı yapıştırıldıysa sondaki kodu alır.
  static String _extractCode(String value) {
    final match = RegExp(r'/katil/([A-Za-z0-9-]+)').firstMatch(value);
    return match?.group(1) ?? value;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(AppSpacing.xxl),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                BrandHeader(
                  subtitle: _byPhone ? 'Kayıtlı cep telefonunuzla giriş yapın' : 'Kayıtlı e-posta adresinizle giriş yapın',
                ),
                const SizedBox(height: 32),
                if (widget.notice != null) ...[
                  _Notice(text: widget.notice!),
                  const SizedBox(height: AppSpacing.lg),
                ],
                SegmentedButton<bool>(
                  segments: const [
                    ButtonSegment(value: true, label: Text('Telefon'), icon: Icon(Icons.phone_iphone)),
                    ButtonSegment(value: false, label: Text('E-posta'), icon: Icon(Icons.mail_outline)),
                  ],
                  selected: {_byPhone},
                  showSelectedIcon: false,
                  onSelectionChanged: _sending
                      ? null
                      : (value) => setState(() {
                            _byPhone = value.first;
                            _error = null;
                          }),
                ),
                const SizedBox(height: AppSpacing.lg),
                if (_byPhone)
                  TextField(
                    key: const ValueKey('phone'),
                    controller: _phoneController,
                    keyboardType: TextInputType.phone,
                    autofillHints: const [AutofillHints.telephoneNumber],
                    inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9 +]')), LengthLimitingTextInputFormatter(17)],
                    textInputAction: TextInputAction.send,
                    onSubmitted: (_) => _sendCode(),
                    style: const TextStyle(fontSize: 20, letterSpacing: 1),
                    decoration: InputDecoration(labelText: 'Cep telefonu', hintText: '05XX XXX XX XX', errorText: _error),
                  )
                else
                  TextField(
                    key: const ValueKey('email'),
                    controller: _emailController,
                    keyboardType: TextInputType.emailAddress,
                    autocorrect: false,
                    autofillHints: const [AutofillHints.email],
                    textInputAction: TextInputAction.send,
                    onSubmitted: (_) => _sendCode(),
                    decoration: InputDecoration(labelText: 'E-posta', hintText: 'ornek@kurs.com', errorText: _error),
                  ),
                const SizedBox(height: AppSpacing.lg),
                FilledButton(
                  onPressed: _sending ? null : _sendCode,
                  child: Text(_sending ? 'Gönderiliyor...' : 'Giriş kodu gönder'),
                ),
                const SizedBox(height: AppSpacing.xl),
                const Divider(),
                const SizedBox(height: AppSpacing.sm),
                OutlinedButton.icon(
                  onPressed: _sending ? null : () => context.push('/login/apply'),
                  icon: const Icon(Icons.add_business),
                  label: const Text('Kursumu kaydet (kurs müdürü)'),
                ),
                const SizedBox(height: AppSpacing.sm),
                TextButton.icon(
                  onPressed: _sending ? null : _enterInvitationCode,
                  icon: const Icon(Icons.qr_code_2),
                  label: const Text('Davet kodum var (yeni hoca)'),
                ),
                if (ApiConstants.showTestHints) ...[
                  const SizedBox(height: AppSpacing.lg),
                  const Text(
                    'Test sunucusu: giriş kodu 123456.',
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 12, color: AppColors.muted),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Notice extends StatelessWidget {
  const _Notice({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(color: AppColors.goldPale, borderRadius: BorderRadius.circular(AppSpacing.radiusMd)),
      child: Text(text, style: const TextStyle(color: Color(0xFF7A5A0C))),
    );
  }
}
