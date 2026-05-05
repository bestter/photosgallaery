import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { getUserRole, isTokenExpired, getUsernameFromToken } from './authHelper';
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


describe('isTokenExpired', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        vi.useFakeTimers();
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should return true if token is falsy or specific string strings', () => {
        expect(isTokenExpired(null)).toBe(true);
        expect(isTokenExpired(undefined)).toBe(true);
        expect(isTokenExpired('')).toBe(true);
        expect(isTokenExpired('null')).toBe(true);
        expect(isTokenExpired('undefined')).toBe(true);
    });

    it('should return true if jwtDecode throws an error', () => {
        jwtDecode.mockImplementation(() => {
            throw new Error('Invalid token');
        });
        expect(isTokenExpired('invalid-token')).toBe(true);
    });

    it('should return true if token is expired (considering 10s margin)', () => {
        const currentTime = 1000;
        vi.setSystemTime(currentTime * 1000);

        // Expiration is less than 1010
        jwtDecode.mockReturnValue({ exp: 1009 });
        expect(isTokenExpired('expired-token')).toBe(true);
    });

    it('should return false if token is not expired (considering 10s margin)', () => {
        const currentTime = 1000;
        vi.setSystemTime(currentTime * 1000);

        // Expiration is exactly 1010
        jwtDecode.mockReturnValue({ exp: 1010 });
        expect(isTokenExpired('valid-token')).toBe(false);

        // Expiration is well into the future
        jwtDecode.mockReturnValue({ exp: 2000 });
        expect(isTokenExpired('valid-token')).toBe(false);
    });
});

describe('getUsernameFromToken', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('should return null if token is falsy', () => {
        expect(getUsernameFromToken(null)).toBeNull();
        expect(getUsernameFromToken(undefined)).toBeNull();
        expect(getUsernameFromToken('')).toBeNull();
    });

    it('should return null if jwtDecode throws an error', () => {
        jwtDecode.mockImplementation(() => {
            throw new Error('Invalid token');
        });
        expect(getUsernameFromToken('invalid-token')).toBeNull();
    });

    it('should extract username from .NET claims URL', () => {
        jwtDecode.mockReturnValue({
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "user1"
        });
        expect(getUsernameFromToken('valid-token')).toBe('user1');
    });

    it('should extract username from unique_name if .NET claims URL is absent', () => {
        jwtDecode.mockReturnValue({
            unique_name: "user2"
        });
        expect(getUsernameFromToken('valid-token')).toBe('user2');
    });

    it('should extract username from sub if both .NET claims URL and unique_name are absent', () => {
        jwtDecode.mockReturnValue({
            sub: "user3"
        });
        expect(getUsernameFromToken('valid-token')).toBe('user3');
    });

    it('should prioritize .NET claims URL over unique_name and sub', () => {
        jwtDecode.mockReturnValue({
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "user1",
            unique_name: "user2",
            sub: "user3"
        });
        expect(getUsernameFromToken('valid-token')).toBe('user1');
    });

    it('should prioritize unique_name over sub', () => {
        jwtDecode.mockReturnValue({
            unique_name: "user2",
            sub: "user3"
        });
        expect(getUsernameFromToken('valid-token')).toBe('user2');
    });
});
