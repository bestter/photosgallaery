import React, { useState, useEffect } from 'react';
import api from '../api';
import toast from 'react-hot-toast';

const InviteModal = ({ isOpen, onClose }) => {
    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');
    const [email, setEmail] = useState('');
    const [message, setMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    
    // Group selection
    const [userGroups, setUserGroups] = useState([]);
    const [selectedGroupId, setSelectedGroupId] = useState('');

    useEffect(() => {
        if (isOpen) {
            const fetchGroups = async () => {
                try {
                    const response = await api.get('/auth/groups');
                    setUserGroups(response.data);
                    if (response.data.length > 0) {
                        setSelectedGroupId(response.data[0].id || response.data[0].Id);
                    }
                } catch (error) {
                    toast.error("Impossible de charger vos cercles pour l'invitation.");
                }
            };
            fetchGroups();
        }
    }, [isOpen]);

    if (!isOpen) return null;

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        if (!selectedGroupId) {
            toast.error("Vous devez sélectionner un cercle.");
            return;
        }

        setIsLoading(true);
        try {
            await api.post('/invitations', {
                groupId: selectedGroupId,
                firstName: firstName,
                lastName: lastName,
                email: email,
                message: message
            });
            toast.success("Invitation envoyée avec succès !");
            
            // Clean up
            setFirstName('');
            setLastName('');
            setEmail('');
            setMessage('');
            onClose();
        } catch (err) {
            toast.error(err.response?.data?.message || "Erreur lors de l'envoi de l'invitation.");
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-surface-container-lowest/80 backdrop-blur-sm font-body text-on-surface">
            {/* Modal Container */}
            <div className="w-full max-w-lg bg-surface border border-outline-variant/40 rounded-xl overflow-hidden shadow-2xl shadow-black/50">
                {/* Modal Header */}
                <div className="px-8 pt-8 pb-4">
                    <h2 className="text-[30px] font-black tracking-tight text-on-surface leading-none mb-1">Inviter dans un cercle</h2>
                    <div className="h-1 w-12 bg-primary rounded-full mb-6"></div>
                    
                    {/* Select Group */}
                    <div className="bg-surface-container-low rounded-lg p-4 mb-4 border-l-4 border-primary/50">
                         <label className="text-[10px] font-bold uppercase tracking-widest text-on-surface-variant mb-1 block">Choisissez le cercle ciblé</label>
                         <select
                            className="w-full bg-slate-800 border-none focus:ring-2 focus:ring-primary rounded-lg py-2 pl-3 pr-4 text-[16px] font-bold text-primary transition-all outline-none"
                            value={selectedGroupId}
                            onChange={(e) => setSelectedGroupId(e.target.value)}
                            required
                        >
                            {userGroups.map((group) => (
                                <option key={group.id || group.Id} value={group.id || group.Id}>
                                    {group.name || group.Name}
                                </option>
                            ))}
                        </select>
                    </div>
                </div>

                {/* Modal Body (Form) */}
                <form className="px-8 pb-8 space-y-6" onSubmit={handleSubmit}>
                    
                    <div className="grid grid-cols-2 gap-4">
                        {/* First Name */}
                        <div className="space-y-2">
                            <label className="text-xs font-semibold text-on-surface-variant ml-1" htmlFor="firstName">Prénom</label>
                            <input
                                className="w-full bg-slate-800 border-none focus:ring-2 focus:ring-primary rounded-lg py-3 px-4 text-on-surface placeholder:text-slate-600 transition-all outline-none"
                                id="firstName" 
                                placeholder="Jean" 
                                type="text" 
                                value={firstName}
                                onChange={(e) => setFirstName(e.target.value)}
                                required
                            />
                        </div>

                        {/* Last Name */}
                        <div className="space-y-2">
                            <label className="text-xs font-semibold text-on-surface-variant ml-1" htmlFor="lastName">Nom</label>
                            <input
                                className="w-full bg-slate-800 border-none focus:ring-2 focus:ring-primary rounded-lg py-3 px-4 text-on-surface placeholder:text-slate-600 transition-all outline-none"
                                id="lastName" 
                                placeholder="Tremblay" 
                                type="text" 
                                value={lastName}
                                onChange={(e) => setLastName(e.target.value)}
                                required
                            />
                        </div>
                    </div>

                    {/* Input Group - Email */}
                    <div className="space-y-2">
                        <label className="text-xs font-semibold text-on-surface-variant ml-1" htmlFor="email">Adresse Courriel</label>
                        <div className="relative">
                            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-on-surface-variant">
                                <span className="material-symbols-outlined text-lg">mail</span>
                            </div>
                            <input
                                className="w-full bg-slate-800 border-none focus:ring-2 focus:ring-primary rounded-lg py-3 pl-10 pr-4 text-on-surface placeholder:text-slate-600 transition-all outline-none"
                                id="email" 
                                placeholder="collegue@exemple.com" 
                                type="email" 
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                required
                            />
                        </div>
                    </div>

                    {/* Textarea Group - Message */}
                    <div className="space-y-2">
                        <label className="text-xs font-semibold text-on-surface-variant ml-1" htmlFor="message">Message personnel (optionnel)</label>
                        <textarea
                            className="w-full bg-slate-800 border-none focus:ring-2 focus:ring-primary rounded-lg p-4 text-on-surface placeholder:text-slate-600 transition-all outline-none resize-none"
                            id="message" 
                            placeholder="Rejoins notre galerie privée..." 
                            rows="3"
                            value={message}
                            onChange={(e) => setMessage(e.target.value)}
                        />
                    </div>

                    {/* Actions */}
                    <div className="flex flex-col sm:flex-row-reverse gap-3 pt-2">
                        <button
                            className="flex-1 bg-primary text-on-primary font-bold py-3 px-6 rounded-lg hover:brightness-110 active:scale-[0.98] transition-all flex items-center justify-center gap-2 disabled:opacity-50"
                            type="submit"
                            disabled={isLoading}
                        >
                            {isLoading ? 'Envoi...' : 'Envoyer l\'invitation'}
                            {!isLoading && <span className="material-symbols-outlined text-lg">send</span>}
                        </button>
                        <button
                            className="flex-1 bg-slate-800 text-on-surface-variant font-semibold py-3 px-6 rounded-lg hover:bg-slate-700 active:scale-[0.98] transition-all"
                            type="button"
                            onClick={onClose}
                            disabled={isLoading}
                        >
                            Annuler
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default InviteModal;
