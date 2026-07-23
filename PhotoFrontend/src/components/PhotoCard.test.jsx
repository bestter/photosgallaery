import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import '@testing-library/jest-dom';
import PhotoCard from './PhotoCard';

describe('PhotoCard', () => {
    const defaultProps = {
        src: 'test-image.jpg',
        alt: 'Test Alt Text',
        author: 'Test Author',
        onClick: vi.fn(),
        onAuthorClick: vi.fn()
    };

    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('renders correctly with basic props', () => {
        render(<PhotoCard {...defaultProps} />);

        const img = screen.getByAltText('Test Alt Text');
        expect(img).toBeInTheDocument();
        expect(img).toHaveAttribute('src', 'test-image.jpg');

        const author = screen.getByText('Test Author');
        expect(author).toBeInTheDocument();
    });

    it('triggers onClick when the main card area is clicked', () => {
        render(<PhotoCard {...defaultProps} />);

        // Find the outer div which has the onClick. The text is inside, so we can click the img.
        const img = screen.getByAltText('Test Alt Text');
        fireEvent.click(img);

        expect(defaultProps.onClick).toHaveBeenCalledTimes(1);
        expect(defaultProps.onAuthorClick).not.toHaveBeenCalled();
    });

    it('triggers onAuthorClick with author name when the author name is clicked', () => {
        render(<PhotoCard {...defaultProps} />);

        const author = screen.getByText('Test Author');
        fireEvent.click(author);

        expect(defaultProps.onAuthorClick).toHaveBeenCalledTimes(1);
        expect(defaultProps.onAuthorClick).toHaveBeenCalledWith('Test Author');

        // stopPropagation should prevent onClick from firing
        expect(defaultProps.onClick).not.toHaveBeenCalled();
    });

    it('strips leading "@" when calling onAuthorClick', () => {
        const propsWithAtAuthor = { ...defaultProps, author: '@cooluser' };
        render(<PhotoCard {...propsWithAtAuthor} />);

        const author = screen.getByText('@cooluser');
        fireEvent.click(author);

        expect(propsWithAtAuthor.onAuthorClick).toHaveBeenCalledTimes(1);
        expect(propsWithAtAuthor.onAuthorClick).toHaveBeenCalledWith('cooluser');
    });

    it('does not trigger error or onAuthorClick if onAuthorClick is not provided', () => {
        const propsWithoutAuthorClick = {
            src: 'test.jpg',
            alt: 'test',
            author: 'author',
            onClick: vi.fn()
        };
        render(<PhotoCard {...propsWithoutAuthorClick} />);

        // Author is a non-interactive span; open control remains the main button.
        expect(screen.getByText('author').tagName).toBe('SPAN');
        fireEvent.click(screen.getByRole('button', { name: 'gallery.view_photo_by' }));
        expect(propsWithoutAuthorClick.onClick).toHaveBeenCalledTimes(1);
    });

    it('triggers onClick when the open button is activated', () => {
        render(<PhotoCard {...defaultProps} />);
        const card = screen.getByRole('button', { name: `gallery.view_photo_by` });
        fireEvent.click(card);
        expect(defaultProps.onClick).toHaveBeenCalledTimes(1);
    });

    it('does not throw when onClick is not provided', () => {
        const propsNoOnClick = { ...defaultProps, onClick: undefined };
        render(<PhotoCard {...propsNoOnClick} />);
        const card = screen.getByRole('button', { name: `gallery.view_photo_by` });
        fireEvent.click(card);
    });
});
