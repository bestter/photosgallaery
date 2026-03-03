import React, { useEffect, useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { getUserRole } from './authHelper';
import Button from './Button';
import api from '../api';

const Navbar = ({ token, setToken }) => {
    const navigate = useNavigate();
    const location = useLocation();
    
    // État pour stocker le nombre de signalements
    const [reportCount, setReportCount] = useState(0);
    const role = getUserRole(token);

    // Fonction de vérification du nombre de signalements
    const checkReports = async () => {
        if (role === 'Admin') {
            try {
                const res = await api.get('/admin/reports');
                // On met à jour le compteur avec la longueur du tableau reçu
                setReportCount(res.data.length);
            } catch (err) {
                console.error("Erreur lors de la vérification des signalements", err);
            }
        }
    };

    // Surveillance automatique toutes les 60 secondes
    useEffect(() => {
        if (role === 'Admin') {
            checkReports();
            const interval = setInterval(checkReports, 60000); 
            return () => clearInterval(interval);
        }
    }, [role, token]);

    // Vérification immédiate à chaque changement de page
    useEffect(() => {
        if (role === 'Admin') {
            checkReports();
        }
    }, [location.pathname]);

    const handleLogout = () => {
        localStorage.removeItem('token');
        if (setToken) setToken(null);
        navigate('/');
    };

    // Le bouton clignote si le compteur est supérieur à 0
    const hasReports = reportCount > 0;

    return (
        <nav className="bg-white shadow-md border-b border-gray-100">
            <style>
                {`
                @keyframes soft-pulse {
                    0% { transform: scale(1); box-shadow: 0 0 0 0 rgba(147, 51, 234, 0.7); }
                    70% { transform: scale(1.05); box-shadow: 0 0 0 10px rgba(147, 51, 234, 0); }
                    100% { transform: scale(1); box-shadow: 0 0 0 0 rgba(147, 51, 234, 0); }
                }
                .animate-soft-pulse {
                    animation: soft-pulse 2s infinite;
                }
                `}
            </style>
            
            <div className="container mx-auto px-6 py-4 flex justify-between items-center">
                <Link to="/" className="flex items-center gap-3 hover:opacity-80 transition-opacity">
                    <span className="text-2xl font-extrabold text-teal-600">MaGalerie</span>
                    <img src="/photoGalleryLogo2.png" alt="Logo" className="h-10 w-auto object-contain" />
                </Link>

                <div className="flex items-center gap-4">
                    {!token ? (
                        <>
                            <Link to="/login" className="text-gray-600 hover:text-teal-600 font-medium transition-colors">
                                Se connecter
                            </Link>
                            <Link to="/register">
                                <Button size="sm" variant="primary">S'inscrire</Button>
                            </Link>
                        </>
                    ) : (
                        <>
                            {role === 'Admin' && (
                                <Link to="/admin">
                                    <Button 
                                        size="sm" 
                                        className={`bg-purple-600 text-white hover:bg-purple-700 border-none flex items-center gap-2 transition-all duration-500 ${hasReports ? 'animate-soft-pulse ring-2 ring-purple-300' : ''}`}
                                    >
                                        👑 Tableau de bord {hasReports && `(${reportCount})`}
                                        {hasReports && (
                                            <span className="relative flex h-2 w-2">
                                                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
                                                <span className="relative inline-flex rounded-full h-2 w-2 bg-red-500"></span>
                                            </span>
                                        )}
                                    </Button>
                                </Link>
                            )}
                            <Button size="sm" variant="outline" onClick={handleLogout} className="text-red-600 border-red-200 hover:bg-red-50">
                                Déconnexion
                            </Button>
                        </>
                    )}
                </div>
            </div>
        </nav>
    );
};

export default Navbar;