import React, { useEffect, useState } from 'react';
import Button from './Button';
import PhotoTag from './PhotoTag';
import LikeButton from './LikeButton';
import api from '../api';
import toast from 'react-hot-toast';
import { getUserRole, getUsernameFromToken } from './authHelper';
import { useNavigate } from 'react-router-dom';

// --- NOUVEAUX IMPORTS POUR LEAFLET ---
import { MapContainer, TileLayer, Marker } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';

// Correction du bug classique de React avec les icônes par défaut de Leaflet
const customMarkerIcon = new L.Icon({
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
});
// -------------------------------------

const ImageModal = ({ picture, onClose, token, onDeleteSuccess }) => {
  const [isImageLoading, setIsImageLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    if (picture) {
      setIsImageLoading(true);
    }
  }, [picture]);

  useEffect(() => {
    const handleEsc = (e) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handleEsc);
    return () => window.removeEventListener('keydown', handleEsc);
  }, [onClose]);

  useEffect(() => {
    if (isImageLoading || !picture) return;

    const timeoutId = setTimeout(() => {
      api.post(`/photos/${picture.id}/view`)
        .catch(err => console.error("Erreur lors de l'enregistrement de la vue:", err));
    }, 2000); // 2 secondes de délai

    return () => clearTimeout(timeoutId);
  }, [isImageLoading, picture]);

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

  const handleUserClick = () => {
    onClose();
    navigate(`/user/${picture.uploaderUsername}`);
  };

  const imageBaseUrl = process.env.NODE_ENV === 'development' ? 'http://localhost:5020' : '';


  const formatBytes = (bytes, decimals = 2) => {
    if (!+bytes) return '0 Octets';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Octets', 'Ko', 'Mo', 'Go', 'To', 'Po', 'Eo', 'Zo', 'Yo'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))} ${sizes[i]}`;
  };

  const formatDate = (dateString) => {
    if (!dateString) return null;
    const options = { year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' };
    return new Date(dateString).toLocaleDateString('fr-FR', options);
  };

  return (
    <div
      className="fixed inset-0 z-50 flex flex-col md:flex-row bg-bg-color animate-in fade-in duration-200"
      onClick={onClose}
    >
      {/* === Left/Main View: Massive Image === */}
      <div className="flex-1 flex items-center justify-center relative p-0 md:p-4 overflow-hidden h-[60vh] md:h-screen" onClick={onClose}>

        <button
          onClick={onClose}
          className="absolute top-4 left-4 z-50 flex items-center justify-center bg-primary/80 hover:bg-primary text-text-color w-10 h-10 md:w-11 md:h-11 rounded-full backdrop-blur-md transition-all border border-accent/30"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>

        {isImageLoading && (
          <div className="absolute inset-0 flex items-center justify-center z-10 pointer-events-none">
            <div className="w-12 h-12 border-4 border-accent/20 border-t-accent rounded-full animate-spin"></div>
          </div>
        )}

        <img
          src={`${imageBaseUrl}${picture.url}`}
          alt="Plein écran"
          onLoad={() => setIsImageLoading(false)}
          onError={() => setIsImageLoading(false)}
          className={`w-full h-full object-contain transition-opacity duration-500 ${isImageLoading ? 'opacity-0' : 'opacity-100'}`}
          onClick={(e) => e.stopPropagation()}
        />
      </div>

      {/* === Right Sidebar: Metadata & Actions === */}
      <div
        className="w-full md:w-80 lg:w-96 bg-primary text-text-color shrink-0 flex flex-col h-[40vh] md:h-screen overflow-y-auto animate-in slide-in-from-bottom md:slide-in-from-right duration-300 rounded-t-2xl md:rounded-none z-20 shadow-[-10px_0_20px_rgba(0,0,0,0.5)] border-l border-accent/20"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="p-6 flex flex-col gap-6 min-h-full">

          {/* Infos utilisateur et actions rapides */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-full bg-accent text-primary flex items-center justify-center font-bold text-lg shadow-sm">
                {picture.uploaderUsername ? picture.uploaderUsername[0].toUpperCase() : '?'}
              </div>
              <div className="flex flex-col">
                <span className="text-xs opacity-70 font-medium">Ajouté par</span>
                <span className="text-sm font-bold leading-tight">
                  {picture.uploaderUsername ? (
                    <button onClick={handleUserClick} className="hover:underline hover:text-accent transition-colors cursor-pointer text-left">
                      {picture.uploaderUsername}
                    </button>
                  ) : 'Anonyme'}
                </span>
              </div>
            </div>

            {token && <LikeButton photoId={picture.id} initialIsLiked={picture.isLikedByCurrentUser || false} initialLikesCount={picture.likesCount || 0} />}
          </div>

          {/* Détails de l'image (Exif) */}
          <div className="border-t border-accent/20 pt-5">
            <h3 className="text-xs font-bold opacity-60 mb-4 tracking-wider uppercase">Détails</h3>

            <div className="flex flex-col gap-5 text-sm">
              {picture.dateTaken && (
                <div className="flex gap-4">
                  <div className="mt-0.5 opacity-60">
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>
                  </div>
                  <div>
                    <p className="font-semibold">{formatDate(picture.dateTaken)}</p>
                    <p className="text-xs opacity-70 mt-0.5">Capture</p>
                  </div>
                </div>
              )}

              {(picture.resolutionWidth || picture.fileSize > 0) && (
                <div className="flex gap-4">
                  <div className="mt-0.5 opacity-60">
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>
                  </div>
                  <div>
                    <p className="font-semibold">
                      {picture.resolutionWidth ? `${picture.resolutionWidth} × ${picture.resolutionHeight}` : ''}
                      {picture.resolutionWidth && picture.fileSize ? ' • ' : ''}
                      {picture.fileSize ? formatBytes(picture.fileSize) : ''}
                    </p>
                    <p className="text-xs opacity-70 mt-0.5">Résolution et taille</p>
                  </div>
                </div>
              )}

              {picture.cameraModel && (
                <div className="flex gap-4">
                  <div className="mt-0.5 opacity-60">
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" /><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" /></svg>
                  </div>
                  <div>
                    <p className="font-semibold">{picture.cameraModel}</p>
                    <p className="text-xs opacity-70 mt-0.5">Appareil</p>
                  </div>
                </div>
              )}

              {picture.viewsCount !== undefined && (
                <div className="flex gap-4">
                  <div className="mt-0.5 opacity-60">
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>
                  </div>
                  <div>
                    <p className="font-semibold">{picture.viewsCount} {picture.viewsCount > 1 ? 'vues' : 'vue'}</p>
                    <p className="text-xs opacity-70 mt-0.5">Consultations</p>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* === NOUVELLE SECTION : CARTE LEAFLET === */}
          {picture.latitude && picture.longitude && (
            <div className="border-t border-accent/20 pt-5">
              <h3 className="text-xs font-bold opacity-60 mb-3 tracking-wider uppercase">Localisation</h3>
              {/* Conteneur de la carte : hauteur fixe (h-48), bords arrondis, caché si débordement */}
              <div className="h-48 w-full rounded-lg overflow-hidden border border-accent/20 relative z-0">
                <MapContainer
                  center={[picture.latitude, picture.longitude]}
                  zoom={13}
                  scrollWheelZoom={false} // Évite de zoomer par erreur en scrollant la barre latérale
                  style={{ height: '100%', width: '100%' }}
                >
                  <TileLayer
                    attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                  />
                  <Marker
                    position={[picture.latitude, picture.longitude]}
                    icon={customMarkerIcon}
                  />
                </MapContainer>
              </div>
            </div>
          )}

          {/* Tags */}
          {picture.tags && picture.tags.length > 0 && (
            <div className="border-t border-accent/20 pt-5">
              <h3 className="text-xs font-bold opacity-60 mb-3 tracking-wider uppercase">Tags</h3>
              <div className="flex flex-wrap gap-2">
                {picture.tags?.map((tag, index) => (
                  <PhotoTag key={index} tag={tag} onClick={onClose} />
                ))}
              </div>
            </div>
          )}



          {/* Actions (Supprimer, Signaler) */}
          <div className="border-t border-accent/20 pt-5 mt-auto pb-4">
            <h3 className="text-xs font-bold opacity-60 mb-3 tracking-wider uppercase">Options</h3>
            {token && canDelete ? (
              <Button variant="outline" className="w-full justify-center text-red-500 border-red-500/50 hover:bg-red-500/10 transition-colors" onClick={handleDelete}>
                <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                Supprimer l'image
              </Button>
            ) : (
              <Button variant="ghost" className="w-full justify-center text-orange-500 hover:bg-orange-500/10 transition-colors" onClick={handleReport}>
                <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 21v-4m0 0V5a2 2 0 012-2h6.5l1 1H21l-3 6 3 6h-8.5l-1-1H5a2 2 0 00-2 2zm9-13.5V9" /></svg>
                Signaler l'image
              </Button>
            )}
          </div>

        </div>
      </div>
    </div>
  );
};

export default ImageModal;