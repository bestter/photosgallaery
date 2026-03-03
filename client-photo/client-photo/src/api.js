import axios from 'axios';
import toast from 'react-hot-toast';

const API_URL = 'http://localhost:5020/api'; // Ajustez selon le port de votre backend

// Ajoute automatiquement le token JWT s'il existe
const axiosInstance = axios.create({
    baseURL: API_URL
});

axiosInstance.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

// Dans api.js
axiosInstance.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response && error.response.status === 401) {
            // On affiche un toast une seule fois pour toutes les erreurs 401
            toast.error("Session expirée. Déconnexion automatique...");
            localStorage.removeItem('token');
            window.location.href = '/login'; // Redirection forcée
        }
        return Promise.reject(error);
    }
);

export default axiosInstance;