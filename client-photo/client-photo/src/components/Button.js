import React from 'react';

const Button = ({ 
  children, 
  size = 'md',       // Taille par défaut : medium
  variant = 'primary', // Style par défaut : plein avec la couleur brand
  className = '',    // Pour ajouter des classes Tailwind spécifiques si besoin
  ...props           // Récupère onClick, type="submit", disabled, etc.
}) => {
  // Les classes de base (communes à tous les boutons)
  const baseClasses = "inline-flex items-center justify-center font-medium transition-all active:scale-95 focus:outline-none focus:ring-2 focus:ring-brand focus:ring-offset-1";

  // Le dictionnaire des tailles
  const sizes = {
    sm: "text-sm px-3 py-1.5 rounded-md",
    md: "text-base px-4 py-2 rounded-lg",
    lg: "text-lg px-6 py-3 rounded-xl font-bold",
  };

  // Le dictionnaire des styles visuels
  const variants = {
    primary: "bg-brand text-white hover:bg-brand-dark shadow-md shadow-cyan-500/30",
    outline: "border-2 border-brand text-brand hover:bg-brand-light/40",
    ghost: "text-brand hover:bg-brand-light/40 hover:text-brand-dark", // Sans fond ni bordure, juste au survol
  };

  // On assemble toutes les classes
  const finalClasses = `${baseClasses} ${sizes[size]} ${variants[variant]} ${className}`;

  return (
    <button className={finalClasses} {...props}>
      {children}
    </button>
  );
};

export default Button;