import React, { useEffect, useState } from 'react';

const ThemeSwitcher = () => {
    // 1. Initialiser le thème actuel
    const [theme, setTheme] = useState(() => {
        const saved = localStorage.getItem('theme');
        if (saved) return saved;
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'ocean' : 'mint';
    });

    // 2. Appliquer le thème sur <html> à chaque changement de la variable d'état
    useEffect(() => {
        document.documentElement.setAttribute('data-theme', theme);
    }, [theme]);

    // 3. Écouter les changements de couleur du système d'exploitation
    useEffect(() => {
        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        
        const handleChange = (e) => {
            // Ne basculer que si l'utilisateur n'a rien forcé dans le localStorage
            if (!localStorage.getItem('theme')) {
                setTheme(e.matches ? 'ocean' : 'mint');
            }
        };

        // Supporter les navigateurs plus anciens avec addListener si addEventListener n'est pas dispo
        if (mediaQuery.addEventListener) {
            mediaQuery.addEventListener('change', handleChange);
            return () => mediaQuery.removeEventListener('change', handleChange);
        } else if (mediaQuery.addListener) {
            mediaQuery.addListener(handleChange);
            return () => mediaQuery.removeListener(handleChange);
        }
    }, []);

    // 4. Gérer la sélection manuelle
    const handleThemeChange = (e) => {
        const newTheme = e.target.value;
        setTheme(newTheme);
        localStorage.setItem('theme', newTheme);
    };

    return (
        <select
            value={theme}
            onChange={handleThemeChange}
            className="bg-primary text-text-color border border-accent rounded-md px-3 py-1 text-sm font-medium focus:outline-none focus:ring-2 focus:ring-accent cursor-pointer transition-colors"
        >
            <option value="ocean">🌊 Océan</option>
            <option value="mint">🌿 Menthe</option>
            <option value="future-dusk">🌌 Crépuscule Futuriste</option>
            <option value="sea-stones">🪨 Pierres Marines</option>
            <option value="neutral">🤍 Neutre</option>
        </select>
    );
};

export default ThemeSwitcher;
