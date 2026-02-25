import React, { useEffect } from 'react';

const ImageModal = ({ picture, onClose }) => {
  useEffect(() => {
    const handleEsc = (e) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handleEsc);
    return () => window.removeEventListener('keydown', handleEsc);
  }, [onClose]);

  // CORRECTION 1 : On vérifie juste si "picture" existe (puisque c'est maintenant une simple URL)
  if (!picture) return null;

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
          aria-label="Fermer"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>

        <div className="flex items-center justify-center overflow-hidden rounded-lg" onClick={onClose}>
          <img
            // CORRECTION 2 : On utilise directement "picture" comme source
            src={picture}
            alt="Plein écran"
            className="max-w-full max-h-[90vh] w-auto h-auto object-contain rounded-lg shadow-inner"
          />
        </div>

      </div>
    </div>
  );
};

export default ImageModal;