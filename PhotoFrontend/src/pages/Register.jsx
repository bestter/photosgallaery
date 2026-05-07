import React, { useState } from "react";
import api from "../api";
import toast from "react-hot-toast";
import { useTranslation } from "react-i18next";
import Footer from "../components/Footer";

const Register = () => {
  const { t } = useTranslation();
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [acceptTerms, setAcceptTerms] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (password !== confirmPassword) {
      toast.error(t("auth.register.error_passwords_match"));
      return;
    }

    if (!acceptTerms) {
      toast.error(t("auth.register.error_accept_terms"));
      return;
    }

    setIsLoading(true);
    try {
      const inviteToken = localStorage.getItem("inviteToken");

      await api.post("/auth/register", {
        username,
        email,
        password,
        inviteToken: inviteToken || undefined,
      });

      if (inviteToken) {
        localStorage.removeItem("inviteToken");
      }

      toast.success(t("auth.register.success"));
      window.location.href = "/login";
    } catch (err) {
      toast.error(
        t("auth.register.error_general"),
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div
      className="font-body text-on-surface min-h-screen flex flex-col relative overflow-y-auto bg-[#081414] z-50"
      style={{
        backgroundImage:
          "radial-gradient(circle at 20% 30%, rgba(0, 206, 209, 0.1) 0%, transparent 40%), radial-gradient(circle at 80% 70%, rgba(0, 206, 209, 0.08) 0%, transparent 40%)",
      }}
    >
      {/* Subtle Background Texture */}
      <div className="absolute inset-0 z-0 opacity-30 pointer-events-none">
        <div className="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] bg-primary/20 blur-[120px] rounded-full"></div>
        <div className="absolute bottom-[-10%] right-[-10%] w-[40%] h-[40%] bg-primary/10 blur-[120px] rounded-full"></div>
      </div>

      {/* Main Registration Shell */}
      <main className="flex-grow flex flex-col items-center justify-center w-full max-w-md mx-auto px-6 py-12 relative z-10">
        {/* Brand Logo / Title */}
        <div className="mb-10 text-center">
          <h1 className="text-primary font-headline font-black text-4xl tracking-tighter uppercase mb-2">
            PixelLyra.com
          </h1>
          <p className="text-on-surface-variant text-sm font-medium tracking-wide">
            Créez votre compte pour commencer
          </p>
        </div>

        {/* Glassmorphism Card */}
        <section className="bg-slate-900/40 backdrop-blur-xl border border-cyan-900/30 rounded-xl p-8 shadow-2xl shadow-black/50">
          <form className="space-y-6" onSubmit={handleSubmit}>
            {/* Nom d'utilisateur */}
            <div className="space-y-2">
              <label
                className="block text-xs font-bold uppercase tracking-widest text-secondary"
                htmlFor="full_name"
              >
                Nom d&apos;utilisateur
              </label>
              <div className="relative group">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-on-surface-variant group-focus-within:text-primary transition-colors">
                  <span className="material-symbols-outlined text-[20px]">
                    person
                  </span>
                </div>
                <input
                  className="w-full bg-surface-container-lowest border-none text-on-surface text-sm rounded-lg pl-10 py-3 focus:ring-2 focus:ring-primary transition-all placeholder:text-slate-600 outline-none"
                  id="full_name"
                  placeholder={t("auth.register.username_placeholder")}
                  type="text"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  required
                  disabled={isLoading}
                  minLength="3"
                />
              </div>
            </div>

            {/* Adresse Courriel */}
            <div className="space-y-2">
              <label
                className="block text-xs font-bold uppercase tracking-widest text-secondary"
                htmlFor="email"
              >
                Adresse Courriel
              </label>
              <div className="relative group">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-on-surface-variant group-focus-within:text-primary transition-colors">
                  <span className="material-symbols-outlined text-[20px]">
                    mail
                  </span>
                </div>
                <input
                  className="w-full bg-surface-container-lowest border-none text-on-surface text-sm rounded-lg pl-10 py-3 focus:ring-2 focus:ring-primary transition-all placeholder:text-slate-600 outline-none"
                  id="email"
                  placeholder={t("auth.register.email_placeholder")}
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  disabled={isLoading}
                />
              </div>
            </div>

            {/* Mot de passe */}
            <div className="space-y-2">
              <label
                className="block text-xs font-bold uppercase tracking-widest text-secondary"
                htmlFor="password"
              >
                Mot de passe
              </label>
              <div className="relative group">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-on-surface-variant group-focus-within:text-primary transition-colors">
                  <span className="material-symbols-outlined text-[20px]">
                    lock
                  </span>
                </div>
                <input
                  className="w-full bg-surface-container-lowest border-none text-on-surface text-sm rounded-lg pl-10 py-3 focus:ring-2 focus:ring-primary transition-all placeholder:text-slate-600 outline-none"
                  id="password"
                  placeholder={t("auth.register.password_placeholder")}
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  disabled={isLoading}
                  minLength="6"
                />
              </div>
            </div>

            {/* Confirmer le mot de passe */}
            <div className="space-y-2">
              <label
                className="block text-xs font-bold uppercase tracking-widest text-secondary"
                htmlFor="confirm_password"
              >
                Confirmer le mot de passe
              </label>
              <div className="relative group">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-on-surface-variant group-focus-within:text-primary transition-colors">
                  <span className="material-symbols-outlined text-[20px]">
                    shield_lock
                  </span>
                </div>
                <input
                  className="w-full bg-surface-container-lowest border-none text-on-surface text-sm rounded-lg pl-10 py-3 focus:ring-2 focus:ring-primary transition-all placeholder:text-slate-600 outline-none"
                  id="confirm_password"
                  placeholder={t("auth.register.password_placeholder")}
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  required
                  disabled={isLoading}
                  minLength="6"
                />
              </div>
            </div>

            {/* Privacy Checkbox */}
            <div className="flex items-start gap-3 py-2">
              <input
                className="mt-1 w-4 h-4 rounded bg-surface-container-lowest border-outline text-primary focus:ring-primary focus:ring-offset-background outline-none"
                id="terms"
                type="checkbox"
                checked={acceptTerms}
                onChange={(e) => setAcceptTerms(e.target.checked)}
                required
                disabled={isLoading}
              />
              <label
                className="text-[12px] text-on-surface-variant leading-relaxed"
                htmlFor="terms"
              >
                {t("auth.register.accept_terms")}{" "}
                <a className="text-primary hover:underline" href="#">
                  Conditions d&apos;utilisation
                </a>{" "}
                et la{" "}
                <a className="text-primary hover:underline" href="#">
                  Politique de confidentialité
                </a>
                .
              </label>
            </div>

            {/* Primary Action Button */}
            <button
              className="w-full bg-primary text-on-primary font-headline font-extrabold text-sm tracking-widest py-4 rounded-lg hover:brightness-110 active:scale-[0.98] transition-all flex items-center justify-center gap-2 disabled:opacity-50"
              type="submit"
              disabled={
                isLoading ||
                !username ||
                !email ||
                !password ||
                !confirmPassword ||
                !acceptTerms
              }
              title={
                !username || !email || !password || !confirmPassword
                  ? t("auth.register.fill_all")
                  : !acceptTerms
                    ? t("auth.register.error_accept_terms")
                    : ""
              }
            >
              {isLoading ? "INSCRIPTION EN COURS..." : "S'INSCRIRE"}
              {!isLoading && (
                <span className="material-symbols-outlined text-[18px]">
                  arrow_forward
                </span>
              )}
            </button>
          </form>
        </section>

        {/* Secondary Link */}
        <div className="mt-8 text-center">
          <p className="text-sm text-on-surface-variant">
            Déjà un compte ?
            <a
              className="text-primary font-bold hover:underline transition-all ml-1 underline-offset-4"
              href="/login"
            >
              Se connecter
            </a>
          </p>
        </div>
      </main>

      <Footer className="bg-transparent border-t-0 pb-8 pt-0" />
    </div>
  );
};

export default Register;
