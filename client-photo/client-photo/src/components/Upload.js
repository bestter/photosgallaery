import React, { useState, useRef } from 'react';
import api from '../api';
import Button from './Button';

const Upload = ({ onUploadSuccess }) => {
    // 1. On passe d'un fichier unique à un tableau de fichiers
    const [files, setFiles] = useState([]); 
    const [isUploading, setIsUploading] = useState(false); // Pour bloquer les doubles clics
    const fileInputRef = useRef(null);

    // Limite stricte de 50 Mo (en octets)
    const MAX_SIZE_BYTES = 50 * 1024 * 1024; 

    const handleFileChange = (event) => {
        // Convertir la FileList en vrai tableau JavaScript
        const selectedFiles = Array.from(event.target.files);
        
        if (selectedFiles.length > 0) {
            // 2. Calculer le poids total de la sélection
            const totalSize = selectedFiles.reduce((acc, file) => acc + file.size, 0);

            if (totalSize > MAX_SIZE_BYTES) {
                alert("La taille totale dépasse la limite de 50 Mo. Veuillez sélectionner moins d'images.");
                if (fileInputRef.current) fileInputRef.current.value = ''; // On vide l'input
                return;
            }

            console.log(`${selectedFiles.length} fichier(s) sélectionné(s).`);
            setFiles(selectedFiles);
        }
    };

    const handleUpload = async () => {
        if (files.length === 0) return alert("Veuillez choisir au moins un fichier");

        const formData = new FormData();
        
        // 3. On attache CHAQUE fichier à la même clé "files" pour que C# reçoive une List<IFormFile>
        files.forEach(file => {
            formData.append('files', file);
        });

        setIsUploading(true);

        try {
            // L'intercepteur d'api.js s'occupe déjà d'injecter le token !
            const response = await api.post('/photos/upload', formData);
            
            // On affiche le message de succès renvoyé par le backend
            alert(response.data.message);
            
            // Si le backend nous signale des doublons, on en informe l'utilisateur
            if (response.data.erreurs && response.data.erreurs.length > 0) {
                alert("Attention, certaines images n'ont pas été ajoutées :\n\n" + response.data.erreurs.join('\n'));
            }

            // On réinitialise tout après un succès
            handleClearSelection();
            if (onUploadSuccess) onUploadSuccess();
            
        } catch (error) {
            if (error.response?.data?.message) {
                alert("Erreur : " + error.response.data.message);
            } else if (error.response && error.response.status === 401) {
                alert("Erreur : Vous devez être connecté pour téléverser des images.");
            } else {
                alert("Erreur inattendue lors de l'envoi des images.");
            }
            console.error(error);
        } finally {
            setIsUploading(false); // On libère le bouton
        }
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

    // Calcul de la taille totale pour l'affichage visuel (en Mo)
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

              {/* Affichage du résumé de la sélection */}
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
                multiple // <-- TRÈS IMPORTANT : Permet la sélection multiple
              />            
              
              {/* Le bouton Envoyer */}
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