import axios from 'axios';

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

export default axiosInstance;