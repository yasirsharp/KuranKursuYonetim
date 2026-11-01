import { describe, it, expect, beforeEach, vi } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import WelcomeTour, { WELCOME_TOUR_STORAGE_KEY, openWelcomeTour } from '../WelcomeTour';

let user: { firstName: string } | null = null;
vi.mock('../../../context/AuthContext', () => ({ useAuth: () => ({ user }) }));

const renderAt = (path = '/login') =>
  render(
    <MemoryRouter initialEntries={[path]}>
      <WelcomeTour />
      <Routes>
        <Route path="*" element={<div>sayfa</div>} />
        <Route path="/mobil" element={<div>İndirme sayfası</div>} />
      </Routes>
    </MemoryRouter>
  );

describe('WelcomeTour', () => {
  beforeEach(() => {
    localStorage.clear();
    user = null;
  });

  it('ilk ziyarette açılır, adımlar arasında ilerler ve bir daha açılmaz', async () => {
    const ue = userEvent.setup();
    renderAt();

    expect(await screen.findByRole('dialog', {}, { timeout: 2000 })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: "Hafız Takip'e hoş geldiniz" })).toBeInTheDocument();

    await ue.click(screen.getByRole('button', { name: 'Devam' }));
    expect(screen.getByRole('heading', { name: 'Dersi saniyeler içinde kaydedin' })).toBeInTheDocument();

    await ue.keyboard('{ArrowRight}');
    await ue.keyboard('{ArrowRight}');
    expect(screen.getByRole('heading', { name: 'Hafız Takip artık cebinizde' })).toBeInTheDocument();

    await ue.click(screen.getByRole('button', { name: 'Hemen başlayalım' }));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(localStorage.getItem(WELCOME_TOUR_STORAGE_KEY)).toBe('1');
  });

  it('giriş yapmış kullanıcıyı adıyla karşılar; Esc ile kapanır', async () => {
    user = { firstName: 'Ahmet' };
    const ue = userEvent.setup();
    renderAt('/');

    expect(await screen.findByRole('heading', { name: 'Hoş geldiniz, Ahmet' }, { timeout: 2000 })).toBeInTheDocument();
    await ue.keyboard('{Escape}');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('görüldüyse kendiliğinden açılmaz ama "tekrar izle" ile açılır', async () => {
    localStorage.setItem(WELCOME_TOUR_STORAGE_KEY, '1');
    renderAt();
    await new Promise((r) => setTimeout(r, 600));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    act(() => openWelcomeTour());
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('indirme sayfasında kendiliğinden açılmaz; son adımdan indirme sayfasına gider', async () => {
    renderAt('/mobil');
    await new Promise((r) => setTimeout(r, 600));
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

    const ue = userEvent.setup();
    act(() => openWelcomeTour());
    await ue.click(screen.getByRole('button', { name: /4\. adım/ }));
    await ue.click(screen.getByRole('button', { name: /Uygulamayı indir/ }));
    expect(screen.getByText('İndirme sayfası')).toBeInTheDocument();
  });
});
