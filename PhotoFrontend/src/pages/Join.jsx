import React, { useEffect } from 'react';

export default function Join() {
    // Dans l'URL, ce sera /join/:inviteToken
    // Toutefois, si on n'utilise pas react-router-dom useParams, on peut lire depuis pathname
    // Si l'appli n'a pas react-router-dom (et utilise window.location), on le récupérera via Split
    
    useEffect(() => {
        const pathParts = window.location.pathname.split('/');
        // ex: ["", "join", "uuid"]
        if (pathParts.length >= 3 && pathParts[1] === 'join') {
            const token = pathParts[2];
            if (token) {
                localStorage.setItem('inviteToken', token);
            }
        }
        
        // Rediriger vers l'inscription
        window.location.href = '/register';
    }, []);

    return (
        <div className="flex h-screen items-center justify-center bg-background text-on-surface font-body">
            <div className="text-center">
                <span className="material-symbols-outlined animate-spin text-4xl text-primary block mb-4">sync</span>
                <p className="text-on-surface-variant font-medium tracking-wide">Validation de l'invitation...</p>
            </div>
        </div>
    );
}
