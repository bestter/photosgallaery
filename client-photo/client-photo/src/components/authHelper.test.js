import { getUserRole, getUsernameFromToken, isTokenExpired } from './authHelper';

const createToken = (payload) => {
    const header = btoa(JSON.stringify({ alg: "HS256", typ: "JWT" }));
    // Handle unicode in btoa for payload if needed, but for simple ascii JSON it's fine.
    const body = btoa(JSON.stringify(payload));
    return `${header}.${body}.signature`;
};

describe('authHelper', () => {
    describe('getUserRole', () => {
        it('should return null when token is empty or null', () => {
            expect(getUserRole(null)).toBeNull();
            expect(getUserRole('')).toBeNull();
            expect(getUserRole(undefined)).toBeNull();
        });

        it('should return null when token is invalid and jwtDecode throws an error', () => {
            expect(getUserRole('invalid.token.string')).toBeNull();
        });

        it('should return Admin when role is 9999 or Admin', () => {
            const token1 = createToken({ "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "9999" });
            const token2 = createToken({ role: "Admin" });
            expect(getUserRole(token1)).toBe("Admin");
            expect(getUserRole(token2)).toBe("Admin");
        });

        it('should return Creator when role is 1 or Creator', () => {
            const token1 = createToken({ "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "1" });
            const token2 = createToken({ role: "Creator" });
            expect(getUserRole(token1)).toBe("Creator");
            expect(getUserRole(token2)).toBe("Creator");
        });

        it('should return the raw role for other roles', () => {
            const token = createToken({ role: "User" });
            expect(getUserRole(token)).toBe("User");
        });
    });

    describe('getUsernameFromToken', () => {
        it('should return null when token is empty or null', () => {
            expect(getUsernameFromToken(null)).toBeNull();
        });

        it('should return null when token is invalid and jwtDecode throws an error', () => {
            expect(getUsernameFromToken('invalid.token.string')).toBeNull();
        });

        it('should return the username from claims', () => {
            const token1 = createToken({ "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "testuser1" });
            const token2 = createToken({ unique_name: "testuser2" });
            const token3 = createToken({ sub: "testuser3" });

            expect(getUsernameFromToken(token1)).toBe("testuser1");
            expect(getUsernameFromToken(token2)).toBe("testuser2");
            expect(getUsernameFromToken(token3)).toBe("testuser3");
        });
    });

    describe('isTokenExpired', () => {
        it('should return true when token is empty or string null/undefined', () => {
            expect(isTokenExpired(null)).toBe(true);
            expect(isTokenExpired('')).toBe(true);
            expect(isTokenExpired('null')).toBe(true);
            expect(isTokenExpired('undefined')).toBe(true);
        });

        it('should return true when token is invalid and jwtDecode throws an error', () => {
            expect(isTokenExpired('invalid.token.string')).toBe(true);
        });

        it('should return true when token is expired', () => {
            const currentTime = Date.now() / 1000;
            const token = createToken({ exp: currentTime - 100 }); // Expired 100 seconds ago
            expect(isTokenExpired(token)).toBe(true);
        });

        it('should return false when token is valid', () => {
            const currentTime = Date.now() / 1000;
            const token = createToken({ exp: currentTime + 3600 }); // Expires in 1 hour
            expect(isTokenExpired(token)).toBe(false);
        });

        it('should return true when token is within 10 seconds of expiring', () => {
            const currentTime = Date.now() / 1000;
            const token = createToken({ exp: currentTime + 5 }); // Expires in 5 seconds
            expect(isTokenExpired(token)).toBe(true);
        });

        it('should return false when token expires in exactly 10 seconds (boundary)', () => {
            // Because Date.now() is called in the test and then in the function,
            // the function's currentTime might be slightly larger, causing exp < currentTime + 10 to be true.
            // So we use 10.5 seconds to safely test the boundary without mocking Date.now
            const currentTime = Date.now() / 1000;
            const token = createToken({ exp: currentTime + 10.5 }); // Just above the boundary
            expect(isTokenExpired(token)).toBe(false);
        });

        it('should return false when token has no exp claim', () => {
            const token = createToken({ role: "User" }); // No exp claim
            expect(isTokenExpired(token)).toBe(false);
        });
    });
});
