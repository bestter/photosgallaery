import React from 'react';
import { Link } from 'react-router-dom';

function EmptyGalleryState({ tagName }) {
    return (
        <div style={{ 
            display: 'flex', 
            flexDirection: 'column', 
            alignItems: 'center', 
            justifyContent: 'center', 
            padding: '60px 20px',
            textAlign: 'center',
            color: '#555'
        }}>
            {/* Icône SVG minimaliste représentant une photo/galerie vide */}
            <svg 
                width="80" height="80" viewBox="0 0 24 24" 
                fill="none" stroke="#00CED1" 
                strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" 
                style={{ marginBottom: '20px', opacity: 0.8 }}
            >
                <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                <circle cx="8.5" cy="8.5" r="1.5"></circle>
                <polyline points="21 15 16 10 5 21"></polyline>
            </svg>

            <h2 style={{ color: '#333', marginBottom: '15px' }}>
                {tagName 
                    ? `Aucune photo trouvée pour "${tagName}"` 
                    : "Votre galerie est vide"}
            </h2>
            
            <p style={{ maxWidth: '450px', marginBottom: '30px', lineHeight: '1.6' }}>
                {tagName 
                    ? "Il semble qu'aucune image ne possède ce tag avec la langue actuelle. Essayez un autre mot-clé ou retournez à la galerie principale pour explorer toutes vos photos." 
                    : "Commencez à donner vie à votre galerie en téléversant vos toutes premières photos !"}
            </p>

            {/* Bouton de retour affiché uniquement si on est dans une recherche par tag */}
            {tagName && (
                <Link 
                    to="/" 
                    style={{
                        backgroundColor: '#00CED1',
                        color: 'white',
                        padding: '12px 24px',
                        borderRadius: '6px',
                        textDecoration: 'none',
                        fontWeight: '600',
                        boxShadow: '0 4px 6px rgba(0, 206, 209, 0.2)',
                        transition: 'all 0.2s ease-in-out'
                    }}
                    onMouseOver={(e) => {
                        e.target.style.backgroundColor = '#00b3b6';
                        e.target.style.transform = 'translateY(-2px)';
                    }}
                    onMouseOut={(e) => {
                        e.target.style.backgroundColor = '#00CED1';
                        e.target.style.transform = 'translateY(0)';
                    }}
                >
                    Retour à toutes les photos
                </Link>
            )}
        </div>
    );
}

export default EmptyGalleryState;