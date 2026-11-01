import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/constants/app_colors.dart';
import '../../providers/onboarding_provider.dart';

class _Slide {
  const _Slide({required this.eyebrow, required this.title, required this.text, required this.icon, required this.badge});

  final String eyebrow;
  final String title;
  final String text;
  final IconData icon;
  final IconData badge;
}

const _slides = [
  _Slide(
    eyebrow: 'HOŞ GELDİNİZ',
    title: "Hafız Takip'e hoş geldiniz",
    text: 'Kursunuzun ders ve yoklama kayıtları artık cebinizde.',
    icon: Icons.menu_book_rounded,
    badge: Icons.star_rounded,
  ),
  _Slide(
    eyebrow: 'DERS KAYDI',
    title: 'Dersi üç dokunuşta kaydedin',
    text: 'Öğrenciye dokunun, sayfa hazır gelir. Performansı seçip kaydedin; sıradaki öğrenci kendiliğinden açılır.',
    icon: Icons.touch_app_rounded,
    badge: Icons.check_rounded,
  ),
  _Slide(
    eyebrow: 'YOKLAMA',
    title: 'Yoklama tek dokunuş',
    text: 'Herkes "Geldi" olarak başlar; dokundukça değişir. Aynı gün yeniden açıp düzeltebilirsiniz.',
    icon: Icons.fact_check_rounded,
    badge: Icons.groups_rounded,
  ),
  _Slide(
    eyebrow: 'HAZIRSINIZ',
    title: 'Hemen başlayalım',
    text: 'Kursa kayıtlı e-posta adresinizle giriş yapın; 6 haneli kod e-postanıza gelir.',
    icon: Icons.mark_email_read_rounded,
    badge: Icons.lock_open_rounded,
  ),
];

/// İlk açılış tanıtımı: koyu zümrüt zemin, dönen yıldız deseni, süzülen simge, altın vurgular.
/// Sistemde "animasyonları kaldır" açıksa hareket yoktur.
class OnboardingScreen extends ConsumerStatefulWidget {
  const OnboardingScreen({super.key});

  @override
  ConsumerState<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends ConsumerState<OnboardingScreen> with TickerProviderStateMixin {
  final _pageController = PageController();
  late final AnimationController _spin = AnimationController(vsync: this, duration: const Duration(seconds: 90));
  late final AnimationController _float = AnimationController(vsync: this, duration: const Duration(milliseconds: 2600));
  int _index = 0;

  bool get _isLast => _index == _slides.length - 1;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (MediaQuery.of(context).disableAnimations) {
      _spin.stop();
      _float.stop();
    } else {
      if (!_spin.isAnimating) _spin.repeat();
      if (!_float.isAnimating) _float.repeat(reverse: true);
    }
  }

  @override
  void dispose() {
    _pageController.dispose();
    _spin.dispose();
    _float.dispose();
    super.dispose();
  }

  void _next() {
    if (_isLast) {
      ref.read(onboardingProvider.notifier).complete();
      return;
    }
    final reduce = MediaQuery.of(context).disableAnimations;
    _pageController.nextPage(
      duration: reduce ? const Duration(milliseconds: 1) : const Duration(milliseconds: 450),
      curve: Curves.easeOutCubic,
    );
  }

