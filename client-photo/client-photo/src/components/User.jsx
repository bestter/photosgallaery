import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import Gallery from './Gallery'; // Ton moteur principal !

const User = (token) => {
    const { username } = useParams();
    // On définit l'onglet actif par défaut sur "published"
    const [activeTab, setActiveTab] = useState('published');

    return (
        <div className="container mx-auto p-6 max-w-7xl">
            {/* En-tête du profil */}
            <div className="flex flex-col items-center mb-8">
                <div className="w-24 h-24 bg-teal-100 text-teal-600 rounded-full flex items-center justify-center text-4xl font-bold mb-4 shadow-sm">
                    {username.charAt(0).toUpperCase()}
                </div>
                <h2 className="text-3xl font-bold text-gray-800">
                    Profil de {username}
                </h2>
            </div>

            {/* Les Onglets de navigation */}
            <div className="flex justify-center gap-8 border-b border-gray-200 mb-8">
                <button
                    onClick={() => setActiveTab('published')}
                    className={`pb-3 text-lg font-medium transition-colors relative ${
                        activeTab === 'published' 
                            ? 'text-teal-600' 
                            : 'text-gray-500 hover:text-gray-700'
                    }`}
                >
                    🖼️ Ses publications
                    {/* La petite barre sous l'onglet actif */}
                    {activeTab === 'published' && (
                        <span className="absolute bottom-0 left-0 w-full h-0.5 bg-teal-600 rounded-t-md"></span>
                    )}
                </button>

                <button
                    onClick={() => setActiveTab('liked')}
                    className={`pb-3 text-lg font-medium transition-colors relative ${
                        activeTab === 'liked' 
                            ? 'text-teal-600' 
                            : 'text-gray-500 hover:text-gray-700'
                    }`}
                >
                    ❤️ Ses coups de cœur
                    {activeTab === 'liked' && (
                        <span className="absolute bottom-0 left-0 w-full h-0.5 bg-teal-600 rounded-t-md"></span>
                    )}
                </button>
            </div>

            {/* LE MOTEUR GALLERY EST APPELÉ ICI */}
            <div className="min-h-[50vh]">
                {activeTab === 'published' ? (
    <Gallery 
        customEndpoint={`/photos/user/${username}`} 
        title={null} // On cache le titre de la galerie
        hideUpload={true} // On cache le formulaire d'upload
        token={token}
    />
) : (
    <Gallery 
        customEndpoint={`/photos/user/${username}/likes`} 
        title={null} 
        hideUpload={true} 
        token={token}
    />
)}
            </div>
        </div>
    );
};

export default User;