import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api'; 
import Button from './Button';
import toast from 'react-hot-toast';

const Register = () => {
    const navigate = useNavigate();

    const [formData, setFormData] = useState({
        username: '',
        email: '',
        password: '',
        confirmPassword: ''
    });
    
    const [isLoading, setIsLoading] = useState(false);

    const handleChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();

        // Validation locale avant l'envoi
        if (formData.password !== formData.confirmPassword) {
            toast.error("Les mots de passe ne correspondent pas.");
            return;
        }

        setIsLoading(true);

        // Utilisation de toast.promise pour une expérience fluide
        toast.promise(
            api.post('auth/register', {
                username: formData.username,
                email: formData.email,
                password: formData.password
            }),
            {
                loading: 'Création de votre compte...',
                success: (response) => {
                    // On attend un peu pour que l'utilisateur lise le message avant de rediriger
                    setTimeout(() => navigate('/login'), 2000);
                    return "Compte créé ! Redirection vers la connexion...";
                },
                error: (err) => {
                    setIsLoading(false);
                    return err.response?.data?.message 
                        || (typeof err.response?.data === 'string' ? err.response.data : null)
                        || "Une erreur est survenue lors de l'inscription.";
                }
            }
        );
    };

    return (
        <div className="register-container" style={{ maxWidth: '400px', margin: '50px auto', padding: '20px', textAlign: 'center' }}>
            <h2 className="text-2xl font-bold mb-6">Créer un compte</h2>
            
            <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
                <input 
                    name="username" 
                    value={formData.username}
                    placeholder="Nom d'utilisateur" 
                    onChange={handleChange} 
                    required 
                    disabled={isLoading}
                    style={{ padding: '10px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
                <input 
                    type="email" 
                    name="email" 
                    value={formData.email}
                    placeholder="Courriel" 
                    onChange={handleChange} 
                    required 
                    disabled={isLoading}
                    style={{ padding: '10px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
                <input 
                    type="password" 
                    name="password" 
                    value={formData.password}
                    placeholder="Mot de passe" 
                    onChange={handleChange} 
                    required 
                    disabled={isLoading}
                    style={{ padding: '10px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
                <input 
                    type="password" 
                    name="confirmPassword" 
                    value={formData.confirmPassword}
                    placeholder="Confirmer le mot de passe" 
                    onChange={handleChange} 
                    required 
                    disabled={isLoading}
                    style={{ padding: '10px', borderRadius: '4px', border: '1px solid #ccc' }}
                />
                
                <Button 
                    type="submit"                    
                    disabled={isLoading}
                    size="md" 
                    variant={isLoading ? "outline" : "primary"}
                >
                   {isLoading ? 'Patientez...' : "S'inscrire"}
                </Button>
            </form>
        </div>
    );
};

export default Register;