import React from 'react';
import buildData from '../build-number.json';

const baseVersion = "1.0"; // Tes premiers chiffres gérés manuellement

const Footer = () => {
    return (
        <footer className="w-full pt-6 pb-8 mt-12 border-t border-gray-100 bg-white/50 backdrop-blur-sm">

            {/* Le numéro de version façon message subtil, au-dessus des éléments */}
            <div className="text-center text-[11px] md:text-xs text-gray-400 mb-6 tracking-wide">
                Version {baseVersion}.{buildData.build}
            </div>

            <div className="container mx-auto px-6 flex flex-col md:flex-row justify-between items-center gap-6">

                {/* 1. Copyright */}
                <div className="text-gray-500 text-sm order-2 md:order-1 text-center md:text-left">
                    © {new Date().getFullYear()} <span className="font-semibold text-accent">MaGalerie</span>.
                    <p className="text-xs mt-1 text-gray-400 italic">Codé au Québec avec passion</p>
                </div>

                {/* 2. Licence GPL v3 */}
                <div className="order-1 md:order-2 flex flex-col items-center gap-2">
                    <a
                        href="https://www.gnu.org/licenses/gpl-3.0.html"
                        target="_blank"
                        rel="noopener noreferrer"
                        className="transition-transform hover:scale-110 active:scale-95"
                    >
                        <img
                            src="/gplv3-or-later.png"
                            alt="GPL v3"
                            className="h-10 w-auto opacity-80 hover:opacity-100 transition-opacity"
                        />
                    </a>
                </div>

                {/* 3. La Signature Gemini */}
                <a
                    href="https://gemini.google.com"
                    target="_blank"
                    rel="noopener noreferrer"
                    className="order-3 flex items-center gap-2 px-4 py-2 bg-gray-50 rounded-full border border-gray-100 shadow-sm transition-all hover:scale-105 hover:bg-white hover:shadow-md group"
                >
                    <span className="text-xs font-medium text-gray-600">
                        Propulsé par
                    </span>
                    <div className="flex items-center gap-1.5">
                        <svg className="w-4 h-4 text-blue-500 group-hover:animate-pulse" fill="currentColor" viewBox="0 0 24 24">
                            <path d="M12 2L14.5 9L22 11.5L14.5 14L12 21L9.5 14L2 11.5L9.5 9L12 2Z" />
                        </svg>
                        <span className="text-sm font-bold text-blue-600">
                            Google Gemini
                        </span>
                    </div>
                </a>

            </div>
        </footer>
    );
};

export default Footer;