import { jwtDecode } from "jwt-decode";

export const getUserRole = (token) => {
    if (!token) return null;
    try {
        const decoded = jwtDecode(token);
        // On récupère la valeur brute du jeton
        let rawRole = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role;
        console.log("Raw role from token:", rawRole);
        // On fait la traduction automatique (bilingue texte/chiffre) !
        if (rawRole === "9999" || rawRole === "Admin") return "Admin";
        if (rawRole === "1" || rawRole === "Creator") return "Creator";
        
        return rawRole; // Par défaut, on retourne ce qu'on a trouvé (ex: "User" ou "0")
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
        console.log("Token expiration time:", decoded.exp, "Current time:", currentTime);
        
        // Marge de sécurité de 10 secondes : on le considère expiré juste avant sa vraie fin
        return decoded.exp < (currentTime + 10); 
    } catch (error) {
        return true; 
    }
};