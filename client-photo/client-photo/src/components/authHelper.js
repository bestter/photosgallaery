import { jwtDecode } from "jwt-decode";

export const getUserRole = (token) => {
    if (!token) return null;
    try {
        const decoded = jwtDecode(token);
        // Attention : .NET Core utilise souvent une URL longue par défaut pour le nom du claim "Role"
        return decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role;
    } catch (error) {
        return null;
    }
};

export const getUsernameFromToken = (token) => {
    if (!token) return null;
    try {
        const decoded = jwtDecode(token);
        // .NET utilise souvent ClaimTypes.Name qui devient cette URL dans le JSON :
        return decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || decoded.unique_name || decoded.sub;
    } catch (error) {
        return null;
    }
};

export const isTokenExpired = (token) => {
    if (!token || token === "null" || token === "undefined") return true;
    try {
        const decoded = jwtDecode(token);
        const currentTime = Date.now() / 1000;
        
        // On ajoute une petite marge de 10 secondes pour être sûr
        return decoded.exp < (currentTime - 10); 
    } catch (error) {
        return true; 
    }
};