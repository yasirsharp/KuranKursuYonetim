/**
 * Merkezi Loglama Servisi
 * 
 * Her dosyada console.log yerine bu servis kullanılır.
 * Ortam bazlı (development/test/production) filtreleme yapar.
 * Hangi modülden, hangi fonksiyondan geldiğini izler.
 * 
 * Kullanım:
 *   const log = Logger.create('StudentListPage');
 *   log.debug('fetchStudents', 'API çağrısı başlatıldı', { page: 1 });
 *   log.info('fetchStudents', '12 öğrenci yüklendi');
 *   log.warn('fetchStudents', 'Boş liste döndü');
 *   log.error('fetchStudents', 'API hatası', error);
 */

type LogLevel = 'DEBUG' | 'INFO' | 'WARN' | 'ERROR';

type Environment = 'development' | 'test' | 'production';

interface LogEntry {
  level: LogLevel;
  timestamp: string;
  module: string;
  method: string;
  message: string;
  data?: unknown;
}

const LOG_ICONS: Record<LogLevel, string> = {
  DEBUG: '🔍',
  INFO: 'ℹ️ ',
  WARN: '⚠️ ',
  ERROR: '❌',
};

const LOG_COLORS: Record<LogLevel, string> = {
  DEBUG: 'color: #6B7280',
  INFO: 'color: #2563EB',
  WARN: 'color: #D97706',
  ERROR: 'color: #DC2626; font-weight: bold',
};

const LEVEL_PRIORITY: Record<LogLevel, number> = {
  DEBUG: 0,
  INFO: 1,
  WARN: 2,
  ERROR: 3,
};

// Ortam bazlı minimum log seviyesi
const MIN_LEVEL: Record<Environment, LogLevel> = {
  development: 'DEBUG',
  test: 'INFO',
  production: 'WARN',
};

function getEnvironment(): Environment {
  const mode = import.meta.env.MODE;
  if (mode === 'production') return 'production';
  if (mode === 'test') return 'test';
  return 'development';
}

function getTimestamp(): string {
  return new Date().toLocaleTimeString('tr-TR', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

function shouldLog(level: LogLevel): boolean {
  const env = getEnvironment();
  const minLevel = MIN_LEVEL[env];
  return LEVEL_PRIORITY[level] >= LEVEL_PRIORITY[minLevel];
}

function formatEntry(entry: LogEntry): string {
  return `${LOG_ICONS[entry.level]} [${entry.level}] [${entry.timestamp}] [${entry.module}.${entry.method}] ${entry.message}`;
}

class ModuleLogger {
  private module: string;

  constructor(module: string) {
    this.module = module;
  }

  debug(method: string, message: string, data?: unknown): void {
    this.log('DEBUG', method, message, data);
  }

  info(method: string, message: string, data?: unknown): void {
    this.log('INFO', method, message, data);
  }

  warn(method: string, message: string, data?: unknown): void {
    this.log('WARN', method, message, data);
  }

  error(method: string, message: string, data?: unknown): void {
    this.log('ERROR', method, message, data);
  }

  private log(level: LogLevel, method: string, message: string, data?: unknown): void {
    if (!shouldLog(level)) return;

    const entry: LogEntry = {
      level,
      timestamp: getTimestamp(),
      module: this.module,
      method,
      message,
      data,
    };

    const formatted = formatEntry(entry);
    const style = LOG_COLORS[level];

    switch (level) {
      case 'DEBUG':
        if (data !== undefined) {
          console.debug(`%c${formatted}`, style, data);
        } else {
          console.debug(`%c${formatted}`, style);
        }
        break;
      case 'INFO':
        if (data !== undefined) {
          console.info(`%c${formatted}`, style, data);
        } else {
          console.info(`%c${formatted}`, style);
        }
        break;
      case 'WARN':
        if (data !== undefined) {
          console.warn(`%c${formatted}`, style, data);
        } else {
          console.warn(`%c${formatted}`, style);
        }
        break;
      case 'ERROR':
        if (data !== undefined) {
          console.error(`%c${formatted}`, style, data);
        } else {
          console.error(`%c${formatted}`, style);
        }
        break;
    }

    // İleride harici servis entegrasyonu (Sentry vb.) buraya eklenebilir
    // if (level === 'ERROR' && getEnvironment() === 'production') {
    //   externalErrorService.capture(entry);
    // }
  }
}

export const Logger = {
  /**
   * Belirli bir modül/bileşen için logger oluşturur.
   * @param module Modül adı (örn: 'StudentListPage', 'ApiClient', 'AuthContext')
   */
  create(module: string): ModuleLogger {
    return new ModuleLogger(module);
  },
};
