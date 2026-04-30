import React from 'react';
import { useTranslation } from 'react-i18next';

export default function ContactFab() {
  const { t } = useTranslation();
  const currentPath = window.location.pathname;

  // Don't show the FAB on the contact page itself
  if (currentPath === '/contact') {
    return null;
  }

  return (
    <button 
      onClick={() => window.location.href = '/contact'}
      className="fixed bottom-8 left-8 flex items-center gap-3 px-5 py-3 rounded-full bg-slate-950/80 backdrop-blur-xl border border-cyan-400/30 text-cyan-400 shadow-[0_0_20px_rgba(0,206,209,0.4)] hover:shadow-[0_0_30px_rgba(0,206,209,0.7)] active:scale-95 transition-all duration-300 group z-[60]"
      aria-label={t('contact.nav_title')}
    >
      <span className="material-symbols-outlined text-cyan-400 transition-transform group-hover:rotate-12" style={{ fontVariationSettings: "'FILL' 1" }}>
        mail
      </span>
      <span className="font-sans font-bold text-[10px] tracking-widest uppercase hidden md:inline">
        {t('contact.nav_title')}
      </span>
    </button>
  );
}
