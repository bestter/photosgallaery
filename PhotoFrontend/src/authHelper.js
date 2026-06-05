import { jwtDecode } from "jwt-decode";

export const saveUserSession = (token) => {
    if (!token) return;
    try {
        const decoded = jwtDecode(token);
        
        let role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role;
        if (role === "9999" || role === "Admin") role = "Admin";
        else if (role === "1" || role === "Creator") role = "Creator";

        const username = decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || decoded.unique_name || decoded.sub;

        const userInfo = {
            role: role,
            username: username,
            exp: decoded.exp
        };

        localStorage.setItem("user_info", JSON.stringify(userInfo));
    } catch (error) {
        console.error("Error saving user session:", error);
    }
};

export const clearUserSession = () => {
    localStorage.removeItem("user_info");
};

export const getUserRole = () => {
    try {
        const userInfoStr = localStorage.getItem("user_info");
        if (!userInfoStr) return null;
        const userInfo = JSON.parse(userInfoStr);
        return userInfo.role;
    } catch (error) {
        return null;
    }
};

export const getUsernameFromToken = () => {
    try {
        const userInfoStr = localStorage.getItem("user_info");
        if (!userInfoStr) return null;
        const userInfo = JSON.parse(userInfoStr);
        return userInfo.username;
    } catch (error) {
        return null;
    }
};

export const isTokenExpired = () => {
    try {
        const userInfoStr = localStorage.getItem("user_info");
        if (!userInfoStr) return true;

        const userInfo = JSON.parse(userInfoStr);
        if (!userInfo.exp) return true;
        
        const currentTime = Date.now() / 1000;
        // Marge de sécurité de 10 secondes : on le considère expiré juste avant sa vraie fin
        return userInfo.exp < (currentTime + 10);
    } catch (error) {
        return true; 
    }
};
