import React, { useEffect, useState } from 'react';
import ImageModal from './ImageModal';
import Button from './Button';
import Upload from './Upload'; 
import EmptyGalleryState from './EmptyGalleryState'; 
import PhotoTag from './PhotoTag'; 
import api from '../api';
import { useParams, useNavigate } from 'react-router-dom';


const Gallery = ({ refreshTrigger, token, setToken, customEndpoint, title = "Galerie Publique", hideUpload = false }) => {
    const [photos, setPhotos] = useState([]);
    const [picture, setPicture] = useState(null);
    const [isLoading, setIsLoading] = useState(true);
    const [currentPage, setCurrentPage] = useState(1);
    const itemsPerPage = 10;
    const { tagName } = useParams();
    
    const [deleteTrigger, setDeleteTrigger] = useState(0); 
    const [uploadTrigger, setUploadTrigger] = useState(0); 

    const navigate = useNavigate();

    // NOUVEAU : On réinitialise la pagination quand le tag change
    useEffect(() => {
        setCurrentPage(1);
    }, [tagName]);
    
    
    const currentLanguage = 'Fr';


    useEffect(() => {
    setIsLoading(true);
    
    // On utilise le endpoint personnalisé ou la route par défaut
    const baseEndpoint = customEndpoint || '/photos';
    
    // On construit l'URL avec la langue
    let apiUrl = `${baseEndpoint}?lang=${currentLanguage}`;

    const safeTagName = tagName ? decodeURIComponent(tagName) : null;
    if (safeTagName) {
        apiUrl += `&tag=${encodeURIComponent(safeTagName)}`;
    }        
        api.get(apiUrl)
            .then(response => {
                setPhotos(response.data.reverse()); 
            })
            .catch(err => console.error("Erreur chargement photos", err))
            .finally(() => setIsLoading(false));
            
    }, [refreshTrigger, deleteTrigger, uploadTrigger, tagName, customEndpoint]);

    const imageBaseUrl = process.env.NODE_ENV === 'development' ? 'http://localhost:5020' : '';

    const getFileName = (url) => {
        if (!url) return '';
        return url.split('/').pop();
    };
    
    const handleUserClick = (photo) => {    
    navigate(`/user/${photo.uploaderUsername}`); // 2. On change de page
};

    const indexOfLastItem = currentPage * itemsPerPage;
    const indexOfFirstItem = indexOfLastItem - itemsPerPage;
    const currentPhotos = photos.slice(indexOfFirstItem, indexOfLastItem); 
    const totalPages = Math.ceil(photos.length / itemsPerPage);
    
    return (
        <div className="container mx-auto p-6">
            <h2 className="text-2xl font-bold mb-8 text-gray-800">Galerie Publique</h2>
            
            <Upload token={token} onUploadSuccess={() => setUploadTrigger(prev => prev + 1)} setToken={setToken}/>
            
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-6 mt-6">
                
                {tagName && (
                <div style={{ marginBottom: '20px', borderBottom: '2px solid #00CED1', paddingBottom: '10px' }}>
                    <h1 style={{ color: '#00CED1', textTransform: 'capitalize' }}>
                        🏷️ {tagName}
                    </h1>
                </div>
            )}

                {isLoading ? (
                    Array.from({ length: 10 }).map((_, index) => (
                        <div key={`skeleton-${index}`} className="flex flex-col gap-2">
                            <div className="w-full aspect-square bg-gray-200 animate-pulse rounded-xl"></div>
                            <div className="h-4 bg-gray-200 animate-pulse rounded w-1/2 mt-1"></div>
                        </div>
                    ))
                ) : photos.length === 0 ? (
                // L'appel de notre nouvel état vide
                <EmptyGalleryState tagName={tagName} />
                    ) : (
                    
                    currentPhotos.map(photo => (
                        <div key={photo.id} className="flex flex-col">
                            {/* C'est ICI qu'on met la classe group pour détecter le survol */}
                            {/* 1. On ENLÈVE le onClick et le cursor-pointer de ce conteneur parent */}
<div className="relative overflow-hidden rounded-xl shadow-md aspect-square bg-gray-100 group">
    
    {/* L'image de fond */}
    <img 
        src={`${imageBaseUrl}/images/thumbnails/${getFileName(photo.url)}`} 
        onError={(e) => {
            e.target.onerror = null; 
            e.target.src = `${imageBaseUrl}${photo.url}`; 
        }}
        alt={getFileName(photo.url)} 
        className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-110"
    />

    {/* 2. LA COUCHE MAGIQUE : Un voile transparent dédié uniquement à ouvrir l'image (z-index 10) */}
    <div 
        className="absolute inset-0 z-10 cursor-pointer"
        onClick={() => setPicture(photo)}
        title="Agrandir l'image"
    ></div>
    
    {/* 3. L'INTERFACE VISUELLE : Par-dessus le bouton invisible (z-index 20) */}
    <div className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex flex-col justify-between p-3 pointer-events-none z-20">
        
        {/* Les tags : On les place au sommet (z-index 30) et on réactive les clics */}
        <div className="flex flex-wrap gap-1 pointer-events-auto relative z-30">
            {photo.tags?.slice(0, 3).map((tag) => (
                <div key={tag.id} className="photo-tags-container">
                    <PhotoTag tag={tag} />
                </div> 
            ))}
            
            {photo.tags?.length > 3 && (
                <span className="text-xs text-white px-1 font-bold mt-0.5">
                    +{photo.tags.length - 3}
                </span>
            )}
        </div>

        {/* Le nom du fichier en bas */}
        <div className="text-white text-xs truncate w-full text-center">
            {getFileName(photo.url)}
        </div>
    </div>
</div>
                           
                            {photo.uploaderUsername ? (
    <p className="text-sm text-gray-500 mt-2 font-medium">
        Par{' '}
        <button 
            onClick={() => handleUserClick(photo)}
            className="text-teal-600 hover:text-teal-800 hover:underline transition-colors cursor-pointer"
        >
            {photo.uploaderUsername}
        </button>
    </p>
) : null}
                        </div>
                    ))
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