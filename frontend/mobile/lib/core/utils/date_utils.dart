import 'package:intl/intl.dart';

/// Backend'deki gün kolonları (DATE) "yyyy-MM-dd" ister; cihazın yerel günü kullanılır.
String apiDate(DateTime date) => DateFormat('yyyy-MM-dd').format(date);

/// "21 Ekim 2026"
String displayDate(DateTime date) => DateFormat('d MMMM y', 'tr_TR').format(date);

/// "21 Eki"
String shortDate(DateTime date) => DateFormat('d MMM', 'tr_TR').format(date);
