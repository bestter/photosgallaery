import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { getUserRole, getUsernameFromToken, isTokenExpired, saveUserSession, clearUserSession } from './authHelper';

// On mock jwtDecode globalement pour ce test
vi.mock('jwt-decode', () => ({
    jwtDecode: vi.fn()
}));
import { jwtDecode } from 'jwt-decode';

describe('authHelper', () => {

    beforeEach(() => {
        vi.clearAllMocks();
        localStorage.clear();
    });

    describe('saveUserSession', () => {
        it('should decode token and save user_info to localStorage', () => {
            jwtDecode.mockReturnValue({
                role: 'Admin',
                unique_name: 'testuser',
                exp: 1234567890
            });

            saveUserSession('fake-token');

            const userInfo = JSON.parse(localStorage.getItem('user_info'));
            expect(userInfo).toEqual({
                role: 'Admin',
                username: 'testuser',
                exp: 1234567890
            });
        });

        it('should handle missing token safely', () => {
            saveUserSession(null);
            expect(localStorage.getItem('user_info')).toBeNull();
        });
    });

    describe('clearUserSession', () => {
        it('should remove user_info from localStorage', () => {
            localStorage.setItem('user_info', JSON.stringify({ role: 'User' }));
            clearUserSession();
            expect(localStorage.getItem('user_info')).toBeNull();
        });
    });

    describe('getUserRole', () => {
        it('should return null if no user_info', () => {
            expect(getUserRole()).toBeNull();
        });

        it('should return role from user_info', () => {
            localStorage.setItem('user_info', JSON.stringify({ role: 'Creator' }));
            expect(getUserRole()).toBe('Creator');
        });
    });

    describe('getUsernameFromToken', () => {
        it('should return null if no user_info', () => {
            expect(getUsernameFromToken()).toBeNull();
        });

        it('should return username from user_info', () => {
            localStorage.setItem('user_info', JSON.stringify({ username: 'alice' }));
            expect(getUsernameFromToken()).toBe('alice');
        });
    });

    describe('isTokenExpired', () => {
        it('should return true if no user_info', () => {
            expect(isTokenExpired()).toBe(true);
        });

        it('should return true if token is expired', () => {
            const pastTime = (Date.now() / 1000) - 1000;
            localStorage.setItem('user_info', JSON.stringify({ exp: pastTime }));
            expect(isTokenExpired()).toBe(true);
        });

        it('should return false if token is not expired', () => {
            const futureTime = (Date.now() / 1000) + 1000;
            localStorage.setItem('user_info', JSON.stringify({ exp: futureTime }));
            expect(isTokenExpired()).toBe(false);
        });
    });
});
