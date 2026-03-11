import React from 'react';
import { Link } from 'react-router-dom';

// Tu peux extraire ceci en un petit composant réutilisable, par exemple <PhotoTag />
function PhotoTag({ tag, onClick }) {
    // 1. On cherche la traduction française, ou on prend la première par défaut
    const translation = tag.translations?.find(t => t.language === 'Fr') || tag.translations?.[0];
    const tagName = translation ? translation.name : 'Inconnu';

    return (
        <Link 
            to={`/tags/${encodeURIComponent(tagName)}`}
            // 2. LA LIGNE MAGIQUE : Empêche le clic de "remonter" vers la vignette parente
            onClick={(e) => {
                e.stopPropagation();
                if (onClick) onClick();
            }} 
            style={{
                backgroundColor: '#00CED1',
                color: 'white',
                padding: '4px 10px',
                borderRadius: '15px',
                textDecoration: 'none',
                fontSize: '0.85em',
                fontWeight: '500',
                marginRight: '6px',
                marginBottom: '6px',
                display: 'inline-block',
                transition: 'opacity 0.2s'
            }}
            onMouseOver={(e) => e.target.style.opacity = 0.8}
            onMouseOut={(e) => e.target.style.opacity = 1}
        >
            #{tagName}
        </Link>
    );
}

export default PhotoTag;