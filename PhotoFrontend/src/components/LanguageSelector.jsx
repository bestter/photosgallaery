import React from 'react';
import { useTranslation } from 'react-i18next';

const LanguageSelector = () => {
  const { i18n } = useTranslation();

  const changeLanguage = (event) => {
    i18n.changeLanguage(event.target.value);
  };

  return (
    <div className="relative group ml-4">
      <select
        value={i18n.resolvedLanguage || i18n.language}
        onChange={changeLanguage}
        className="appearance-none bg-surface-variant/30 border border-cyan-400/10 rounded px-3 py-1.5 pr-8 text-[10px] font-bold uppercase tracking-widest font-['Inter'] text-slate-400 hover:text-cyan-300 hover:border-cyan-400/30 focus:outline-none focus:ring-1 focus:ring-primary focus:bg-surface-variant cursor-pointer transition-all shadow-sm"
      >
        <option value="en" className="bg-surface-container-high text-on-surface font-sans">EN - English</option>
        <option value="fr" className="bg-surface-container-high text-on-surface font-sans">FR - Français</option>
      </select>
      <div className="absolute inset-y-0 right-0 flex items-center px-2 pointer-events-none text-slate-500 group-hover:text-cyan-300 transition-colors">
        <span className="material-symbols-outlined text-[14px]">language</span>
      </div>
    </div>
  );
};

export default LanguageSelector;
