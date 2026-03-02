import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api';
import Button from './Button';

const Login = ({ setToken }) => { 
    const navigate = useNavigate(); // Initialisation de la fonction de navigation
    
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [success, setSuccess] = useState(''); // Nouvel état pour le message de succès

    const handleSubmit = async (e) => {
        e.preventDefault(); 
        await handleLogin(username, password);
    };

    const handleLogin = async (username, password) => {
        try {
            const response = await api.post('auth/login', {
                username,
                password
            });

            localStorage.setItem('token', response.data.token);
            
            if (setToken) {
                setToken(response.data.token);
            } else {
                console.warn("La fonction setToken n'a pas été fournie au composant Login.");
            }
            
            // 1. On affiche le message de succès
            setSuccess("Connexion réussie ! Redirection vers l'accueil...");
            
            // 2. On attend 2 secondes (2000 ms) puis on redirige vers l'accueil ('/')
            setTimeout(() => {
                navigate('/');
            }, 2000);

        } catch (error) {
            const errorMessage = error.response?.data?.message 
                      || (typeof error.response?.data === 'string' ? error.response.data : null)
                      || "Une erreur est survenue lors de la connexion.";

            console.error("Erreur de connexion", errorMessage);
            alert(errorMessage); 
        }
    };

    return (
        <div className="login-container" style={{ maxWidth: '400px', margin: '0 auto', padding: '20px' }}>
            <h2>Connexion</h2>
            
            {/* 3. Affichage du message de succès s'il y en a un */}
            {success && (
                <div style={{
                    color: '#2e7d32',
                    backgroundColor: '#e8f5e9',
                    padding: '10px',
                    borderRadius: '4px',
                    marginBottom: '15px',
                    textAlign: 'center',
                    fontWeight: 'bold'
                }}>
                    {success}
                </div>
            )}

            <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}> 
                <input 
                    type="text" 
                    placeholder="Nom d'utilisateur" 
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    required 
                    disabled={success !== ''} // Désactive l'input pendant la redirection
                    style={{ padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
                <input 
                    type="password" 
                    placeholder="Mot de passe" 
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required 
                    disabled={success !== ''} // Désactive l'input pendant la redirection
                    style={{ padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
                
                <Button 
                    type="submit"
                    size="md" 
                    variant="primary"
                    disabled={success !== ''} // Désactive le bouton pendant la redirection
                    style={{ marginTop: '10px' }}
                >
                    {success ? 'Redirection...' : 'Se connecter'}
                </Button>
            </form>
        </div>
    );
};

export default Login;