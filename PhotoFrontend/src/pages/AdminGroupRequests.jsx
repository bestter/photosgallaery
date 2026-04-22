import React, { useState, useEffect } from 'react';
import { getUserRole, isTokenExpired } from '../authHelper';
import api from '../api';
import AdminLayout from '../components/AdminLayout';

export default function AdminGroupRequests() {
    const [requests, setRequests] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const token = localStorage.getItem('token');
        if (!token || isTokenExpired(token) || getUserRole(token) !== 'Admin') {
            window.location.href = '/';
            return;
        }

        const fetchRequests = async () => {
            try {
                const response = await api.get('/GroupRequests');
                setRequests(response.data);
            } catch (error) {
                console.error("Erreur lors de la récupération des requêtes:", error);
            } finally {
                setLoading(false);
            }
        };

        fetchRequests();
    }, []);

    const handleReject = async (id) => {
        if (!window.confirm("Êtes-vous sûr de vouloir refuser et supprimer cette demande ?")) return;
        try {
            await api.delete(`/GroupRequests/${id}`);
            setRequests(requests.filter(r => r.id !== id));
        } catch (error) {
            console.error("Erreur lors de la suppression:", error);
            alert("Erreur lors de la suppression.");
        }
    };

    const handleAccept = (request) => {
        const url = new URL(window.location.origin + '/admin-groups');
        url.searchParams.set('createName', request.name);
        url.searchParams.set('createDesc', request.description);
        url.searchParams.set('requesterId', request.requester.id);
        url.searchParams.set('requestId', request.id);
        window.location.href = url.toString();
    };

    return (
        <AdminLayout
            title="Group Creation Requests"
            subtitle="Review and moderate pending group applications before they are published to the network. Ensure alignment with community guidelines."
        >
            {/* Data Table / Grid */}
            <div className="bg-surface-container-low rounded-lg overflow-hidden border border-outline-variant/30 shadow-[0_20px_25px_-5px_rgba(0,0,0,0.3)]">
                <table className="w-full text-left border-collapse">
                    <thead>
                        <tr className="bg-surface-container/50 border-b border-outline-variant/40">
                            <th className="py-4 px-6 font-label text-on-surface-variant font-semibold text-xs tracking-wider uppercase">Group Name</th>
                            <th className="py-4 px-6 font-label text-on-surface-variant font-semibold text-xs tracking-wider uppercase">Description</th>
                            <th className="py-4 px-6 font-label text-on-surface-variant font-semibold text-xs tracking-wider uppercase">Requester</th>
                            <th className="py-4 px-6 font-label text-on-surface-variant font-semibold text-xs tracking-wider uppercase text-right">Actions</th>
                        </tr>
                    </thead>
                    <tbody className="divide-y divide-outline-variant/20 text-sm">
                        {loading ? (
                            <tr>
                                <td colSpan="4" className="py-8 text-center text-on-surface-variant">Chargement des requêtes...</td>
                            </tr>
                        ) : requests.length === 0 ? (
                            <tr>
                                <td colSpan="4" className="py-8 text-center text-on-surface-variant">Aucune requête en attente.</td>
                            </tr>
                        ) : requests.map((req) => (
                            <tr key={req.id} className="hover:bg-surface-container-high/50 transition-colors group">
                                <td className="py-4 px-6">
                                    <div className="font-bold text-on-surface group-hover:text-primary transition-colors">{req.name}</div>
                                    <div className="text-xs text-on-surface-variant mt-0.5 flex items-center gap-1">
                                        <span className="w-1.5 h-1.5 rounded-full bg-primary/50"></span>
                                        {new Date(req.requestedAt).toLocaleDateString()}
                                    </div>
                                </td>
                                <td className="py-4 px-6 max-w-[250px]">
                                    <p className="truncate text-on-surface-variant" title={req.description}>
                                        {req.description}
                                    </p>
                                </td>
                                <td className="py-4 px-6">
                                    <div className="flex items-center gap-3">
                                        <div className="w-8 h-8 rounded-full bg-surface-container-highest flex items-center justify-center text-on-surface text-xs font-bold border border-outline-variant">
                                            {req.requester?.username?.charAt(0)?.toUpperCase()}
                                        </div>
                                        <div>
                                            <div className="font-medium text-on-surface">{req.requester?.username}</div>
                                            <div className="text-[10px] text-on-surface-variant uppercase tracking-wide">{req.requester?.email}</div>
                                        </div>
                                    </div>
                                </td>
                                <td className="py-4 px-6 text-right">
                                    <div className="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                                        <button
                                            onClick={() => handleReject(req.id)}
                                            className="px-3 py-1.5 text-xs font-label font-bold text-error hover:bg-error/10 rounded transition-colors"
                                        >
                                            Reject
                                        </button>
                                        <button
                                            onClick={() => handleAccept(req)}
                                            className="px-4 py-1.5 text-xs font-label font-bold bg-primary text-on-primary hover:bg-primary-fixed-dim rounded transition-colors shadow-[0_0_15px_rgba(0,206,209,0.3)]"
                                        >
                                            Accept
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </AdminLayout>
    );
}
