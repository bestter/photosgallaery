    import React, { useState, useEffect, useCallback } from 'react';
    import api from '../api';
    import toast from 'react-hot-toast';
    import ImageModal from './ImageModal'; // On importe ton modal !

    const UserLikesGallery = ({ username, token }) => {
        const [likedPhotos, setLikedPhotos] = useState([]);
        const [isLoading, setIsLoading] = useState(true);
        const [selectedPhoto, setSelectedPhoto] = useState(null);

        const fetchLikedPhotos = useCallback(async () => {
        setIsLoading(true);
        try {
            const response = await api.get(`/photos/user/${username}/likes`);
            setLikedPhotos(response.data);
        } catch (error) {
            console.error("Erreur de chargement des likes", error);
            toast.error("Impossible de charger les photos aimées.");
        } finally {
            setIsLoading(false);
        }
    }, [username]);

    // 3. Ton useEffect qui UTILISE la fonction (il la voit bien maintenant !)
    useEffect(() => {
        if (username) {
            fetchLikedPhotos();
        }
    }, [username, fetchLikedPhotos]);

        const imageBaseUrl = process.env.NODE_ENV === 'development' ? 'http://localhost:5020' : '';

        if (isLoading) {
            return (
                <div className="flex justify-center items-center p-12">
                    <div className="w-10 h-10 border-4 border-gray-200 border-t-teal-600 rounded-full animate-spin"></div>
                </div>
            );
        }

        return (
            <div className="w-full">
                <h3 className="text-xl font-bold mb-6 text-gray-800 border-b pb-2">
                    Coups de cœur de {username} ❤️
                </h3>

                {likedPhotos.length === 0 ? (
                    <div className="text-center p-8 bg-gray-50 rounded-xl border border-gray-100">
                        <p className="text-gray-500 italic">
                            {username} n'a pas encore aimé de photos.
                        </p>
                    </div>
                ) : (
                    <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                        {likedPhotos.map(photo => (
                            <div 
                                key={photo.id} 
                                className="relative group cursor-pointer overflow-hidden rounded-xl shadow-sm hover:shadow-md transition-all aspect-square bg-gray-100"
                                onClick={() => setSelectedPhoto(photo)}
                            >
                                <img 
                                    src={`${imageBaseUrl}${photo.url || photo.fileName}`} // Adapte selon le nom de ta propriété
                                    alt={`Aimée par ${username}`}
                                    className="w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
                                />
                                {/* Petit overlay au survol pour faire joli */}
                                <div className="absolute inset-0 bg-black/0 group-hover:bg-black/20 transition-colors duration-300" />
                            </div>
                        ))}
                    </div>
                )}

                {/* Ton fameux modal qui s'ouvre si on clique sur une photo */}
                {selectedPhoto && (
                    <ImageModal 
                        picture={selectedPhoto} 
                        onClose={() => setSelectedPhoto(null)} 
                        token={token}
                        // Si on supprime la photo depuis le modal, on rafraîchit la liste !
                        onDeleteSuccess={() => {
                            setSelectedPhoto(null);
                            fetchLikedPhotos();
                        }}
                    />
                )}
            </div>
        );
    };

    export default UserLikesGallery;