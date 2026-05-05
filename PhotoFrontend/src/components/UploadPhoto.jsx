/* eslint-disable no-unused-vars */
import React, { useState, useRef, useEffect, useCallback } from "react";
import api from "../api";
import toast from "react-hot-toast";
import { isTokenExpired, getUserRole } from "../authHelper";

const UploadPhoto = ({ onUploadSuccess, token, setToken, initialGroupId }) => {
  const [files, setFiles] = useState([]);
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef(null);
  const [tags, setTags] = useState([]);
  const [tagInput, setTagInput] = useState("");
  const [suggestions, setSuggestions] = useState([]);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");

  const selectedFile = files.length > 0 ? files[0] : null;

  // NOUVEAU: État pour la case à cocher (cochée par défaut)
  const [includeGps, setIncludeGps] = useState(true);

  // Groupes pour l'upload Closed Loop
  const [userGroups, setUserGroups] = useState([]);
  const [selectedGroupId, setSelectedGroupId] = useState(initialGroupId || "");

  const MAX_SIZE_BYTES = 50 * 1024 * 1024;

  const isSessionValid = useCallback(() => {
    if (!token || isTokenExpired(token)) {
      return false;
    }
    return true;
  }, [token]);

  useEffect(() => {
    // Charger les groupes de l'utilisateur
    const fetchGroups = async () => {
      try {
        const response = await api.get("/auth/groups");
        setUserGroups(response.data);

        if (
          initialGroupId &&
          response.data.some((g) => (g.id || g.Id) === initialGroupId)
        ) {
          setSelectedGroupId(initialGroupId);
        } else if (!selectedGroupId && response.data.length > 0) {
          setSelectedGroupId(response.data[0].id || response.data[0].Id);
        }
      } catch (error) {
        console.error("Erreur lors du chargement des groupes", error);
        toast.error("Impossible de charger vos cercles.");
      }
    };

    if (isSessionValid()) {
      fetchGroups();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, initialGroupId, isSessionValid]);

  useEffect(() => {
    const delayDebounceFn = setTimeout(async () => {
      if (tagInput.length > 1) {
        try {
          const response = await api.get(`/tags/search?q=${tagInput}`);
          setSuggestions(response.data);
        } catch (err) {
          console.error("Erreur lors de la recherche de tags", err);
        }
      } else {
        setSuggestions([]);
      }
    }, 300);

    return () => clearTimeout(delayDebounceFn);
  }, [tagInput]);

  const handleClearSelection = () => {
    setFiles([]);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const addTagToList = (tagName) => {
    if (!tags.includes(tagName) && tags.length < 12) {
      setTags([...tags, tagName]);
    }
    setTagInput("");
    setSuggestions([]);
  };

  const addTag = (e) => {
    if (e.key === "Enter" && tagInput.trim() !== "") {
      e.preventDefault();
      addTagToList(tagInput.trim());
    }
  };

  const removeTag = (tagToRemove) => {
    setTags(tags.filter((t) => t !== tagToRemove));
  };

  const handleFileChange = (event) => {
    const selectedFiles = Array.from(event.target.files);

    if (selectedFiles.length > 0) {
      const totalSize = selectedFiles.reduce((acc, file) => acc + file.size, 0);

      if (totalSize > MAX_SIZE_BYTES) {
        toast.error("La taille totale dépasse la limite de 50 Mo.");
        if (fileInputRef.current) fileInputRef.current.value = "";
        return;
      }

      setFiles(selectedFiles);
      toast.success(`${selectedFiles.length} fichier(s) prêt(s) !`, {
        icon: "📸",
      });
    }
  };

  const canUpload = () => {
    const role = getUserRole(token);
    if (!role) return false;

    if (Array.isArray(role)) {
      return role.some(
        (r) => r.toLowerCase() === "admin" || r.toLowerCase() === "creator",
      );
    }

    if (typeof role === "string") {
      return role.toLowerCase() === "admin" || role.toLowerCase() === "creator";
    }

    return false;
  };

  const handleUpload = async () => {
    if (!isSessionValid()) {
      toast.error("Votre session a expiré. Veuillez vous reconnecter.", {
        icon: "🔒",
      });
      if (setToken) setToken(null);
      localStorage.removeItem("token");
      return;
    }

    if (files.length === 0)
      return toast.error("Veuillez choisir au moins un fichier");

    const tagsList =
      typeof tags === "string"
        ? tags
            .split(",")
            .map((t) => t.trim())
            .filter((t) => t !== "")
        : tags;

    if (tagsList.length < 1 || tagsList.length > 12) {
      alert("Veuillez sélectionner entre 1 et 12 tags.");
      setIsUploading(false);
      return;
    }

    const formData = new FormData();
    files.forEach((file) => {
      formData.append("files", file);
    });

    formData.append("tags", JSON.stringify(tagsList));
    formData.append("title", title);
    formData.append("description", description);

    // NOUVEAU: On ajoute la valeur de la case à cocher au formulaire envoyé au backend
    formData.append("includeGps", includeGps);

    // Ajout du GroupId sélectionné (Closed Loop)
    if (selectedGroupId) {
      formData.append("groupId", selectedGroupId);
    } else {
      return toast.error(
        "Veuillez sélectionner un cercle pour cette publication.",
      );
    }

    setIsUploading(true);

    toast
      .promise(api.post("/photos/upload", formData), {
        loading: "Téléversement de vos images...",
        success: (response) => {
          if (response.data.erreurs && response.data.erreurs.length > 0) {
            toast(`Certains doublons ont été ignorés.`, {
              icon: "⚠️",
              duration: 4000,
            });
          }

          handleClearSelection();
          setTags([]);
          if (onUploadSuccess) onUploadSuccess();
          return response.data.message || "Images ajoutées !";
        },
        error: (error) => {
          if (error.response?.status === 401)
            return "Session expirée. Reconnectez-vous.";
          if (error.response?.status === 403)
            return "Vous n'avez pas l'autorisation de faire cela.";
          return error.response?.data?.message || "Erreur lors de l'envoi.";
        },
      })
      .finally(() => {
        setIsUploading(false);
      });
  };

  const handleCustomButtonClick = () => {
    fileInputRef.current.click();
  };

  const totalSizeDisplay = (
    files.reduce((acc, file) => acc + file.size, 0) /
    (1024 * 1024)
  ).toFixed(2);

  return (
    <div className="flex flex-1 justify-center py-10 px-4 font-display">
      <div className="flex flex-col max-w-[800px] flex-1 gap-8">
        {/* En-tête de la section */}
        <div className="flex flex-col gap-2">
          <h1 className="text-slate-900 dark:text-slate-100 text-4xl font-black tracking-tight">
            Ajouter une photo
          </h1>
          <p className="text-slate-500 dark:text-slate-400 text-lg">
            Partagez vos moments avec la communauté PixelLyra.com.
          </p>
        </div>

        {/* Zone de Drag & Drop (Glisser-déposer) */}
        <label className="flex flex-col bg-slate-100/50 dark:bg-primary/5 rounded-xl border-2 border-dashed border-primary/30 p-8 lg:p-14 text-center items-center gap-6 group hover:border-primary transition-all cursor-pointer relative">
          <input
            type="file"
            className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
            onChange={handleFileChange}
            accept="image/png, image/jpeg"
          />
          <div className="flex items-center justify-center w-16 h-16 rounded-full bg-primary/10 text-primary group-hover:scale-110 transition-transform">
            <span className="material-symbols-outlined text-4xl">
              cloud_upload
            </span>
          </div>
          <div className="flex flex-col gap-2">
            <p className="text-slate-900 dark:text-slate-100 text-xl font-bold tracking-tight">
              {selectedFile
                ? selectedFile.name
                : "Glissez-déposez votre photo ici"}
            </p>
            <p className="text-slate-500 dark:text-slate-400 text-sm">
              {selectedFile
                ? `${(selectedFile.size / (1024 * 1024)).toFixed(2)} MB`
                : "ou cliquez pour parcourir vos fichiers (JPG, PNG, max 20 Mo)"}
            </p>
          </div>
          {!selectedFile && (
            <div className="flex items-center justify-center rounded-lg h-12 px-8 bg-primary text-background-dark font-bold text-base shadow-lg shadow-primary/20 pointer-events-none">
              Sélectionner un fichier
            </div>
          )}
        </label>

        {/* Formulaire */}
        <form
          onSubmit={(e) => {
            e.preventDefault();
            handleUpload();
          }}
          className="grid grid-cols-1 gap-8"
        >
          <div className="flex flex-col gap-6">
            {/* Titre */}
            <div className="flex flex-col gap-2">
              <label
                htmlFor="photo-title"
                className="text-slate-900 dark:text-slate-100 text-sm font-bold tracking-wide uppercase"
              >
                Titre de la photo
              </label>
              <input
                id="photo-title"
                className="w-full rounded-lg bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 focus:border-primary focus:ring-1 focus:ring-primary text-slate-900 dark:text-slate-100 p-4 transition-all"
                placeholder="Entrez un titre accrocheur"
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
              />
            </div>

            {/* Description */}
            <div className="flex flex-col gap-2">
              <label
                htmlFor="photo-description"
                className="text-slate-900 dark:text-slate-100 text-sm font-bold tracking-wide uppercase"
              >
                Description
              </label>
              <textarea
                id="photo-description"
                className="w-full rounded-lg bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 focus:border-primary focus:ring-1 focus:ring-primary text-slate-900 dark:text-slate-100 p-4 transition-all"
                placeholder="Racontez l'histoire derrière ce cliché..."
                rows="4"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

            {/* Tags simples */}
            <div className="flex flex-col gap-2">
              <label
                htmlFor="photo-tags"
                className="text-slate-900 dark:text-slate-100 text-sm font-bold tracking-wide uppercase"
              >
                Tags
              </label>
              <input
                id="photo-tags"
                className="w-full rounded-lg bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 focus:border-primary focus:ring-1 focus:ring-primary text-slate-900 dark:text-slate-100 p-4 transition-all"
                placeholder="Ajoutez des tags séparés par des virgules (ex: nature, voyage)"
                type="text"
                value={tags}
                onChange={(e) => setTags(e.target.value)}
              />
            </div>

            {/* Cercle (Groupe) */}
            <div className="flex flex-col gap-2">
              <label
                htmlFor="photo-group"
                className="text-slate-900 dark:text-slate-100 text-sm font-bold tracking-wide uppercase"
              >
                Visibilité (Cercle)
              </label>
              <select
                id="photo-group"
                className="w-full rounded-lg bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 focus:border-primary focus:ring-1 focus:ring-primary text-slate-900 dark:text-slate-100 p-4 transition-all"
                value={selectedGroupId}
                onChange={(e) => setSelectedGroupId(e.target.value)}
                required
              >
                {userGroups.map((group) => (
                  <option
                    key={group.id || group.Id}
                    value={group.id || group.Id}
                  >
                    {group.name || group.Name}
                  </option>
                ))}
              </select>
            </div>

            {/* Géolocalisation */}
            <div className="flex items-center gap-3 mt-4 py-2 group cursor-pointer">
              <input
                id="geo-location"
                type="checkbox"
                className="w-5 h-5 rounded border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-primary focus:ring-primary focus:ring-offset-background-dark transition-all cursor-pointer"
                checked={includeGps}
                onChange={(e) => setIncludeGps(e.target.checked)}
              />
              <label
                className="text-slate-900 dark:text-slate-100 text-sm font-medium cursor-pointer select-none"
                htmlFor="geo-location"
              >
                Ajouter les coordonnées de géolocalisation
              </label>
            </div>
          </div>

          {/* Boutons d'action */}
          <div className="flex items-center justify-end gap-4 pt-6 border-t border-primary/10 mb-20">
            <button
              type="button"
              onClick={() => onUploadSuccess && onUploadSuccess()}
              className="px-6 py-3 text-sm font-bold text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              disabled={isUploading}
            >
              Annuler
            </button>
            <button
              type="submit"
              disabled={isUploading || files.length === 0}
              title={files.length === 0 ? "Veuillez choisir au moins un fichier" : ""}
              className="px-10 py-3 bg-primary text-background-dark text-sm font-bold rounded-lg shadow-lg shadow-primary/20 hover:brightness-110 active:scale-95 transition-all disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:brightness-100 disabled:active:scale-100"
            >
              {isUploading ? (
                <>
                  <span className="material-symbols-outlined animate-spin mr-2" aria-hidden="true">
                    sync
                  </span>
                  Téléversement...
                </>
              ) : (
                "Publier la photo"
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default UploadPhoto;
