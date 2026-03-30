import React, { useState } from 'react';
import api from '../api';
import toast from 'react-hot-toast';

export default function ReportModal({ photo, onClose, onReportSuccess }) {
    const [reason, setReason] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);

    if (!photo) return null;

    const photoId = photo.id || photo.Id;
    const author = photo.uploaderUsername || photo.UploaderUsername || 'Anonyme';
    const thumbnailSrc = photo.thumbnailUrl || photo.Url || photo.fullUrl;
    const fileName = photo.fileName || photo.FileName || `Photo #${photoId}`;

    const handleSubmit = async () => {
        if (!reason.trim()) {
            toast.error("Veuillez entrer une raison pour le signalement.");
            return;
        }

        setIsSubmitting(true);
        try {
            await api.post(`/photos/${photoId}/report`, { reason });
            toast.success("Signalement enregistré avec succès.");
            if (onReportSuccess) onReportSuccess();
            onClose();
        } catch (err) {
            toast.error("Erreur lors du signalement.");
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="fixed inset-0 z-[200] bg-black/80 backdrop-blur-sm flex items-center justify-center p-4" onClick={onClose}>
            {/* Modal Container */}
            <div 
                className="max-w-md w-full bg-[#152b2b] shadow-2xl rounded-xl overflow-hidden border border-[#1e293b]/40 relative"
                onClick={(e) => e.stopPropagation()}
            >
                {/* Header / Identity Area */}
                <div className="px-6 pt-6 pb-2 flex justify-between items-start">
                    <div>
                        <h1 className="text-xl font-black text-primary tracking-tight italic uppercase">Vision</h1>
                        <p className="text-[10px] font-bold uppercase tracking-widest text-slate-400 mt-1">Report Content</p>
                    </div>
                    <button onClick={onClose} className="text-slate-400 hover:text-primary transition-colors">
                        <span className="material-symbols-outlined">close</span>
                    </button>
                </div>

                <div className="p-6 space-y-6">
                    {/* Reported Image Thumbnail Section */}
                    <div className="flex items-center space-x-4 p-3 bg-[#0f2323] rounded-lg border border-[#1e293b]/20">
                        <div className="relative w-20 h-20 flex-shrink-0 overflow-hidden rounded-lg shadow-inner">
                            <img alt="Thumbnail" className="w-full h-full object-cover" src={thumbnailSrc} />
                            <div className="absolute inset-0 bg-gradient-to-t from-[#0f2323]/60 to-transparent"></div>
                        </div>
                        <div>
                            <span className="text-[10px] font-bold uppercase tracking-widest text-primary block mb-1">Flagging Item</span>
                            <h2 className="text-sm font-bold text-slate-100 leading-tight truncate max-w-[200px]">{fileName}</h2>
                            <p className="text-xs text-slate-400 mt-1 italic">Par {author}</p>
                        </div>
                    </div>

                    {/* Form Section */}
                    <div className="space-y-4">
                        <div className="space-y-2">
                            <label className="text-[10px] font-bold uppercase tracking-widest text-slate-400 ml-1" htmlFor="reason">
                                Reason
                            </label>
                            <textarea
                                id="reason"
                                value={reason}
                                onChange={(e) => setReason(e.target.value)}
                                className="w-full bg-[#244545]/50 border-none rounded-lg p-4 text-sm text-slate-100 placeholder:text-slate-400/40 focus:ring-2 focus:ring-primary focus:bg-[#244545] transition-all outline-none resize-none"
                                placeholder="Explain why this content violates our terms..."
                                rows="4"
                            ></textarea>
                        </div>

                        {/* Guidance Note */}
                        <div className="flex items-start space-x-2 text-slate-400">
                            <span className="material-symbols-outlined text-sm" style={{ fontVariationSettings: "'FILL' 1" }}>info</span>
                            <p className="text-[11px] leading-relaxed">
                                Reports are reviewed within 24 hours. Abuse of the reporting system may result in account restrictions.
                            </p>
                        </div>
                    </div>

                    {/* Action Buttons */}
                    <div className="flex flex-col gap-3 pt-2">
                        <button
                            onClick={handleSubmit}
                            disabled={isSubmitting}
                            className="w-full bg-primary text-[#0f2323] font-bold py-3 px-6 rounded-lg hover:brightness-110 active:scale-[0.98] transition-all flex items-center justify-center space-x-2 disabled:opacity-50"
                        >
                            <span className="text-sm font-bold uppercase tracking-wider">
                                {isSubmitting ? "Envoi..." : "Submit Report"}
                            </span>
                            {!isSubmitting && <span className="material-symbols-outlined text-base">send</span>}
                        </button>
                        <button
                            onClick={onClose}
                            className="w-full bg-transparent border border-[#1e293b] text-slate-400 font-bold py-3 px-6 rounded-lg hover:bg-[#1c3838] hover:text-slate-100 transition-all flex items-center justify-center"
                        >
                            <span className="text-sm font-bold uppercase tracking-wider">Cancel</span>
                        </button>
                    </div>
                </div>

                {/* Progress/Status Bar Mock */}
                <div className="h-1 w-full bg-[#244545]">
                    <div className="h-full bg-primary/30 w-1/3"></div>
                </div>

                {/* Background Elements for Mood */}
                <div className="absolute inset-0 -z-10 overflow-hidden pointer-events-none">
                    <div className="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] bg-primary/5 blur-[80px] rounded-full"></div>
                    <div className="absolute bottom-[-10%] right-[-10%] w-[40%] h-[40%] bg-primary/10 blur-[80px] rounded-full"></div>
                </div>
            </div>
        </div>
    );
}
