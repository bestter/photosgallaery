import { getUserRole, getUsernameFromToken, isTokenExpired, saveUserSession, clearUserSession } from './authHelper';

const createToken = (payload) => {
    const header = btoa(JSON.stringify({ alg: "HS256", typ: "JWT" }));
    const body = btoa(JSON.stringify(payload));
    return `${header}.${body}.signature`;
};

describe('authHelper', () => {
    beforeEach(() => {
        localStorage.clear();
    });

    describe('saveUserSession', () => {
        it('should decode token and save user_info to localStorage', () => {
            const token = createToken({ role: "Admin", unique_name: "testuser", exp: 1234567890 });
            saveUserSession(token);
            const userInfo = JSON.parse(localStorage.getItem('user_info'));
            expect(userInfo.role).toBe("Admin");
            expect(userInfo.username).toBe("testuser");
            expect(userInfo.exp).toBe(1234567890);
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
        it('should return null when user_info is absent', () => {
            expect(getUserRole()).toBeNull();
        });

        it('should return Admin when role is 9999 or Admin', () => {
            const token = createToken({ "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "9999" });
            saveUserSession(token);
            expect(getUserRole()).toBe("Admin");
        });

        it('should return Creator when role is 1 or Creator', () => {
            const token = createToken({ role: "Creator" });
            saveUserSession(token);
            expect(getUserRole()).toBe("Creator");
        });

        it('should return the raw role for other roles', () => {
            const token = createToken({ role: "User" });
            saveUserSession(token);
            expect(getUserRole()).toBe("User");
        });
    });

    describe('getUsernameFromToken', () => {
        it('should return null when user_info is absent', () => {
            expect(getUsernameFromToken()).toBeNull();
        });

        it('should return the username from claims', () => {
            const token = createToken({ "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "testuser1" });
            saveUserSession(token);
            expect(getUsernameFromToken()).toBe("testuser1");
        });
    });

    describe('isTokenExpired', () => {
        beforeAll(() => {
            jest.useFakeTimers();
            jest.setSystemTime(new Date('2024-01-01T12:00:00Z').getTime());
        });

        afterAll(() => {
            jest.useRealTimers();
        });

        it('should return true when user_info is absent', () => {
            expect(isTokenExpired()).toBe(true);
        });

        it('should return true when token is expired', () => {
            const currentTime = Date.now() / 1000;
            const token = createToken({ exp: currentTime - 100 });
            saveUserSession(token);
            expect(isTokenExpired()).toBe(true);
        });

        it('should return false when token is valid', () => {
            const currentTime = Date.now() / 1000;
            const token = createToken({ exp: currentTime + 3600 });
            saveUserSession(token);
            expect(isTokenExpired()).toBe(false);
        });

        it('should return true when token is within 10 seconds of expiring', () => {
            const currentTime = Date.now() / 1000;
            const token = createToken({ exp: currentTime + 5 });
            saveUserSession(token);
            expect(isTokenExpired()).toBe(true);
        });

        it('should return false when token expires in exactly 10 seconds (boundary)', () => {
            const currentTime = Date.now() / 1000;
            const token = createToken({ exp: currentTime + 10 });
            saveUserSession(token);
            expect(isTokenExpired()).toBe(false);
        });

        it('should return true when token has no exp claim', () => {
            const token = createToken({ role: "User" });
            saveUserSession(token);
            expect(isTokenExpired()).toBe(true);
        });
    });
});
