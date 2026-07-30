import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import LanguageSelector from './LanguageSelector';

const changeLanguageMock = vi.fn();

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key, defaultValue) => defaultValue || key,
    i18n: {
      language: 'fr',
      resolvedLanguage: 'fr',
      changeLanguage: changeLanguageMock,
    },
  }),
}));

describe('LanguageSelector Component', () => {
  it('renders language selector dropdown with options', () => {
    render(<LanguageSelector />);
    const selectElement = screen.getByRole('combobox');
    expect(selectElement).toBeInTheDocument();
    expect(screen.getByText('EN - English')).toBeInTheDocument();
    expect(screen.getByText('FR - Français')).toBeInTheDocument();
  });

  it('calls changeLanguage when option is selected', () => {
    render(<LanguageSelector />);
    const selectElement = screen.getByRole('combobox');
    fireEvent.change(selectElement, { target: { value: 'en' } });
    expect(changeLanguageMock).toHaveBeenCalledWith('en');
  });
});
