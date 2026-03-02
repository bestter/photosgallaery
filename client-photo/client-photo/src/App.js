import React, { useState } from 'react';
import { BrowserRouter as Router, Routes, Route, Link, useNavigate } from 'react-router-dom';
import Gallery from './components/Gallery';
import Login from './components/Login';
import Register from './components/Register';
import Upload from './components/Upload';
import api from './api';
import Button from './components/Button';
import logo from './photoGalleryLogo2.png';

// 💡 ASTUCE : L'intercepteur doit être en dehors du composant App 
// pour ne pas être recréé à chaque rendu de la page.
api.interceptors.request.use(config => {
    const token = localStorage.getItem('token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

function App() {
    const [token, setToken] = useState(localStorage.getItem('token'));
    const [refreshGallery, setRefreshGallery] = useState(0);

    // On garde ta fonction, mais elle sera maintenant utilisée PAR le composant Login
    const handleLogin = async (username, password) => {
        try {
            const response = await api.post('auth/login', { username, password });
            localStorage.setItem('token', response.data.token);
            setToken(response.data.token);
            console.log("Connexion réussie ! Jeton enregistré.");
            return true; // Pour dire au composant Login que ça a marché
        } catch (error) {
            console.error("Erreur de connexion", error);
            return false;
        }
    };

    const logout = () => {
        localStorage.removeItem('token');
        setToken(null);
    };

    const handleUploadSuccess = () => {
        setRefreshGallery(prev => prev + 1);
    };

    return (
        <Router>
            <div>
                <header className="w-full bg-white border-b border-gray-200 shadow-sm">
                    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-20 flex items-center justify-between">
                        
                        <div className="flex items-center gap-x-4">
                            <div className="relative h-12 w-12 flex-shrink-0">
                                {/* J'ai ajouté un Link sur le logo pour revenir à l'accueil facilement */}
                                <Link to="/">
                                    <img src={logo} alt="Logo" className="h-full w-full object-contain" />
                                </Link>
                            </div>
                            <h1 className="text-2xl font-bold tracking-tight text-gray-900">
                                Mon Gestionnaire <span className="text-cyan-600">Photo</span>
                            </h1>
                        </div>

                        {/* Section Authentification */}
<div className="flex items-center gap-x-2">
    {token ? (
        <Button 
            size="sm" 
            variant="outline" 
            className="border-gray-300 hover:bg-gray-50"
            onClick={logout}
        >
            Se déconnecter
        </Button>
    ) : (
        <>
            {/* Bouton Connexion */}
            <Link to="/login">
                <Button 
                    size="sm" 
                    variant="outline"
                    className="border-cyan-600 text-cyan-600 hover:bg-cyan-50 bg-white"
                >
                    Connexion
                </Button>
            </Link>
            
            {/* Nouveau : Bouton S'inscrire */}
            <Link to="/register">
                <Button 
                    size="sm" 
                    className="bg-cyan-600 hover:bg-cyan-700 text-white"
                >
                    S'inscrire
                </Button>
            </Link>
        </>
    )}
</div>
                    </div>
                </header>
                
                <main className="block p-4">
                    {/* 🛑 Configuration des routes (les pages) */}
                    <Routes>
                        {/* Page d'accueil avec l'Upload et la Galerie */}
                        <Route path="/" element={
                            <>
                                {token && <Upload onUploadSuccess={handleUploadSuccess} />}
                                <Gallery refreshTrigger={refreshGallery} token={token} />
                            </>
                        } />
                        
                        {/* Page de connexion qui affiche enfin ton composant ! */}
                        <Route path="/login" element={<Login onLoginSubmit={handleLogin}  setToken={setToken} />} />
                                                
                        {/* Page d'inscription (au cas où tu en aies besoin) */}
                        <Route path="/register" element={<Register />} />
                    </Routes>
                </main>
            </div>
        </Router>
    );
}

export default App;