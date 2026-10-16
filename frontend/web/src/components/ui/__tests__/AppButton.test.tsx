import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AppButton } from '../AppButton';

describe('AppButton Component', () => {
  it('verilen çocuk metni doğru render etmeli', () => {
    render(<AppButton>Kaydet</AppButton>);

    expect(screen.getByRole('button', { name: /kaydet/i })).toBeInTheDocument();
  });

  it('tıklandığında onClick fonksiyonunu tetiklemeli', async () => {
    const handleClick = vi.fn();
    render(<AppButton onClick={handleClick}>Tıkla</AppButton>);

    const button = screen.getByRole('button', { name: /tıkla/i });
    await userEvent.click(button);

    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('disabled durumdayken tıklanamamalı ve disabled niteliğine sahip olmalı', async () => {
    const handleClick = vi.fn();
    render(<AppButton disabled onClick={handleClick}>Pasif Buton</AppButton>);

    const button = screen.getByRole('button', { name: /pasif buton/i });
    expect(button).toBeDisabled();

    await userEvent.click(button);
    expect(handleClick).not.toHaveBeenCalled();
  });

  it('fullWidth propu verildiğinde width %100 stilini almalı', () => {
    render(<AppButton fullWidth>Geniş Buton</AppButton>);

    const button = screen.getByRole('button', { name: /geniş buton/i });
    expect(button.style.width).toBe('100%');
  });
});
