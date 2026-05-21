import { vi, describe, it, expect, afterEach } from 'vitest';
import { getUserRole, getUsernameFromToken, isTokenExpired } from './authHelper';
import { jwtDecode } from 'jwt-decode';

// Mock the jwt-decode module
vi.mock('jwt-decode');

describe('authHelper', () => {
    afterEach(() => {
        vi.clearAllMocks();
    });

    describe('getUserRole', () => {
        it('should return null if no token is provided', () => {
            expect(getUserRole(null)).toBeNull();
            expect(getUserRole(undefined)).toBeNull();
            expect(getUserRole('')).toBeNull();
        });

        it('should return null if token is invalid (jwtDecode throws)', () => {
            jwtDecode.mockImplementation(() => { throw new Error('Invalid token'); });
            expect(getUserRole('invalid.token.here')).toBeNull();
        });

        it('should extract Admin role from Microsoft schema claim', () => {
            jwtDecode.mockReturnValue({
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Admin"
            });
            expect(getUserRole('valid.token')).toBe('Admin');
        });

        it('should extract Admin role from 9999', () => {
            jwtDecode.mockReturnValue({
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "9999"
            });
            expect(getUserRole('valid.token')).toBe('Admin');
        });

        it('should extract Creator role from Creator', () => {
            jwtDecode.mockReturnValue({ role: "Creator" });
            expect(getUserRole('valid.token')).toBe('Creator');
        });

        it('should extract Creator role from 1', () => {
            jwtDecode.mockReturnValue({ role: "1" });
            expect(getUserRole('valid.token')).toBe('Creator');
        });

        it('should return raw role if not Admin or Creator', () => {
            jwtDecode.mockReturnValue({ role: "User" });
            expect(getUserRole('valid.token')).toBe('User');

            jwtDecode.mockReturnValue({ role: "0" });
            expect(getUserRole('valid.token')).toBe('0');
        });
    });

    describe('getUsernameFromToken', () => {
        it('should return null if no token is provided', () => {
            expect(getUsernameFromToken(null)).toBeNull();
            expect(getUsernameFromToken(undefined)).toBeNull();
            expect(getUsernameFromToken('')).toBeNull();
        });

        it('should return null if token is invalid (jwtDecode throws)', () => {
            jwtDecode.mockImplementation(() => { throw new Error('Invalid token'); });
            expect(getUsernameFromToken('invalid.token.here')).toBeNull();
        });

        it('should extract username from Microsoft schema claim', () => {
            jwtDecode.mockReturnValue({
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "testuser"
            });
            expect(getUsernameFromToken('valid.token')).toBe('testuser');
        });

        it('should extract username from unique_name', () => {
            jwtDecode.mockReturnValue({ unique_name: "testuser2" });
            expect(getUsernameFromToken('valid.token')).toBe('testuser2');
        });

        it('should extract username from sub', () => {
            jwtDecode.mockReturnValue({ sub: "testuser3" });
            expect(getUsernameFromToken('valid.token')).toBe('testuser3');
        });

        it('should prioritize claims correctly', () => {
            jwtDecode.mockReturnValue({
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "prio1",
                unique_name: "prio2",
                sub: "prio3"
            });
            expect(getUsernameFromToken('valid.token')).toBe('prio1');

            jwtDecode.mockReturnValue({
                unique_name: "prio2",
                sub: "prio3"
            });
            expect(getUsernameFromToken('valid.token')).toBe('prio2');
        });
    });

    describe('isTokenExpired', () => {
        it('should return true if no token is provided', () => {
            expect(isTokenExpired(null)).toBe(true);
            expect(isTokenExpired(undefined)).toBe(true);
            expect(isTokenExpired('')).toBe(true);
        });

        it('should return true for string "null" or "undefined"', () => {
            expect(isTokenExpired('null')).toBe(true);
            expect(isTokenExpired('undefined')).toBe(true);
        });

        it('should return true if token is invalid (jwtDecode throws)', () => {
            jwtDecode.mockImplementation(() => { throw new Error('Invalid token'); });
            expect(isTokenExpired('invalid.token.here')).toBe(true);
        });

        it('should return true if token is expired (past expiration date)', () => {
            const currentTime = Math.floor(Date.now() / 1000);
            // expired 1 hour ago
            jwtDecode.mockReturnValue({ exp: currentTime - 3600 });
            expect(isTokenExpired('valid.token')).toBe(true);
        });

        it('should return true if token is within 10 seconds of expiring', () => {
            const currentTime = Math.floor(Date.now() / 1000);
            // expiring in 5 seconds (within 10s margin)
            jwtDecode.mockReturnValue({ exp: currentTime + 5 });
            expect(isTokenExpired('valid.token')).toBe(true);
        });

        it('should return false if token is valid and not expiring soon', () => {
            const currentTime = Math.floor(Date.now() / 1000);
            // expiring in 1 hour
            jwtDecode.mockReturnValue({ exp: currentTime + 3600 });
            expect(isTokenExpired('valid.token')).toBe(false);
        });
    });
});
