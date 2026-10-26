import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import '@testing-library/jest-dom';
import ImageModal from './ImageModal';

vi.mock('../api', () => ({
    default: {
        post: vi.fn(),
        get: vi.fn()
    }
}));

vi.mock('react-hot-toast', () => ({
    default: {
        success: vi.fn(),
        error: vi.fn(),
        loading: vi.fn()
    }
}));

vi.mock('../authHelper', () => ({
    getUsernameFromToken: vi.fn(() => 'testUser')
}));

vi.mock('./ReportModal', () => ({
    default: () => <div data-testid="report-modal">ReportModal</div>
}));

vi.mock('react-i18next', () => ({
    useTranslation: () => ({
        t: (key, options) => {
            if (key === 'gallery.photo_by' && options?.author) {
                return `Photo by ${options.author}`;
            }
            if (key === 'components.image_modal.dialog_label') {
                return 'Image details';
            }
            if (key === 'components.image_modal.unknown_author') {
                return 'Anonymous';
            }
            if (typeof options === 'string') {
                return options;
            }
            return key;
        }
    })
}));

describe('ImageModal', () => {
    const mockPhoto = {
        id: 123,
        fullUrl: 'https://example.com/test-large.jpg',
        uploaderUsername: 'alice',
        dateTaken: '2026-01-01T00:00:00Z',
        cameraModel: 'Canon EOS',
        likesCount: 5,
        viewsCount: 42,
        tags: []
    };

    const defaultProps = {
        photo: mockPhoto,
        onClose: vi.fn(),
        onPrev: vi.fn(),
        onNext: vi.fn(),
        onTagClick: vi.fn(),
        onAuthorClick: vi.fn()
    };

    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('returns null if no photo is passed', () => {
        const { container } = render(<ImageModal {...defaultProps} photo={null} />);
        expect(container).toBeEmptyDOMElement();
    });

    it('renders dialog container with appropriate aria-label', () => {
        render(<ImageModal {...defaultProps} />);
        const dialog = screen.getByRole('dialog');
        expect(dialog).toBeInTheDocument();
        expect(dialog).toHaveAttribute('aria-label', 'Image details');
    });

    it('renders a semantic image with a contextualized alt description', () => {
        render(<ImageModal {...defaultProps} />);
        const img = screen.getByRole('img', { name: 'Photo by alice' });
        expect(img).toBeInTheDocument();
        expect(img.tagName).toBe('IMG');
        expect(img).toHaveAttribute('src', 'https://example.com/test-large.jpg');
        expect(img).toHaveAttribute('alt', 'Photo by alice');
    });

    it('shows a loading status until the image loads', () => {
      render(<ImageModal {...defaultProps} />);
      const image = screen.getByRole('img', { name: 'Photo by alice' });

      expect(screen.getByRole('status')).toBeInTheDocument();
      expect(image).toHaveClass('opacity-0');
      fireEvent.load(image);
      expect(screen.queryByRole('status')).not.toBeInTheDocument();
      expect(image).toHaveClass('opacity-100');
    });

    it('keeps the loaded image visible when the parent recreates the photo with the same URL', () => {
      const { rerender } = render(<ImageModal {...defaultProps} />);
      const image = screen.getByRole('img', { name: 'Photo by alice' });
      fireEvent.load(image);

      rerender(<ImageModal {...defaultProps} photo={{ ...mockPhoto, viewsCount: 43 }} />);

      expect(screen.getByRole('img', { name: 'Photo by alice' })).toBe(image);
      expect(image).toHaveClass('opacity-100');
      expect(screen.queryByRole('status')).not.toBeInTheDocument();
    });

    it('starts loading again when navigating to a different URL and back', () => {
      const { rerender } = render(<ImageModal {...defaultProps} />);
      fireEvent.load(screen.getByRole('img', { name: 'Photo by alice' }));

      rerender(<ImageModal {...defaultProps} photo={{ ...mockPhoto, fullUrl: 'https://example.com/next.jpg' }} />);
      const nextImage = screen.getByRole('img', { name: 'Photo by alice' });
      expect(nextImage).toHaveAttribute('src', 'https://example.com/next.jpg');
      expect(nextImage).toHaveClass('opacity-0');
      expect(screen.getByRole('status')).toBeInTheDocument();
      fireEvent.load(nextImage);
      expect(screen.queryByRole('status')).not.toBeInTheDocument();

      rerender(<ImageModal {...defaultProps} />);
      expect(screen.getByRole('img', { name: 'Photo by alice' })).toHaveClass('opacity-0');
      expect(screen.getByRole('status')).toBeInTheDocument();
      fireEvent.load(screen.getByRole('img', { name: 'Photo by alice' }));
      expect(screen.queryByRole('status')).not.toBeInTheDocument();
    });

    it('replaces the loading status with a translated accessible error when the image fails', () => {
      render(<ImageModal {...defaultProps} />);
      fireEvent.error(screen.getByRole('img', { name: 'Photo by alice' }));

      expect(screen.queryByRole('status')).not.toBeInTheDocument();
      expect(screen.getByRole('alert')).toHaveTextContent('components.image_modal.load_error');
      expect(screen.queryByRole('img', { name: 'Photo by alice' })).not.toBeInTheDocument();
    });

    it('preserves an error for the same URL and clears it when navigating to another image', () => {
      const { rerender } = render(<ImageModal {...defaultProps} />);
      fireEvent.error(screen.getByRole('img', { name: 'Photo by alice' }));

      rerender(<ImageModal {...defaultProps} photo={{ ...mockPhoto }} />);
      expect(screen.getByRole('alert')).toBeInTheDocument();
      expect(screen.queryByRole('status')).not.toBeInTheDocument();

      rerender(<ImageModal {...defaultProps} photo={{ ...mockPhoto, fullUrl: 'https://example.com/next.jpg' }} />);
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
      expect(screen.getByRole('status')).toBeInTheDocument();
      fireEvent.load(screen.getByRole('img', { name: 'Photo by alice' }));
      expect(screen.queryByRole('status')).not.toBeInTheDocument();
      expect(screen.getByRole('img', { name: 'Photo by alice' })).toHaveClass('opacity-100');
    });

    it('uses photo.title as the image alt description if present', () => {
        const photoWithTitle = { ...mockPhoto, title: 'Sunset at the Beach' };
        render(<ImageModal {...defaultProps} photo={photoWithTitle} />);
        const img = screen.getByRole('img', { name: 'Sunset at the Beach' });
        expect(img).toBeInTheDocument();
    });

    it('falls back to anonymous author when uploaderUsername is missing', () => {
        const photoNoAuthor = { ...mockPhoto, uploaderUsername: null };
        render(<ImageModal {...defaultProps} photo={photoNoAuthor} />);
        const img = screen.getByRole('img', { name: 'Photo by Anonymous' });
        expect(img).toBeInTheDocument();
    });

    it('calls onClose when close button is clicked', () => {
        render(<ImageModal {...defaultProps} />);
        const closeBtn = screen.getByRole('button', { name: 'Close' });
        fireEvent.click(closeBtn);
        expect(defaultProps.onClose).toHaveBeenCalledTimes(1);
    });

    it('calls onPrev and onNext when arrow buttons are clicked', () => {
        render(<ImageModal {...defaultProps} />);
        const prevBtn = screen.getByRole('button', { name: 'Previous image' });
        const nextBtn = screen.getByRole('button', { name: 'Next image' });

        fireEvent.click(prevBtn);
        expect(defaultProps.onPrev).toHaveBeenCalledTimes(1);

        fireEvent.click(nextBtn);
        expect(defaultProps.onNext).toHaveBeenCalledTimes(1);
    });

    it('handles keyboard navigation with ArrowLeft, ArrowRight, and Escape', () => {
        render(<ImageModal {...defaultProps} />);

        fireEvent.keyDown(window, { key: 'ArrowLeft' });
        expect(defaultProps.onPrev).toHaveBeenCalledTimes(1);

        fireEvent.keyDown(window, { key: 'ArrowRight' });
        expect(defaultProps.onNext).toHaveBeenCalledTimes(1);

        fireEvent.keyDown(window, { key: 'Escape' });
        expect(defaultProps.onClose).toHaveBeenCalledTimes(1);
    });
});
