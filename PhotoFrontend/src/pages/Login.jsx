import React, { useState } from "react";
import api from "../api";
import toast from "react-hot-toast";
import { useTranslation } from "react-i18next";
import { saveUserSession } from "../authHelper";
import Footer from "../components/Footer";

const Login = () => {
  const { t } = useTranslation();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    try {
      // Ajustez l'endpoint selon votre API
      const response = await api.post("/auth/login", { username, password });
      if (response.data && response.data.token) {
        saveUserSession(response.data.token);
        toast.success(t("auth.login.success"));
        window.location.href = "/";
      }
    } catch (err) {
      toast.error(t("auth.login.error"));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="bg-background text-on-background font-body min-h-screen flex flex-col glass-background fixed inset-0 z-50 overflow-y-auto">
      {/* Global Navigation Suppressed for Transactional Page (Login) */}
      <main className="flex-grow flex items-center justify-center px-6 py-12 w-full z-10 relative">
        {/* Login Card */}
        <div className="w-full max-w-md bg-surface-container-low p-8 lg:p-10 rounded-xl border border-outline-variant/40 shadow-2xl relative overflow-hidden">
          {/* Decorative Accent */}
          <div className="absolute top-0 left-0 w-full h-1 bg-primary"></div>

          <div className="flex flex-col items-center text-center mb-10">
            {/* Brand Identity */}
            <div className="mb-6">
              <img
                alt="PixelLyra Logo"
                className="h-24 w-auto mx-auto"
                src="/3hv8x.jpg"
              />
            </div>
            <h1 className="text-3xl font-extrabold tracking-tight text-on-surface font-headline mb-2">
              {t("auth.login.title")}
            </h1>
            <p className="text-on-surface-variant text-sm font-medium">
              {t("auth.login.subtitle")}
            </p>
          </div>

          <form className="space-y-6" onSubmit={handleSubmit}>
            {/* Username Input */}
            <div className="space-y-2">
              <label
                className="block text-[10px] font-bold uppercase tracking-widest text-primary ml-1"
                htmlFor="username"
              >
                {t("auth.login.username_label")} <span className="text-red-500" aria-hidden="true">*</span>
              </label>
              <div className="relative group">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-on-surface-variant group-focus-within:text-primary transition-colors">
                  <span
                    className="material-symbols-outlined"
                    aria-hidden="true"
                    data-icon="person"
                  >
                    person
                  </span>
                </div>
                <input
                  className="block w-full pl-10 pr-3 py-3 bg-surface-variant border-none rounded text-on-surface placeholder-on-surface-variant/50 focus:ring-2 focus:ring-primary focus:bg-surface-container-high transition-all outline-none"
                  id="username"
                  placeholder={t("auth.login.username_placeholder")}
                  type="text"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  required
                  disabled={isLoading}
                />
              </div>
            </div>

            {/* Password Input */}
            <div className="space-y-2">
              <div className="flex justify-between items-center px-1">
                <label
                  className="text-[10px] font-bold uppercase tracking-widest text-primary"
                  htmlFor="password"
                >
                  {t("auth.login.password_label")} <span className="text-red-500" aria-hidden="true">*</span>
                </label>
                <a
                  className="text-[10px] font-bold uppercase tracking-widest text-on-surface-variant hover:text-primary transition-colors"
                  href="#"
                >
                  {t("auth.login.forgot_password")}
                </a>
              </div>
              <div className="relative group">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-on-surface-variant group-focus-within:text-primary transition-colors">
                  <span aria-hidden="true" className="material-symbols-outlined" data-icon="lock">
                    lock
                  </span>
                </div>
                <input
                  className="block w-full pl-10 pr-10 py-3 bg-surface-variant border-none rounded text-on-surface placeholder-on-surface-variant/50 focus:ring-2 focus:ring-primary focus:bg-surface-container-high transition-all outline-none"
                  id="password"
                  placeholder={t("auth.login.password_placeholder")}
                  type={showPassword ? "text" : "password"}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  disabled={isLoading}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-on-surface-variant hover:text-primary transition-colors focus:outline-none focus-visible:text-primary"
                  aria-label={showPassword ? t("common.hide_password", "Hide password") : t("common.show_password", "Show password")}
                  title={showPassword ? t("common.hide_password", "Hide password") : t("common.show_password", "Show password")}
                  aria-pressed={showPassword}
                >
                  <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
                    {showPassword ? "visibility_off" : "visibility"}
                  </span>
                </button>
              </div>
            </div>

            {/* Primary CTA */}
            <button
              className="w-full bg-primary text-on-primary font-bold py-4 rounded uppercase tracking-widest text-xs hover:opacity-90 active:scale-[0.98] transition-all shadow-lg shadow-primary/20 disabled:opacity-50 flex items-center justify-center gap-2"
              type="submit"
              disabled={isLoading || !username || !password}
              title={
                !username || !password ? t("contact.error_fill_all") : ""
              }
            >
              {isLoading ? (
                <>
                  <span className="material-symbols-outlined animate-spin text-[18px]" aria-hidden="true">sync</span>
                  {t("auth.login.loading")}
                </>
              ) : (
                t("auth.login.submit")
              )}
            </button>
          </form>

          {/* Secondary Action */}
          <div className="mt-8 text-center">
            <p className="text-sm text-on-surface-variant">
              {t("auth.login.no_account")}
              <a
                className="text-primary font-bold hover:underline decoration-2 underline-offset-4 ml-1"
                href="/register"
              >
                {t("auth.login.register_link")}
              </a>
            </p>
          </div>
        </div>
      </main>

      <Footer className="bg-transparent border-t-0 pb-8 pt-0" />

      {/* Aesthetic Layer: Subdued background image for depth */}
      <div className="fixed inset-0 -z-10 opacity-10 pointer-events-none">
        <img
          alt="Aesthetic depth image"
          className="w-full h-full object-cover grayscale mix-blend-overlay"
          src="https://lh3.googleusercontent.com/aida-public/AB6AXuCkddj18PimOapu8gM8vrh4Sux73NOpEf9tG5oxaWzWvJx61B33wB26tGj86YJpIEqkGJE3GJuoP70INRdBGPArOPIFz_bqp0HkdJyQ8XvLd2QvI5slEwJZY70i14DVIDRQcUQrCykqRZbUKCV4Z7kWIUzqdWpa89J1etA2dNp_vclW163tB-4RtC7VmFJX8GxDpT-QprYO4xiw0UDpcttcjbwduQZKycfPmXoUsLjIAJRBypw4Dep_D_NDwACY5y43KfMhTUaXcnE"
        />
      </div>
    </div>
  );
};

export default Login;
