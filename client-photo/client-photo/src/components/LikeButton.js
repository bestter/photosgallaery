import React, { useState } from 'react';
import api from '../api';
import toast from 'react-hot-toast';

const LikeButton = ({ photoId, initialIsLiked = false, initialLikesCount = 0 }) => {
    // On garde l'état localement pour que ça réagisse vite
    const [isLiked, setIsLiked] = useState(initialIsLiked);
    const [likesCount, setLikesCount] = useState(initialLikesCount);
    const [isLoading, setIsLoading] = useState(false);

    const handleToggleLike = async () => {
        // 1. Mise à jour optimiste (on change l'interface tout de suite !)
        const previousIsLiked = isLiked;
        const previousLikesCount = likesCount;

        setIsLiked(!isLiked);
        setLikesCount(isLiked ? likesCount - 1 : likesCount + 1);
        setIsLoading(true);

        try {
            // 2. Appel à l'API en arrière-plan
            await api.post(`/photos/${photoId}/like`);
            
        } catch (error) {
            // 3. En cas d'erreur (ex: perte de connexion, pas connecté), on annule !
            setIsLiked(previousIsLiked);
            setLikesCount(previousLikesCount);
            
            if (error.response?.status === 401) {
                toast.error("Tu dois être connecté pour aimer une photo !");
            } else {
                toast.error("Oups, impossible d'enregistrer ton j'aime.");
            }
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <button 
            onClick={handleToggleLike}
            disabled={isLoading}
            className="flex items-center gap-1.5 px-2 py-1 rounded-full transition-all duration-200 hover:bg-red-50 group"
            title={isLiked ? "Je n'aime plus" : "J'aime"}
        >
            <svg 
                xmlns="http://www.w3.org/2000/svg" 
                viewBox="0 0 24 24" 
                strokeWidth={1.5} 
                stroke="currentColor" 
                className={`w-5 h-5 transition-transform group-hover:scale-110 active:scale-95 ${
                    isLiked ? 'fill-red-500 text-red-500' : 'fill-none text-gray-500'
                }`}
            >
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 8.25c0-2.485-2.099-4.5-4.688-4.5-1.935 0-3.597 1.126-4.312 2.733-.715-1.607-2.377-2.733-4.313-2.733C5.1 3.75 3 5.765 3 8.25c0 7.22 9 12 9 12s9-4.78 9-12z" />
            </svg>
            
            <span className={`text-sm font-medium ${isLiked ? 'text-red-500' : 'text-gray-500'}`}>
                {likesCount > 0 ? likesCount : ''}
            </span>
        </button>
    );
};

export default LikeButton;