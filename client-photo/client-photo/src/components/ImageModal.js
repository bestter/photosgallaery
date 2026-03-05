import React, { useEffect } from 'react';
import Button from './Button';
import api from '../api';
import toast from 'react-hot-toast';
import { getUserRole, getUsernameFromToken } from './authHelper'; // Ajoute getUsernameFromToken

const ImageModal = ({ picture, onClose, token, onDeleteSuccess }) => {
  useEffect(() => {
    const handleEsc = (e) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handleEsc);
    return () => window.removeEventListener('keydown', handleEsc);
  }, [onClose]);

  if (!picture) return null;

  // 1. Extraire les infos de l'utilisateur actuel
  const currentUser = getUsernameFromToken(token);
  const isAdmin = getUserRole(token) === 'Admin';
  const canDelete = isAdmin || (currentUser === picture.uploaderUsername);

  // Nouvelle logique de suppression avec Toast de confirmation
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

  // La fonction qui fait le travail réel
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

        <div className="flex items-center justify-center overflow-hidden rounded-lg bg-gray-100">
          <img
            src={`${imageBaseUrl}${picture.url}`} 
            alt="Plein écran"
            className="max-w-full max-h-[75vh] w-auto h-auto object-contain rounded-lg shadow-inner"
          />
        </div>
<div className="w-full mt-4 flex items-center justify-between px-2">
      <div className="text-sm text-gray-500 font-medium">
        {picture.uploaderUsername ? `Ajouté par ${picture.uploaderUsername}` : ''}
      </div>
      
      <div>
        {/* MODIFICATION ICI : On vérifie canDelete au lieu de juste token */}
        {token && canDelete ? (
          <Button variant="outline" size="sm" onClick={handleDelete} className="text-red-600 border-red-600 hover:bg-red-50">
            🗑️ Supprimer
          </Button>
        ) : (
          /* Si pas le droit de supprimer, on montre le bouton signaler */
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