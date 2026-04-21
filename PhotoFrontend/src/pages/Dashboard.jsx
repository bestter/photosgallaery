import React, { useState, useEffect } from 'react';
import { getUserRole, isTokenExpired, getUsernameFromToken } from '../authHelper';
import api from '../api';

export default function Dashboard() {
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const currentUsername = getUsernameFromToken(localStorage.getItem('token'));

    // Vérification de la session et du rôle via le token
    useEffect(() => {
        const token = localStorage.getItem('token');
        if (!token || isTokenExpired(token) || getUserRole(token) !== 'Admin') {
            window.location.href = '/';
            return;
        }

        const fetchUsers = async () => {
            try {
                const response = await api.get('/admin/users');
                setUsers(response.data);
            } catch (error) {
                console.error("Erreur lors de la récupération des usagers:", error);
            } finally {
                setLoading(false);
            }
        };

        fetchUsers();
    }, []);

    const handleRoleUpdate = async (userId, newRole) => {
        try {
            await api.put(`/admin/users/${userId}/role`, { role: newRole });
            setUsers(prev => prev.map(u => (u.id === userId || u.Id === userId) ? { ...u, role: newRole, Role: newRole } : u));
        } catch (error) {
            console.error(`Erreur lors de la promotion en ${newRole}:`, error);
            alert(`Erreur lors de la modification du rôle.`);
        }
    };

    const handleLogout = () => {
        localStorage.removeItem('token');
        window.location.href = '/login';
    };

    return (
        <div className="flex h-screen overflow-hidden bg-background-light dark:bg-background-dark text-slate-900 dark:text-slate-100 font-display">
            {/* Sidebar Navigation */}
            <aside className="w-72 bg-slate-50 dark:bg-slate-900/50 border-r border-slate-200 dark:border-slate-800 flex flex-col">
                <div className="p-6 flex items-center gap-3 cursor-pointer" onClick={() => window.location.href = '/'}>
                    <div className="w-10 h-10 rounded-lg bg-primary flex items-center justify-center text-background-dark">
                        <span className="material-symbols-outlined font-bold">visibility</span>
                    </div>
                    <div>
                        <h1 className="text-xl font-bold tracking-tight text-slate-900 dark:text-white leading-none">PixelLyra.com</h1>
                        <p className="text-xs text-slate-500 dark:text-primary/70 font-medium">Admin Console</p>
                    </div>
                </div>
                <nav className="flex-1 px-4 py-4 space-y-2">
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-lg transition-colors" href="#">
                        <span className="material-symbols-outlined">dashboard</span>
                        <span className="font-medium">Dashboard</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-primary bg-primary/20 border-l-4 border-primary rounded-r-lg transition-colors" href="#">
                        <span className="material-symbols-outlined">person</span>
                        <span className="font-medium">Gestion des usagers</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-lg transition-colors" href="/admin-groups" onClick={(e) => { e.preventDefault(); window.location.href = '/admin-groups'; }}>
                        <span className="material-symbols-outlined">group</span>
                        <span className="font-medium">Gestion des groupes</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-lg transition-colors" href="/moderation" onClick={(e) => { e.preventDefault(); window.location.href = '/moderation'; }}>
                        <span className="material-symbols-outlined">shield</span>
                        <span className="font-medium">Modération</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-lg transition-colors" href="#">
                        <span className="material-symbols-outlined">analytics</span>
                        <span className="font-medium">Analytiques</span>
                    </a>
                    <div className="pt-4 pb-2 px-4">
                        <p className="text-[10px] uppercase tracking-widest text-slate-400 font-bold">Système</p>
                    </div>
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-lg transition-colors" href="#">
                        <span className="material-symbols-outlined">settings</span>
                        <span className="font-medium">Paramètres</span>
                    </a>
                </nav>
                <div className="p-4 border-t border-slate-200 dark:border-slate-800">
                    <div className="flex items-center gap-3 p-2 bg-slate-200/50 dark:bg-slate-800/50 rounded-xl">
                        <div className="w-10 h-10 rounded-full bg-slate-300 dark:bg-slate-700 overflow-hidden">
                            <img className="w-full h-full object-cover" alt="Photo de profil de l'administrateur" src="https://lh3.googleusercontent.com/aida-public/AB6AXuAsc4bmTKugYUORaxc6vxqflO6NoNmjWPxQKbgJtBcgJP8TSjnvs2V_1ASI4iun3bziTFq4DNMCrp15vG2wzcLq1t4YZtJXEmL6uxnJ1BGwNz--nCnzxRQe7KwSDCm1aVnr00WyhUnU6u2oWRLV2itIcEdcCvc7J3rOwP2XD7tWLFNge-rXJVcztBnIVJLHxtKLe0fJpJuBSKGtVYRzVy-AhgLzplthWJ_CVnLHCn50m7sh8ZJuVbTvYEPszjbk91Ny229YHHH_Ulg" />
                        </div>
                        <div className="flex-1 min-w-0">
                            <p className="text-sm font-semibold truncate">Marc-André V.</p>
                            <p className="text-xs text-slate-500 truncate">Super Admin</p>
                        </div>
                        <button onClick={handleLogout} className="text-slate-400 hover:text-slate-600 dark:hover:text-white">
                            <span className="material-symbols-outlined">logout</span>
                        </button>
                    </div>
                </div>
            </aside>
            {/* Main Content */}
            <main className="flex-1 flex flex-col overflow-hidden bg-background-light dark:bg-background-dark">
                {/* Header */}
                <header className="h-20 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between px-8 bg-white/50 dark:bg-background-dark/50 backdrop-blur-md">
                    <div className="flex items-center gap-4 flex-1 max-w-xl">
                        <div className="relative w-full">
                            <span className="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-xl">search</span>
                            <input className="w-full pl-11 pr-4 py-2.5 bg-slate-100 dark:bg-slate-800 border-none rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all" placeholder="Rechercher un utilisateur, un email..." type="text" />
                        </div>
                    </div>
                    <div className="flex items-center gap-4">
                        <button className="relative p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors">
                            <span className="material-symbols-outlined">notifications</span>
                            <span className="absolute top-2 right-2 w-2 h-2 bg-primary rounded-full ring-2 ring-white dark:ring-background-dark"></span>
                        </button>
                        <button className="p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors" onClick={() => document.documentElement.classList.toggle('dark')}>
                            <span className="material-symbols-outlined">dark_mode</span>
                        </button>
                    </div>
                </header>
                {/* Dashboard Content */}
                <div className="flex-1 overflow-y-auto p-8">
                    <div className="mb-8">
                        <h2 className="text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">Gestion des usagers</h2>
                        <p className="text-slate-500 dark:text-slate-400 mt-1">Gérez les rôles, les permissions et la sécurité des comptes utilisateurs.</p>
                    </div>
                    {/* Stats Summary */}
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
                        <div className="bg-white dark:bg-slate-900/40 p-6 rounded-2xl border border-slate-200 dark:border-slate-800">
                            <div className="flex items-center justify-between mb-4">
                                <span className="p-2 bg-primary/10 text-primary rounded-lg material-symbols-outlined">person</span>
                            </div>
                            <p className="text-slate-500 dark:text-slate-400 text-sm font-medium">Utilisateurs Totaux</p>
                            <h3 className="text-3xl font-bold mt-1">{loading ? '-' : users.length}</h3>
                        </div>
                        <div className="bg-white dark:bg-slate-900/40 p-6 rounded-2xl border border-slate-200 dark:border-slate-800">
                            <div className="flex items-center justify-between mb-4">
                                <span className="p-2 bg-primary/10 text-primary rounded-lg material-symbols-outlined">camera</span>
                            </div>
                            <p className="text-slate-500 dark:text-slate-400 text-sm font-medium">Créateurs Actifs</p>
                            <h3 className="text-3xl font-bold mt-1">{loading ? '-' : users.filter(u => u.role === 'Creator' || u.Role === 'Creator').length}</h3>
                        </div>
                    </div>
                    {/* Users Table */}
                    <div className="bg-white dark:bg-slate-900/40 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
                        <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
                            <h4 className="font-bold text-lg">Liste des utilisateurs</h4>
                            <div className="flex gap-2">
                                <button className="flex items-center gap-2 px-4 py-2 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-sm font-medium rounded-lg transition-colors">
                                    <span className="material-symbols-outlined text-sm">filter_list</span> Filtrer
                                </button>
                                <button className="flex items-center gap-2 px-4 py-2 bg-primary text-background-dark hover:bg-primary/90 text-sm font-bold rounded-lg transition-colors">
                                    <span className="material-symbols-outlined text-sm">person_add</span> Nouvel usager
                                </button>
                            </div>
                        </div>
                        <div className="overflow-x-auto">
                            <table className="w-full text-left">
                                <thead className="bg-slate-50 dark:bg-slate-800/50">
                                    <tr>
                                        <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Nom d'usager</th>
                                        <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Adresse courriel</th>
                                        <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Groupes</th>
                                        <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Rôle actuel</th>
                                        <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">Actions</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                                    {loading ? (
                                        <tr><td colSpan="5" className="text-center py-4 text-slate-500">Chargement...</td></tr>
                                    ) : users.map(user => {
                                        const userId = user.id || user.Id;
                                        const username = user.username || user.Username;
                                        const email = user.email || user.Email;
                                        const role = user.role || user.Role;
                                        const groups = user.groups || user.Groups || [];
                                        const isCurrentUser = username === currentUsername;

                                        return (
                                            <tr key={userId} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                                                <td className="px-6 py-4 whitespace-nowrap">
                                                    <div className="flex items-center gap-3">
                                                        <div className="w-8 h-8 rounded-full bg-slate-200 dark:bg-slate-700 overflow-hidden flex items-center justify-center font-bold text-slate-500 dark:text-slate-300">
                                                            {username ? username.substring(0, 2).toUpperCase() : '?'}
                                                        </div>
                                                        <span className="font-semibold text-slate-900 dark:text-white">{username}</span>
                                                    </div>
                                                </td>
                                                <td className="px-6 py-4 text-slate-500 dark:text-slate-400">{email}</td>
                                                <td className="px-6 py-4">
                                                    {groups && groups.length > 0 ? (
                                                        groups.length > 2 ? (
                                                            <div className="group relative inline-block cursor-pointer">
                                                                <span className="px-2.5 py-1 text-xs font-medium bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 rounded-full border border-slate-200 dark:border-slate-700 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
                                                                    Groupes ({groups.length})
                                                                </span>
                                                                <div className="absolute left-1/2 -translate-x-1/2 bottom-full mb-2 hidden group-hover:block w-max bg-slate-800 text-white text-xs rounded-lg py-2 px-3 shadow-lg z-10 before:content-[''] before:absolute before:-bottom-1 before:left-1/2 before:-translate-x-1/2 before:border-4 before:border-transparent before:border-t-slate-800">
                                                                    <div className="flex flex-col gap-1 items-center">
                                                                        {groups.map((g, i) => (
                                                                            <span key={i} className="whitespace-nowrap font-medium">{g}</span>
                                                                        ))}
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        ) : (
                                                            <div className="flex flex-wrap gap-1">
                                                                {groups.map((g, i) => (
                                                                    <span key={i} className="px-2.5 py-1 text-xs font-medium bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 rounded-full border border-slate-200 dark:border-slate-700">
                                                                        {g}
                                                                    </span>
                                                                ))}
                                                            </div>
                                                        )
                                                    ) : (
                                                        <span className="text-xs text-slate-400 italic">Aucun</span>
                                                    )}
                                                </td>
                                                <td className="px-6 py-4">
                                                    {role === 'Admin' && <span className="px-2.5 py-1 text-xs font-bold bg-emerald-500/20 text-emerald-500 rounded-full border border-emerald-500/30">Admin</span>}
                                                    {role === 'Creator' && <span className="px-2.5 py-1 text-xs font-medium bg-primary/20 text-primary rounded-full border border-primary/30">Créateur</span>}
                                                    {(role === 'User' || (!role && role !== 'Forbidden')) && <span className="px-2.5 py-1 text-xs font-medium bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 rounded-full border border-slate-200 dark:border-slate-700">Utilisateur</span>}
                                                    {role === 'Forbidden' && <span className="px-2.5 py-1 text-xs font-bold bg-red-500/20 text-red-500 rounded-full border border-red-500/30">Banni</span>}
                                                </td>
                                                <td className="px-6 py-4 text-right">
                                                    <div className="flex items-center justify-end gap-2">
                                                        {role !== 'Admin' && (
                                                            role === 'Creator' ? (
                                                                <button onClick={() => handleRoleUpdate(userId, 'User')} className="px-3 py-1.5 text-xs font-bold text-amber-600 border border-amber-500/50 hover:bg-amber-500/10 rounded-lg transition-colors">Rétrograder créateur</button>
                                                            ) : (
                                                                <button onClick={() => handleRoleUpdate(userId, 'Creator')} className="px-3 py-1.5 text-xs font-bold text-primary border border-primary/50 hover:bg-primary/10 rounded-lg transition-colors">Promouvoir créateur</button>
                                                            )
                                                        )}
                                                        {role === 'Admin' ? (
                                                            isCurrentUser ? (
                                                                <button disabled className="px-3 py-1.5 text-xs font-bold text-slate-400 border border-slate-400/30 cursor-not-allowed rounded-lg bg-slate-100 dark:bg-slate-800" title="Action non permise sur votre propre compte">Admin actuel</button>
                                                            ) : (
                                                                <button onClick={() => handleRoleUpdate(userId, 'User')} className="px-3 py-1.5 text-xs font-bold text-amber-600 border border-amber-500/50 hover:bg-amber-500/10 rounded-lg transition-colors">Rétrograder admin</button>
                                                            )
                                                        ) : (
                                                            <button onClick={() => handleRoleUpdate(userId, 'Admin')} className="px-3 py-1.5 text-xs font-bold bg-primary text-background-dark hover:bg-primary/90 rounded-lg transition-colors">Promouvoir admin</button>
                                                        )}
                                                        <button
                                                            disabled={isCurrentUser}
                                                            onClick={() => {
                                                                if (!isCurrentUser) {
                                                                    handleRoleUpdate(userId, role === 'Forbidden' ? 'User' : 'Forbidden');
                                                                }
                                                            }}
                                                            className={`p-1.5 rounded-lg transition-colors ${isCurrentUser ? 'text-slate-400 cursor-not-allowed bg-slate-100 dark:bg-slate-800' : (role === 'Forbidden' ? 'text-emerald-500 hover:bg-emerald-500/10' : 'text-red-500 hover:bg-red-500/10')}`}
                                                            title={isCurrentUser ? "Action non permise sur votre propre compte" : (role === 'Forbidden' ? "Réactiver ce compte" : "Bannir")}
                                                        >
                                                            <span className="material-symbols-outlined text-lg leading-none">{role === 'Forbidden' ? 'lock_open' : 'block'}</span>
                                                        </button>
                                                    </div>
                                                </td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                        </div>
                        <div className="px-6 py-4 bg-slate-50 dark:bg-slate-800/30 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between">
                            <p className="text-xs text-slate-500 font-medium tracking-wide">Affichage de {users.length} utilisateurs</p>
                            <div className="flex gap-1">
                                <button className="w-8 h-8 flex items-center justify-center rounded bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-400 hover:text-primary transition-colors">
                                    <span className="material-symbols-outlined text-sm">chevron_left</span>
                                </button>
                                <button className="w-8 h-8 flex items-center justify-center rounded bg-primary text-background-dark font-bold text-xs">1</button>
                                <button className="w-8 h-8 flex items-center justify-center rounded bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 hover:text-primary transition-colors text-xs font-bold">2</button>
                                <button className="w-8 h-8 flex items-center justify-center rounded bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 hover:text-primary transition-colors text-xs font-bold">3</button>
                                <button className="w-8 h-8 flex items-center justify-center rounded bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-400 hover:text-primary transition-colors">
                                    <span className="material-symbols-outlined text-sm">chevron_right</span>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </main>
        </div>
    );
}
