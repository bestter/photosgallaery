import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getUserRole } from './authHelper'; // L'utilitaire qu'on a créé pour décoder le token
import Button from './Button';

const Navbar = ({ token, setToken }) => {
    const navigate = useNavigate();
    
    // On extrait le rôle à partir du token (sera null si l'usager n'est pas connecté)
    const role = getUserRole(token);

    const handleLogout = () => {
        // On supprime le token du navigateur
        localStorage.removeItem('token');
        // On met à jour l'état global (ce qui va cacher les boutons privés)
        if (setToken) setToken(null);
        
        // On ramène l'utilisateur à l'accueil
        navigate('/');
    };

    return (
        <nav className="bg-white shadow-md border-b border-gray-100">
            <div className="container mx-auto px-6 py-4 flex justify-between items-center">
                
                
{/* Section Gauche : Le Logo / Titre qui ramène à l'accueil */}
<Link to="/" className="flex items-center gap-3 hover:opacity-80 transition-opacity">
    <span className="text-2xl font-extrabold text-teal-600">
        Ma Galerie
    </span>
    <img 
        src="/photoGalleryLogo2.png" 
        alt="Logo MaGalerie" 
        className="h-10 w-auto object-contain" 
    />
</Link>
                {/* Section Droite : Les contrôles dynamiques */}
                <div className="flex items-center gap-4">
                    
                    {/* VISITEUR : Pas de token */}
                    {!token ? (
                        <>
                            <Link to="/login" className="text-gray-600 hover:text-teal-600 font-medium transition-colors">
                                Se connecter
                            </Link>
                            <Link to="/register">
                                <Button size="sm" variant="primary">S'inscrire</Button>
                            </Link>
                        </>
                    ) : (
                        /* MEMBRE CONNECTÉ : A un token */
                        <>
                            {/* LE BOUTON SECRET : Uniquement si le rôle est Admin */}
                            {role === 'Admin' && (
                                <Link to="/admin">
                                    <Button size="sm" className="bg-purple-600 text-white hover:bg-purple-700 border-none flex items-center gap-2">
                                        👑 Tableau de bord
                                    </Button>
                                </Link>
                            )}

                            {/* BOUTON DÉCONNEXION : Pour tous les membres connectés */}
                            <Button size="sm" variant="outline" onClick={handleLogout} className="border-red-200 text-red-600 hover:bg-red-50">
                                Déconnexion
                            </Button>
                        </>
                    )}
                    
                </div>
            </div>
        </nav>
    );
};

export default Navbar;