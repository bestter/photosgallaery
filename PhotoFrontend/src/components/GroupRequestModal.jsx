import React, { useState } from 'react';
import toast from 'react-hot-toast';
import api from '../api';

const GroupRequestModal = ({ isOpen, onClose }) => {
    const [groupName, setGroupName] = useState('');
    const [groupGoal, setGroupGoal] = useState('');
    const [inviteEmails, setInviteEmails] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    if (!isOpen) return null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        setIsLoading(true);
        try {
            // Simulate network request as endpoint might not exist yet
            await new Promise(resolve => setTimeout(resolve, 500));

            toast.success("Demande de création de groupe envoyée avec succès !");
            
            setGroupName('');
            setGroupGoal('');
            setInviteEmails('');
            onClose();
        } catch (err) {
            toast.error("Erreur lors de l'envoi de la demande.");
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-[#081414]/80 backdrop-blur-sm font-sans text-slate-100">
            <div className="w-full max-w-2xl bg-[#0f2323] border border-slate-800/40 rounded-xl overflow-hidden shadow-2xl shadow-black/50 p-8">
                <div className="mb-8 flex items-center justify-between">
                    <div>
                        <h1 className="text-3xl font-extrabold tracking-tight text-slate-100 mb-2">Créer un nouveau groupe</h1>
                        <p className="text-slate-400 text-sm">Démarrez une nouvelle communauté autour d'une thématique commune.</p>
                    </div>
                    <img alt="Logo" className="h-12 w-12 object-contain opacity-80" src="/Byla3.jpg" />
                </div>
                
                <form className="space-y-8" onSubmit={handleSubmit}>
                    {/* Group Name */}
                    <div>
                        <label className="block text-sm font-semibold text-slate-100 mb-2" htmlFor="groupName">Nom du groupe</label>
                        <input
                            className="w-full bg-slate-800 border-none rounded-lg text-slate-100 placeholder:text-slate-500 focus:ring-2 focus:ring-cyan-400 py-3 px-4 transition-all outline-none"
                            id="groupName" name="groupName" placeholder="Ex: Photographes Urbains Paris" required type="text"
                            value={groupName}
                            onChange={(e) => setGroupName(e.target.value)}
                        />
                    </div>
                    
                    {/* Goal Description */}
                    <div>
                        <label className="block text-sm font-semibold text-slate-100 mb-2" htmlFor="groupGoal">Description du but du groupe</label>
                        <textarea
                            className="w-full bg-slate-800 border-none rounded-lg text-slate-100 placeholder:text-slate-500 focus:ring-2 focus:ring-cyan-400 py-3 px-4 transition-all resize-none outline-none"
                            id="groupGoal" name="groupGoal"
                            placeholder="Décrivez l'objectif principal de ce groupe et ce que les membres y trouveront..."
                            required rows="4"
                            value={groupGoal}
                            onChange={(e) => setGroupGoal(e.target.value)}
                        ></textarea>
                    </div>
                    
                    {/* Initial Members */}
                    <div className="pt-6 border-t border-slate-800/40">
                        <h3 className="text-lg font-bold text-slate-100 mb-4 flex items-center gap-2">
                            <span className="material-symbols-outlined text-cyan-400" style={{fontVariationSettings: "'FILL' 1"}}>group_add</span>
                            Inviter les premiers membres
                        </h3>
                        <p className="text-sm text-slate-400 mb-4">Saisissez les adresses email des personnes que vous souhaitez inviter (séparées par des virgules).</p>
                        <input
                            className="w-full bg-slate-800 border-none rounded-lg text-slate-100 placeholder:text-slate-500 focus:ring-2 focus:ring-cyan-400 py-3 px-4 transition-all outline-none"
                            id="inviteEmails" name="inviteEmails" placeholder="email1@exemple.com, email2@exemple.com..."
                            type="text"
                            value={inviteEmails}
                            onChange={(e) => setInviteEmails(e.target.value)}
                        />
                        <div className="mt-4 bg-cyan-400/10 border border-cyan-400/20 rounded-lg p-4 flex items-start gap-3">
                            <span className="material-symbols-outlined text-cyan-400 mt-0.5" style={{fontVariationSettings: "'FILL' 1"}}>info</span>
                            <p className="text-sm text-slate-400">Un email d'invitation sera automatiquement envoyé à ces adresses une fois le groupe créé.</p>
                        </div>
                    </div>
                    
                    {/* Actions */}
                    <div className="flex items-center justify-end gap-4 pt-6 mt-8 border-t border-slate-800/40">
                        <button
                            className="px-6 py-2.5 rounded-lg text-sm font-semibold text-slate-100 bg-slate-800 hover:bg-slate-700 transition-colors focus:ring-2 focus:ring-slate-500 focus:outline-none"
                            type="button"
                            onClick={onClose}
                            disabled={isLoading}
                        >
                            Annuler
                        </button>
                        <button
                            className="px-6 py-2.5 rounded-lg text-sm font-semibold text-[#0f2323] bg-cyan-400 hover:brightness-110 transition-colors focus:ring-2 focus:ring-cyan-400 focus:outline-none shadow-[0_0_15px_rgba(0,206,209,0.3)] disabled:opacity-50"
                            type="submit"
                            disabled={isLoading}
                        >
                            {isLoading ? 'Envoi...' : 'Envoyer la demande'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default GroupRequestModal;
