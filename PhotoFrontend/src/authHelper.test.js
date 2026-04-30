import { describe, it, expect, vi, beforeEach } from 'vitest';
import { getUserRole } from './authHelper';
import { jwtDecode } from 'jwt-decode';

vi.mock('jwt-decode');

describe('getUserRole', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('should return null if token is falsy', () => {
        expect(getUserRole(null)).toBeNull();
        expect(getUserRole(undefined)).toBeNull();
        expect(getUserRole('')).toBeNull();
    });

    it('should return null if jwtDecode throws an error', () => {
        jwtDecode.mockImplementation(() => {
            throw new Error('Invalid token');
        });
        expect(getUserRole('invalid-token')).toBeNull();
    });

    it('should extract role from Microsoft claims URL', () => {
        jwtDecode.mockReturnValue({
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "User"
        });
        expect(getUserRole('valid-token')).toBe('User');
    });

    it('should extract role from the role property if claims URL is absent', () => {
        jwtDecode.mockReturnValue({
            role: "Manager"
        });
        expect(getUserRole('valid-token')).toBe('Manager');
    });

    it('should return "Admin" if role is "9999"', () => {
        jwtDecode.mockReturnValue({ role: "9999" });
        expect(getUserRole('valid-token')).toBe('Admin');
    });

    it('should return "Admin" if role is "Admin"', () => {
        jwtDecode.mockReturnValue({ role: "Admin" });
        expect(getUserRole('valid-token')).toBe('Admin');
    });

    it('should return "Creator" if role is "1"', () => {
        jwtDecode.mockReturnValue({ role: "1" });
        expect(getUserRole('valid-token')).toBe('Creator');
    });

    it('should return "Creator" if role is "Creator"', () => {
        jwtDecode.mockReturnValue({ role: "Creator" });
        expect(getUserRole('valid-token')).toBe('Creator');
    });

    it('should return the raw role for other values', () => {
        jwtDecode.mockReturnValue({ role: "0" });
        expect(getUserRole('valid-token')).toBe('0');

        jwtDecode.mockReturnValue({ role: "Guest" });
        expect(getUserRole('valid-token')).toBe('Guest');
    });
});
