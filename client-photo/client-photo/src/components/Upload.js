import React, { useState, useRef } from 'react';
import api from '../api';
import Button from './Button';

const Upload = ({ onUploadSuccess }) => {
    const [file, setFile] = useState(null);
    const [fileName, setFileName] = useState(null);
    const fileInputRef = useRef(null);
  
    const handleUpload = async () => {
        if (!file) return alert("Veuillez choisir un fichier");

        // Suppression de setFileName(file.name) ici. C'était trop tard !

        const formData = new FormData();
        formData.append('file', file);

        const token = localStorage.getItem('token');
        if (!token) {
            console.error("Aucun token trouvé, l'utilisateur n'est probablement pas connecté.");
            return alert("Erreur: Vous devez être connecté pour uploader."); // Ajout d'une alerte pour l'utilisateur
        }
        console.debug('token ' + token);

        try {
            // debugger; // Pense à retirer le debugger en prod
            await api.post('/photos/upload', formData, {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    //'Content-Type': 'multipart/form-data', // Axios s'en charge généralement tout seul
                }
            });
            alert("Upload réussi !");
            // On réinitialise tout après un succès
            setFile(null);
            setFileName(null);
            if (fileInputRef.current) fileInputRef.current.value = ''; // On vide aussi l'input
                        
            if (onUploadSuccess) onUploadSuccess();
            // Dans Upload.jsx, à la fin de handleUpload :
        } catch (error) {
            // Si l'erreur vient de notre serveur et qu'il y a un message (ex: doublon)
            if (error.response && error.response.status === 409) {
                alert("Erreur : " + error.response.data.message);
            }
            // Pour toutes les autres erreurs (401 Non autorisé, etc.)
            else if (error.response && error.response.status === 401) {
                alert("Erreur : Vous devez être connecté pour uploader.");
            } else {
                alert("Erreur lors de l'envoi de l'image.");
            }
            console.error(error);
        }
    };

  const handleCustomButtonClick = () => {
    fileInputRef.current.click(); 
  };

  // LA CORRECTION PRINCIPALE EST ICI
  const handleFileChange = (event) => {
    const selectedFile = event.target.files[0];
    if (selectedFile) {
      console.log("Fichier sélectionné :", selectedFile.name);
      setFile(selectedFile);      // On stocke l'objet Fichier complet pour l'envoi
      setFileName(selectedFile.name); // NOUVEAU : On stocke le nom pour l'affichage visuel
    }
  };
    
  // CORRECTION SECONDAIRE ICI
  const handleClearSelection = () => {
    setFileName(null);
    setFile(null); // NOUVEAU : Il faut aussi vider le fichier prêt à être envoyé !
    if (fileInputRef.current) {
      fileInputRef.current.value = ''; 
    }
  };

    return (
        <div className="border border-dashed border-blue-500 my-4 mx-0 p-4 rounded-lg"> {/* J'ai ajusté les marges Tailwind pour que ce soit plus propre */}
            <h3 className="text-lg font-bold mb-2">Upload de Photo (Membres seulement)</h3>
            <label className="block mb-4 text-sm font-medium text-gray-700" htmlFor="file_input">Téléverser</label>
            
            <div className="flex flex-row items-center gap-4 flex-wrap"> {/* flex-wrap au cas où l'écran soit petit */}
              
              <Button 
                onClick={handleCustomButtonClick} 
                size="md" 
                variant={fileName ? "outline" : "primary"} // Change de style si un fichier est sélectionné
              >
                {fileName ? "Changer l'image" : "Sélectionner une image"}
              </Button>

              {fileName && (
                <div className="flex items-center gap-2 bg-gray-100 px-3 py-1.5 rounded-lg border border-gray-200 animate-in fade-in slide-in-from-left-2 duration-300">
                  <span className="text-sm text-gray-700 max-w-[200px] truncate font-medium">
                    {fileName}
                  </span>
                  
                  <button 
                    onClick={handleClearSelection}
                    className="text-gray-400 hover:text-red-500 transition-colors p-1"
                    title="Retirer le fichier"
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
              />            
              
              {/* Le bouton Envoyer ne s'affiche que si un fichier est sélectionné */}
              {file && (
                 <Button onClick={handleUpload} id="file_input" size="md">
                    Envoyer
                 </Button>
              )}

            </div>
        </div>
    ); 
};

export default Upload;