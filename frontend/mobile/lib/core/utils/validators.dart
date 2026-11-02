final _emailPattern = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');

bool isValidEmail(String value) => _emailPattern.hasMatch(value.trim());

/// Yalnızca rakamlardan oluşan 6 haneli giriş kodu.
bool isValidCode(String value) => RegExp(r'^\d{6}$').hasMatch(value);

/// Cep numarasını sunucunun beklediği 10 haneye indirir (0532…, +90 532…, 532… → 5321112233).
/// Geçerli bir cep numarası değilse null.
String? normalizePhone(String value) {
  var digits = value.replaceAll(RegExp(r'\D'), '');
  if (digits.length == 12 && digits.startsWith('90')) digits = digits.substring(2);
  if (digits.length == 11 && digits.startsWith('0')) digits = digits.substring(1);
  return RegExp(r'^5\d{9}$').hasMatch(digits) ? digits : null;
}

/// Ekranda gösterim: 5321112233 → 0532 111 22 33
String formatPhone(String normalized) =>
    '0${normalized.substring(0, 3)} ${normalized.substring(3, 6)} ${normalized.substring(6, 8)} ${normalized.substring(8)}';
