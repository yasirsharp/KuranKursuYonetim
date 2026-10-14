import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

// Her test sonrası render temizliği ve localStorage sıfırlaması
afterEach(() => {
  cleanup();
  localStorage.clear();
});
