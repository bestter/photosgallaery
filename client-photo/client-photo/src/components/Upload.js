import React, { useState, useRef } from 'react';
import api from '../api';
import Button from './Button';
import toast from 'react-hot-toast'; // Importation de toast

const Upload = ({ onUploadSuccess }) => {
    const [files, setFiles] = useState([]); 
    const [isUploading, setIsUploading] = useState(false); 
    const fileInputRef = useRef(null);

    const MAX_SIZE_BYTES = 50 * 1024 * 1024; 

    const handleFileChange = (event) => {
        const selectedFiles = Array.from(event.target.files);
        
        if (selectedFiles.length > 0) {
            const totalSize = selectedFiles.reduce((acc, file) => acc + file.size, 0);

            if (totalSize > MAX_SIZE_BYTES) {
                // Remplacement de alert par toast.error
                toast.error("La taille totale dépasse la limite de 50 Mo.");
                if (fileInputRef.current) fileInputRef.current.value = ''; 
                return;
            }

            setFiles(selectedFiles);
            toast.success(`${selectedFiles.length} fichier(s) prêt(s) !`, { icon: '📸' });
        }
    };

    const handleUpload = async () => {
        if (files.length === 0) return toast.error("Veuillez choisir au moins un fichier");

        const formData = new FormData();
        files.forEach(file => {
            formData.append('files', file);
        });

        setIsUploading(true);

        // Utilisation de toast.promise pour une gestion élégante du chargement
        toast.promise(
            api.post('/photos/upload', formData),
            {
                loading: 'Téléversement de vos images...',
                success: (response) => {
                    // Si le backend signale des doublons malgré le succès partiel
                    if (response.data.erreurs && response.data.erreurs.length > 0) {
                        toast(
                            `Certains doublons ont été ignorés.`,
                            { icon: '⚠️', duration: 4000 }
                        );
                    }
                    
                    handleClearSelection();
                    if (onUploadSuccess) onUploadSuccess();
                    return response.data.message || "Images ajoutées !";
                },
                error: (error) => {
                    if (error.response?.status === 401) return "Session expirée. Reconnectez-vous.";
                    return error.response?.data?.message || "Erreur lors de l'envoi.";
                }
            }
        ).finally(() => {
            setIsUploading(false);
        });
    };

    const handleCustomButtonClick = () => {
        fileInputRef.current.click(); 
    };

    const handleClearSelection = () => {
        setFiles([]); 
        if (fileInputRef.current) {
            fileInputRef.current.value = ''; 
        }
    };

    const totalSizeDisplay = (files.reduce((acc, file) => acc + file.size, 0) / (1024 * 1024)).toFixed(2);

    return (
        <div className="border border-dashed border-blue-500 my-4 mx-0 p-4 rounded-lg"> 
            <h3 className="text-lg font-bold mb-2">Upload de Photos (Membres seulement)</h3>
            <label className="block mb-4 text-sm font-medium text-gray-700" htmlFor="file_input">Téléverser jusqu'à 50 Mo</label>
            
            <div className="flex flex-row items-center gap-4 flex-wrap"> 
              
              <Button 
                onClick={handleCustomButtonClick} 
                size="md" 
                variant={files.length > 0 ? "outline" : "primary"} 
                disabled={isUploading}
              >
                {files.length > 0 ? "Changer la sélection" : "Sélectionner des images"}
              </Button>

              {files.length > 0 && (
                <div className="flex items-center gap-2 bg-gray-100 px-3 py-1.5 rounded-lg border border-gray-200 animate-in fade-in slide-in-from-left-2 duration-300">
                  <span className="text-sm text-gray-700 font-medium">
                    {files.length} fichier(s) - {totalSizeDisplay} Mo
                  </span>
                  
                  <button 
                    onClick={handleClearSelection}
                    disabled={isUploading}
                    className="text-gray-400 hover:text-red-500 transition-colors p-1"
                    title="Retirer la sélection"
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                      <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                    </svg>
                  </button>
                </div>
              )}

              <input 
                type="file" 
                onChange={handleFileChange} 
                ref={fileInputRef}
                className="hidden" 
                accept="image/*"  
                multiple 
              />            
              
              {files.length > 0 && (
                 <Button onClick={handleUpload} id="file_input" size="md" disabled={isUploading}>
                    {isUploading ? "Envoi en cours..." : "Envoyer"}
                 </Button>
              )}

            </div>
        </div>
    ); 
};

export default Upload;