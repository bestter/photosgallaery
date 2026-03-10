import React, { useState, useRef, useEffect } from 'react';
import api from '../api';
import Button from './Button';
import toast from 'react-hot-toast';
import { isTokenExpired, getUserRole } from './authHelper';

const Upload = ({ onUploadSuccess, token, setToken }) => {
    const [files, setFiles] = useState([]); 
    const [isUploading, setIsUploading] = useState(false); 
    const fileInputRef = useRef(null);
    const [tags, setTags] = useState([]);
    const [tagInput, setTagInput] = useState("");
    
    // NOUVEAU: État pour stocker les suggestions de l'API
    const [suggestions, setSuggestions] = useState([]);

    const MAX_SIZE_BYTES = 50 * 1024 * 1024; 

    // NOUVEAU: Le moteur de recherche (Autocomplete)
    useEffect(() => {
        const delayDebounceFn = setTimeout(async () => {
            if (tagInput.length > 1) {
                try {
                    // Utilisation de ton instance "api" existante !
                    const response = await api.get(`/tags/search?q=${tagInput}`);
                    setSuggestions(response.data);
                } catch (err) {
                    console.error("Erreur lors de la recherche de tags", err);
                }
            } else {
                setSuggestions([]); // On vide si le champ est vide
            }
        }, 300);

        return () => clearTimeout(delayDebounceFn);
    }, [tagInput]);

    const handleClearSelection = () => {
        setFiles([]); 
        if (fileInputRef.current) {
            fileInputRef.current.value = ''; 
        }
    };

    // NOUVEAU: Fonction centralisée pour ajouter un tag
    const addTagToList = (tagName) => {
        if (!tags.includes(tagName) && tags.length < 12) {
            setTags([...tags, tagName]);
        }
        setTagInput("");
        setSuggestions([]); // Cache les suggestions après un choix
    };

    // MODIFIÉ: Utilise maintenant addTagToList
    const addTag = (e) => {
        if (e.key === 'Enter' && tagInput.trim() !== "") {
            e.preventDefault();
            addTagToList(tagInput.trim());
        }
    };

    const removeTag = (tagToRemove) => {
        setTags(tags.filter(t => t !== tagToRemove));
    };

    const handleFileChange = (event) => {
        const selectedFiles = Array.from(event.target.files);
        
        if (selectedFiles.length > 0) {
            const totalSize = selectedFiles.reduce((acc, file) => acc + file.size, 0);

            if (totalSize > MAX_SIZE_BYTES) {
                toast.error("La taille totale dépasse la limite de 50 Mo.");
                if (fileInputRef.current) fileInputRef.current.value = ''; 
                return;
            }

            setFiles(selectedFiles);
            toast.success(`${selectedFiles.length} fichier(s) prêt(s) !`, { icon: '📸' });
        }
    };

    const isSessionValid = () => {
        if (!token || isTokenExpired(token)) {
            return false;
        }
        return true;
    };

    const canUpload = () => {
        const role = getUserRole(token);
        console.debug("Rôle brut extrait du token:", role); // Pour t'aider à débugger dans F12

        // Sécurité : si aucun rôle n'est trouvé
        if (!role) return false;

        // Cas 1 : Le backend renvoie un tableau de rôles (ex: ["User", "Creator"])
        if (Array.isArray(role)) {
            return role.some(r => 
                r.toLowerCase() === "admin" || 
                r.toLowerCase() === "creator"
            );
        }

        // Cas 2 : Le backend renvoie une simple chaîne de caractères (ex: "Creator")
        if (typeof role === 'string') {
            return role.toLowerCase() === "admin" || role.toLowerCase() === "creator";
        }

        return false;
    };

    const handleUpload = async () => {
        if (!isSessionValid()) {
            toast.error("Votre session a expiré. Veuillez vous reconnecter.", { icon: '🔒' });
            if (setToken) setToken(null); 
            localStorage.removeItem('token'); 
            return;
        }

        if (files.length === 0) return toast.error("Veuillez choisir au moins un fichier");

        if (tags.length < 1 || tags.length > 12) {
            alert("Veuillez sélectionner entre 1 et 12 tags.");
            return;
        }

        const formData = new FormData();
        files.forEach(file => {
            formData.append('files', file);
        });

        formData.append("tags", JSON.stringify(tags));

        setIsUploading(true);

        toast.promise(
            api.post('/photos/upload', formData),
            {
                loading: 'Téléversement de vos images...',
                success: (response) => {
                    if (response.data.erreurs && response.data.erreurs.length > 0) {
                        toast(
                            `Certains doublons ont été ignorés.`,
                            { icon: '⚠️', duration: 4000 }
                        );
                    }
                    
                    handleClearSelection();
                    setTags([]); // On vide les tags après un succès
                    if (onUploadSuccess) onUploadSuccess();
                    return response.data.message || "Images ajoutées !";
                },
                error: (error) => {
                    if (error.response?.status === 401) return "Session expirée. Reconnectez-vous.";
                    if (error.response?.status === 403) return "Vous n'avez pas l'autorisation de faire cela.";
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

    const totalSizeDisplay = (files.reduce((acc, file) => acc + file.size, 0) / (1024 * 1024)).toFixed(2);
   
    return (
        
        isSessionValid() && canUpload() ? (
            <div className="border border-dashed border-blue-500 my-4 mx-0 p-4 rounded-lg animate-in fade-in duration-500">
            <h3 className="text-lg font-bold mb-2">Upload de Photos (Membres seulement)</h3>
            
            <label className="block mb-4 text-sm font-medium text-gray-700" htmlFor="real_file_input">
                Téléverser jusqu'à 50 Mo
            </label>
            
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
                id="real_file_input"
                type="file" 
                onChange={handleFileChange} 
                ref={fileInputRef}
                className="hidden" 
                accept="image/*"  
                multiple 
              />            
              
              {/* SECTION DES TAGS MODIFIÉE */}
              <div className="mt-4 relative w-full">
                <label>Tags (Appuyez sur Entrée) :</label>
                <input 
                  type="text"
                  value={tagInput}
                  onChange={(e) => setTagInput(e.target.value)}
                  onKeyDown={addTag}
                  placeholder="ex: Nature, Voyage..."
                  className="border p-2 ml-2 rounded"
                  autoComplete="off" // Empêche le navigateur de mettre ses propres suggestions par-dessus
                />

                {/* La boîte de suggestions flottante */}
                {suggestions.length > 0 && (
                    <ul className="absolute z-10 bg-white border border-gray-300 shadow-lg mt-1 ml-2 rounded w-64 max-h-48 overflow-y-auto">
                        {suggestions.map((s, index) => (
                            <li 
                                key={index}
                                onClick={() => addTagToList(s)}
                                className="p-2 hover:bg-gray-100 cursor-pointer text-gray-800"
                            >
                                {s}
                            </li>
                        ))}
                    </ul>
                )}
              </div>

              {/* Affichage des badges */}
              <div className="flex flex-wrap gap-2 mt-2 w-full">
                {tags.map(tag => (
                  <span key={tag} className="bg-[#008B8B] text-white px-3 py-1 rounded-full text-sm font-medium flex items-center shadow-sm">
                    {tag} 
                    <button onClick={() => removeTag(tag)} className="ml-2 text-white hover:text-gray-200 font-bold">×</button>
                  </span>
                ))}
              </div>

              {files.length > 0 && (
                 <div className="w-full mt-4">
                     <Button onClick={handleUpload} size="md" disabled={isUploading || tags.length < 1}>
                        {isUploading ? "Envoi en cours..." : "Envoyer"}
                     </Button>
                 </div>
              )}

            </div>        
        </div>
        ) : null
    ); 
};

export default Upload;