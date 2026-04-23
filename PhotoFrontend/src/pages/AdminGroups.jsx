import React, { useState, useEffect, useMemo } from 'react';
import { getUserRole, isTokenExpired } from '../authHelper';
import api from '../api';
import AdminLayout from '../components/AdminLayout';

export default function AdminGroups() {
    const [groups, setGroups] = useState([]);
    const [loading, setLoading] = useState(true);
    const [newGroupName, setNewGroupName] = useState('');
    const [newGroupDescription, setNewGroupDescription] = useState('');
    const [requesterId, setRequesterId] = useState('');
    const [requestId, setRequestId] = useState('');

    // Member management state
    const [selectedGroup, setSelectedGroup] = useState(null);
    const [members, setMembers] = useState([]);
    const [allUsers, setAllUsers] = useState([]);
    const [selectedUserId, setSelectedUserId] = useState('');
    const [selectedRole, setSelectedRole] = useState(1);

    const availableUsers = useMemo(() => {
        const memberIds = new Set(members.map(m => m.userId || m.UserId));
        return allUsers.filter(u => !memberIds.has(u.id || u.Id));
    }, [members, allUsers]);

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

                if (params.get('createName')) setNewGroupName(params.get('createName'));
                if (params.get('createDesc')) setNewGroupDescription(params.get('createDesc'));
                if (params.get('requesterId')) setRequesterId(params.get('requesterId'));
                if (params.get('requestId')) setRequestId(params.get('requestId'));
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
            const body = { name: newGroupName, description: newGroupDescription };
            if (requesterId) body.requesterId = parseInt(requesterId);
            if (requestId) body.requestId = requestId;

            const response = await api.post('/admin/groups', body);
            setGroups([response.data, ...groups]);
            setNewGroupName('');
            setNewGroupDescription('');
            setRequesterId('');
            setRequestId('');

            // Clean up url
            const url = new URL(window.location.href);
            url.searchParams.delete('createName');
            url.searchParams.delete('createDesc');
            url.searchParams.delete('requesterId');
            url.searchParams.delete('requestId');
            window.history.pushState({}, '', url);

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

    const topActions = (
        <div className="flex items-center gap-4 flex-1 max-w-xl mr-auto">
            {selectedGroup && (
                <button onClick={handleBackToGroups} className="p-2 text-on-surface-variant hover:bg-surface-container-low rounded-lg transition-colors flex items-center gap-2">
                    <span className="material-symbols-outlined">arrow_back</span>
                    Retour aux groupes
                </button>
            )}
        </div>
    );

    return (
        <AdminLayout
            title={selectedGroup ? `Membres : ${selectedGroup.name || selectedGroup.Name}` : "Gestion des groupes"}
            subtitle={selectedGroup ? "Ajoutez ou retirez des membres du groupe." : "Créez et gérez les groupes pour la plateforme."}
            topActions={topActions}
        >
            <div className="space-y-8">
                {!selectedGroup ? (
                    <>
                            {/* Create Group */}
                            <div className="bg-surface-container-low p-6 rounded-2xl border border-outline-variant/30 mb-8 max-w-2xl shadow-[0_10px_25px_-5px_rgba(0,0,0,0.1)]">
                                <h3 className="text-lg font-bold mb-4 text-on-surface">Créer un nouveau groupe</h3>
                                {requesterId && (
                                    <div className="mb-4 p-3 bg-primary/10 border border-primary/20 rounded-xl text-sm text-primary">
                                        Création d'un groupe à partir d'une demande. Le demandeur sera automatiquement ajouté en tant qu'administrateur.
                                    </div>
                                )}
                                <form onSubmit={handleCreateGroup} className="flex flex-col gap-4">
                                    <div className="flex gap-4">
                                        <input
                                            type="text"
                                            value={newGroupName}
                                            onChange={(e) => setNewGroupName(e.target.value)}
                                            placeholder="Nom du groupe..."
                                            className="flex-1 px-4 py-2 bg-surface-container border border-outline-variant/30 rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all text-on-surface"
                                            required
                                        />
                                        <button type="submit" className="px-6 py-2 bg-primary text-background-dark hover:bg-primary/90 text-sm font-bold rounded-xl transition-colors whitespace-nowrap shadow-[0_0_15px_rgba(0,206,209,0.3)]">
                                            Ajouter
                                        </button>
                                    </div>
                                    <textarea
                                        value={newGroupDescription}
                                        onChange={(e) => setNewGroupDescription(e.target.value)}
                                        placeholder="Description du groupe..."
                                        className="w-full px-4 py-2 bg-surface-container border border-outline-variant/30 rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all resize-none h-20 text-on-surface"
                                    />
                                </form>
                            </div>
                            {/* Groups Table */}
                            <div className="bg-surface-container-low rounded-2xl border border-outline-variant/30 overflow-hidden shadow-md">
                                <div className="px-6 py-4 border-b border-outline-variant/30 flex items-center justify-between">
                                    <h4 className="font-bold text-lg text-on-surface">Liste des groupes</h4>
                                </div>
                                <div className="overflow-x-auto">
                                    <table className="w-full text-left border-collapse">
                                        <thead className="bg-surface-container/50 border-b border-outline-variant/40">
                                            <tr>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">Nom du groupe</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">Date de création</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">Membres</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider text-right">Actions</th>
                                            </tr>
                                        </thead>
                                        <tbody className="divide-y divide-outline-variant/20 text-sm">
                                            {loading ? (
                                                <tr><td colSpan="4" className="text-center py-4 text-on-surface-variant">Chargement...</td></tr>
                                            ) : groups.map(group => {
                                                const dateStr = new Date(group.createdAt || group.CreatedAt).toLocaleDateString();
                                                return (
                                                    <tr key={group.id || group.Id} className="hover:bg-surface-container-high/50 transition-colors group">
                                                        <td className="px-6 py-4 font-semibold text-on-surface group-hover:text-primary transition-colors">
                                                            {group.name || group.Name}
                                                            <div className="text-xs text-on-surface-variant mt-1 flex items-center gap-1">
                                                                <span className="material-symbols-outlined text-[12px]">link</span>
                                                                <a href={`/group/${group.shortName || group.ShortName}`} target="_blank" rel="noreferrer" className="hover:text-primary hover:underline">
                                                                    Lien public du groupe
                                                                </a>
                                                            </div>
                                                        </td>
                                                        <td className="px-6 py-4 text-on-surface-variant">{dateStr}</td>
                                                        <td className="px-6 py-4 text-on-surface-variant">{group.userCount || group.UserCount || 0}</td>
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
                                    <h2 className="text-3xl font-extrabold text-on-surface tracking-tight">
                                        Membres : {selectedGroup.name || selectedGroup.Name}
                                    </h2>
                                    <p className="text-on-surface-variant mt-1">Ajoutez ou retirez des membres du groupe.</p>
                                </div>
                            </div>

                            {/* Add Member form */}
                            <div className="bg-surface-container-low p-6 rounded-2xl border border-outline-variant/30 mb-8 max-w-2xl shadow-md">
                                <h3 className="text-lg font-bold mb-4 text-on-surface">Ajouter un utilisateur</h3>
                                <form onSubmit={handleAddMember} className="flex gap-4">
                                    <select
                                        className="flex-1 px-4 py-2 bg-surface-container text-on-surface border-none rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all"
                                        value={selectedUserId}
                                        onChange={(e) => setSelectedUserId(e.target.value)}
                                        required
                                    >
                                        <option value="">-- Sélectionner un utilisateur --</option>
                                        {availableUsers.map(user => (
                                            <option key={user.id || user.Id} value={user.id || user.Id}>
                                                {user.username || user.Username} ({user.email || user.Email})
                                            </option>
                                        ))}
                                    </select>
                                    <select
                                        className="px-4 py-2 bg-surface-container text-on-surface border-none rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all w-32 focus:outline-none"
                                        value={selectedRole}
                                        onChange={(e) => setSelectedRole(parseInt(e.target.value))}
                                    >
                                        <option value={1}>Membre</option>
                                        <option value={9999}>Admin</option>
                                        <option value={0}>Aucun</option>
                                    </select>
                                    <button type="submit" className="px-6 py-2 bg-primary text-background-dark hover:bg-primary/90 text-sm font-bold rounded-xl transition-colors focus:ring-2 focus:ring-primary focus:outline-none shadow-[0_0_15px_rgba(0,206,209,0.3)]">
                                        Ajouter au groupe
                                    </button>
                                </form>
                            </div>

                            {/* Members Table */}
                            <div className="bg-surface-container-low rounded-2xl border border-outline-variant/30 overflow-hidden shadow-md">
                                <div className="px-6 py-4 border-b border-outline-variant/30 flex items-center justify-between">
                                    <h4 className="font-bold text-lg text-on-surface">Membres actuels</h4>
                                </div>
                                <div className="overflow-x-auto">
                                    <table className="w-full text-left">
                                        <thead className="bg-surface-container/50 border-b border-outline-variant/40">
                                            <tr>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">Utilisateur</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">Rôle</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">Date d'ajout</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider text-right">Actions</th>
                                            </tr>
                                        </thead>
                                        <tbody className="divide-y divide-outline-variant/20 text-sm">
                                            {members.map(member => {
                                                const joinedDate = new Date(member.joinedAt || member.JoinedAt).toLocaleDateString();
                                                return (
                                                    <tr key={member.userId || member.UserId} className="hover:bg-surface-container-high/50 group transition-colors">
                                                        <td className="px-6 py-4 font-semibold text-on-surface group-hover:text-primary transition-colors">
                                                            {member.username || member.Username} <span className="font-normal text-on-surface-variant text-sm">({member.email || member.Email})</span>
                                                        </td>
                                                        <td className="px-6 py-4 font-medium text-on-surface">
                                                            <select
                                                                className="px-2 py-1 bg-transparent border-b border-outline-variant/30 focus:border-primary focus:ring-0 text-sm transition-all outline-none"
                                                                value={member.role !== undefined ? member.role : member.Role}
                                                                onChange={(e) => handleRoleChange(member.userId || member.UserId, parseInt(e.target.value))}
                                                            >
                                                                <option value={1} className="bg-surface-container text-on-surface">Membre</option>
                                                                <option value={9999} className="bg-surface-container text-on-surface">Admin</option>
                                                                <option value={0} className="bg-surface-container text-on-surface">Aucun</option>
                                                            </select>
                                                        </td>
                                                        <td className="px-6 py-4 text-on-surface-variant">{joinedDate}</td>
                                                        <td className="px-6 py-4 text-right">
                                                            <button
                                                                onClick={() => handleRemoveMember(member.userId || member.UserId)}
                                                                className="px-3 py-1.5 text-xs font-bold text-error border border-error/50 hover:bg-error/10 rounded-lg transition-colors"
                                                            >
                                                                Retirer
                                                            </button>
                                                        </td>
                                                    </tr>
                                                );
                                            })}
                                            {members.length === 0 && (
                                                <tr>
                                                    <td colSpan="4" className="text-center py-4 text-on-surface-variant">Aucun membre dans ce groupe.</td>
                                                </tr>
                                            )}
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </>
                    )}
            </div>
        </AdminLayout>
    );
}
