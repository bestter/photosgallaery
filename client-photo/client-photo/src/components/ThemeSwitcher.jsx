import React, { useEffect, useState } from 'react';

const ThemeSwitcher = () => {
    // Initialiser le thème depuis le localStorage ou utiliser 'ocean' par défaut
    const [theme, setTheme] = useState(localStorage.getItem('theme') || 'ocean');

    useEffect(() => {
        // Appliquer le thème à l'élément <html>
        document.documentElement.setAttribute('data-theme', theme);
        // Sauvegarder le choix
        localStorage.setItem('theme', theme);
    }, [theme]);

    return (
        <select
            value={theme}
            onChange={(e) => setTheme(e.target.value)}
            className="bg-primary text-text-color border border-accent rounded-md px-3 py-1 text-sm font-medium focus:outline-none focus:ring-2 focus:ring-accent cursor-pointer transition-colors"
        >
            <option value="ocean">🌊 Océan</option>
            <option value="mint">🌿 Menthe</option>
            <option value="future-dusk">🌌 Crépuscule Futuriste</option>
        </select>
    );
};

export default ThemeSwitcher;
