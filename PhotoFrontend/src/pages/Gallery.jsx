import React, { useState, useEffect } from "react";
import UploadPhoto from "../components/UploadPhoto";
import ImageModal from "../components/ImageModal";
import InviteModal from "../components/InviteModal";
import GroupRequestModal from "../components/GroupRequestModal";
import GroupSelector from "../components/GroupSelector";
import { getUserRole, isTokenExpired } from "../authHelper";
import api from "../api";

export default function Gallery() {
  const [isUploadOpen, setIsUploadOpen] = useState(false);
  const [isInviteOpen, setIsInviteOpen] = useState(false);
  const [isGroupRequestOpen, setIsGroupRequestOpen] = useState(false);
  const [selectedPhotoIndex, setSelectedPhotoIndex] = useState(null);
  const [photos, setPhotos] = useState([]);
  const [selectedTag, setSelectedTag] = useState(null);
  const [selectedAuthor, setSelectedAuthor] = useState(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  // Nouveaux états pour les groupes
  const [userGroups, setUserGroups] = useState([]);
  const [activeGroupId, setActiveGroupId] = useState(null);

  // Vérification de la session via le token
  const token = localStorage.getItem("token");
  const isLoggedIn = token && !isTokenExpired(token);
  const userRole = isLoggedIn ? getUserRole(token) : null;

  // Permissions
  const canUpload =
    isLoggedIn && (userRole === "Admin" || userRole === "Creator");
  const canSeeDashboard = isLoggedIn && userRole === "Admin";

  // Récupération des photos depuis l'API, dépendante du groupe sélectionné
  const fetchPhotos = async (groupId) => {
    try {
      setIsLoading(true);
      const url = groupId ? `/photos?groupId=${groupId}` : "/photos";
      const response = await api.get(url);
      setPhotos(response.data);
    } catch (error) {
      console.error("Erreur lors de la récupération des photos :", error);
    } finally {
      setIsLoading(false);
    }
  };

  // Récupérer les groupes
  useEffect(() => {
    if (isLoggedIn) {
      api
        .get("/auth/groups")
        .then((res) => {
          setUserGroups(res.data);

          // Vérifier si un shortName est dans l'URL
          let urlGroupId = null;
          const pathParts = window.location.pathname.split("/");
          if (pathParts.length >= 3 && pathParts[1] === "group") {
            const shortName = pathParts[2];
            const matchedGroup = res.data.find(
              (g) => (g.shortName || g.ShortName) === shortName,
            );
            if (matchedGroup) {
              urlGroupId = matchedGroup.id || matchedGroup.Id;
            }
          } else {
            // Fallback pour ancien format ?groupId=
            const params = new URLSearchParams(window.location.search);
            urlGroupId = params.get("groupId");
          }

          if (urlGroupId) {
            // Si le groupe est présent, on l'utilise
            setActiveGroupId(urlGroupId);
          } else if (res.data.length > 0) {
            // Sinon le premier groupe par défaut
            setActiveGroupId(res.data[0].id || res.data[0].Id);
          } else {
            // S'il n'a pas de groupe
            fetchPhotos(null);
          }
        })
        .catch((err) => {
          console.error("Erreur lors de la récupération des groupes", err);
          fetchPhotos(null);
        });
    }
  }, [isLoggedIn]);

  useEffect(() => {
    if (activeGroupId) {
      fetchPhotos(activeGroupId);

      // Maintenir l'URL synchronisée avec le groupe actif
      if (userGroups && userGroups.length > 0) {
        const activeGroup = userGroups.find(
          (g) => (g.id || g.Id) === activeGroupId,
        );
        if (activeGroup && (activeGroup.shortName || activeGroup.ShortName)) {
          const url = new URL(window.location.href);
          url.pathname = `/group/${activeGroup.shortName || activeGroup.ShortName}`;
          url.searchParams.delete("groupId");
          window.history.replaceState({}, "", url);
        }
      }
    }
  }, [activeGroupId, userGroups]);

  // Helper pour générer l'URL complète de l'image sécurisée
  const getImageUrl = (url) => {
    if (!url) return "";
    let fullUrl = url;
    if (!url.startsWith("http")) {
      const backendRoot = api.defaults.baseURL.replace(/\/api$/, "");
      fullUrl = backendRoot + url;
    }

    // Ajouter le jeton aux requêtes d'images pour passer l'autorisation côté backend
    if (token) {
      const separator = fullUrl.includes("?") ? "&" : "?";
      fullUrl += `${separator}access_token=${token}`;
    }
    return fullUrl;
  };

  const filteredPhotos = photos.filter((photo) => {
    let matchTag = true;
    if (selectedTag) {
      const photoTags = photo.tags || photo.Tags || [];
      matchTag = photoTags.some((tagObj) => {
        const tagTranslations =
          tagObj.translations || tagObj.Translations || [];
        const frTranslation =
          tagTranslations.find((t) => t.language === 0 || t.Language === 0) ||
          tagTranslations[0];
        const tagName = frTranslation
          ? frTranslation.name || frTranslation.Name
          : "Tag";
        return tagName === selectedTag;
      });
    }

    let matchAuthor = true;
    if (selectedAuthor) {
      const author =
        photo.uploaderUsername || photo.UploaderUsername || "Anonyme";
      matchAuthor = author === selectedAuthor;
    }

    let matchSearch = true;
    if (searchQuery) {
      const photoTags = photo.tags || photo.Tags || [];
      const query = searchQuery.toLowerCase();
      matchSearch = photoTags.some((tagObj) => {
        const tagTranslations =
          tagObj.translations || tagObj.Translations || [];
        return tagTranslations.some((t) => {
          const tagName = t.name || t.Name;
          return tagName && tagName.toLowerCase().includes(query);
        });
      });
    }

    return matchTag && matchAuthor && matchSearch;
  });

  return (
    <div className="bg-[#0f2323] font-sans text-slate-100 min-h-screen">
      {/* Header */}
      <header className="fixed top-0 w-full z-50 bg-slate-900/80 backdrop-blur-md border-b border-cyan-400/10 shadow-xl shadow-black/20 flex justify-between items-center px-4 md:px-6 py-3">
        <div className="flex items-center gap-4 md:gap-8">
          {/* Brand Logo */}
          <div
            className="text-xl font-black tracking-tight text-cyan-400 cursor-pointer active:scale-95 transition-transform"
            onClick={() => window.location.reload()}
          >
            <img
              alt="PixelLyra Logo"
              className="h-8 w-auto object-contain"
              src="/Byla3.jpg"
            />
          </div>
          {/* Navigation & Group Switcher */}
          <nav className="hidden md:flex items-center gap-6 font-sans text-sm font-medium tracking-tight">
            <GroupSelector
              groups={userGroups}
              activeGroupId={activeGroupId}
              onGroupSelect={setActiveGroupId}
            />
          </nav>
        </div>

        {/* Search and Actions */}
        <div className="hidden md:flex items-center gap-6 flex-1 max-w-2xl px-8">
          <div className="relative w-full">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-500 text-[20px]">
              search
            </span>
            <input
              className="w-full bg-slate-800 border-none rounded-lg pl-10 pr-4 py-2 text-sm focus:ring-2 focus:ring-cyan-400 text-slate-100 transition-all placeholder:text-slate-500"
              name="search"
              placeholder="Search inspirations, tags, authors..."
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>
        </div>

        <div className="flex items-center gap-2 md:gap-4">
          {isLoggedIn && (
            <button
              onClick={() => setIsGroupRequestOpen(true)}
              className="hidden md:block border border-cyan-400 text-cyan-400 hover:bg-cyan-400/10 px-4 py-1.5 rounded text-sm font-bold active:scale-95 transition-colors"
            >
              Créer un groupe
            </button>
          )}
          {canUpload && (
            <button
              onClick={() => setIsUploadOpen(true)}
              className="hidden md:block bg-cyan-400 text-[#0f2323] px-4 py-1.5 rounded text-sm font-bold active:scale-95 transition-transform hover:brightness-110"
            >
              Upload
            </button>
          )}
          <div className="flex items-center gap-1 md:gap-3">
            {canSeeDashboard && (
              <button
                onClick={() => (window.location.href = "/dashboard")}
                className="text-slate-400 hover:text-cyan-400 hover:bg-cyan-400/10 p-2 rounded transition-colors"
                aria-label="Aller au Dashboard"
                title="Dashboard"
              >
                <span className="material-symbols-outlined" aria-hidden="true">
                  dashboard
                </span>
              </button>
            )}
            {isLoggedIn && (
              <button
                onClick={() => setIsInviteOpen(true)}
                className="text-slate-400 hover:text-cyan-400 hover:bg-cyan-400/10 p-2 rounded transition-colors relative"
                aria-label="Inviter un utilisateur"
                title="Inviter"
              >
                <span className="material-symbols-outlined" aria-hidden="true">
                  group_add
                </span>
              </button>
            )}
            {!isLoggedIn && (
              <>
                <button
                  onClick={() => (window.location.href = "/login")}
                  className="text-slate-400 hover:text-cyan-400 hover:bg-cyan-400/10 px-3 py-1.5 rounded font-bold text-sm transition-colors"
                >
                  Login
                </button>
                <button
                  onClick={() => (window.location.href = "/register")}
                  className="bg-cyan-400 text-[#0f2323] px-4 py-1.5 rounded text-sm font-bold active:scale-95 transition-transform hover:brightness-110"
                >
                  S&apos;abonner
                </button>
              </>
            )}
            {isLoggedIn && (
              <button
                onClick={() => {
                  localStorage.removeItem("token");
                  window.location.reload();
                }}
                className="text-slate-400 hover:text-error hover:bg-error/10 p-2 rounded transition-colors"
                aria-label="Déconnexion"
                title="Déconnexion"
              >
                <span className="material-symbols-outlined" aria-hidden="true">
                  logout
                </span>
              </button>
            )}
          </div>
        </div>
      </header>

      <main className="pt-24 pb-12 px-4 md:px-6 max-w-[1600px] mx-auto">
        {/* Search Bar for Mobile */}
        <div className="md:hidden relative w-full mb-6">
          <span className="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-slate-500 text-[20px]">
            search
          </span>
          <input
            className="w-full bg-slate-800 border-none rounded-lg pl-10 pr-4 py-3 text-sm focus:ring-2 focus:ring-cyan-400 text-slate-100 transition-all placeholder:text-slate-500 shadow-lg"
            name="searchMobile"
            placeholder="Search inspirations..."
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>

        {/* Dashboard Header */}
        <div className="mb-10 flex flex-col md:flex-row justify-between items-start md:items-end gap-4">
          <div>
            <div className="text-[10px] font-bold uppercase tracking-[0.2em] text-cyan-400 mb-2">
              Workspace / Gallery
            </div>
            <h1 className="text-[1.875rem] font-extrabold tracking-tight text-slate-100">
              {activeGroupId
                ? userGroups.find((g) => (g.id || g.Id) === activeGroupId)
                    ?.name ||
                  userGroups.find((g) => (g.id || g.Id) === activeGroupId)
                    ?.Name ||
                  "Gallery"
                : "Gallery"}
            </h1>
          </div>
          <div className="flex flex-wrap gap-2">
            <button
              onClick={() => {
                setSelectedTag(null);
                setSelectedAuthor(null);
              }}
              className={`flex items-center gap-2 px-3 py-1.5 rounded text-[12px] font-semibold transition-colors ${!selectedTag && !selectedAuthor ? "bg-cyan-400 text-[#0f2323]" : "bg-slate-800 text-slate-300 hover:bg-slate-700"}`}
            >
              <span className="material-symbols-outlined text-[16px]">
                grid_view
              </span>
              All Discoveries
            </button>
            {selectedTag && (
              <button
                className="flex items-center gap-2 bg-cyan-400 text-[#0f2323] px-3 py-1.5 rounded text-[12px] font-semibold transition-colors"
                onClick={() => setSelectedTag(null)}
              >
                <span className="material-symbols-outlined text-[16px]">
                  label
                </span>
                Tag: {selectedTag}
                <span className="material-symbols-outlined text-[14px] ml-1 hover:text-white">
                  close
                </span>
              </button>
            )}
            {selectedAuthor && (
              <button
                className="flex items-center gap-2 bg-cyan-400 text-[#0f2323] px-3 py-1.5 rounded text-[12px] font-semibold transition-colors"
                onClick={() => setSelectedAuthor(null)}
              >
                <span className="material-symbols-outlined text-[16px]">
                  person
                </span>
                {selectedAuthor}
                <span className="material-symbols-outlined text-[14px] ml-1 hover:text-white">
                  close
                </span>
              </button>
            )}
          </div>
        </div>

        {/* Bento Grid */}
        {isLoading ? (
          <div className="flex justify-center items-center py-20 text-cyan-400">
            <span className="material-symbols-outlined animate-spin text-4xl">
              sync
            </span>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6 auto-rows-[280px]">
            {filteredPhotos.map((photo, index) => {
              const photoId = photo.id || photo.Id;
              const author =
                photo.uploaderUsername || photo.UploaderUsername || "Anonyme";
              const originalUrl = photo.url || photo.Url;
              const thumbnailUrl = originalUrl
                ? getImageUrl(
                    originalUrl.replace("/images/", "/images/thumbnails/"),
                  )
                : "";

              // Get tags for display
              const photoTagsRaw = photo.tags || photo.Tags || [];
              const photoTags = photoTagsRaw
                .map((tagObj) => {
                  const tagTranslations =
                    tagObj.translations || tagObj.Translations || [];
                  const frTranslation =
                    tagTranslations.find(
                      (t) => t.language === 0 || t.Language === 0,
                    ) || tagTranslations[0];
                  return frTranslation
                    ? frTranslation.name || frTranslation.Name
                    : "Tag";
                })
                .filter(Boolean);

              const isLarge = index % 4 === 0;
              const isWide = index % 4 === 3;

              if (isLarge) {
                return (
                  <div
                    key={photoId}
                    onClick={() => setSelectedPhotoIndex(index)}
                    className="md:col-span-2 md:row-span-2 group relative overflow-hidden rounded-xl bg-[#0f2323] border border-slate-800/60 cursor-pointer shadow-lg hover:shadow-cyan-400/10 transition-all duration-300 focus-visible:ring-2 focus-visible:ring-cyan-400 focus-visible:outline-none"
                    role="button"
                    tabIndex={0}
                    aria-label={`Ouvrir la photo ${photo.title || "par " + author}`}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === " ") {
                        e.preventDefault();
                        setSelectedPhotoIndex(index);
                      }
                    }}
                  >
                    <img
                      alt={`Photo par ${author}`}
                      className="w-full h-full object-cover grayscale-[20%] group-hover:grayscale-0 transition-all duration-700 scale-105 group-hover:scale-100"
                      src={thumbnailUrl || getImageUrl(originalUrl)}
                    />
                    <div className="absolute inset-0 bg-gradient-to-t from-[#081414] via-transparent to-transparent opacity-90"></div>
                    <div className="absolute bottom-0 left-0 p-6 w-full">
                      {photoTags.length > 0 && (
                        <span className="bg-cyan-400 text-[#0f2323] text-[10px] font-bold px-2 py-0.5 rounded uppercase tracking-wider mb-3 inline-block">
                          {photoTags[0]}
                        </span>
                      )}
                      <h2 className="text-2xl font-bold text-white mb-1 truncate">
                        {photo.title || `Captured by @${author}`}
                      </h2>
                      <div className="flex items-center gap-4 mt-3">
                        <div
                          className="flex items-center gap-2 text-slate-300 text-[12px] font-semibold hover:text-cyan-400"
                          onClick={(e) => {
                            e.stopPropagation();
                            setSelectedAuthor(author);
                            window.scrollTo({ top: 0, behavior: "smooth" });
                          }}
                        >
                          <span className="material-symbols-outlined text-[18px]">
                            person
                          </span>{" "}
                          @{author}
                        </div>
                      </div>
                    </div>
                  </div>
                );
              }

              if (isWide) {
                return (
                  <div
                    key={photoId}
                    onClick={() => setSelectedPhotoIndex(index)}
                    className="md:col-span-2 group relative overflow-hidden rounded-xl bg-[#0f2323] border border-slate-800/60 cursor-pointer shadow-lg hover:shadow-cyan-400/10 transition-all duration-300 focus-visible:ring-2 focus-visible:ring-cyan-400 focus-visible:outline-none"
                    role="button"
                    tabIndex={0}
                    aria-label={`Ouvrir la photo ${photo.title || "par " + author}`}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === " ") {
                        e.preventDefault();
                        setSelectedPhotoIndex(index);
                      }
                    }}
                  >
                    <img
                      alt={`Photo par ${author}`}
                      className="w-full h-full object-cover transition-all duration-700 group-hover:scale-105"
                      src={thumbnailUrl || getImageUrl(originalUrl)}
                    />
                    <div className="absolute inset-0 bg-slate-900/60 opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex items-center justify-center backdrop-blur-sm">
                      <button className="bg-white text-slate-950 font-bold px-6 py-2 rounded-full transform translate-y-4 group-hover:translate-y-0 transition-transform duration-300">
                        View Image
                      </button>
                    </div>
                    <div className="absolute top-4 left-4">
                      <div className="bg-slate-950/80 backdrop-blur-md px-3 py-1 rounded text-[10px] font-bold text-cyan-400 uppercase tracking-widest border border-cyan-400/20">
                        {photoTags[0] || "Sans catégories"}
                      </div>
                    </div>
                    <div
                      className="absolute bottom-4 right-4 bg-slate-900/80 backdrop-blur-md px-2 py-1 rounded text-[10px] text-slate-300 flex items-center gap-1 hover:text-cyan-400 transition-colors"
                      onClick={(e) => {
                        e.stopPropagation();
                        setSelectedAuthor(author);
                        window.scrollTo({ top: 0, behavior: "smooth" });
                      }}
                    >
                      <span className="material-symbols-outlined text-[14px]">
                        person
                      </span>{" "}
                      {author}
                    </div>
                  </div>
                );
              }

              return (
                <div
                  key={photoId}
                  onClick={() => setSelectedPhotoIndex(index)}
                  className="group relative overflow-hidden rounded-xl bg-[#0f2323] border border-slate-800/60 flex flex-col cursor-pointer shadow-lg hover:shadow-cyan-400/10 transition-all duration-300 focus-visible:ring-2 focus-visible:ring-cyan-400 focus-visible:outline-none"
                  role="button"
                  tabIndex={0}
                  aria-label={`Ouvrir la photo ${photo.title || "par " + author}`}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" || e.key === " ") {
                      e.preventDefault();
                      setSelectedPhotoIndex(index);
                    }
                  }}
                >
                  <div className="relative flex-1 overflow-hidden">
                    <img
                      alt={`Photo par ${author}`}
                      className="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
                      src={thumbnailUrl || getImageUrl(originalUrl)}
                    />
                  </div>
                  <div className="p-4 bg-[#152b2b] shrink-0 border-t border-slate-800/60">
                    <div className="flex justify-between items-start mb-2">
                      <h3 className="text-sm font-bold text-slate-100 truncate pr-2">
                        {photo.title || `Photo by @${author}`}
                      </h3>
                      <span className="material-symbols-outlined text-slate-500 text-[18px] hover:text-cyan-400 transition-colors shrink-0">
                        visibility
                      </span>
                    </div>
                    <div className="flex items-center gap-2 overflow-hidden">
                      {photoTags.slice(0, 2).map((tag) => (
                        <span
                          key={tag}
                          className="text-[10px] px-2 py-0.5 rounded bg-slate-900 text-slate-400 font-semibold uppercase whitespace-nowrap"
                        >
                          {tag}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
              );
            })}

            {filteredPhotos.length === 0 && (
              <div className="col-span-full flex flex-col items-center justify-center text-slate-500 mt-10 p-12 border border-slate-800/60 rounded-xl bg-[#152b2b]/50">
                <span className="material-symbols-outlined text-5xl mb-4 text-slate-500" aria-hidden="true">
                  image_not_supported
                </span>
                <h3 className="text-xl font-bold text-slate-300 mb-2">Aucune image trouvée</h3>
                <p className="text-sm mb-6 max-w-md text-center">
                  {searchQuery || selectedTag || selectedAuthor
                    ? "Essayez de modifier vos filtres de recherche pour trouver ce que vous cherchez."
                    : "Cet espace est encore vide. Soyez le premier à le remplir !"}
                </p>
                {canUpload && !searchQuery && !selectedTag && !selectedAuthor && (
                  <button
                    onClick={() => setIsUploadOpen(true)}
                    className="bg-cyan-400 text-[#0f2323] px-6 py-2.5 rounded-lg text-sm font-bold active:scale-95 transition-transform hover:brightness-110 flex items-center gap-2"
                  >
                    <span className="material-symbols-outlined text-[20px]" aria-hidden="true">add_photo_alternate</span>
                    Ajouter une photo
                  </button>
                )}
              </div>
            )}
          </div>
        )}
      </main>

      {/* Contextual FAB for Upload */}
      {canUpload && (
        <button
          onClick={() => setIsUploadOpen(true)}
          className="fixed bottom-8 right-8 w-14 h-14 bg-cyan-400 text-[#0f2323] rounded-full shadow-[0_0_20px_rgba(34,211,238,0.3)] flex items-center justify-center hover:scale-110 active:scale-95 transition-all z-40"
          aria-label="Upload a photo"
          title="Upload a photo"
        >
          <span
            className="material-symbols-outlined text-[28px]"
            style={{ fontVariationSettings: "'FILL' 1" }}
            aria-hidden="true"
          >
            add
          </span>
        </button>
      )}

      {isUploadOpen && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/50 backdrop-blur-sm p-4 sm:p-6 lg:p-8">
          <div className="relative w-full max-w-4xl max-h-full overflow-y-auto bg-white dark:bg-background-dark rounded-3xl shadow-2xl">
            <button
              onClick={() => setIsUploadOpen(false)}
              className="absolute top-4 right-4 z-10 size-10 flex items-center justify-center rounded-full bg-slate-100 dark:bg-slate-800 text-slate-500 hover:text-slate-900 dark:hover:text-slate-100 transition-colors"
              aria-label="Fermer la modal d'upload"
              title="Fermer"
            >
              <span className="material-symbols-outlined" aria-hidden="true">
                close
              </span>
            </button>
            <div className="p-2 sm:p-4">
              <UploadPhoto
                token={token}
                initialGroupId={activeGroupId}
                onUploadSuccess={() => {
                  setIsUploadOpen(false);
                  fetchPhotos(activeGroupId); // Recharge les photos pour le groupe actif après Upload
                }}
              />
            </div>
          </div>
        </div>
      )}

      {/* Image Detail Modal */}
      {selectedPhotoIndex !== null && filteredPhotos[selectedPhotoIndex] && (
        <ImageModal
          photo={{
            ...filteredPhotos[selectedPhotoIndex],
            fullUrl: getImageUrl(
              filteredPhotos[selectedPhotoIndex].url ||
                filteredPhotos[selectedPhotoIndex].Url,
            ),
          }}
          onClose={() => setSelectedPhotoIndex(null)}
          onPrev={
            selectedPhotoIndex > 0
              ? () => setSelectedPhotoIndex(selectedPhotoIndex - 1)
              : null
          }
          onNext={
            selectedPhotoIndex < filteredPhotos.length - 1
              ? () => setSelectedPhotoIndex(selectedPhotoIndex + 1)
              : null
          }
          onTagClick={(tag) => {
            setSelectedTag(tag);
            setSelectedPhotoIndex(null);
          }}
          onAuthorClick={(author) => {
            setSelectedAuthor(author);
            setSelectedPhotoIndex(null);
          }}
        />
      )}

      <InviteModal
        isOpen={isInviteOpen}
        onClose={() => setIsInviteOpen(false)}
      />

      <GroupRequestModal
        isOpen={isGroupRequestOpen}
        onClose={() => setIsGroupRequestOpen(false)}
      />
    </div>
  );
}
