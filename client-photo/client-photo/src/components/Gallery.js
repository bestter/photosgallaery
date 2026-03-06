import React, { useEffect, useState } from 'react';
import ImageModal from './ImageModal';
import Button from './Button';
import Upload from './Upload'; 
import api from '../api';

const Gallery = ({ refreshTrigger, token }) => { 
    const [photos, setPhotos] = useState([]);
    const [picture, setPicture] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;
    
    const [deleteTrigger, setDeleteTrigger] = useState(0); 
    const [uploadTrigger, setUploadTrigger] = useState(0); 

    useEffect(() => {
        setIsLoading(true);
        api.get('/photos')
            .then(response => {
                setPhotos(response.data.reverse()); 
            })
            .catch(err => console.error("Erreur chargement photos", err))
            .finally(() => setIsLoading(false));
            
    }, [refreshTrigger, deleteTrigger, uploadTrigger]);

    const imageBaseUrl = process.env.NODE_ENV === 'development' ? 'http://localhost:5020' : '';

    const getFileName = (url) => {
        if (!url) return '';
        return url.split('/').pop();
    };

    const indexOfLastItem = currentPage * itemsPerPage;
    const indexOfFirstItem = indexOfLastItem - itemsPerPage;
    const currentPhotos = photos.slice(indexOfFirstItem, indexOfLastItem); 
    const totalPages = Math.ceil(photos.length / itemsPerPage);
    
    return (
        <div className="container mx-auto p-6">
            <h2 className="text-2xl font-bold mb-8 text-gray-800">Galerie Publique</h2>
            
            <Upload token={token} onUploadSuccess={() => setUploadTrigger(prev => prev + 1)} />
            
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-6 mt-6">
                
                {isLoading ? (
                    Array.from({ length: 10 }).map((_, index) => (
                        <div key={`skeleton-${index}`} className="flex flex-col gap-2">
                            <div className="w-full aspect-square bg-gray-200 animate-pulse rounded-xl"></div>
                            <div className="h-4 bg-gray-200 animate-pulse rounded w-1/2 mt-1"></div>
                        </div>
                    ))
                ) : currentPhotos.length > 0 ? (
                    
                    currentPhotos.map(photo => (
                        <div key={photo.id} className="flex flex-col">
                            {/* C'est ICI qu'on met la classe group pour détecter le survol */}
                            <div 
                                className="relative overflow-hidden rounded-xl shadow-md cursor-pointer aspect-square bg-gray-100 group"
                                onClick={() => setPicture(photo)}
                            >
                                {/* h-48 remplacé par h-full */}
                                <img 
                                    src={`${imageBaseUrl}${photo.url}`} 
                                    alt={getFileName(photo.url)} 
                                    className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-110"
                                />
                                
                                {/* Le voile global qui apparaît au survol */}
                                <div className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex flex-col justify-between p-3 pointer-events-none">
                                    
                                   {/* En haut : Les tags */}
<div className="flex flex-wrap gap-1">
    {photo.tags?.slice(0, 3).map((tag, i) => {
        // On va chercher le nom dans la première traduction disponible
        const tagName = typeof tag === 'object' 
            ? (tag.translations?.[0]?.name || "Inconnu") 
            : tag;

        return (
            <span key={i} className="text-xs bg-[#008B8B] text-white px-2 py-0.5 rounded-full shadow-sm">
                {tagName}
            </span>
        );
    })}
    {photo.tags?.length > 3 && (
        <span className="text-xs text-white px-1 font-bold mt-0.5">
            +{photo.tags.length - 3}
        </span>
    )}
</div>

                                    {/* En bas : Le nom du fichier */}
                                    <div className="text-white text-xs truncate w-full text-center">
                                        {getFileName(photo.url)}
                                    </div>
                                </div>
                            </div>
                            
                            <p className="text-sm text-gray-500 mt-2 font-medium">
                                Par: <span className="text-brand">{photo.uploaderUsername}</span>
                            </p>
                        </div>
                    ))
                ) : (
                    <p className="col-span-full text-center text-gray-500 py-10">
                        Aucune photo n'a été publiée pour le moment.
                    </p>
                )}
            </div>

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

           {picture && (
               <ImageModal 
                   picture={picture} 
                   onClose={() => setPicture(null)} 
                   token={token}                
                   onDeleteSuccess={() => {
                       setDeleteTrigger(prev => prev + 1);
                       setPicture(null); // On ferme la modale après suppression
                   }} 
               />
           )}
        </div>
    );
};

export default Gallery;