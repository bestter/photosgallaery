import React, { useState } from 'react';
import { toast } from 'react-hot-toast';
import api from '../api';
import { getUserRole, isTokenExpired } from "../authHelper";
import { useTranslation } from 'react-i18next';

export default function Contact() {
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    subject: '',
    message: ''
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Vérification de la session
  const token = localStorage.getItem("token");
  const isLoggedIn = token && !isTokenExpired(token);
  const canSeeDashboard = isLoggedIn && getUserRole(token) === "Admin";

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.id]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.name || !formData.email || !formData.subject || !formData.message) {
      toast.error(t('contact.error_fill_all'));
      return;
    }

    setIsSubmitting(true);
    try {
      await api.post('/contact', {
        name: formData.name,
        email: formData.email,
        subject: formData.subject,
        message: formData.message
      });
      toast.success(t('contact.success_msg'));
      setFormData({ name: '', email: '', subject: '', message: '' });
    } catch (error) {
      console.error(error);
      toast.error(t('contact.error_msg'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="bg-[#0f2323] font-sans text-slate-100 min-h-screen flex flex-col">
      {/* Header - Actual Menu Design */}
      <header className="fixed top-0 w-full z-50 bg-slate-900/80 backdrop-blur-md border-b border-cyan-400/10 shadow-xl shadow-black/20 flex justify-between items-center px-4 md:px-6 py-3">
        <div className="flex items-center gap-4 md:gap-8">
          <div
            className="text-xl font-black tracking-tight text-cyan-400 cursor-pointer active:scale-95 transition-transform flex items-center"
            onClick={() => window.location.href = '/'}
          >
            <img alt="PixelLyra Logo" className="h-8 w-auto object-contain" src="/Byla3.jpg" />
          </div>
          <nav className="hidden md:flex items-center gap-6 font-sans text-sm font-medium tracking-tight">
            <a href="/" className="text-slate-400 hover:text-cyan-400 transition-colors">{t('gallery.gallery_title')}</a>
            <a href="/contact" className="text-cyan-400 font-bold border-b-2 border-cyan-400 pb-1">{t('contact.nav_title')}</a>
          </nav>
        </div>
        <div className="flex items-center gap-2 md:gap-4">
          <div className="flex items-center gap-1 md:gap-3">
            {canSeeDashboard && (
              <button onClick={() => window.location.href = '/dashboard'} className="text-slate-400 hover:text-cyan-400 hover:bg-cyan-400/10 p-2 rounded transition-colors" aria-label={t('gallery.dashboard_tooltip')} title={t('gallery.dashboard_tooltip')}>
                <span className="material-symbols-outlined" aria-hidden="true">dashboard</span>
              </button>
            )}
            {!isLoggedIn ? (
              <>
                <button onClick={() => window.location.href = '/login'} className="text-slate-400 hover:text-cyan-400 hover:bg-cyan-400/10 px-3 py-1.5 rounded font-bold text-sm transition-colors">{t('gallery.login')}</button>
                <button onClick={() => window.location.href = '/register'} className="bg-cyan-400 text-[#0f2323] px-4 py-1.5 rounded text-sm font-bold active:scale-95 transition-transform hover:brightness-110">{t('gallery.subscribe')}</button>
              </>
            ) : (
              <button onClick={() => { localStorage.removeItem('token'); window.location.href = '/login'; }} className="text-slate-400 hover:text-error hover:bg-error/10 p-2 rounded transition-colors" aria-label={t('gallery.logout_tooltip')} title={t('gallery.logout_tooltip')}>
                <span className="material-symbols-outlined" aria-hidden="true">logout</span>
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-grow pt-[8rem] pb-12 px-4 md:px-8 max-w-7xl mx-auto w-full flex flex-col md:flex-row gap-12 items-start justify-center">
        {/* Contact Form (Glassmorphism Card) */}
        <div className="w-full md:w-2/3 bg-[#152b2b]/50 backdrop-blur-lg border border-cyan-400/10 rounded-xl p-8 shadow-2xl relative overflow-hidden">
          <div className="absolute -top-24 -left-24 w-48 h-48 bg-cyan-400/20 rounded-full blur-[100px] pointer-events-none"></div>
          <div className="mb-8 relative z-10">
            <h1 className="text-3xl font-extrabold tracking-tight text-slate-100 mb-2">{t('contact.title')}</h1>
            <p className="text-slate-400 text-sm">{t('contact.subtitle')}</p>
          </div>
          <form className="space-y-6 relative z-10" onSubmit={handleSubmit}>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-2">
                <label className="text-xs font-bold tracking-wider uppercase text-cyan-400" htmlFor="name">{t('contact.name_label')}</label>
                <input className="w-full bg-slate-800/80 border border-slate-700/50 rounded px-4 py-3 text-slate-100 focus:outline-none focus:ring-2 focus:ring-cyan-400 focus:border-transparent transition-all placeholder-slate-500" id="name" placeholder={t('contact.name_placeholder')} type="text" value={formData.name} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <label className="text-xs font-bold tracking-wider uppercase text-cyan-400" htmlFor="email">{t('contact.email_label')}</label>
                <input className="w-full bg-slate-800/80 border border-slate-700/50 rounded px-4 py-3 text-slate-100 focus:outline-none focus:ring-2 focus:ring-cyan-400 focus:border-transparent transition-all placeholder-slate-500" id="email" placeholder={t('contact.email_placeholder')} type="email" value={formData.email} onChange={handleChange} />
              </div>
            </div>
            <div className="space-y-2">
              <label className="text-xs font-bold tracking-wider uppercase text-cyan-400" htmlFor="subject">{t('contact.subject_label')}</label>
              <input className="w-full bg-slate-800/80 border border-slate-700/50 rounded px-4 py-3 text-slate-100 focus:outline-none focus:ring-2 focus:ring-cyan-400 focus:border-transparent transition-all placeholder-slate-500" id="subject" placeholder={t('contact.subject_placeholder')} type="text" value={formData.subject} onChange={handleChange} />
            </div>
            <div className="space-y-2">
              <label className="text-xs font-bold tracking-wider uppercase text-cyan-400" htmlFor="message">{t('contact.message_label')}</label>
              <textarea className="w-full bg-slate-800/80 border border-slate-700/50 rounded px-4 py-3 text-slate-100 focus:outline-none focus:ring-2 focus:ring-cyan-400 focus:border-transparent transition-all placeholder-slate-500 resize-none" id="message" placeholder={t('contact.message_placeholder')} rows="5" value={formData.message} onChange={handleChange}></textarea>
            </div>
            <button disabled={isSubmitting} className="w-full bg-cyan-400 text-[#0f2323] font-bold py-3 px-6 rounded flex items-center justify-center gap-2 hover:brightness-110 shadow-[0_0_15px_rgba(34,211,238,0.4)] transition-all active:scale-[0.98] disabled:opacity-50" type="submit">
              <span>{isSubmitting ? t('contact.submitting') : t('contact.submit')}</span>
              <span className="material-symbols-outlined text-[18px]" aria-hidden="true">send</span>
            </button>
          </form>
        </div>

        {/* Contact Info & Socials */}
        <div className="w-full md:w-1/3 flex flex-col gap-8">
          <div className="bg-[#152b2b]/50 border border-cyan-400/10 rounded-xl p-6 shadow-lg">
            <h3 className="text-xs font-bold tracking-[0.1em] uppercase text-cyan-400 mb-4">{t('contact.connect')}</h3>
            <div className="flex flex-col gap-3">
              <a className="flex items-center gap-3 text-sm text-slate-400 hover:text-cyan-400 transition-colors bg-slate-800/50 p-3 rounded border border-transparent hover:border-cyan-400/20" href="#">
                <span className="material-symbols-outlined" aria-hidden="true">camera</span>
                {t('contact.instagram')}
              </a>
              <a className="flex items-center gap-3 text-sm text-slate-400 hover:text-cyan-400 transition-colors bg-slate-800/50 p-3 rounded border border-transparent hover:border-cyan-400/20" href="#">
                <span className="material-symbols-outlined" aria-hidden="true">play_circle</span>
                {t('contact.vimeo')}
              </a>
              <a className="flex items-center gap-3 text-sm text-slate-400 hover:text-cyan-400 transition-colors bg-slate-800/50 p-3 rounded border border-transparent hover:border-cyan-400/20" href="#">
                <span className="material-symbols-outlined" aria-hidden="true">terminal</span>
                {t('contact.dev_api')}
              </a>
            </div>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="w-full border-t border-cyan-400/10 py-12 mt-auto bg-slate-900/80 text-cyan-400 font-sans text-[10px] font-bold uppercase tracking-widest">
        <div className="max-w-7xl mx-auto px-8 flex flex-col md:flex-row justify-between items-center gap-6">
          <div className="text-[0.75rem] font-black tracking-[0.15em] text-cyan-400">PIXELLYRA</div>
          <div className="flex gap-6 flex-wrap justify-center">
            <a className="text-slate-500 hover:text-cyan-400 transition-colors" href="#">{t('footer.privacy_policy')}</a>
            <a className="text-slate-500 hover:text-cyan-400 transition-colors" href="#">{t('footer.terms_of_service')}</a>
            <a className="text-slate-500 hover:text-cyan-400 transition-colors" href="#">{t('contact.instagram')}</a>
            <a className="text-slate-500 hover:text-cyan-400 transition-colors" href="#">{t('contact.vimeo')}</a>
          </div>
          <div className="text-slate-500">
            {t('footer.copyright')}
          </div>
        </div>
      </footer>
    </div>
  );
}
