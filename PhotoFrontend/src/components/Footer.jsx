import React from 'react';
import { useTranslation } from 'react-i18next';
import LanguageSelector from './LanguageSelector';

const Footer = ({ className = "" }) => {
  const { t } = useTranslation();

  return (
    <footer className={`flex flex-col md:flex-row justify-center md:justify-between items-center gap-6 pt-6 pb-28 md:pb-28 px-8 md:px-12 w-full bg-slate-900/80 backdrop-blur-md border-t border-cyan-400/10 z-10 relative ${className}`}>
      <span className="text-[10px] font-bold uppercase tracking-widest font-['Inter'] text-slate-500 text-center md:text-left">
        {t("footer.copyright", "PixelLyra.com System v2.4.0 • Encrypted Connection")}
      </span>
      <div className="flex flex-wrap justify-center gap-4 md:gap-6 items-center">
        <a className="text-[10px] font-bold uppercase tracking-widest font-['Inter'] text-slate-500 hover:text-cyan-300 transition-colors" href="#">
          {t("footer.privacy_policy", "Politique de confidentialité")}
        </a>
        <a className="text-[10px] font-bold uppercase tracking-widest font-['Inter'] text-slate-500 hover:text-cyan-300 transition-colors" href="#">
          {t("footer.terms_of_service", "Conditions d'utilisation")}
        </a>
        <a className="text-[10px] font-bold uppercase tracking-widest font-['Inter'] text-slate-500 hover:text-cyan-300 transition-colors" href="/contact">
          {t("footer.contact_support", "Contact")}
        </a>
        <LanguageSelector />
      </div>
    </footer>
  );
};

export default Footer;
