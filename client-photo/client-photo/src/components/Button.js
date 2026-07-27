import React from 'react';

// Module-scope static maps so they are not rebuilt every render.
const SIZES = {
  sm: "text-sm px-3 py-1.5 rounded-md",
  md: "text-base px-4 py-2 rounded-lg",
  lg: "text-lg px-6 py-3 rounded-xl font-bold",
};

const VARIANTS = {
  primary: "bg-brand text-white hover:bg-brand-dark shadow-md shadow-cyan-500/30",
  outline: "border-2 border-brand text-brand hover:bg-brand-light/40",
  ghost: "text-brand hover:bg-brand-light/40 hover:text-brand-dark",
};

const BASE_CLASSES = "inline-flex items-center justify-center font-medium transition-all active:scale-95 focus:outline-none focus:ring-2 focus:ring-brand focus:ring-offset-1";

const Button = ({ 
  children, 
  size = 'md',
  variant = 'primary',
  className = '',
  ...props
}) => {
  const finalClasses = `${BASE_CLASSES} ${SIZES[size]} ${VARIANTS[variant]} ${className}`;

  return (
    <button className={finalClasses} {...props}>
      {children}
    </button>
  );
};

export default Button;
