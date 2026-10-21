import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/constants/app_colors.dart';
import '../../../core/constants/app_spacing.dart';
import '../../../core/constants/labels.dart';
import '../../../core/network/api_exception.dart';
import '../../../core/utils/validators.dart';
import '../../../data/models/session_model.dart';
import '../../providers/auth_provider.dart';
import '../../widgets/brand_header.dart';

/// 2. adım: e-postaya gelen 6 haneli kod; adres birden fazla kursta kayıtlıysa kurs seçimi.
class CodeScreen extends ConsumerStatefulWidget {
  const CodeScreen({super.key, required this.email});

  final String email;

  @override
  ConsumerState<CodeScreen> createState() => _CodeScreenState();
}

class _CodeScreenState extends ConsumerState<CodeScreen> {
  final _codeController = TextEditingController();
  bool _busy = false;
  String? _error;
  List<TenantOption>? _tenantOptions;

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _verify({String? tenantId}) async {
    final code = _codeController.text.trim();
    if (!isValidCode(code)) {
      setState(() => _error = '6 haneli kodu eksiksiz girin.');
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final result = await ref
          .read(authRepositoryProvider)
          .verifyEmailCode(email: widget.email, code: code, tenantId: tenantId);
      if (result.requiresTenantSelection) {
        setState(() => _tenantOptions = result.tenantOptions);
        return;
      }
      // Yönlendirme oturum durumuna göre router'da yapılır
      await ref.read(authProvider.notifier).signIn(result.session!);
    } on ApiException catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(backgroundColor: Colors.transparent, foregroundColor: AppColors.text),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppSpacing.xxl),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: _tenantOptions == null ? _buildCodeStep() : _buildTenantStep(_tenantOptions!),
          ),
        ),
      ),
    );
  }

  List<Widget> _buildCodeStep() => [
        BrandHeader(subtitle: '${widget.email} adresine gönderilen 6 haneli kodu girin'),
        const SizedBox(height: 32),
        TextField(
          controller: _codeController,
          autofocus: true,
          keyboardType: TextInputType.number,
          textAlign: TextAlign.center,
          maxLength: 6,
          autofillHints: const [AutofillHints.oneTimeCode],
          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
          style: const TextStyle(fontSize: 28, letterSpacing: 10, fontWeight: FontWeight.w700),
          decoration: InputDecoration(counterText: '', hintText: '••••••', errorText: _error),
          onChanged: (value) {
            if (value.length == 6 && !_busy) _verify();
          },
        ),
        const SizedBox(height: AppSpacing.lg),
        FilledButton(onPressed: _busy ? null : _verify, child: Text(_busy ? 'Doğrulanıyor...' : 'Giriş yap')),
      ];

  List<Widget> _buildTenantStep(List<TenantOption> options) => [
        const BrandHeader(subtitle: 'Bu adres birden fazla kursta kayıtlı. Giriş yapacağınız kursu seçin.'),
        const SizedBox(height: 24),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.md),
            child: Text(_error!, style: const TextStyle(color: AppColors.red)),
          ),
        for (final option in options)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.sm),
            child: Card(
              child: ListTile(
                minVerticalPadding: AppSpacing.md,
                title: Text(option.tenantName, style: const TextStyle(fontWeight: FontWeight.w700)),
                subtitle: Text(RoleLabels.labels[option.role] ?? option.role),
                trailing: const Icon(Icons.chevron_right),
                enabled: !_busy,
                onTap: () => _verify(tenantId: option.tenantId),
              ),
            ),
          ),
      ];
}
