import React, { useEffect, useState } from 'react';
import ImageModal from './ImageModal';
import Button from './Button';
import api from '../api';

// 1. On accepte la nouvelle prop "refreshTrigger"
const Gallery = ({ refreshTrigger, token }) => { 
    const [photos, setPhotos] = useState([]);
    const [picture, setPicture] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;
    
    // 1. NOUVEAU : Un compteur dédié aux suppressions
    const [deleteTrigger, setDeleteTrigger] = useState(0); 

    useEffect(() => {
        setIsLoading(true);
        api.get('/photos')
            .then(response => {
                setPhotos(response.data.reverse()); 
            })
            .catch(err => console.error("Erreur chargement photos", err))
            .finally(() => setIsLoading(false));
            
    // 2. MODIFICATION : On ajoute deleteTrigger pour que la galerie se recharge aussi à la suppression
    }, [refreshTrigger, deleteTrigger]);

    const getFileName = (url) => {
        if (!url) return '';
        return url.split('/').pop();
    };

    // LOGIQUE DE PAGINATION (Côté Frontend)
    const indexOfLastItem = currentPage * itemsPerPage;
    const indexOfFirstItem = indexOfLastItem - itemsPerPage;
    const currentPhotos = photos.slice(indexOfFirstItem, indexOfLastItem); // Les 10 photos de la page
    const totalPages = Math.ceil(photos.length / itemsPerPage);

    return (
        <div className="container mx-auto p-6">
            <h2 className="text-2xl font-bold mb-8 text-gray-800">Galerie Publique</h2>
            
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-6">
                
                {/* 1. ÉTAT DE CHARGEMENT : Affiche 10 carrés gris animés */}
                {isLoading ? (
                    Array.from({ length: 10 }).map((_, index) => (
                        <div key={`skeleton-${index}`} className="flex flex-col gap-2">
                            <div className="w-full aspect-square bg-gray-200 animate-pulse rounded-xl"></div>
                            <div className="h-4 bg-gray-200 animate-pulse rounded w-1/2 mt-1"></div>
                        </div>
                    ))
                ) : currentPhotos.length > 0 ? (
                    
                    /* 2. AFFICHAGE DES PHOTOS (si chargement terminé et photos présentes) */
                    currentPhotos.map(photo => (
                        <div key={photo.id} className="relative group flex flex-col">
                            <div 
                                className="relative overflow-hidden rounded-xl shadow-md cursor-pointer aspect-square bg-gray-100"
                                onClick={() => setPicture(photo)}
                            >
                                <img 
                                    src={photo.url} 
                                    alt={getFileName(photo.url)} 
                                    className="object-cover w-full h-full transition-transform duration-300 group-hover:scale-110" 
                                />
                                <div className="absolute bottom-0 left-0 right-0 bg-black/75 text-white text-xs p-3 transform translate-y-full transition-transform duration-300 group-hover:translate-y-0 flex items-center justify-center">
                                    <span className="truncate max-w-full" title={getFileName(photo.url)}>
                                        {getFileName(photo.url)}
                                    </span>
                                </div>
                            </div>
                            <p className="text-sm text-gray-500 mt-2 font-medium">
                                Par: <span className="text-brand">{photo.uploaderUsername}</span>
                            </p>
                        </div>
                    ))
                ) : (
                    /* 3. ÉTAT VIDE : Si aucune photo dans la base de données */
                    <p className="col-span-full text-center text-gray-500 py-10">
                        Aucune photo n'a été publiée pour le moment.
                    </p>
                )}

            </div>

            {/* 4. CONTRÔLES DE PAGINATION */}
            {!isLoading && photos.length > itemsPerPage && (
                <div className="flex items-center justify-center gap-4 mt-12">
                    <Button 
                        variant="outline" 
                        onClick={() => setCurrentPage(prev => Math.max(prev - 1, 1))}
                        disabled={currentPage === 1}
                        className={currentPage === 1 ? "opacity-50 cursor-not-allowed" : ""}
                    >
                        Précédent
                    </Button>
                    
                    <span className="text-sm font-medium text-gray-600">
                        Page {currentPage} sur {totalPages}
                    </span>
                    
                    <Button 
                        variant="outline" 
                        onClick={() => setCurrentPage(prev => Math.min(prev + 1, totalPages))}
                        disabled={currentPage === totalPages}
                        className={currentPage === totalPages ? "opacity-50 cursor-not-allowed" : ""}
                    >
                        Suivant
                    </Button>
                </div>
            )}

           <ImageModal 
                picture={picture} 
                onClose={() => setPicture(null)} 
                token={token}                
                onDeleteSuccess={() => setDeleteTrigger(prev => prev + 1)} 
            />
        </div>
    );
};

export default Gallery;