  @override
  Widget build(BuildContext context) {
    final slide = _slides[_index];
    return Scaffold(
      backgroundColor: AppColors.card,
      body: Column(
        children: [
          Expanded(flex: 11, child: _buildHero(slide)),
          Expanded(flex: 10, child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildHero(_Slide slide) {
    return Container(
      clipBehavior: Clip.hardEdge, // dönen desen bu alanın dışına taşmasın
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF0F3D27), AppColors.green, AppColors.greenLight],
        ),
        border: Border(bottom: BorderSide(color: AppColors.goldLight, width: 3)),
      ),
      child: Stack(
        alignment: Alignment.center,
        children: [
          Positioned.fill(
            child: AnimatedBuilder(
              animation: _spin,
              builder: (_, _) => Transform.rotate(
                angle: _spin.value * 2 * math.pi,
                child: Transform.scale(scale: 1.8, child: const CustomPaint(painter: _StarPatternPainter())),
              ),
            ),
          ),
          SafeArea(
            child: Align(
              alignment: Alignment.topRight,
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: TextButton(
                  onPressed: () => ref.read(onboardingProvider.notifier).complete(),
                  style: TextButton.styleFrom(
                    foregroundColor: AppColors.goldPale,
                    minimumSize: const Size(64, 44),
                    shape: StadiumBorder(side: BorderSide(color: AppColors.goldPale.withValues(alpha: 0.4))),
                  ),
                  child: Text(_isLast ? 'Kapat' : 'Geç', style: const TextStyle(fontWeight: FontWeight.w700)),
                ),
              ),
            ),
          ),
          AnimatedBuilder(
            animation: _float,
            builder: (_, child) => Transform.translate(
              offset: Offset(0, -10 * Curves.easeInOut.transform(_float.value)),
              child: child,
            ),
            child: AnimatedSwitcher(
              duration: const Duration(milliseconds: 450),
              transitionBuilder: (child, animation) => ScaleTransition(
                scale: Tween(begin: 0.7, end: 1.0).animate(CurvedAnimation(parent: animation, curve: Curves.easeOutBack)),
                child: FadeTransition(opacity: animation, child: child),
              ),
              child: _Emblem(key: ValueKey(_index), icon: slide.icon, badge: slide.badge),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    return SafeArea(
      top: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 20, 24, 16),
        child: Column(
          children: [
            Expanded(
              child: PageView.builder(
                controller: _pageController,
                itemCount: _slides.length,
                onPageChanged: (i) => setState(() => _index = i),
                itemBuilder: (_, i) => _SlideText(slide: _slides[i]),
              ),
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                for (var i = 0; i < _slides.length; i++)
                  AnimatedContainer(
                    duration: const Duration(milliseconds: 350),
                    margin: const EdgeInsets.symmetric(horizontal: 4),
                    width: i == _index ? 30 : 9,
                    height: 9,
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(9),
                      color: i == _index ? null : (i < _index ? AppColors.greenLight : AppColors.border),
                      gradient: i == _index ? const LinearGradient(colors: [AppColors.gold, AppColors.goldLight]) : null,
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 20),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: _next,
                child: AnimatedSwitcher(
                  duration: const Duration(milliseconds: 250),
                  child: Text(_isLast ? 'Giriş yap' : 'Devam', key: ValueKey(_isLast)),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SlideText extends StatelessWidget {
  const _SlideText({required this.slide});

  final _Slide slide;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(
          slide.eyebrow,
          style: const TextStyle(color: AppColors.gold, fontWeight: FontWeight.w800, letterSpacing: 1.6, fontSize: 12),
        ),
        const SizedBox(height: 10),
        Text(
          slide.title,
          textAlign: TextAlign.center,
          style: const TextStyle(color: AppColors.green, fontSize: 26, fontWeight: FontWeight.w800, height: 1.2),
        ),
        const SizedBox(height: 12),
        Text(
          slide.text,
          textAlign: TextAlign.center,
          style: const TextStyle(color: AppColors.muted, fontSize: 17, height: 1.5),
        ),
      ],
    );
  }
}

/// Altın çerçeveli daire içinde simge ve köşede altın rozet.
class _Emblem extends StatelessWidget {
  const _Emblem({super.key, required this.icon, required this.badge});

  final IconData icon;
  final IconData badge;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 170,
      height: 170,
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          Container(
            width: 160,
            height: 160,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: AppColors.card,
              border: Border.all(color: AppColors.goldLight, width: 4),
              boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.25), blurRadius: 24, offset: const Offset(0, 12))],
            ),
            child: Icon(icon, size: 76, color: AppColors.green),
          ),
          Positioned(
            right: 0,
            top: 6,
            child: Container(
              width: 48,
              height: 48,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                gradient: const LinearGradient(colors: [AppColors.gold, AppColors.goldLight]),
                border: Border.all(color: AppColors.card, width: 3),
              ),
              child: Icon(badge, color: Colors.white, size: 26),
            ),
          ),
        ],
      ),
    );
  }
}

/// Sekiz köşeli yıldızlardan oluşan ince altın desen.
class _StarPatternPainter extends CustomPainter {
  const _StarPatternPainter();

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1
      ..color = AppColors.goldLight.withValues(alpha: 0.22);
    const step = 56.0;
    for (var x = -step; x < size.width + step; x += step) {
      for (var y = -step; y < size.height + step; y += step) {
        final center = Offset(x, y);
        final square = Rect.fromCenter(center: center, width: 26, height: 26);
        canvas.drawRect(square, paint);
        canvas.save();
        canvas.translate(center.dx, center.dy);
        canvas.rotate(math.pi / 4);
        canvas.drawRect(Rect.fromCenter(center: Offset.zero, width: 26, height: 26), paint);
        canvas.restore();
        canvas.drawCircle(center, 5, paint);
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
