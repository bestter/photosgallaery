import React, { useState, useEffect } from 'react';
import { getUserRole, isTokenExpired } from '../authHelper';
import api from '../api';

export default function AdminGroups() {
    const [groups, setGroups] = useState([]);
    const [loading, setLoading] = useState(true);
    const [newGroupName, setNewGroupName] = useState('');

    // Member management state
    const [selectedGroup, setSelectedGroup] = useState(null);
    const [members, setMembers] = useState([]);
    const [allUsers, setAllUsers] = useState([]);
    const [selectedUserId, setSelectedUserId] = useState('');
    const [selectedRole, setSelectedRole] = useState(1);

    useEffect(() => {
        const token = localStorage.getItem('token');
        if (!token || isTokenExpired(token) || getUserRole(token) !== 'Admin') {
            window.location.href = '/';
            return;
        }

        const fetchGroups = async () => {
            try {
                const response = await api.get('/admin/groups');
                const loadedGroups = response.data;
                setGroups(loadedGroups);
                
                // Read from URL
                const params = new URLSearchParams(window.location.search);
                const queryGroupId = params.get('groupId');
                if (queryGroupId) {
                    const groupToSelect = loadedGroups.find(g => (g.id || g.Id) === queryGroupId);
                    if (groupToSelect) {
                        handleManageMembers(groupToSelect, false);
                    }
                }
            } catch (error) {
                console.error("Erreur lors de la récupération des groupes:", error);
            } finally {
                setLoading(false);
            }
        };

        fetchGroups();
    }, []);

    const handleCreateGroup = async (e) => {
        e.preventDefault();
        if (!newGroupName.trim()) return;

        try {
            const response = await api.post('/admin/groups', { name: newGroupName });
            setGroups([response.data, ...groups]);
            setNewGroupName('');
        } catch (error) {
            console.error("Erreur lors de la création du groupe:", error);
            alert("Erreur lors de la création.");
        }
    };

    const handleDeleteGroup = async (id) => {
        if (!window.confirm("Êtes-vous sûr de vouloir supprimer ce groupe ?")) return;
        try {
            await api.delete(`/admin/groups/${id}`);
            setGroups(groups.filter(g => (g.id || g.Id) !== id));
        } catch (error) {
            console.error("Erreur lors de la suppression du groupe:", error);
            alert("Erreur lors de la suppression.");
        }
    };

    const handleManageMembers = async (group, updateUrl = true) => {
        setSelectedGroup(group);
        const groupId = group.id || group.Id;
        
        if (updateUrl) {
            const url = new URL(window.location.href);
            url.searchParams.set('groupId', groupId);
            window.history.pushState({}, '', url);
        }

        try {
            const res = await api.get(`/admin/groups/${groupId}/members`);
            setMembers(res.data);

            if (allUsers.length === 0) {
                const usersRes = await api.get('/admin/users');
                setAllUsers(usersRes.data);
            }
        } catch (error) {
            console.error("Erreur récupération membres", error);
        }
    };

    const handleBackToGroups = () => {
        setSelectedGroup(null);
        const url = new URL(window.location.href);
        url.searchParams.delete('groupId');
        window.history.pushState({}, '', url);
    };

    const handleAddMember = async (e) => {
        e.preventDefault();
        if (!selectedUserId) return;
        const groupId = selectedGroup.id || selectedGroup.Id;
        try {
            await api.post(`/admin/groups/${groupId}/members`, { userId: parseInt(selectedUserId), role: selectedRole });
            // Refresh members
            const res = await api.get(`/admin/groups/${groupId}/members`);
            setMembers(res.data);
            
            // Update group userCount
            setGroups(groups.map(g => (g.id || g.Id) === groupId ? { ...g, userCount: (g.userCount || 0) + 1 } : g));
            setSelectedUserId('');
        } catch (error) {
            alert(error.response?.data?.message || "Erreur lors de l'ajout.");
        }
    };

    const handleRemoveMember = async (userId) => {
        if (!window.confirm("Confirmer le retrait de cet utilisateur ?")) return;
        const groupId = selectedGroup.id || selectedGroup.Id;
        try {
            await api.delete(`/admin/groups/${groupId}/members/${userId}`);
            setMembers(members.filter(m => (m.userId || m.UserId) !== userId));
            // Update group userCount
            setGroups(groups.map(g => (g.id || g.Id) === groupId ? { ...g, userCount: Math.max((g.userCount || 1) - 1, 0) } : g));
        } catch (error) {
            alert("Erreur lors du retrait.");
        }
    };

    const handleRoleChange = async (userId, newRole) => {
        const groupId = selectedGroup.id || selectedGroup.Id;
        try {
            await api.put(`/admin/groups/${groupId}/members/${userId}/role`, { role: newRole });
            setMembers(members.map(m => (m.userId || m.UserId) === userId ? { ...m, role: newRole, Role: newRole } : m));
        } catch (error) {
            console.error("Erreur mise à jour rôle:", error);
            alert("Erreur lors de la mise à jour du rôle.");
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
                        <h1 className="text-xl font-bold tracking-tight text-slate-900 dark:text-white leading-none">Vision</h1>
                        <p className="text-xs text-slate-500 dark:text-primary/70 font-medium">Admin Console</p>
                    </div>
                </div>
                <nav className="flex-1 px-4 py-4 space-y-2">
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-lg transition-colors" href="#">
                        <span className="material-symbols-outlined">dashboard</span>
                        <span className="font-medium">Dashboard</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-lg transition-colors" href="/dashboard" onClick={(e) => { e.preventDefault(); window.location.href = '/dashboard'; }}>
                        <span className="material-symbols-outlined">person</span>
                        <span className="font-medium">Gestion des usagers</span>
                    </a>
                    <a className="flex items-center gap-3 px-4 py-3 text-primary bg-primary/20 border-l-4 border-primary rounded-r-lg transition-colors" href="/admin-groups" onClick={(e) => { e.preventDefault(); window.location.href = '/admin-groups'; }}>
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
                            <img className="w-full h-full object-cover" alt="Photo" src="https://lh3.googleusercontent.com/aida-public/AB6AXuAsc4bmTKugYUORaxc6vxqflO6NoNmjWPxQKbgJtBcgJP8TSjnvs2V_1ASI4iun3bziTFq4DNMCrp15vG2wzcLq1t4YZtJXEmL6uxnJ1BGwNz--nCnzxRQe7KwSDCm1aVnr00WyhUnU6u2oWRLV2itIcEdcCvc7J3rOwP2XD7tWLFNge-rXJVcztBnIVJLHxtKLe0fJpJuBSKGtVYRzVy-AhgLzplthWJ_CVnLHCn50m7sh8ZJuVbTvYEPszjbk91Ny229YHHH_Ulg" />
                        </div>
                        <div className="flex-1 min-w-0">
                            <button onClick={handleLogout} className="text-slate-400 hover:text-slate-600 dark:hover:text-white">
                                <span className="material-symbols-outlined">logout</span>
                            </button>
                        </div>
                    </div>
                </div>
            </aside>
            {/* Main Content */}
            <main className="flex-1 flex flex-col overflow-hidden bg-background-light dark:bg-background-dark">
                {/* Header */}
                <header className="h-20 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between px-8 bg-white/50 dark:bg-background-dark/50 backdrop-blur-md">
                    <div className="flex items-center gap-4 flex-1 max-w-xl">
                       {selectedGroup && (
                           <button onClick={handleBackToGroups} className="p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors flex items-center gap-2">
                               <span className="material-symbols-outlined">arrow_back</span>
                               Retour aux groupes
                           </button>
                       )}
                       {!selectedGroup && <h2 className="text-2xl font-bold">Groupes</h2>}
                    </div>
                    <div className="flex items-center gap-4">
                        <button className="p-2 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition-colors" onClick={() => document.documentElement.classList.toggle('dark')}>
                            <span className="material-symbols-outlined">dark_mode</span>
                        </button>
                    </div>
                </header>

                {/* Content */}
                <div className="flex-1 overflow-y-auto p-8">
                    {!selectedGroup ? (
                        <>
                            <div className="mb-8">
                                <h2 className="text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">Gestion des groupes</h2>
                                <p className="text-slate-500 dark:text-slate-400 mt-1">Créez et gérez les groupes pour la plateforme.</p>
                            </div>
                            {/* Create Group */}
                            <div className="bg-white dark:bg-slate-900/40 p-6 rounded-2xl border border-slate-200 dark:border-slate-800 mb-8 max-w-2xl">
                                <h3 className="text-lg font-bold mb-4">Créer un nouveau groupe</h3>
                                <form onSubmit={handleCreateGroup} className="flex gap-4">
                                    <input 
                                        type="text"
                                        value={newGroupName}
                                        onChange={(e) => setNewGroupName(e.target.value)}
                                        placeholder="Nom du groupe..."
                                        className="flex-1 px-4 py-2 bg-slate-100 dark:bg-slate-800 border-none rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all"
                                        required
                                    />
                                    <button type="submit" className="px-6 py-2 bg-primary text-background-dark hover:bg-primary/90 text-sm font-bold rounded-xl transition-colors">
                                        Ajouter
                                    </button>
                                </form>
                            </div>
                            {/* Groups Table */}
                            <div className="bg-white dark:bg-slate-900/40 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
                                <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
                                    <h4 className="font-bold text-lg">Liste des groupes</h4>
                                </div>
                                <div className="overflow-x-auto">
                                    <table className="w-full text-left">
                                        <thead className="bg-slate-50 dark:bg-slate-800/50">
                                            <tr>
                                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Nom du groupe</th>
                                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Date de création</th>
                                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Membres</th>
                                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">Actions</th>
                                            </tr>
                                        </thead>
                                        <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                                            {loading ? (
                                                <tr><td colSpan="4" className="text-center py-4 text-slate-500">Chargement...</td></tr>
                                            ) : groups.map(group => {
                                                const dateStr = new Date(group.createdAt || group.CreatedAt).toLocaleDateString();
                                                return (
                                                    <tr key={group.id || group.Id} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                                                        <td className="px-6 py-4 font-semibold text-slate-900 dark:text-white">
                                                            {group.name || group.Name}
                                                            <div className="text-xs text-slate-400 mt-1 flex items-center gap-1">
                                                                <span className="material-symbols-outlined text-[12px]">link</span>
                                                                <a href={`/?groupId=${group.id || group.Id}`} target="_blank" rel="noreferrer" className="hover:text-primary hover:underline">
                                                                    Lien public du groupe
                                                                </a>
                                                            </div>
                                                        </td>
                                                        <td className="px-6 py-4 text-slate-500">{dateStr}</td>
                                                        <td className="px-6 py-4 text-slate-500">{group.userCount || group.UserCount || 0}</td>
                                                        <td className="px-6 py-4 text-right">
                                                            <button 
                                                                onClick={() => handleManageMembers(group)}
                                                                className="px-3 py-1.5 mr-2 text-primary bg-primary/10 hover:bg-primary/20 font-bold rounded-lg transition-colors text-sm"
                                                            >
                                                                Membres
                                                            </button>
                                                            <button 
                                                                onClick={() => handleDeleteGroup(group.id || group.Id)}
                                                                className="p-1.5 text-red-500 hover:bg-red-500/10 rounded-lg transition-colors align-middle"
                                                                title="Supprimer ce groupe"
                                                            >
                                                                <span className="material-symbols-outlined">delete</span>
                                                            </button>
                                                        </td>
                                                    </tr>
                                                );
                                            })}
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </>
                    ) : (
                        <>
                            <div className="mb-8 flex items-center justify-between">
                                <div>
                                    <h2 className="text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
                                        Membres : {selectedGroup.name || selectedGroup.Name}
                                    </h2>
                                    <p className="text-slate-500 dark:text-slate-400 mt-1">Ajoutez ou retirez des membres du groupe.</p>
                                </div>
                            </div>
                            
                            {/* Add Member form */}
                            <div className="bg-white dark:bg-slate-900/40 p-6 rounded-2xl border border-slate-200 dark:border-slate-800 mb-8 max-w-2xl">
                                <h3 className="text-lg font-bold mb-4">Ajouter un utilisateur</h3>
                                <form onSubmit={handleAddMember} className="flex gap-4">
                                    <select 
                                        className="flex-1 px-4 py-2 bg-slate-100 dark:bg-slate-800 border-none rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all"
                                        value={selectedUserId}
                                        onChange={(e) => setSelectedUserId(e.target.value)}
                                        required
                                    >
                                        <option value="">-- Sélectionner un utilisateur --</option>
                                        {allUsers.filter(u => !members.some(m => (m.userId || m.UserId) === (u.id || u.Id))).map(user => (
                                            <option key={user.id || user.Id} value={user.id || user.Id}>
                                                {user.username || user.Username} ({user.email || user.Email})
                                            </option>
                                        ))}
                                    </select>
                                    <select
                                        className="px-4 py-2 bg-slate-100 dark:bg-slate-800 border-none rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all w-32 focus:outline-none"
                                        value={selectedRole}
                                        onChange={(e) => setSelectedRole(parseInt(e.target.value))}
                                    >
                                        <option value={1}>Membre</option>
                                        <option value={9999}>Admin</option>
                                        <option value={0}>Aucun</option>
                                    </select>
                                    <button type="submit" className="px-6 py-2 bg-primary text-background-dark hover:bg-primary/90 text-sm font-bold rounded-xl transition-colors focus:ring-2 focus:ring-primary focus:outline-none">
                                        Ajouter au groupe
                                    </button>
                                </form>
                            </div>

                            {/* Members Table */}
                            <div className="bg-white dark:bg-slate-900/40 rounded-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
                                <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
                                    <h4 className="font-bold text-lg">Membres actuels</h4>
                                </div>
                                <div className="overflow-x-auto">
                                    <table className="w-full text-left">
                                        <thead className="bg-slate-50 dark:bg-slate-800/50">
                                            <tr>
                                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Utilisateur</th>
                                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Rôle</th>
                                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider">Date d'ajout</th>
                                                <th className="px-6 py-4 text-xs font-bold text-slate-500 uppercase tracking-wider text-right">Actions</th>
                                            </tr>
                                        </thead>
                                        <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                                            {members.map(member => {
                                                const joinedDate = new Date(member.joinedAt || member.JoinedAt).toLocaleDateString();
                                                return (
                                                    <tr key={member.userId || member.UserId} className="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                                                        <td className="px-6 py-4 font-semibold text-slate-900 dark:text-white">
                                                            {member.username || member.Username} <span className="font-normal text-slate-500 text-sm">({member.email || member.Email})</span>
                                                        </td>
                                                        <td className="px-6 py-4 font-medium text-slate-700 dark:text-slate-300">
                                                            <select
                                                                className="px-2 py-1 bg-transparent border-b border-slate-300 dark:border-slate-700 focus:border-primary focus:ring-0 text-sm transition-all outline-none"
                                                                value={member.role !== undefined ? member.role : member.Role}
                                                                onChange={(e) => handleRoleChange(member.userId || member.UserId, parseInt(e.target.value))}
                                                            >
                                                                <option value={1} className="dark:bg-slate-800">Membre</option>
                                                                <option value={9999} className="dark:bg-slate-800">Admin</option>
                                                                <option value={0} className="dark:bg-slate-800">Aucun</option>
                                                            </select>
                                                        </td>
                                                        <td className="px-6 py-4 text-slate-500">{joinedDate}</td>
                                                        <td className="px-6 py-4 text-right">
                                                            <button 
                                                                onClick={() => handleRemoveMember(member.userId || member.UserId)}
                                                                className="px-3 py-1.5 text-xs font-bold text-red-600 border border-red-500/50 hover:bg-red-500/10 rounded-lg transition-colors"
                                                            >
                                                                Retirer
                                                            </button>
                                                        </td>
                                                    </tr>
                                                );
                                            })}
                                            {members.length === 0 && (
                                                <tr>
                                                    <td colSpan="4" className="text-center py-4 text-slate-500">Aucun membre dans ce groupe.</td>
                                                </tr>
                                            )}
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </>
                    )}
                </div>
            </main>
        </div>
    );
}
