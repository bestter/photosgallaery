import React, { useState, useEffect } from 'react';
import api from '../api';
import toast from 'react-hot-toast';
import ReportModal from './ReportModal';
import { getUsernameFromToken } from '../authHelper';

export default function ImageModal({ photo: initialPhoto, onClose, onPrev, onNext, onTagClick, onAuthorClick }) {
    const [photo, setPhoto] = useState(initialPhoto);
    const [isLiking, setIsLiking] = useState(false);
    const [isReporting, setIsReporting] = useState(false);
    const [hasReported, setHasReported] = useState(false);

    useEffect(() => {
        setPhoto(initialPhoto);
        setHasReported(initialPhoto?.isReportedByCurrentUser || initialPhoto?.IsReportedByCurrentUser || false);

        let timer;
        // Enregistre silencieusement une "vue" lorsque l'image s'ouvre, après 2 sec
        if (initialPhoto) {
            const photoId = initialPhoto.id || initialPhoto.Id;
            if (photoId) {
                timer = setTimeout(() => {
                    api.post(`/photos/${photoId}/view`).catch(err => console.error("Could not register view", err));
                }, 2000);
            }
        }

        return () => {
            if (timer) clearTimeout(timer);
        };
    }, [initialPhoto]);

    useEffect(() => {
        const handleKeyDown = (e) => {
            if (e.key === 'Escape') {
                onClose();
            }
        };

        window.addEventListener('keydown', handleKeyDown);

        return () => {
            window.removeEventListener('keydown', handleKeyDown);
        };
    }, [onClose]);

    if (!photo) return null;

    // Formatting date safely
    const dateTaken = photo.dateTaken || photo.DateTaken ? new Date(photo.dateTaken || photo.DateTaken).toLocaleDateString() : 'N/D';
    const uploadedAt = photo.uploadedAt || photo.UploadedAt ? new Date(photo.uploadedAt || photo.UploadedAt).toLocaleDateString() : 'Nouvelle';

    // Metadata
    const cameraModel = photo.cameraModel || photo.CameraModel || 'Inconnu';
    const latitude = photo.latitude ?? photo.Latitude;
    const longitude = photo.longitude ?? photo.Longitude;
    const author = photo.uploaderUsername || photo.UploaderUsername || 'Anonyme';
    const tags = photo.tags || photo.Tags || [];

    const token = localStorage.getItem('token');
    const currentUser = getUsernameFromToken(token);
    const isMyPhoto = currentUser && currentUser === author;

    // L'url hi-res de l'image (injectée par le parent)
    const imgSrc = photo.fullUrl;

    // Likes & Views
    const likesCount = photo.likesCount || photo.LikesCount || 0;
    const isLiked = photo.isLikedByCurrentUser || photo.IsLikedByCurrentUser || false;
    const viewsCount = photo.viewsCount || photo.ViewsCount || 0;

    // prevent clicks in modal content from closing the modal
    const handleContentClick = (e) => e.stopPropagation();

    // HANDLERS DES BOUTONS --------------------------
    const handleDownload = async () => {
        try {
            toast.loading("Préparation du téléchargement...", { id: "download" });
            const response = await fetch(imgSrc);
            const blob = await response.blob();
            const blobUrl = window.URL.createObjectURL(blob);

            const link = document.createElement('a');
            link.href = blobUrl;
            link.download = photo.fileName || photo.FileName || `PixelLyra.com_download_${Date.now()}.jpg`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(blobUrl);

            toast.success("Téléchargement réussi !", { id: "download" });
        } catch (err) {
            toast.error("Erreur lors du téléchargement.", { id: "download" });
        }
    };

    const handleShare = async () => {
        const shareData = {
            title: `Photo par ${author} sur PixelLyra.com`,
            text: `Regarde cette magnifique photo de ${author} !`,
            url: window.location.origin
        };

        try {
            if (navigator.share) {
                await navigator.share(shareData);
                toast.success("Partagé avec succès !");
            } else {
                await navigator.clipboard.writeText(imgSrc);
                toast.success("Lien de l'image copié dans le presse-papier !");
            }
        } catch (err) {
            console.error("Partage annulé", err);
        }
    };

    const handleLike = async () => {
        const photoId = photo.id || photo.Id;
        if (!photoId || isLiking) return;

        setIsLiking(true);
        try {
            const res = await api.post(`/photos/${photoId}/like`);
            const nowLiked = res.data.liked;

            setPhoto(prev => ({
                ...prev,
                likesCount: nowLiked ? (prev.likesCount || 0) + 1 : Math.max(0, (prev.likesCount || 0) - 1),
                isLikedByCurrentUser: nowLiked
            }));
        } catch (err) {
            // L'erreur API devrait être captée par ton intercepteur ou retourner 401 si non connecté
        } finally {
            setIsLiking(false);
        }
    };

    return (
        <div
            className="fixed inset-0 z-[150] bg-background-dark/90 backdrop-blur-sm flex items-center justify-center print:bg-transparent print:backdrop-blur-none"
            onClick={onClose}
        >
            {/* 🖨️ NOUVEAU : Styles spécifiques pour l'impression (CTRL+P) */}
            <style>{`
                @media print {
                    /* On cache visuellement le reste du site */
                    body * {
                        visibility: hidden;
                    }
                    /* On affiche uniquement cette image spécifique, en plein écran */
                    #printable-image {
                        visibility: visible;
                        position: fixed;
                        left: 0;
                        top: 0;
                        width: 100vw;
                        height: 100vh;
                        object-fit: contain;
                        margin: 0;
                        padding: 0;
                    }
                }
            `}</style>

            <img id="printable-image" src={imgSrc} alt="Image à imprimer" className="hidden print:block" />

            {/* Modal Container */}
            <div
                className="relative w-full h-full bg-slate-900/40 overflow-hidden flex flex-col md:flex-row print:hidden"
                onClick={handleContentClick}
            >
                {/* Close Button (Top Right) */}
                <button
                    onClick={onClose}
                    className="absolute top-4 right-4 z-20 p-2 rounded-full bg-background-dark/50 text-slate-100 hover:bg-primary/20 transition-colors"
                >
                    <span className="material-symbols-outlined">close</span>
                </button>

                {/* Left Section: Large Image */}
                <div className="flex-1 bg-black flex items-center justify-center relative group min-h-[300px] md:min-h-0">
                    <div
                        className="absolute inset-0 bg-center bg-no-repeat bg-contain"
                        style={{ backgroundImage: `url('${imgSrc}')` }}
                    ></div>

                    {/* Navigation Arrows */}
                    {onPrev && (
                        <button onClick={onPrev} className="absolute left-4 p-2 rounded-full bg-background-dark/40 text-white hover:bg-primary/40">
                            <span className="material-symbols-outlined">chevron_left</span>
                        </button>
                    )}
                    {onNext && (
                        <button onClick={onNext} className="absolute right-4 p-2 rounded-full bg-background-dark/40 text-white hover:bg-primary/40">
                            <span className="material-symbols-outlined">chevron_right</span>
                        </button>
                    )}
                </div>

                {/* Right Section: Metadata */}
                <div className="w-full md:w-[400px] bg-background-dark p-6 overflow-y-auto flex flex-col gap-6 border-l border-slate-800">

                    {/* Photographer Header */}
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-4">
                            <div className="size-12 rounded-full bg-primary/20 flex items-center justify-center overflow-hidden border border-primary/30 text-primary">
                                <span className="material-symbols-outlined text-2xl">person</span>
                            </div>
                            <div>
                                <h3
                                    className="text-slate-100 font-bold text-lg leading-tight hover:text-primary cursor-pointer transition-colors"
                                    onClick={() => onAuthorClick && onAuthorClick(author)}
                                >
                                    {author}
                                </h3>
                                <p className="text-primary text-xs uppercase tracking-wider font-semibold">Photographe</p>
                            </div>
                        </div>
                    </div>

                    <hr className="border-slate-800" />

                    {/* Action Buttons */}
                    <div className="grid grid-cols-4 gap-3">
                        <button
                            onClick={handleDownload}
                            className="flex flex-col items-center justify-center gap-1 p-3 rounded-lg bg-primary text-background-dark hover:opacity-90 transition-opacity">
                            <span className="material-symbols-outlined">download</span>
                            <span className="text-[10px] font-bold uppercase">Download</span>
                        </button>

                        <button
                            onClick={handleLike}
                            className={`flex flex-col items-center justify-center gap-1 p-3 rounded-lg border transition-colors ${isLiked
                                ? "bg-primary text-background-dark border-primary hover:opacity-90"
                                : "bg-primary/10 text-primary border-primary/30 hover:bg-primary/20"
                                } ${isLiking || isMyPhoto ? "opacity-50 cursor-not-allowed" : ""}`}
                            disabled={isLiking || isMyPhoto}
                            title={isMyPhoto ? "Vous ne pouvez pas aimer votre propre photo" : ""}
                        >
                            {/* Material Symbol a l'attribut FILL qui peut changer selon s'il est aimé ou non via la classe CSS parent (ou font-variation-settings) */}
                            <span className="material-symbols-outlined" style={{ fontVariationSettings: isLiked ? "'FILL' 1" : "'FILL' 0" }}>favorite</span>
                            <span className="text-[10px] font-bold uppercase">
                                {likesCount} Likes
                            </span>
                        </button>

                        <button
                            onClick={handleShare}
                            className="flex flex-col items-center justify-center gap-1 p-3 rounded-lg bg-primary/10 text-primary border border-primary/30 hover:bg-primary/20 transition-colors">
                            <span className="material-symbols-outlined">share</span>
                            <span className="text-[10px] font-bold uppercase">Share</span>
                        </button>

                        {hasReported ? (
                            <button
                                disabled
                                className="flex flex-col items-center justify-center gap-1 p-3 rounded-lg bg-primary/10 text-slate-500 border border-slate-700 hover:bg-primary/20 transition-colors opacity-50 cursor-not-allowed">
                                <span className="material-symbols-outlined text-slate-500">flag</span>
                                <span className="text-[10px] font-bold uppercase">Reported</span>
                            </button>
                        ) : (
                            <button
                                onClick={() => setIsReporting(true)}
                                disabled={isMyPhoto}
                                title={isMyPhoto ? "Vous ne pouvez pas signaler votre propre photo" : ""}
                                className={`flex flex-col items-center justify-center gap-1 p-3 rounded-lg transition-colors ${isMyPhoto
                                    ? "bg-primary/10 text-slate-500 border border-slate-700 opacity-50 cursor-not-allowed"
                                    : "bg-primary/10 text-primary border border-primary/30 hover:bg-primary/20"
                                    }`}>
                                <span className="material-symbols-outlined">flag</span>
                                <span className="text-[10px] font-bold uppercase">Report</span>
                            </button>
                        )}
                    </div>

                    {/* Info Table */}
                    <div className="space-y-4">
                        <div className="flex justify-between items-center text-sm">
                            <span className="text-slate-400">Ajoutée le</span>
                            <span className="text-slate-100 font-medium">{uploadedAt}</span>
                        </div>
                        <div className="bg-slate-800/40 rounded-xl p-4 grid grid-cols-2 gap-2 border border-slate-700/50">
                            <div className="text-center">
                                <p className="text-[10px] text-slate-400 uppercase font-bold mb-1">Prise le</p>
                                <p className="text-primary font-mono font-bold text-xs">{dateTaken}</p>
                            </div>
                            <div className="text-center border-l border-slate-700/50">
                                <p className="text-[10px] text-slate-400 uppercase font-bold mb-1">Vues</p>
                                <p className="text-primary font-mono font-bold text-xs">{viewsCount}</p>
                            </div>
                        </div>
                    </div>

                    {/* Camera Info */}
                    <div className="flex items-center gap-3 p-3 bg-slate-800/20 rounded-lg border border-slate-800">
                        <span className="material-symbols-outlined text-slate-400">photo_camera</span>
                        <div className="text-xs">
                            <p className="text-slate-400">Appareil utilisé</p>
                            <p className="text-slate-100 font-semibold">{cameraModel}</p>
                        </div>
                    </div>

                    {/* Tags */}
                    {tags && tags.length > 0 && (
                        <div className="space-y-3">
                            <p className="text-xs font-bold text-slate-400 uppercase tracking-widest">Tags</p>
                            <div className="flex flex-wrap gap-2">
                                {tags.map((tagObj, idx) => {
                                    const tagTranslations = tagObj.translations || tagObj.Translations || [];
                                    const frTranslation = tagTranslations.find(t => t.language === 0 || t.Language === 0) || tagTranslations[0];
                                    const tagName = frTranslation ? (frTranslation.name || frTranslation.Name) : 'Tag';

                                    return (
                                        <span
                                            key={idx}
                                            className="px-3 py-1 bg-slate-800 text-slate-300 rounded-full text-xs hover:bg-primary/20 hover:text-primary transition-colors cursor-pointer"
                                            onClick={() => onTagClick && onTagClick(tagName)}
                                        >
                                            {tagName}
                                        </span>
                                    );
                                })}
                            </div>
                        </div>
                    )}

                    {/* Location */}
                    {latitude != null && longitude != null && (
                        <div className="mt-auto pt-6 border-t border-slate-800 flex items-center justify-between">
                            <div className="flex items-center gap-2">
                                <span className="material-symbols-outlined text-primary text-sm">location_on</span>
                                <span className="text-xs text-slate-400">
                                    Localisation estimée
                                </span>
                            </div>
                            <button
                                className="text-xs font-bold text-primary hover:underline"
                                onClick={() => window.open(`https://www.google.com/maps/search/?api=1&query=${latitude},${longitude}`, '_blank')}
                            >
                                Voir Carte
                            </button>
                        </div>
                    )}
                </div>
            </div>

            {isReporting && (
                <ReportModal
                    photo={photo}
                    onClose={() => setIsReporting(false)}
                    onReportSuccess={() => setHasReported(true)}
                />
            )}
        </div>
    );
}
