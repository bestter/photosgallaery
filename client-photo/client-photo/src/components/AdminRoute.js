import React from 'react';
import { Navigate } from 'react-router-dom';
import { getUserRole } from './authHelper'; // Ajuste le chemin selon ton projet

const AdminRoute = ({ children, token }) => {
    const role = getUserRole(token);

    // Si pas de token ou si le rôle n'est pas Admin, on expulse vers l'accueil
    if (!token || role !== 'Admin') {
        return <Navigate to="/" replace />;
    }

    // Sinon, on affiche le composant enfant (le tableau de bord)
    return children;
};

export default AdminRoute;