import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/constants/api_constants.dart';
import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/validators.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/brand_header.dart';

/// 1. adım: kayıtlı e-posta adresine giriş kodu istenir.
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key, this.notice});

  /// Oturum düştüğünde vb. gösterilecek bilgi.
  final String? notice;

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _emailController = TextEditingController();
  bool _sending = false;
  String? _error;

  @override
  void dispose() {
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _sendCode() async {
    final email = _emailController.text.trim();
    if (!isValidEmail(email)) {
      setState(() => _error = 'Geçerli bir e-posta adresi girin.');
      return;
    }

    setState(() {
      _sending = true;
      _error = null;
    });
    try {
      await ref.read(authRepositoryProvider).sendEmailCode(email);
      if (mounted) context.push('/login/code', extra: email);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _sending = false);
    }
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
                const BrandHeader(subtitle: 'Kayıtlı e-posta adresinizle giriş yapın'),
                const SizedBox(height: 32),
                if (widget.notice != null) ...[
                  _Notice(text: widget.notice!),
                  const SizedBox(height: AppSpacing.lg),
                ],
                TextField(
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
                if (ApiConstants.showTestHints) ...[
                  const SizedBox(height: AppSpacing.lg),
                  const Text(
                    'Test sunucusu: e-posta gönderilmez, giriş kodu 123456.',
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
