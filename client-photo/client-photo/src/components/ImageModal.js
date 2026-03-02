import React, { useEffect } from 'react';
import Button from './Button';
import api from '../api';

const ImageModal = ({ picture, onClose, token, onDeleteSuccess }) => {
  useEffect(() => {
    const handleEsc = (e) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handleEsc);
    return () => window.removeEventListener('keydown', handleEsc);
  }, [onClose]);

  if (!picture) return null;

  // Fonction de suppression (Admin / Propriétaire)
  const handleDelete = async () => {
    if (window.confirm("Es-tu sûr de vouloir supprimer cette image définitivement ?")) {
      try {
        // L'intercepteur d'api.js ajoute automatiquement le token, plus besoin de le préciser !
        await api.delete(`/photos/${picture.id}`);
        
        alert("L'image a été supprimée avec succès !");
        
        if (onDeleteSuccess) onDeleteSuccess(); // Avertit la Galerie de recharger les images
        onClose(); // Ferme la fenêtre modale
        
      } catch (error) {
        console.error("Erreur lors de la suppression", error);
        alert("Erreur: Impossible de supprimer l'image. Vérifie que tu es bien connecté.");
      }
    }
  };

  // Fonction pour le Visiteur
  const handleReport = async () => {
    const reason = window.prompt("Pourquoi signalez-vous cette image ?");
    if (reason) {
      try {
        // À décommenter quand la route C# sera prête
        // await api.post(`/photos/${picture.id}/report`, { reason });
        alert("Merci. L'administrateur a été notifié de ce problème.");
        onClose();
      } catch (error) {
        console.error("Erreur lors du signalement", error);
      }
    }
  };

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
            src={picture.url} 
            alt="Plein écran"
            className="max-w-full max-h-[75vh] w-auto h-auto object-contain rounded-lg shadow-inner"
          />
        </div>

        {/* Barre d'actions */}
        <div className="w-full mt-4 flex items-center justify-between px-2">
          <div className="text-sm text-gray-500">
            {picture.uploaderUsername ? `Ajouté par ${picture.uploaderUsername}` : ''}
          </div>
          
          <div>
            {token ? (
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