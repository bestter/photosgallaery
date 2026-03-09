import React, { useEffect, useState } from 'react'; // 👈 useState est ajouté ici
import Button from './Button';
import PhotoTag from './PhotoTag'; 
import api from '../api';
import toast from 'react-hot-toast';
import { getUserRole, getUsernameFromToken } from './authHelper';

const ImageModal = ({ picture, onClose, token, onDeleteSuccess }) => {
  // --- NOUVELLES LIGNES POUR LE CHARGEMENT ---
  const [isImageLoading, setIsImageLoading] = useState(true);

  useEffect(() => {
    if (picture) {
      setIsImageLoading(true); // On remet à zéro quand on change de photo
    }
  }, [picture]);
  // -------------------------------------------

  useEffect(() => {
    const handleEsc = (e) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handleEsc);
    return () => window.removeEventListener('keydown', handleEsc);
  }, [onClose]);

  if (!picture) return null;

  const currentUser = getUsernameFromToken(token);
  const isAdmin = getUserRole(token) === 'Admin';
  const canDelete = isAdmin || (currentUser === picture.uploaderUsername);

  const handleDelete = () => {
    toast((t) => (
      <div className="flex flex-col gap-2">
        <span className="font-medium text-gray-800">
          Supprimer cette image définitivement ?
        </span>
        <div className="flex gap-2 justify-end">
          <button
            onClick={() => toast.dismiss(t.id)}
            className="px-3 py-1 text-xs bg-gray-200 rounded hover:bg-gray-300 transition-colors"
          >
            Annuler
          </button>
          <button
            onClick={() => {
              toast.dismiss(t.id);
              executeDelete();
            }}
            className="px-3 py-1 text-xs bg-red-600 text-white rounded hover:bg-red-700 transition-colors"
          >
            Confirmer
          </button>
        </div>
      </div>
    ), {
      duration: 6000,
      position: 'top-center',
      style: { border: '1px solid #fee2e2', padding: '16px' }
    });
  };

  const executeDelete = async () => {
    toast.promise(
      api.delete(`/photos/${picture.id}`),
      {
        loading: 'Suppression en cours...',
        success: () => {
          if (onDeleteSuccess) onDeleteSuccess();
          onClose();
          return "L'image a été retirée.";
        },
        error: "Erreur lors de la suppression."
      }
    );
  };

  const handleReport = async () => {
    const reason = window.prompt("Pourquoi signalez-vous cette image ?");
    
    if (reason && reason.trim() !== "") {
      toast.promise(
        api.post(`/photos/${picture.id}/report`, { reason }),
        {
          loading: 'Envoi du signalement...',
          success: () => {
            onClose();
            return "Merci. L'administrateur a été notifié.";
          },
          error: "Échec de l'envoi du signalement."
        }
      );
    }
  };

  const imageBaseUrl = process.env.NODE_ENV === 'development' ? 'http://localhost:5020' : '';

  return (
    <div 
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 backdrop-blur-sm p-4 animate-in fade-in duration-200"
      onClick={onClose}
    >
      <div 
        className="relative bg-white rounded-2xl shadow-2xl p-2 md:p-4 max-w-[95vw] max-h-[95vh] flex flex-col items-center justify-center animate-in zoom-in-95 duration-300"
        onClick={(e) => e.stopPropagation()}
      >
        <button
          onClick={onClose}
          className="absolute -top-4 -right-4 md:top-4 md:right-4 z-20 flex items-center justify-center bg-red-600 hover:bg-red-700 text-white w-10 h-10 md:w-12 md:h-12 rounded-full shadow-xl transition-all hover:scale-110 active:scale-90 border-2 border-white"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>

        {/* --- NOUVELLES LIGNES POUR LE CONTENEUR D'IMAGE ET LE SPINNER --- */}
        <div className="flex items-center justify-center overflow-hidden rounded-lg bg-gray-100 w-full relative min-h-[30vh]">
          
          {isImageLoading && (
            <div className="absolute inset-0 flex items-center justify-center z-10">
              <div className="w-12 h-12 border-4 border-gray-300 border-t-[#008B8B] rounded-full animate-spin"></div>
            </div>
          )}

          <img
            src={`${imageBaseUrl}${picture.url}`} 
            alt="Plein écran"
            onLoad={() => setIsImageLoading(false)} // L'image est arrivée !
            onError={() => setIsImageLoading(false)} // L'image a planté !
            className={`max-w-full max-h-[70vh] w-auto h-auto object-contain rounded-lg shadow-inner transition-opacity duration-500 ${isImageLoading ? 'opacity-0' : 'opacity-100'}`}
          />
        </div> 
        {/* --------------------------------------------------------------- */}

        {picture.tags && picture.tags.length > 0 && (
          <div className="mt-4 flex flex-wrap gap-2 justify-center w-full">
            {picture.tags?.map((tag, index) => {
              
              return (
                <PhotoTag tag={tag} />
              );
            })}
          </div>
        )}
        
        <div className="w-full mt-4 flex items-center justify-between px-2">
          <div className="text-sm text-gray-500 font-medium">
            {picture.uploaderUsername ? `Ajouté par ${picture.uploaderUsername}` : ''}
          </div>
          
          <div>
            {token && canDelete ? (
              <Button variant="outline" size="sm" onClick={handleDelete} className="text-red-600 border-red-600 hover:bg-red-50">
                🗑️ Supprimer
              </Button>
            ) : (
              <Button variant="ghost" size="sm" onClick={handleReport} className="text-orange-500 hover:bg-orange-50">
                🚩 Signaler
              </Button>
            )}
          </div>
        </div>
      </div>    
    </div>
  );
};

export default ImageModal;