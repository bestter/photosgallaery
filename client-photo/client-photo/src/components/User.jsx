import React from 'react';
import { useParams } from 'react-router-dom'; // 👈 Important pour lire l'URL
import UserLikesGallery from './UserLikesGallery'; 

const User = () => {
    // On extrait le nom d'utilisateur directement depuis l'URL (/user/:username)
    const { username } = useParams();
    const token = localStorage.getItem('token');

    return (
        <div className="container mx-auto p-6 max-w-5xl">
            <h2 className="text-3xl font-bold mb-8 text-gray-800 border-b pb-4">
                Profil de {username}
            </h2>

            {/* Ton composant s'occupe de faire le reste du travail ! */}
            <UserLikesGallery username={username} token={token} />
        </div>
    );
};

export default User;