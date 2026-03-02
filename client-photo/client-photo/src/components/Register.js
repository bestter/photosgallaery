import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api'; 
import Button from './Button';

const Register = () => {
    const navigate = useNavigate(); // Initialisation de la fonction de navigation

    const [formData, setFormData] = useState({
        username: '',
        email: '',
        password: '',
        confirmPassword: ''
    });
    
    const [error, setError] = useState('');
    const [success, setSuccess] = useState(''); // Nouvel état pour le succès
    const [isLoading, setIsLoading] = useState(false); // Pour bloquer le bouton pendant la requête

    const handleChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setSuccess('');

        // Validation simple
        if (formData.password !== formData.confirmPassword) {
            setError("Les mots de passe ne correspondent pas.");
            return;
        }

        setIsLoading(true);

        try {
            const response = await api.post('auth/register', {
                username: formData.username,
                email: formData.email,
                password: formData.password
            });

            console.log("Compte créé avec succès !", response.data);
            
            // On affiche le message de succès
            setSuccess("Compte créé avec succès ! Redirection vers la connexion...");
            
            // On attend 3 secondes (3000 ms) puis on redirige vers /login
            setTimeout(() => {
                navigate('/login');
            }, 3000);

        } catch (err) {
            setError(err.response?.data?.message || "Une erreur est survenue lors de l'inscription.");
            setIsLoading(false); // On réactive le bouton seulement s'il y a une erreur
        }
    };

    const styles = {
        button: {
            backgroundColor: (isLoading || success) ? '#a9dfdf' : '#008b8b', // Couleur plus claire si désactivé
            color: 'white',
            padding: '10px 20px',
            border: 'none',
            borderRadius: '4px',
            cursor: (isLoading || success) ? 'not-allowed' : 'pointer',
            width: '100%',
            marginTop: '10px'
        },
        errorBox: {
            color: '#d32f2f',
            backgroundColor: '#ffebee',
            padding: '10px',
            borderRadius: '4px',
            marginBottom: '15px'
        },
        successBox: {
            color: '#2e7d32',
            backgroundColor: '#e8f5e9',
            padding: '10px',
            borderRadius: '4px',
            marginBottom: '15px'
        }
    };

    return (
        <div className="register-container" style={{ maxWidth: '400px', margin: '0 auto', padding: '20px' }}>
            <h2>Créer un compte</h2>
            
            {/* Affichage dynamique des messages */}
            {error && <div style={styles.errorBox}>{error}</div>}
            {success && <div style={styles.successBox}>{success}</div>}
            
            <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                <input 
                    name="username" 
                    placeholder="Nom d'utilisateur" 
                    onChange={handleChange} 
                    required 
                    style={{ padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
                    disabled={success !== ''} // Désactive l'input pendant la redirection
                />
                <input 
                    type="email" 
                    name="email" 
                    placeholder="Courriel" 
                    onChange={handleChange} 
                    required 
                    style={{ padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
                    disabled={success !== ''}
                />
                <input 
                    type="password" 
                    name="password" 
                    placeholder="Mot de passe" 
                    onChange={handleChange} 
                    required 
                    style={{ padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
                    disabled={success !== ''}
                />
                <input 
                    type="password" 
                    name="confirmPassword" 
                    placeholder="Confirmer le mot de passe" 
                    onChange={handleChange} 
                    required 
                    style={{ padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
                    disabled={success !== ''}
                />
                <Button 
                    type="submit"                     
                    disabled={isLoading || success !== ''}
                    size="md" 
                    variant={isLoading || success !== '' ?  "primary": "outline"} 
              >
                   {isLoading ? 'Création en cours...' : success ? 'Redirection...' : "S'inscrire"}
              </Button>
            </form>
        </div>
    );
};

export default Register;