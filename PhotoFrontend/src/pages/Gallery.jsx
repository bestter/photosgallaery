import React, { useState, useEffect } from 'react';
import PhotoCard from '../components/PhotoCard';
import UploadPhoto from '../components/UploadPhoto';
import ImageModal from '../components/ImageModal';
import { getUserRole, isTokenExpired } from '../authHelper';
import api from '../api';

export default function Gallery() {
    const [isUploadOpen, setIsUploadOpen] = useState(false);
    const [selectedPhotoIndex, setSelectedPhotoIndex] = useState(null);
    const [photos, setPhotos] = useState([]);
    const [selectedTag, setSelectedTag] = useState(null);
    const [selectedAuthor, setSelectedAuthor] = useState(null);
    const [isLoading, setIsLoading] = useState(true);

    // Vérification de la session via le token
    const token = localStorage.getItem('token');
    const isLoggedIn = token && !isTokenExpired(token);
    const userRole = isLoggedIn ? getUserRole(token) : null;

    // Permissions
    const canUpload = isLoggedIn && (userRole === 'Admin' || userRole === 'Creator');
    const canSeeDashboard = isLoggedIn && userRole === 'Admin';

    // Récupération des photos depuis l'API
    const fetchPhotos = async () => {
        try {
            setIsLoading(true);
            const response = await api.get('/photos');
            setPhotos(response.data);
        } catch (error) {
            console.error("Erreur lors de la récupération des photos :", error);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchPhotos();
    }, []);

    // Helper pour générer l'URL complète de l'image (si hébergée localement par l'API)
    const getImageUrl = (url) => {
        if (!url) return '';
        if (url.startsWith('http')) return url;
        const backendRoot = api.defaults.baseURL.replace(/\/api$/, '');
        return backendRoot + url;
    };

    const filteredPhotos = photos.filter(photo => {
        let matchTag = true;
        if (selectedTag) {
            const photoTags = photo.tags || photo.Tags || [];
            matchTag = photoTags.some(tagObj => {
                const tagTranslations = tagObj.translations || tagObj.Translations || [];
                const frTranslation = tagTranslations.find(t => t.language === 0 || t.Language === 0) || tagTranslations[0];
                const tagName = frTranslation ? (frTranslation.name || frTranslation.Name) : 'Tag';
                return tagName === selectedTag;
            });
        }
        
        let matchAuthor = true;
        if (selectedAuthor) {
            const author = photo.uploaderUsername || photo.UploaderUsername || "Anonyme";
            matchAuthor = author === selectedAuthor;
        }

        return matchTag && matchAuthor;
    });

    return (
        <div className="bg-background-light dark:bg-background-dark font-display text-slate-900 dark:text-slate-100 min-h-screen">

            {/* Header */}
            <header className="sticky top-0 z-50 bg-background-light/80 dark:bg-background-dark/80 backdrop-blur-md border-b border-slate-200 dark:border-slate-800 px-4 lg:px-10 py-4">
                <div className="max-w-7xl mx-auto flex items-center justify-between gap-4 lg:gap-8">
                    <div className="flex items-center gap-2 shrink-0">
                        <div className="size-8 bg-primary rounded-lg flex items-center justify-center text-background-dark">
                            <span className="material-symbols-outlined font-bold">camera</span>
                        </div>
                        <h1 className="hidden md:block text-xl font-bold tracking-tight">Vision</h1>
                    </div>

                    <div className="flex-1 max-w-2xl">
                        <label className="relative block">
                            <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400 dark:text-slate-500">
                                <span className="material-symbols-outlined">search</span>
                            </span>
                            <input className="w-full bg-slate-100 dark:bg-slate-800/50 border-none rounded-xl py-2.5 pl-10 pr-4 focus:ring-2 focus:ring-primary text-sm placeholder:text-slate-400" name="search" placeholder="Search for inspiration..." type="text" />
                        </label>
                    </div>

                    <div className="flex items-center gap-3 shrink-0">
                        {canSeeDashboard && (
                            <button 
                                onClick={() => window.location.href = '/dashboard'}
                                className="hidden md:flex items-center gap-2 px-4 py-2.5 rounded-xl border border-primary/30 text-primary hover:bg-primary/10 transition-all font-semibold text-sm">
                                <span className="material-symbols-outlined text-lg">dashboard</span>
                                <span>Dashboard</span>
                            </button>
                        )}
                        {canUpload && (
                            <button 
                                onClick={() => setIsUploadOpen(true)}
                                className="bg-primary hover:bg-primary/90 text-background-dark px-5 py-2.5 rounded-xl font-semibold text-sm transition-all flex items-center gap-2">
                                <span className="material-symbols-outlined text-lg">cloud_upload</span>
                                <span className="hidden sm:inline">Upload</span>
                            </button>
                        )}
                        {!isLoggedIn && (
                            <button 
                                onClick={() => window.location.href = '/login'}
                                className="hidden md:flex items-center gap-2 px-4 py-2.5 rounded-xl border border-primary/30 text-primary hover:bg-primary/10 transition-all font-semibold text-sm">
                                <span className="material-symbols-outlined text-lg">login</span>
                                <span>Login</span>
                            </button>
                        )}
                        {isLoggedIn && (
                            <button 
                                onClick={() => {
                                    localStorage.removeItem('token');
                                    window.location.reload();
                                }}
                                className="hidden md:flex items-center justify-center p-2.5 rounded-xl border border-error/30 text-error hover:bg-error/10 transition-all font-semibold text-sm"
                                title="Déconnexion"
                            >
                                <span className="material-symbols-outlined text-lg">logout</span>
                            </button>
                        )}
                    </div>
                </div>
            </header>

            <main className="max-w-7xl mx-auto px-4 lg:px-10 py-8">
                {/* Categories */}
                <div className="flex gap-3 overflow-x-auto pb-6 scrollbar-hide no-scrollbar">
                    <button 
                        onClick={() => { setSelectedTag(null); setSelectedAuthor(null); }}
                        className={`shrink-0 px-5 py-2 rounded-full text-sm font-medium ${!selectedTag && !selectedAuthor ? 'bg-primary text-background-dark' : 'bg-slate-100 dark:bg-slate-800'}`}
                    >
                        All Discoveries
                    </button>
                    {selectedTag && (
                        <button className="shrink-0 px-5 py-2 bg-primary text-background-dark rounded-full text-sm font-medium">
                            Tag: {selectedTag}
                        </button>
                    )}
                    {selectedAuthor && (
                        <button className="shrink-0 px-5 py-2 bg-primary text-background-dark rounded-full text-sm font-medium flex items-center gap-2">
                            <span className="material-symbols-outlined text-[14px]">person</span>
                            {selectedAuthor}
                        </button>
                    )}
                </div>

                {/* Masonry Grid */}
                {isLoading ? (
                    <div className="flex justify-center items-center py-20 text-slate-400">
                        <span className="material-symbols-outlined animate-spin text-4xl">sync</span>
                    </div>
                ) : (
                    <div className="masonry">
                        {filteredPhotos.map((photo, index) => {
                            const photoId = photo.id || photo.Id;
                            const author = photo.uploaderUsername || photo.UploaderUsername || "Anonyme";
                            const originalUrl = photo.url || photo.Url;
                            
                            // Génère l'URL de la miniature en insérant "thumbnails/" après "/images/"
                            const thumbnailUrl = originalUrl ? getImageUrl(originalUrl.replace('/images/', '/images/thumbnails/')) : '';
                            
                            return (
                                <PhotoCard
                                    key={photoId}
                                    src={thumbnailUrl || getImageUrl(originalUrl)}
                                    alt={`Photo par ${author}`}
                                    author={`@${author}`}
                                    onClick={() => setSelectedPhotoIndex(index)}
                                    onAuthorClick={(clickedAuthor) => {
                                        setSelectedAuthor(clickedAuthor);
                                        window.scrollTo({ top: 0, behavior: 'smooth' });
                                    }}
                                />
                            );
                        })}
                        {filteredPhotos.length === 0 && (
                            <div className="col-span-full text-center text-slate-500 mt-10">
                                Aucune image pour le moment.
                            </div>
                        )}
                    </div>
                )}
            </main>

            {/* Upload Modal */}
            {isUploadOpen && (
                <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/50 backdrop-blur-sm p-4 sm:p-6 lg:p-8">
                    <div className="relative w-full max-w-4xl max-h-full overflow-y-auto bg-white dark:bg-background-dark rounded-3xl shadow-2xl">
                        <button 
                            onClick={() => setIsUploadOpen(false)}
                            className="absolute top-4 right-4 z-10 size-10 flex items-center justify-center rounded-full bg-slate-100 dark:bg-slate-800 text-slate-500 hover:text-slate-900 dark:hover:text-slate-100 transition-colors"
                        >
                            <span className="material-symbols-outlined">close</span>
                        </button>
                        <div className="p-2 sm:p-4">
                            <UploadPhoto 
                                token={token}
                                onUploadSuccess={() => {
                                    setIsUploadOpen(false);
                                    fetchPhotos(); // Recharge les photos après Upload
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
                        fullUrl: getImageUrl(filteredPhotos[selectedPhotoIndex].url || filteredPhotos[selectedPhotoIndex].Url)
                    }} 
                    onClose={() => setSelectedPhotoIndex(null)}
                    onPrev={selectedPhotoIndex > 0 ? () => setSelectedPhotoIndex(selectedPhotoIndex - 1) : null}
                    onNext={selectedPhotoIndex < filteredPhotos.length - 1 ? () => setSelectedPhotoIndex(selectedPhotoIndex + 1) : null}
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
        </div>
    );
}