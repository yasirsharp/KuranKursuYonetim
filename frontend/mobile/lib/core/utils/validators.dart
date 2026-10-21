final _emailPattern = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');

bool isValidEmail(String value) => _emailPattern.hasMatch(value.trim());

/// Yalnızca rakamlardan oluşan 6 haneli giriş kodu.
bool isValidCode(String value) => RegExp(r'^\d{6}$').hasMatch(value);
