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

  return (
    <div className="masonry-item relative group rounded-lg">
      <button
        type="button"
        className="block w-full overflow-hidden rounded-lg cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background-dark p-0 border-0 bg-transparent"
        onClick={onClick}
        aria-label={t('gallery.view_photo_by', { author })}
        title={t('gallery.view_photo_by', { author })}
      >
        <img
          className="w-full h-auto object-cover transform group-hover:scale-105 transition-transform duration-500"
          src={src}
          alt={alt}
        />
      </button>
      {/* Overlay is a sibling so the author control is not nested inside the open button. */}
      <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 transition-opacity flex flex-col justify-end p-4 rounded-lg pointer-events-none">
        {onAuthorClick ? (
          <button
            type="button"
            className="text-white text-xs font-medium hover:text-primary transition-colors hover:underline z-10 cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary rounded px-1 -ml-1 pointer-events-auto self-start"
            onClick={() => {
              const authorName = author.startsWith("@")
                ? author.slice(1)
                : author;
              onAuthorClick(authorName);
            }}
            aria-label={t('gallery.view_profile_of', { author })}
            title={t('gallery.view_profile_of', { author })}
          >
            {author}
          </button>
        ) : (
          <span className="text-white text-xs font-medium self-start">
            {author}
          </span>
        )}
      </div>
    </div>
  );
}
