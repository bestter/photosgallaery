import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import Footer from './Footer';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key, defaultValue) => defaultValue || key,
    i18n: {
      language: 'fr',
      resolvedLanguage: 'fr',
      changeLanguage: vi.fn(),
    },
  }),
}));

describe('Footer Component', () => {
  it('renders copyright and navigation links', () => {
    render(<Footer />);
    expect(screen.getByText(/PixelLyra\.com/i)).toBeInTheDocument();
    expect(screen.getByText(/Politique de confidentialité/i)).toBeInTheDocument();
    expect(screen.getByText(/Conditions d'utilisation/i)).toBeInTheDocument();
    expect(screen.getByText(/Contact/i)).toBeInTheDocument();
  });
});
