import axios from 'axios';
import toast from 'react-hot-toast';

// Automatically attaches the JWT token if present
const baseURL = process.env.NODE_ENV === 'development' 
    ? 'http://127.0.0.1:5020/api'  // 💻 Sur ton PC de développement (Port C#)
    : '/api';                      // 🌍 Sur ton serveur Linux Mint (Via Apache)

let csrfToken = null;
let csrfTokenPromise = null;

export const fetchCsrfToken = async (force = false) => {
    if (csrfToken && !force) return csrfToken;
    if (csrfTokenPromise && !force) return csrfTokenPromise;

    csrfTokenPromise = axios.get(`${baseURL}/Auth/csrf-token`, { withCredentials: true })
        .then(response => {
            csrfToken = response.data.token;
            return csrfToken;
        })
        .catch(error => {
            console.error('Failed to fetch CSRF token:', error);
            csrfToken = null;
            return null;
        })
        .finally(() => {
            csrfTokenPromise = null;
        });

    return csrfTokenPromise;
};

const axiosInstance = axios.create({
    baseURL: baseURL,
    withCredentials: true // 🛡️ Important pour envoyer les cookies HttpOnly automatiquement
});

axiosInstance.interceptors.request.use((config) => {
    // 🛡️ Securité de base pour identifier que la requête vient bien de l'app React
    config.headers['X-App-Client'] = 'PhotoApp-Web';

    if (csrfToken) {
        config.headers['X-CSRF-TOKEN'] = csrfToken;
    }

    // The JWT token is now handled via HttpOnly cookie,
    // so we don't attach it to the Authorization header from localStorage here.
    return config;
});

// Dans api.js
axiosInstance.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        if (error.response && originalRequest && !originalRequest._retry) {
            const method = originalRequest.method ? originalRequest.method.toLowerCase() : '';
            const isMutating = ['post', 'put', 'delete', 'patch'].includes(method);

            const hasCsrfErrorHeader = error.response.headers && error.response.headers['x-csrf-error'];
            const isCsrfError = hasCsrfErrorHeader || 
                (isMutating && (error.response.status === 400 || error.response.status === 403) &&
                 (error.response.data?.code === 'INVALID_CSRF_TOKEN' || 
                  (typeof error.response.data === 'string' && error.response.data.toLowerCase().includes('antiforgery')) ||
                  (originalRequest.headers && originalRequest.headers['X-CSRF-TOKEN'])));

            if (isCsrfError) {
                originalRequest._retry = true;
                const newToken = await fetchCsrfToken(true);
                if (newToken) {
                    originalRequest.headers = originalRequest.headers || {};
                    originalRequest.headers['X-CSRF-TOKEN'] = newToken;
                    return axiosInstance(originalRequest);
                }
            }
        }

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
