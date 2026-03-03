import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api';
import Button from './Button';
import toast from 'react-hot-toast';

const Login = ({ setToken }) => { 
    const navigate = useNavigate();
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [isPending, setIsPending] = useState(false); // État pour bloquer le formulaire

    const handleSubmit = async (e) => {
        e.preventDefault(); 
        setIsPending(true);

        // Utilisation de toast.promise pour tout gérer d'un coup
        toast.promise(
            api.post('auth/login', { username, password }),
            {
                loading: 'Connexion en cours...',
                success: (response) => {
                    localStorage.setItem('token', response.data.token);
                    if (setToken) setToken(response.data.token);
                    
                    // Redirection rapide après le succès
                    setTimeout(() => navigate('/'), 1000);
                    return `Ravi de vous revoir, ${username} !`;
                },
                error: (error) => {
                    setIsPending(false);
                    return error.response?.data?.message 
                        || "Identifiants invalides ou erreur serveur.";
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
            </form>
        </div>
    );
};

export default Login;