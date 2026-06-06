import React, { useState, useEffect, useMemo } from 'react';
import { getUserRole, isTokenExpired } from '../authHelper';
import api from '../api';
import toast from 'react-hot-toast';
import AdminLayout from '../components/AdminLayout';
import { useTranslation } from "react-i18next";

export default function AdminGroups() {
    const { t } = useTranslation();
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
    const [isCreating, setIsCreating] = useState(false);
    const [isAddingMember, setIsAddingMember] = useState(false);

    // O(N) optimized filtering using a Set and useMemo to prevent unnecessary N*M computations on each render
    const availableUsers = useMemo(() => {
        const memberIds = new Set(members.map(m => m.userId || m.UserId));
        return allUsers.filter(u => !memberIds.has(u.id || u.Id));
    }, [members, allUsers]);

    useEffect(() => {

        if (isTokenExpired() || getUserRole() !== 'Admin') {
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
                toast.error("Erreur lors de la récupération des groupes.");
            } finally {
                setLoading(false);
            }
        };

        fetchGroups();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const handleCreateGroup = async (e) => {
        e.preventDefault();
        if (!newGroupName.trim()) return;

        setIsCreating(true);

        try {
            const body = { name: newGroupName, description: newGroupDescription };
            if (requesterId) body.requesterId = parseInt(requesterId);
            if (requestId) body.requestId = requestId;

            const response = await api.post('/admin/groups', body);
            setGroups([response.data, ...groups]);
            toast.success("Groupe créé avec succès.");
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
            toast.error("Erreur lors de la création du groupe.");
        } finally {
            setIsCreating(false);
        }
    };

    const handleDeleteGroup = async (id) => {
        if (!window.confirm("Êtes-vous sûr de vouloir supprimer ce groupe ?")) return;
        try {
            await api.delete(`/admin/groups/${id}`);
            setGroups(groups.filter(g => (g.id || g.Id) !== id));
        } catch (error) {
            toast.error("Erreur lors de la suppression du groupe.");
        }
    };

    async function handleManageMembers(group, updateUrl = true) {
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
            toast.error("Erreur lors de la récupération des membres.");
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
        setIsAddingMember(true);
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
            toast.error(error.response?.data?.message || "Erreur lors de l'ajout.");
        } finally {
            setIsAddingMember(false);
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
            toast.error("Erreur lors du retrait.");
        }
    };

    const handleRoleChange = async (userId, newRole) => {
        const groupId = selectedGroup.id || selectedGroup.Id;
        try {
            await api.put(`/admin/groups/${groupId}/members/${userId}/role`, { role: newRole });
            setMembers(members.map(m => (m.userId || m.UserId) === userId ? { ...m, role: newRole, Role: newRole } : m));
        } catch (error) {
            toast.error("Erreur lors de la mise à jour du rôle.");
        }
    };

    const topActions = (
        <div className="flex items-center gap-4 flex-1 max-w-xl mr-auto">
            {selectedGroup && (
                <button onClick={handleBackToGroups} className="p-2 text-on-surface-variant hover:bg-surface-container-low rounded-lg transition-colors flex items-center gap-2">
                    <span className="material-symbols-outlined" aria-hidden="true">arrow_back</span>
                    Retour aux groupes
                </button>
            )}
        </div>
    );

    return (
        <AdminLayout
            title={selectedGroup ? `Membres : ${selectedGroup.name || selectedGroup.Name}` : "Gestion des groupes"}
            subtitle={selectedGroup ? t("admin.groups.members_subtitle") : "Créez et gérez les groupes pour la plateforme."}
            topActions={topActions}
        >
            <div className="space-y-8">
                {!selectedGroup ? (
                    <>
                            {/* Create Group */}
                            <div className="bg-surface-container-low p-6 rounded-2xl border border-outline-variant/30 mb-8 max-w-2xl shadow-[0_10px_25px_-5px_rgba(0,0,0,0.1)]">
                                <h3 className="text-lg font-bold mb-4 text-on-surface">{t("admin.groups.create_title")}</h3>
                                {requesterId && (
                                    <div className="mb-4 p-3 bg-primary/10 border border-primary/20 rounded-xl text-sm text-primary">
                                        Création d&apos;un groupe à partir d&apos;une demande. Le demandeur sera automatiquement ajouté en tant qu&apos;administrateur.
                                    </div>
                                )}
                                <form onSubmit={handleCreateGroup} className="flex flex-col gap-4">
                                    <div className="flex gap-4">
                                        <input
                                            type="text"
                                            value={newGroupName}
                                            onChange={(e) => setNewGroupName(e.target.value)}
                                            placeholder={t("admin.groups.table.name") + "..."}
                                            className="flex-1 px-4 py-2 bg-surface-container border border-outline-variant/30 rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all text-on-surface"
                                            required
                                        />
                                        <button type="submit" disabled={isCreating} className="px-6 py-2 bg-primary text-background-dark hover:bg-primary/90 text-sm font-bold rounded-xl transition-colors whitespace-nowrap shadow-[0_0_15px_rgba(0,206,209,0.3)] disabled:opacity-50 flex items-center justify-center gap-2">
                                            {isCreating && <span className="material-symbols-outlined animate-spin text-[18px]" aria-hidden="true">sync</span>}
                                            {t("admin.groups.action.add", "Ajouter")}
                                        </button>
                                    </div>
                                    <textarea
                                        value={newGroupDescription}
                                        onChange={(e) => setNewGroupDescription(e.target.value)}
                                        placeholder={t("groups.group_desc_placeholder")}
                                        className="w-full px-4 py-2 bg-surface-container border border-outline-variant/30 rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all resize-none h-20 text-on-surface"
                                    />
                                </form>
                            </div>
                            {/* Groups Table */}
                            <div className="bg-surface-container-low rounded-2xl border border-outline-variant/30 overflow-hidden shadow-md">
                                <div className="px-6 py-4 border-b border-outline-variant/30 flex items-center justify-between">
                                    <h4 className="font-bold text-lg text-on-surface">{t("admin.groups.list_title")}</h4>
                                </div>
                                <div className="overflow-x-auto">
                                    <table className="w-full text-left border-collapse">
                                        <thead className="bg-surface-container/50 border-b border-outline-variant/40">
                                            <tr>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">{t("admin.groups.table.name")}</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">{t("admin.groups.table.date")}</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">{t("admin.groups.table.members")}</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider text-right">{t("admin.groups.table.actions")}</th>
                                            </tr>
                                        </thead>
                                        <tbody className="divide-y divide-outline-variant/20 text-sm">
                                            {loading ? (
                                                <tr><td colSpan="4" className="text-center py-4 text-on-surface-variant">{t("admin.groups.loading")}</td></tr>
                                            ) : groups.map(group => {
                                                const dateStr = new Date(group.createdAt || group.CreatedAt).toLocaleDateString();
                                                return (
                                                    <tr key={group.id || group.Id} className="hover:bg-surface-container-high/50 transition-colors group">
                                                        <td className="px-6 py-4 font-semibold text-on-surface group-hover:text-primary transition-colors">
                                                            {group.name || group.Name}
                                                            <div className="text-xs text-on-surface-variant mt-1 flex items-center gap-1">
                                                                <span aria-hidden="true" className="material-symbols-outlined text-[12px]">link</span>
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
                                                                title={t("admin.groups.action.delete_group_tooltip")}
                                                                aria-label={t("admin.groups.action.delete_group_aria", { name: group.name || group.Name })}
                                                            >
                                                                <span aria-hidden="true" className="material-symbols-outlined">delete</span>
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
                                    <p className="text-on-surface-variant mt-1">{t("admin.groups.members_subtitle")}</p>
                                </div>
                            </div>

                            {/* Add Member form */}
                            <div className="bg-surface-container-low p-6 rounded-2xl border border-outline-variant/30 mb-8 max-w-2xl shadow-md">
                                <h3 className="text-lg font-bold mb-4 text-on-surface">{t("admin.groups.add_member")}</h3>
                                <form onSubmit={handleAddMember} className="flex gap-4">
                                    <select
                                        className="flex-1 px-4 py-2 bg-surface-container text-on-surface border-none rounded-xl focus:ring-2 focus:ring-primary text-sm transition-all"
                                        value={selectedUserId}
                                        onChange={(e) => setSelectedUserId(e.target.value)}
                                        required
                                    >
                                        <option value="">{t("admin.groups.select_user")}</option>
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
                                        <option value={1}>{t("admin.groups.role.member")}</option>
                                        <option value={9999}>{t("admin.groups.role.admin")}</option>
                                        <option value={0}>{t("admin.groups.role.none")}</option>
                                    </select>
                                    <button type="submit" disabled={isAddingMember} className="px-6 py-2 bg-primary text-background-dark hover:bg-primary/90 text-sm font-bold rounded-xl transition-colors focus:ring-2 focus:ring-primary focus:outline-none shadow-[0_0_15px_rgba(0,206,209,0.3)] disabled:opacity-50 flex items-center justify-center gap-2">
                                        {isAddingMember && <span className="material-symbols-outlined animate-spin text-[18px]" aria-hidden="true">sync</span>}
                                        {t("admin.groups.action.add_member", "Ajouter au groupe")}
                                    </button>
                                </form>
                            </div>

                            {/* Members Table */}
                            <div className="bg-surface-container-low rounded-2xl border border-outline-variant/30 overflow-hidden shadow-md">
                                <div className="px-6 py-4 border-b border-outline-variant/30 flex items-center justify-between">
                                    <h4 className="font-bold text-lg text-on-surface">{t("admin.groups.current_members")}</h4>
                                </div>
                                <div className="overflow-x-auto">
                                    <table className="w-full text-left">
                                        <thead className="bg-surface-container/50 border-b border-outline-variant/40">
                                            <tr>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">{t("admin.groups.members_table.user")}</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">{t("admin.groups.members_table.role")}</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider">{t("admin.groups.members_table.added")}</th>
                                                <th className="px-6 py-4 text-xs font-bold text-on-surface-variant uppercase tracking-wider text-right">{t("admin.groups.table.actions")}</th>
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
                                                                <option value={1} className="bg-surface-container text-on-surface">{t("admin.groups.role.member")}</option>
                                                                <option value={9999} className="bg-surface-container text-on-surface">{t("admin.groups.role.admin")}</option>
                                                                <option value={0} className="bg-surface-container text-on-surface">{t("admin.groups.role.none")}</option>
                                                            </select>
                                                        </td>
                                                        <td className="px-6 py-4 text-on-surface-variant">{joinedDate}</td>
                                                        <td className="px-6 py-4 text-right">
                                                            <button
                                                                onClick={() => handleRemoveMember(member.userId || member.UserId)}
                                                                className="px-3 py-1.5 text-xs font-bold text-error border border-error/50 hover:bg-error/10 rounded-lg transition-colors"
                                                                aria-label={t("admin.groups.action.remove_member_aria", { name: member.username || member.Username || member.userId || member.UserId })}
                                                            >
                                                                {t("admin.groups.action.remove")}
                                                            </button>
                                                        </td>
                                                    </tr>
                                                );
                                            })}
                                            {members.length === 0 && (
                                                <tr>
                                                    <td colSpan="4" className="text-center py-4 text-on-surface-variant">{t("admin.groups.no_members")}</td>
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
