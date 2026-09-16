import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import '@testing-library/jest-dom';
import GroupSelector from './GroupSelector';

vi.mock('react-i18next', () => ({
    useTranslation: () => ({
        t: (key, options) => {
            if (typeof options === 'object' && options?.name) {
                return `Select a group: ${options.name}`;
            }
            if (typeof options === 'string') {
                return options;
            }
            return key;
        }
    })
}));

describe('GroupSelector', () => {
    const mockGroups = [
        { id: '1', name: 'Nature Collective' },
        { id: '2', name: 'Urban Explorers' },
        { id: '3', name: 'Portrait Guild' }
    ];
    const defaultProps = {
        groups: mockGroups,
        activeGroupId: '1',
        onGroupSelect: vi.fn()
    };

    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('returns null when groups array is null or empty', () => {
        const { container: containerNull } = render(<GroupSelector groups={null} />);
        expect(containerNull).toBeEmptyDOMElement();

        const { container: containerEmpty } = render(<GroupSelector groups={[]} />);
        expect(containerEmpty).toBeEmptyDOMElement();
    });

    it('renders static label without dropdown when only one group exists', () => {
        render(<GroupSelector groups={[{ id: '1', name: 'Single Group' }]} />);

        expect(screen.getByText('Single Group')).toBeInTheDocument();
        expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    });

    it('toggles dropdown visibility on button click', () => {
        render(<GroupSelector {...defaultProps} />);

        const toggleButton = screen.getByRole('button', { name: /Select a group: Nature Collective/i });
        expect(toggleButton).toHaveAttribute('aria-expanded', 'false');

        fireEvent.click(toggleButton);
        expect(toggleButton).toHaveAttribute('aria-expanded', 'true');

        fireEvent.click(toggleButton);
        expect(toggleButton).toHaveAttribute('aria-expanded', 'false');
    });

    it('calls onGroupSelect and restores focus to toggle button when an item is selected', () => {
        render(<GroupSelector {...defaultProps} />);

        const toggleButton = screen.getByRole('button', { name: /Select a group: Nature Collective/i });
        fireEvent.click(toggleButton);

        const urbanOption = screen.getByRole('menuitem', { name: /Urban Explorers/i });
        urbanOption.focus();
        expect(document.activeElement).toBe(urbanOption);

        fireEvent.click(urbanOption);

        expect(defaultProps.onGroupSelect).toHaveBeenCalledWith('2');
        expect(toggleButton).toHaveAttribute('aria-expanded', 'false');
        expect(document.activeElement).toBe(toggleButton);
    });

    it('closes menu and restores focus to toggle button when Escape is pressed', () => {
        render(<GroupSelector {...defaultProps} />);

        const toggleButton = screen.getByRole('button', { name: /Select a group: Nature Collective/i });
        fireEvent.click(toggleButton);
        expect(toggleButton).toHaveAttribute('aria-expanded', 'true');

        fireEvent.keyDown(document, { key: 'Escape' });

        expect(toggleButton).toHaveAttribute('aria-expanded', 'false');
        expect(document.activeElement).toBe(toggleButton);
    });

    it('closes dropdown when clicking outside', () => {
        render(
            <div>
                <span data-testid="outside-element">Outside</span>
                <GroupSelector {...defaultProps} />
            </div>
        );

        const toggleButton = screen.getByRole('button', { name: /Select a group: Nature Collective/i });
        fireEvent.click(toggleButton);
        expect(toggleButton).toHaveAttribute('aria-expanded', 'true');

        const outside = screen.getByTestId('outside-element');
        fireEvent.mouseDown(outside);

        expect(toggleButton).toHaveAttribute('aria-expanded', 'false');
    });
});
