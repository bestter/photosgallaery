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

            const userInfo = JSON.parse(localStorage.getItem('user_info:v1') || localStorage.getItem('user_info'));
            expect(userInfo).toEqual({
                role: 'Admin',
                username: 'testuser',
                exp: 1234567890
            });
        });


        it('should handle alternative role claim keys and mappings (Admin)', () => {
            jwtDecode.mockReturnValue({
                'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': '9999',
                unique_name: 'adminuser',
                exp: 1234567890
            });

            saveUserSession('fake-token');

            const userInfo = JSON.parse(localStorage.getItem('user_info:v1'));
            expect(userInfo.role).toBe('Admin');
        });

        it('should handle alternative role claim keys and mappings (Creator)', () => {
            jwtDecode.mockReturnValue({
                'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': '1',
                unique_name: 'creatoruser',
                exp: 1234567890
            });

            saveUserSession('fake-token');

            const userInfo = JSON.parse(localStorage.getItem('user_info:v1'));
            expect(userInfo.role).toBe('Creator');
        });

        it('should handle alternative username claim keys (name claim)', () => {
            jwtDecode.mockReturnValue({
                role: 'User',
                'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'name_claim_user',
                exp: 1234567890
            });

            saveUserSession('fake-token');

            const userInfo = JSON.parse(localStorage.getItem('user_info:v1'));
            expect(userInfo.username).toBe('name_claim_user');
        });

        it('should handle alternative username claim keys (sub)', () => {
            jwtDecode.mockReturnValue({
                role: 'User',
                sub: 'sub_claim_user',
                exp: 1234567890
            });

            saveUserSession('fake-token');

            const userInfo = JSON.parse(localStorage.getItem('user_info:v1'));
            expect(userInfo.username).toBe('sub_claim_user');
        });

        it('should handle jwtDecode exceptions gracefully', () => {
            jwtDecode.mockImplementation(() => {
                throw new Error('Invalid token');
            });
            const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

            saveUserSession('invalid-token');

            expect(localStorage.getItem('user_info:v1')).toBeNull();
            expect(consoleSpy).toHaveBeenCalledWith('Error saving user session:', expect.any(Error));

            consoleSpy.mockRestore();
        });

        it('should handle missing token safely', () => {
            saveUserSession(null);
            expect(localStorage.getItem('user_info:v1')).toBeNull();
        });

        it('should handle localStorage error gracefully', () => {
            jwtDecode.mockReturnValue({
                role: 'Admin',
                unique_name: 'testuser',
                exp: 1234567890
            });

            const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

            vi.stubGlobal('localStorage', {
                setItem: vi.fn(() => { throw new Error('localStorage is full'); }),
                getItem: vi.fn(),
                removeItem: vi.fn(),
                clear: vi.fn()
            });

            saveUserSession('fake-token');

            expect(consoleSpy).toHaveBeenCalledWith('Error saving user session:', expect.any(Error));

            vi.unstubAllGlobals();
            vi.restoreAllMocks();
        });
    });

    describe('clearUserSession', () => {
        it('should remove user_info from localStorage', () => {
            localStorage.setItem('user_info:v1', JSON.stringify({ role: 'User' }));
            clearUserSession();
            expect(localStorage.getItem('user_info:v1')).toBeNull();
        });
    });

    describe('getUserRole', () => {
        it('should return null if no user_info', () => {
            expect(getUserRole()).toBeNull();
        });

        it('should return role from user_info', () => {
            localStorage.setItem('user_info:v1', JSON.stringify({ role: 'Creator' }));
            expect(getUserRole()).toBe('Creator');
        });
    });

    describe('getUsernameFromToken', () => {
        it('should return null if no user_info', () => {
            expect(getUsernameFromToken()).toBeNull();
        });

        it('should return username from user_info', () => {
            localStorage.setItem('user_info:v1', JSON.stringify({ username: 'alice' }));
            expect(getUsernameFromToken()).toBe('alice');
        });
    });

    describe('isTokenExpired', () => {
        it('should return true if no user_info', () => {
            expect(isTokenExpired()).toBe(true);
        });

        it('should return true if token is expired', () => {
            const pastTime = (Date.now() / 1000) - 1000;
            localStorage.setItem('user_info:v1', JSON.stringify({ exp: pastTime }));
            expect(isTokenExpired()).toBe(true);
        });

        it('should return false if token is not expired', () => {
            const futureTime = (Date.now() / 1000) + 1000;
            localStorage.setItem('user_info:v1', JSON.stringify({ exp: futureTime }));
            expect(isTokenExpired()).toBe(false);
        });
    });
});
