import React, { useState, useEffect } from 'react';
import { getUserRole, isTokenExpired } from '../authHelper';
import api from '../api';

export default function Moderation() {
    const [reports, setReports] = useState([]);

    // Vérification de la session et du rôle via le token
    useEffect(() => {
        const token = localStorage.getItem('token');
        if (!token || isTokenExpired(token) || getUserRole(token) !== 'Admin') {
            window.location.href = '/';
            return;
        }

        const fetchReports = async () => {
            try {
                const response = await api.get('/admin/reports');
                setReports(response.data);
            } catch (error) {
                console.error("Erreur lors de la récupération des signalements:", error);
            }
        };

        fetchReports();
    }, []);

    const totalReports = reports.length;
    const processedReports = reports.filter(r => r.status === 'Processed' || r.Status === 'Processed').length;
    const pendingReports = totalReports - processedReports;

    const pendingReportsList = reports.filter(r => r.status === 'Pending' || r.Status === 'Pending' || (!r.status && !r.Status));

    const handleDismiss = async (reportId) => {
        try {
            await api.delete(`/admin/reports/${reportId}`);
            setReports(prev => prev.map(r =>
                (r.reportId === reportId || r.ReportId === reportId)
                    ? { ...r, status: 'Processed', Status: 'Processed' }
                    : r
            ));
        } catch (error) {
            console.error("Erreur lors de l'annulation du signalement:", error);
        }
    };

    const getImageUrl = (url) => {
        if (!url) return '';
        if (url.startsWith('http')) return url;
        const backendRoot = api.defaults.baseURL.replace(/\/api$/, '');
        return backendRoot + url;
    };

    const handleLogout = () => {
        localStorage.removeItem('token');
        window.location.href = '/login';
    };

    return (
        <div className="flex min-h-screen bg-background-light dark:bg-background-dark text-slate-900 dark:text-slate-100 font-display">
            {/* Sidebar */}
            <aside className="w-64 bg-slate-50 dark:bg-slate-900/50 border-r border-slate-200 dark:border-primary/10 flex flex-col">
                <div className="p-6 cursor-pointer" onClick={() => window.location.href = '/'}>
                    <div className="flex items-center gap-2">
                        <span className="material-symbols-outlined text-primary text-3xl">visibility</span>
                        <div>
                            <h1 className="text-xl font-bold tracking-tight text-slate-900 dark:text-white leading-none">PixelLyra.com Admin</h1>
                            <p className="text-xs text-primary/70 font-medium mt-1">Système de Modération</p>
                        </div>
                    </div>
                </div>
                <nav className="flex-1 px-3 py-4 space-y-1">
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-primary/5 rounded-lg transition-colors" href="/" onClick={(e) => { e.preventDefault(); window.location.href = '/'; }}>
                        <span className="material-symbols-outlined">dashboard</span>
                        <span className="text-sm font-medium">Tableau de bord</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-primary bg-primary/20 border-l-4 border-primary rounded-r-lg transition-colors" href="#" onClick={(e) => { e.preventDefault(); }}>
                        <span className="material-symbols-outlined">shield_with_heart</span>
                        <span className="text-sm font-medium">Modération</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-primary/5 rounded-lg transition-colors" href="/dashboard" onClick={(e) => { e.preventDefault(); window.location.href = '/dashboard'; }}>
                        <span className="material-symbols-outlined">person</span>
                        <span className="text-sm font-medium">Gestion des usagers</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-primary/5 rounded-lg transition-colors" href="/admin-groups" onClick={(e) => { e.preventDefault(); window.location.href = '/admin-groups'; }}>
                        <span className="material-symbols-outlined">group</span>
                        <span className="text-sm font-medium">Gestion des groupes</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-primary/5 rounded-lg transition-colors" href="#" onClick={(e) => e.preventDefault()}>
                        <span className="material-symbols-outlined">settings</span>
                        <span className="text-sm font-medium">Paramètres</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-primary/5 rounded-lg transition-colors" href="#" onClick={(e) => e.preventDefault()}>
                        <span className="material-symbols-outlined">description</span>
                        <span className="text-sm font-medium">Logs système</span>
                    </a>
                </nav>
                <div className="p-4 border-t border-slate-200 dark:border-primary/10">
                    <div className="flex items-center gap-3 p-2">
                        <div className="w-10 h-10 rounded-full bg-primary/20 flex items-center justify-center border border-primary/30">
                            <span className="material-symbols-outlined text-primary">account_circle</span>
                        </div>
                        <div className="overflow-hidden flex-1">
                            <p className="text-sm font-semibold truncate">Alexandre M.</p>
                            <p className="text-xs text-slate-500 truncate">Super Admin</p>
                        </div>
                        <button onClick={handleLogout} className="text-slate-400 hover:text-slate-600 dark:hover:text-white ml-2">
                            <span className="material-symbols-outlined">logout</span>
                        </button>
                    </div>
                </div>
            </aside>
            {/* Main Content */}
            <main className="flex-1 p-8 overflow-y-auto">
                <header className="mb-8 flex justify-between items-center">
                    <div>
                        <h2 className="text-2xl font-bold text-slate-900 dark:text-white">Centre de Modération</h2>
                        <p className="text-slate-500 dark:text-slate-400">Gérez les contenus signalés par la communauté.</p>
                    </div>
                    <button className="p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors" onClick={() => document.documentElement.classList.toggle('dark')}>
                        <span className="material-symbols-outlined">dark_mode</span>
                    </button>
                </header>
                {/* Stats Grid */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-10">
                    <div className="bg-white dark:bg-slate-900 p-6 rounded-xl border border-slate-200 dark:border-primary/10 shadow-sm">
                        <div className="flex justify-between items-start">
                            <div>
                                <p className="text-sm font-medium text-slate-500 dark:text-slate-400">Total Signalements</p>
                                <p className="text-3xl font-bold mt-1">{totalReports}</p>
                            </div>
                            <div className="p-2 bg-primary/10 rounded-lg">
                                <span className="material-symbols-outlined text-primary">flag</span>
                            </div>
                        </div>
                        <div className="mt-4 flex items-center gap-1">
                            <span className="text-emerald-500 text-sm font-semibold">+12%</span>
                            <span className="text-xs text-slate-400 italic">depuis le mois dernier</span>
                        </div>
                    </div>
                    <div className="bg-white dark:bg-slate-900 p-6 rounded-xl border border-slate-200 dark:border-primary/10 shadow-sm">
                        <div className="flex justify-between items-start">
                            <div>
                                <p className="text-sm font-medium text-slate-500 dark:text-slate-400">En attente</p>
                                <p className="text-3xl font-bold mt-1 text-primary">{pendingReports}</p>
                            </div>
                            <div className="p-2 bg-amber-500/10 rounded-lg">
                                <span className="material-symbols-outlined text-amber-500">pending_actions</span>
                            </div>
                        </div>
                        <div className="mt-4 flex items-center gap-1">
                            <span className="text-rose-500 text-sm font-semibold">-5%</span>
                            <span className="text-xs text-slate-400 italic">réduction du backlog</span>
                        </div>
                    </div>
                    <div className="bg-white dark:bg-slate-900 p-6 rounded-xl border border-slate-200 dark:border-primary/10 shadow-sm">
                        <div className="flex justify-between items-start">
                            <div>
                                <p className="text-sm font-medium text-slate-500 dark:text-slate-400">Traités</p>
                                <p className="text-3xl font-bold mt-1">{processedReports}</p>
                            </div>
                            <div className="p-2 bg-emerald-500/10 rounded-lg">
                                <span className="material-symbols-outlined text-emerald-500">check_circle</span>
                            </div>
                        </div>
                        <div className="mt-4 flex items-center gap-1">
                            <span className="text-emerald-500 text-sm font-semibold">+8%</span>
                            <span className="text-xs text-slate-400 italic">efficacité accrue</span>
                        </div>
                    </div>
                </div>
                {/* Flagged Images Table Section */}
                <div className="bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-primary/10 shadow-sm overflow-hidden">
                    <div className="p-6 border-b border-slate-200 dark:border-primary/10 flex flex-wrap justify-between items-center gap-4">
                        <h3 className="text-lg font-bold">Images Signalées</h3>
                        <div className="flex gap-3">
                            <div className="relative">
                                <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-sm">search</span>
                                <input className="pl-10 pr-4 py-2 bg-slate-100 dark:bg-slate-800 border-none rounded-lg text-sm focus:ring-2 focus:ring-primary w-64" placeholder="Rechercher..." type="text" />
                            </div>
                            <button className="flex items-center gap-2 px-4 py-2 bg-primary/10 text-primary rounded-lg text-sm font-medium hover:bg-primary/20 transition-colors">
                                <span className="material-symbols-outlined text-sm">filter_list</span> Filtrer
                            </button>
                        </div>
                    </div>
                    <div className="overflow-x-auto">
                        <table className="w-full text-left border-collapse">
                            <thead>
                                <tr className="bg-slate-50 dark:bg-slate-800/50">
                                    <th className="px-6 py-4 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">Image</th>
                                    <th className="px-6 py-4 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">Nom de l'image</th>
                                    <th className="px-6 py-4 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">Utilisateur signalé</th>
                                    <th className="px-6 py-4 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">Motif du signalement</th>
                                    <th className="px-6 py-4 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 text-right">Actions</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-200 dark:divide-primary/10">
                                {pendingReportsList.map(report => {
                                    const rId = report.reportId || report.ReportId;
                                    const pUrl = report.photoUrl || report.PhotoUrl;
                                    const uploader = report.uploader || report.Uploader;
                                    const reason = report.reason || report.Reason;
                                    const photoId = report.photoId || report.PhotoId;

                                    return (
                                        <tr key={rId} className="hover:bg-slate-50 dark:hover:bg-primary/5 transition-colors">
                                            <td className="px-6 py-4">
                                                <div className="w-12 h-12 rounded bg-slate-200 dark:bg-slate-800 overflow-hidden border border-slate-300 dark:border-slate-700">
                                                    <img className="w-full h-full object-cover" alt="Image signalée" src={getImageUrl(pUrl)} />
                                                </div>
                                            </td>
                                            <td className="px-6 py-4">
                                                <a className="text-primary font-medium hover:underline text-sm" href="#" onClick={e => e.preventDefault()}>Photo #{photoId}</a>
                                            </td>
                                            <td className="px-6 py-4">
                                                <div className="flex items-center gap-2">
                                                    <div className="w-6 h-6 rounded-full bg-primary/20 flex items-center justify-center text-[10px] font-bold text-primary">
                                                        {uploader ? uploader.substring(0, 2).toUpperCase() : '??'}
                                                    </div>
                                                    <span className="text-primary font-medium text-sm">{uploader || 'Anonyme'}</span>
                                                </div>
                                            </td>
                                            <td className="px-6 py-4">
                                                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-rose-100 dark:bg-rose-500/10 text-rose-700 dark:text-rose-400 border border-rose-200 dark:border-rose-500/20">{reason}</span>
                                            </td>
                                            <td className="px-6 py-4 text-right space-x-2">
                                                <button className="px-3 py-1.5 bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold rounded shadow-sm transition-all uppercase tracking-wide">Effacer</button>
                                                <button onClick={() => handleDismiss(rId)} className="px-3 py-1.5 bg-slate-200 dark:bg-slate-800 hover:bg-slate-300 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-xs font-bold rounded shadow-sm transition-all uppercase tracking-wide">Annuler</button>
                                            </td>
                                        </tr>
                                    );
                                })}
                                {pendingReportsList.length === 0 && (
                                    <tr>
                                        <td colSpan="5" className="px-6 py-4 text-center text-slate-500">
                                            Aucun signalement en attente.
                                        </td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                    {/* Pagination */}
                    <div className="p-6 border-t border-slate-200 dark:border-primary/10 flex items-center justify-between">
                        <p className="text-sm text-slate-500 dark:text-slate-400">Affichage de 1 à 4 sur 156 résultats</p>
                        <div className="flex gap-2">
                            <button className="px-3 py-1 border border-slate-200 dark:border-primary/20 rounded hover:bg-slate-50 dark:hover:bg-primary/10 disabled:opacity-50 transition-all text-sm font-medium">Précédent</button>
                            <button className="px-3 py-1 bg-primary text-background-dark font-bold rounded text-sm">1</button>
                            <button className="px-3 py-1 border border-slate-200 dark:border-primary/20 rounded hover:bg-slate-50 dark:hover:bg-primary/10 transition-all text-sm font-medium">2</button>
                            <button className="px-3 py-1 border border-slate-200 dark:border-primary/20 rounded hover:bg-slate-50 dark:hover:bg-primary/10 transition-all text-sm font-medium">3</button>
                            <button className="px-3 py-1 border border-slate-200 dark:border-primary/20 rounded hover:bg-slate-50 dark:hover:bg-primary/10 transition-all text-sm font-medium">Suivant</button>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    );
}
