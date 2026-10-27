import React from 'react';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import UploadPhoto from './UploadPhoto';
import api from '../api';
import toast from 'react-hot-toast';
import * as authHelper from '../authHelper';

vi.mock('../api');
vi.mock('react-hot-toast');
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key, options) => {
      if (options?.count !== undefined) return `${key}:${options.count}`;
      return key;
    },
    i18n: {
      language: 'fr',
      resolvedLanguage: 'fr',
      changeLanguage: vi.fn(),
    },
  }),
}));

describe('UploadPhoto Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(authHelper, 'isTokenExpired').mockReturnValue(false);
    vi.spyOn(authHelper, 'getUserRole').mockReturnValue('Admin');
    api.get.mockResolvedValue({
      data: [{ id: 1, name: 'Default Circle' }],
    });
  });

  it('renders input with only supported formats (jpeg, png, webp)', async () => {
    render(<UploadPhoto />);
    await waitFor(() => expect(screen.getByText('Default Circle')).toBeInTheDocument());
    const input = document.querySelector('input[type="file"]');
    expect(input).toBeInTheDocument();
    expect(input).toHaveAttribute('accept', 'image/jpeg,image/png,image/webp');
  });

  it('rejects unsupported file formats such as avif', async () => {
    render(<UploadPhoto />);
    await waitFor(() => expect(screen.getByText('Default Circle')).toBeInTheDocument());
    const input = document.querySelector('input[type="file"]');
    const avifFile = new File(['dummy'], 'photo.avif', { type: 'image/avif' });

    fireEvent.change(input, { target: { files: [avifFile] } });

    expect(toast.error).toHaveBeenCalledWith('components.upload.error.unsupported_format');
  });

  it('accepts supported formats (jpeg, png, webp)', async () => {
    render(<UploadPhoto />);
    await waitFor(() => expect(screen.getByText('Default Circle')).toBeInTheDocument());
    const input = document.querySelector('input[type="file"]');
    const jpgFile = new File(['dummy'], 'photo.jpg', { type: 'image/jpeg' });

    fireEvent.change(input, { target: { files: [jpgFile] } });

    expect(toast.success).toHaveBeenCalledWith(
      expect.stringContaining('components.upload.success'),
      expect.anything()
    );
  });

  it('handles 503 server busy error accurately', async () => {
    render(<UploadPhoto initialGroupId={1} />);
    await waitFor(() => expect(screen.getByText('Default Circle')).toBeInTheDocument());

    // Select file
    const input = document.querySelector('input[type="file"]');
    const jpgFile = new File(['dummy'], 'photo.jpg', { type: 'image/jpeg' });
    fireEvent.change(input, { target: { files: [jpgFile] } });

    // Enter title & tag
    fireEvent.change(document.getElementById('photo-title'), { target: { value: 'Test Title' } });
    fireEvent.change(document.getElementById('photo-tags'), { target: { value: 'nature' } });

    // Mock API post rejection with 503
    api.post.mockRejectedValueOnce({
      response: {
        status: 503,
        data: { message: 'Le serveur est actuellement saturé.' }
      }
    });

    let errorHandler;
    toast.promise.mockImplementation((promise, handlers) => {
      errorHandler = handlers.error;
      return promise.catch((err) => {
        if (errorHandler) {
          errorHandler(err);
        }
      });
    });

    fireEvent.submit(document.querySelector('form'));

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith('/photos/upload', expect.any(FormData));
    });

    const errorMsg = errorHandler({
      response: {
        status: 503,
        data: { message: 'Le serveur est actuellement saturé.' }
      }
    });

    expect(errorMsg).toBe('Le serveur est actuellement saturé.');
  });

  it('treats zero-success response with errors as failure and displays rejection summary', async () => {
    render(<UploadPhoto initialGroupId={1} />);
    await waitFor(() => expect(screen.getByText('Default Circle')).toBeInTheDocument());

    const input = document.querySelector('input[type="file"]');
    const jpgFile = new File(['dummy'], 'corrupt.jpg', { type: 'image/jpeg' });
    fireEvent.change(input, { target: { files: [jpgFile] } });

    fireEvent.change(document.getElementById('photo-title'), { target: { value: 'Test Title' } });
    fireEvent.change(document.getElementById('photo-tags'), { target: { value: 'nature' } });

    let successHandler;
    toast.promise.mockImplementation((_, handlers) => {
      successHandler = handlers.success;
      return Promise.resolve();
    });

    fireEvent.submit(document.querySelector('form'));

    await waitFor(() => {
      expect(toast.promise).toHaveBeenCalled();
    });

    expect(() => {
      successHandler({
        data: {
          message: '0 image(s) téléversée(s) avec succès.',
          photos: [],
          erreurs: ["Le fichier 'corrupt.jpg' est corrompu ou illisible."]
        }
      });
    }).toThrow("Le fichier 'corrupt.jpg' est corrompu ou illisible.");
  });

  it('shows warning on partial batch rejection and calls onUploadSuccess', async () => {
    const onUploadSuccess = vi.fn();
    render(<UploadPhoto initialGroupId={1} onUploadSuccess={onUploadSuccess} />);
    await waitFor(() => expect(screen.getByText('Default Circle')).toBeInTheDocument());

    const input = document.querySelector('input[type="file"]');
    const jpgFile = new File(['dummy'], 'valid.jpg', { type: 'image/jpeg' });
    fireEvent.change(input, { target: { files: [jpgFile] } });

    fireEvent.change(document.getElementById('photo-title'), { target: { value: 'Test Title' } });
    fireEvent.change(document.getElementById('photo-tags'), { target: { value: 'nature' } });

    let successHandler;
    toast.promise.mockImplementation((_, handlers) => {
      successHandler = handlers.success;
      return Promise.resolve();
    });

    fireEvent.submit(document.querySelector('form'));

    await waitFor(() => {
      expect(toast.promise).toHaveBeenCalled();
    });

    let msg;
    act(() => {
      msg = successHandler({
        data: {
          message: '1 image(s) téléversée(s) avec succès.',
          photos: [{ id: 1, fileName: 'valid.jpg' }],
          erreurs: ["L'image 'dup.jpg' existe déjà dans la galerie."]
        }
      });
    });

    expect(msg).toBe('1 image(s) téléversée(s) avec succès.');
    expect(toast).toHaveBeenCalledWith('components.upload.error.partial_rejections:1', expect.objectContaining({ icon: '⚠️' }));
    expect(onUploadSuccess).toHaveBeenCalled();
  });
});
