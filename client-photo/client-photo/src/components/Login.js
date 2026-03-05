import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import api from '../api';
import Button from './Button';
import toast from 'react-hot-toast';
import { getUserRole } from './authHelper';

const Login = ({ setToken }) => { 
    const navigate = useNavigate();
    const location = useLocation(); // Pour lire l'URL
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [isPending, setIsPending] = useState(false);
    const [errorMessage, setErrorMessage] = useState(null); // NOUVEAU : État pour le message d'erreur persistant

    // LE NOUVEAU DÉTECTEUR D'ÉJECTION 🚨
    useEffect(() => {
        // On analyse l'URL (ex: /login?ejected=true)
        const queryParams = new URLSearchParams(location.search);
        if (queryParams.get('ejected') === 'true') {
            // On affiche directement l'erreur dans ton beau bloc rouge !
            setErrorMessage("⛔ Votre session a expiré ou votre accès a été révoqué par l'administration.");
        }
    }, [location]);

    const handleSubmit = async (e) => {
        e.preventDefault(); 
        
        setErrorMessage(null); // On efface l'ancienne erreur à chaque nouvelle tentative
        setIsPending(true);

        toast.promise(
            api.post('auth/login', { username, password }),
            {
                loading: 'Connexion en cours...',
                success: (response) => {
                    localStorage.setItem('token', response.data.token);
                    if (setToken) setToken(response.data.token);
                    
                    const role = getUserRole(response.data.token);                    
                    if (role === 'Admin') {
                        toast((t) => (
                            <span className="flex items-center gap-2">
                                Il y a des signalements en attente !
                                <button 
                                    onClick={() => {
                                        toast.dismiss(t.id);
                                        navigate('/admin');
                                    }}
                                    className="bg-purple-600 text-white px-2 py-1 rounded text-xs font-bold hover:bg-purple-700"
                                >
                                    Voir
                                </button>
                            </span>
                        ), { duration: 6000, icon: '🚨', position: 'top-center' });
                    }

                    setTimeout(() => navigate('/'), 1000);
                    return `Ravi de vous revoir, ${username} !`;
                },
                error: (error) => {
                    setIsPending(false);
                    setPassword(''); // UX : On vide le mot de passe

                    // Détermination du message d'erreur
                    let msg = "Identifiants invalides ou erreur serveur.";
                    
                    if (error.response?.status === 403) {
                        msg = `⛔ ${error.response.data.message || "Ce compte a été suspendu par l'administration."}`;
                    } else if (error.response?.status === 401) {
                        msg = "🔒 Nom d'utilisateur ou mot de passe incorrect.";
                    } else if (error.response?.data?.message) {
                        msg = error.response.data.message;
                    }

                    // On sauvegarde le message pour l'afficher DANS le formulaire
                    setErrorMessage(msg); 
                    
                    // On le retourne aussi pour le toast (double affichage)
                    return msg; 
                },
            }
        );
    };

    return (
        <div className="login-container" style={{ maxWidth: '400px', margin: '50px auto', padding: '20px', textAlign: 'center' }}>
            <h2 className="text-2xl font-bold mb-6">Connexion</h2>
            
            <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}> 
                <input 
                    type="text" 
                    placeholder="Nom d'utilisateur" 
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    required 
                    disabled={isPending}
                    className="p-2 border rounded focus:ring-2 focus:ring-teal-500 outline-none"
                    style={{ padding: '10px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
                <input 
                    type="password" 
                    placeholder="Mot de passe" 
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required 
                    disabled={isPending}
                    className="p-2 border rounded focus:ring-2 focus:ring-teal-500 outline-none"
                    style={{ padding: '10px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
                
                <Button 
                    type="submit"
                    size="md" 
                    variant="primary"
                    disabled={isPending}
                    style={{ marginTop: '10px' }}
                >
                    {isPending ? 'Chargement...' : 'Se connecter'}
                </Button>

                {/* LA NOUVELLE ZONE D'ERREUR BIEN VISIBLE */}
                {errorMessage && (
                    <div 
                        className="animate-pulse" 
                        style={{ 
                            marginTop: '10px', 
                            padding: '12px', 
                            backgroundColor: '#fee2e2', // Un fond rouge clair
                            color: '#b91c1c', // Un texte rouge foncé
                            border: '1px solid #f87171', 
                            borderRadius: '6px', 
                            fontWeight: '600',
                            fontSize: '0.9rem',
                            textAlign: 'left'
                        }}
                    >
                        {errorMessage}
                    </div>
                )}
            </form>
        </div>
    );
};

export default Login;