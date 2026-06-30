import axios from 'axios';
import toast from 'react-hot-toast';

// Ajoute automatiquement le token JWT s'il existe
const baseURL = process.env.NODE_ENV === 'development' 
    ? 'http://127.0.0.1:5020/api'  // 💻 Sur ton PC de développement (Port C#)
    : '/api';                      // 🌍 Sur ton serveur Linux Mint (Via Apache)

const axiosInstance = axios.create({
    baseURL: baseURL,
    withCredentials: true // 🛡️ Important pour envoyer les cookies HttpOnly automatiquement
});

axiosInstance.interceptors.request.use((config) => {
    // 🛡️ Securité de base pour identifier que la requête vient bien de l'app React
    config.headers['X-App-Client'] = 'PhotoApp-Web';

    // The JWT token is now handled via HttpOnly cookie,
    // so we don't attach it to the Authorization header from localStorage here.
    return config;
});

// Dans api.js
axiosInstance.interceptors.response.use(
    (response) => response,
    (error) => {
        if (!error.response) {
            toast.error(
                "Serveur injoignable. Le service est temporairement indisponible.", 
                { 
                    icon: '🔌', // Une petite prise débranchée
                    duration: 6000,
                    style: {
                        borderRadius: '10px',
                        background: '#1f2937', // Un gris très foncé chic (Tailwind gray-800)
                        color: '#fff',
                        border: '1px solid #374151'
                    },
                }
            );
        } 
        else // 2. LE SIÈGE ÉJECTABLE (Erreur 401 ou 403)
        if (error.response.status === 401 || error.response.status === 403) {
            if (window.location.pathname !== '/login') {
                localStorage.removeItem('user_info');
                
                // On supprime le toast.error ici car il sera tué par le rechargement.
                // À la place, on ajoute un paramètre caché dans l'URL :
                window.location.href = '/login?ejected=true'; 
            }
        }
        else if (error.response.status === 500) {
            toast.error("Erreur interne du serveur. Nos techniciens sont sur le coup !", { icon: '🔥' });
        }
        return Promise.reject(error);
    }
);

export default axiosInstance;
