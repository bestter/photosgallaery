import React, { useState, useEffect } from 'react';
import api from '../api';
import Button from './Button';
import toast from 'react-hot-toast';import { getUsernameFromToken } from './authHelper';


const AdminDashboard = () => {
    // C'est ici que ça doit vivre ! 👇
    const currentUser = getUsernameFromToken(localStorage.getItem('token'));
    const [users, setUsers] = useState([]);
    const [reports, setReports] = useState([]); // Pour tes futurs signalements
    const [isLoading, setIsLoading] = useState(true);    

    useEffect(() => {
        fetchUsers();
        fetchReports(); // Tu pourras décommenter quand la route C# existera
    }, []);

    const fetchUsers = async () => {
        try {
            // Assure-toi d'avoir cette route dans ton backend avec [Authorize(Roles = "Admin")]
            const response = await api.get('/admin/users');
            setUsers(response.data);
        } catch (error) {
            console.error("Erreur de chargement des utilisateurs", error);
        } finally {
            setIsLoading(false);
        }
    };

    const toggleAdminRole = async (userId, currentRole) => {
        const newRole = currentRole === 'Admin' ? 'User' : 'Admin';
        const confirmMessage = newRole === 'Admin' 
            ? "Promouvoir cet utilisateur comme Administrateur ?" 
            : "Retirer les droits d'administration à cet utilisateur ?";

        if (!window.confirm(confirmMessage)) return;

        try {
            // Route à créer côté C# : [HttpPut("users/{id}/role")]
            await api.put(`/admin/users/${userId}/role`, { role: newRole });
            
            // Met à jour l'affichage localement sans recharger la page
            setUsers(users.map(u => u.id === userId ? { ...u, role: newRole } : u));
            toast.success("Rôle mis à jour avec succès.");
        } catch (error) {
            console.error("Erreur lors de la mise à jour du rôle", error);
            toast.error("Impossible de modifier le rôle.");
        }
    };


const toggleCreatorRole = async (userId, currentRole) => {
    // On bascule entre 'Creator' et 'User' (et non Admin)
    const newRole = currentRole === 'Creator' ? 'User' : 'Creator';
    const confirmMessage = newRole === 'Creator' 
        ? "Donner le droit de téléverser à cet utilisateur ?" 
        : "Retirer les droits de créateur à cet utilisateur ?";

    if (!window.confirm(confirmMessage)) return;

    try {
        await api.put(`/admin/users/${userId}/role`, { role: newRole });
        setUsers(users.map(u => u.id === userId ? { ...u, role: newRole } : u));
        toast.success("Rôle mis à jour avec succès.");
    } catch (error) {
        console.error("Erreur lors de la mise à jour du rôle", error);
        toast.error("Impossible de modifier le rôle.");
    }
};

const toggleBanUser = async (userId, currentRole) => {
    // S'il est banni, on le remet 'User' de base. Sinon, on le bannit.
    const newRole = currentRole === 'Forbidden' ? 'User' : 'Forbidden';
    const confirmMessage = newRole === 'Forbidden' 
        ? "Bannir cet utilisateur ? Il ne pourra plus se connecter au site." 
        : "Lever la suspension de cet utilisateur ?";

    if (!window.confirm(confirmMessage)) return;

    try {
        await api.put(`/admin/users/${userId}/role`, { role: newRole });
        setUsers(users.map(u => u.id === userId ? { ...u, role: newRole } : u));
        toast.success(newRole === 'Forbidden' ? "Utilisateur banni 🔨" : "Compte réactivé 🔓");
    } catch (error) {
        console.error("Erreur lors du bannissement", error);
        toast.error("Impossible de modifier le statut.");
    }
};

const fetchReports = async () => {
    try {
        const response = await api.get('/admin/reports');
        setReports(response.data);
    } catch (error) {
        console.error("Erreur de chargement des signalements", error);
        toast.error("Erreur de chargement des signalements");
    }
};

// Fonction optionnelle pour supprimer la photo directement depuis le tableau de bord
const handleDeleteReportedPhoto = async (photoId) => {
    if (!window.confirm("Supprimer définitivement cette image ?")) return;
    try {
        await api.delete(`/photos/${photoId}`);
        toast.success("Image supprimée.");
        fetchReports(); // Rafraîchir la liste
    } catch (error) {
        toast.error("Erreur lors de la suppression.");
    }
};

// Fonction pour retirer le signalement sans toucher à la photo
const handleDismissReport = async (reportId) => {
    if (!window.confirm("Ignorer ce signalement ? La photo sera conservée.")) return;
    try {
        // Il faudra créer cette route côté C# : [HttpDelete("reports/{id}")]
        await api.delete(`/admin/reports/${reportId}`);
        toast.success("Signalement retiré avec succès.");
        fetchReports(); // On rafraîchit la liste pour faire disparaître la carte
    } catch (error) {
        console.error("Erreur lors du retrait du signalement", error);
        toast.error("Erreur lors du retrait du signalement.");
    }
};

const imageBaseUrl = process.env.NODE_ENV === 'development' ? 'http://localhost:5020' : '';

    if (isLoading) return <div className="p-6 text-center">Chargement du panneau d'administration...</div>;

    return (
        <div className="container mx-auto p-6 max-w-5xl">
            <h2 className="text-3xl font-bold mb-8 text-gray-800">Panneau d'Administration</h2>

            <div className="bg-white rounded-xl shadow-md p-6 border border-gray-100 mb-8">
                <h3 className="text-xl font-semibold mb-4 border-b pb-2">Gestion des Utilisateurs</h3>
                
                <div className="overflow-x-auto">
                    <table className="w-full text-left border-collapse">
                        <thead>
                            <tr className="bg-gray-50 text-gray-600 text-sm">
                                <th className="p-3 border-b">Utilisateur</th>
                                <th className="p-3 border-b">Courriel</th>
                                <th className="p-3 border-b">Rôle Actuel</th>
                                <th className="p-3 border-b">Creator</th>
                                <th className="p-3 border-b">Admin</th>
                                <th className="p-3 border-b">Bannir</th>
                            </tr>
                        </thead>
                        <tbody>
                            {users.map(user => (
                                <tr key={user.id} className="hover:bg-gray-50 transition-colors">
                                    <td className="p-3 border-b font-medium">{user.username}</td>
                                    <td className="p-3 border-b text-gray-500">{user.email}</td>
                                    <td className="p-3 border-b">
                                        <span className={`px-2 py-1 rounded-full text-xs font-bold ${
                                            user.role === 'Admin' ? 'bg-purple-100 text-purple-700' : 'bg-gray-100 text-gray-600'
                                        }`}>
                                            {user.role}
                                        </span>
                                    </td>
                                    <td className="p-3 border-b">
    {user.username?.toLowerCase() === currentUser?.toLowerCase() ? (
        <span className="text-gray-400 italic text-sm">Action impossible</span>
    ) : (
        <Button 
            size="sm" 
            variant={user.role === 'Creator' ? 'outline' : 'primary'}
            disabled={user.role === 'Admin'} 
            onClick={() => toggleCreatorRole(user.id, user.role)}
        >
            {user.role === 'Creator' ? 'Rétrograder' : 'Promouvoir créateur'}
        </Button>
    )}
</td>
                                   <td className="p-3 border-b">
    {user.username?.toLowerCase() === currentUser?.toLowerCase() ? (
        <span className="text-teal-600 font-bold text-sm">👑 C'est vous</span>
    ) : (
        <Button 
            size="sm" 
            variant={user.role === 'Admin' ? 'outline' : 'primary'}
            onClick={() => toggleAdminRole(user.id, user.role)}
        >
            {user.role === 'Admin' ? 'Rétrograder' : 'Promouvoir Admin'}
        </Button>
    )}
</td>
                                    <td className="p-3 border-b">
    {user.username?.toLowerCase() === currentUser?.toLowerCase() ? (
        <span className="text-gray-400 italic text-sm">-</span>
    ) : (
        <Button 
            size="sm" 
            variant={user.role === 'Forbidden' ? 'primary' : 'outline'}
            onClick={() => toggleBanUser(user.id, user.role)}
            className={user.role === 'Forbidden' ? "bg-red-600 hover:bg-red-700 text-white border-none" : "text-red-600 border-red-200 hover:bg-red-50"}
        >
            {user.role === 'Forbidden' ? 'Débannir' : 'Bannir 🚫'}
        </Button>
    )}
</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>

           {/* Section Signalements */}
<div className="bg-white rounded-xl shadow-md p-6 border border-gray-100">
    <h3 className="text-xl font-semibold mb-4 border-b pb-2">Images Signalées ({reports.length})</h3>
    
    {reports.length === 0 ? (
        <p className="text-gray-500 italic">Aucun signalement pour le moment. Tout est calme !</p>
    ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {reports.map(report => (
                <div key={report.reportId} className="border rounded-lg overflow-hidden flex flex-col shadow-sm">
                    <img src=  {`${imageBaseUrl}${report.photoUrl}`} alt="Signalée" className="w-full h-40 object-cover" />
                    <div className="p-4 bg-red-50 flex-grow">
                        <p className="text-sm text-red-800 font-bold mb-1">Motif :</p>
                        <p className="text-sm text-gray-700 italic mb-3">"{report.reason}"</p>
                        <p className="text-xs text-gray-500">Posté par : {report.uploader}</p>
                        <p className="text-xs text-gray-500">
                            Date : {new Date(report.reportedAt).toLocaleDateString()}
                        </p>
                    </div>
                    <div className="p-3 bg-white border-t flex justify-between gap-2">
    {/* Nouveau bouton pour ignorer le signalement */}
    <Button 
        variant="outline" 
        size="sm" 
        className="text-gray-600 border-gray-300 hover:bg-gray-100"
        onClick={() => handleDismissReport(report.reportId)}
    >
        ✅ Ignorer
    </Button>

    <Button 
        variant="outline" 
        size="sm" 
        className="text-red-600 border-red-600 hover:bg-red-50"
        onClick={() => handleDeleteReportedPhoto(report.photoId)}
    >
        🗑️ Supprimer l'image
    </Button>
</div>
                </div>
            ))}
        </div>
    )}
</div>
        </div>
    );
};

export default AdminDashboard;