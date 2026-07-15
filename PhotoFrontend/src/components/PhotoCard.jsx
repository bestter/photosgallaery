import React from "react";
import { useTranslation } from "react-i18next";

export default function PhotoCard({
  src,
  alt,
  author,
  onClick,
  onAuthorClick,
}) {
  const { t } = useTranslation();
  const handleKeyDown = (e) => {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      if (onClick) onClick(e);
    }
  };

  return (
    <div
      className="masonry-item relative group cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background-dark rounded-lg"
      onClick={onClick}
      role="button"
      tabIndex={0}
      onKeyDown={handleKeyDown}
      aria-label={t('gallery.view_photo_by', { author })}
    >
      <div className="overflow-hidden rounded-lg">
        <img
          className="w-full h-auto object-cover transform group-hover:scale-105 transition-transform duration-500"
          src={src}
          alt={alt}
        />
      </div>
      <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 group-focus-visible:opacity-100 focus-within:opacity-100 transition-opacity flex flex-col justify-end p-4 rounded-lg">
        <div className="flex items-center justify-between">
          {onAuthorClick ? (
            <button
              type="button"
              className="text-white text-xs font-medium hover:text-primary transition-colors hover:underline z-10 cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary rounded px-1 -ml-1"
              onClick={(e) => {
                e.stopPropagation();
                const authorName = author.startsWith("@")
                  ? author.slice(1)
                  : author;
                onAuthorClick(authorName);
              }}
              aria-label={t('gallery.view_profile_of', { author })}
            >
              {author}
            </button>
          ) : (
            <span className="text-white text-xs font-medium">
              {author}
            </span>
          )}
        </div>
      </div>
    </div>
  );
}
