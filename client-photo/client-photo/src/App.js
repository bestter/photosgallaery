import React, { useState } from 'react';
import Gallery from './components/Gallery';
import Upload from './components/Upload';
import api from './api';
import Button from './components/Button';

function App() {
    // Simulation simple de Login pour l'exemple
    // Dans la réalité, vous auriez un formulaire Username/Password appelant /api/auth/login
    const [token, setToken] = useState(localStorage.getItem('token'));
   // 1. On crée le déclencheur (un simple nombre qui commence à 0)
    const [refreshGallery, setRefreshGallery] = useState(0);


    const handleLogin = async (username, password) => {
        debugger;
        try {
            const response = await api.post('auth/login', {
                username,
                password
            });

            // On stocke le jeton "eyJ..." retourné par le serveur
            localStorage.setItem('token', response.data.token);
            setToken(response.data.token);
            console.log("Connexion réussie ! Jeton enregistré.");
        } catch (error) {
            console.error("Erreur de connexion", error);
        }
    };

    const logout = () => {
        localStorage.removeItem('token');
        setToken(null);
    };

 

    // 2. Cette fonction sera appelée par le composant Upload quand c'est fini
    const handleUploadSuccess = () => {
        setRefreshGallery(prev => prev + 1); // On ajoute +1 au compteur
    };

    return (
        <div>
            <header className="p-6 max-w-sm mx-auto bg-white rounded-xl shadow-lg flex items-center gap-x-4">
                <h1>Mon Gestionnaire Photo</h1>
                <div className="text-xl font-medium text-black">
                    {token ? (
                        <Button size="sm" variant="outline" onClick={logout}>Se déconnecter</Button>
                    ) : (
                            <Button size="sm" variant="outline" onClick={() => { handleLogin('admin', 'password'); } }>Se connecter (Test)</Button>
                    )}
                </div>
            </header>
            
            <main className="block">
                {/* Le composant Upload n'est affiché que si on a un token, 
                    mais de toute façon le backend rejettera la requête sans token valide */}
                {token && <Upload onUploadSuccess={handleUploadSuccess} />}
                
                <Gallery refreshTrigger={refreshGallery} token={token} />
            </main>
        </div>
    );
}

export default App;