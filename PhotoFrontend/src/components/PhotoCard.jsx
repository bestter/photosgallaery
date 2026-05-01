import React from "react";

export default function PhotoCard({
  src,
  alt,
  author,
  onClick,
  onAuthorClick,
}) {
  const handleKeyDown = (e) => {
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      if (onClick) onClick(e);
    }
  };

  const handleAuthorKeyDown = (e) => {
    if (onAuthorClick && (e.key === "Enter" || e.key === " ")) {
      e.preventDefault();
      e.stopPropagation();
      const authorName = author.startsWith("@") ? author.slice(1) : author;
      onAuthorClick(authorName);
    }
  };

  return (
    <div
      className="masonry-item relative group cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background-dark rounded-lg"
      onClick={onClick}
      role="button"
      tabIndex={0}
      onKeyDown={handleKeyDown}
      aria-label={`View photo by ${author}`}
    >
      <div className="overflow-hidden rounded-lg">
        <img
          className="w-full h-auto object-cover transform group-hover:scale-105 transition-transform duration-500"
          src={src}
          alt={alt}
        />
      </div>
      <div className="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex flex-col justify-end p-4 rounded-lg">
        <div className="flex items-center justify-between">
          <span
            className={`text-white text-xs font-medium ${onAuthorClick ? "hover:text-primary transition-colors hover:underline z-10 cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary rounded px-1 -ml-1" : ""}`}
            onClick={(e) => {
              if (onAuthorClick) {
                e.stopPropagation();
                const authorName = author.startsWith("@")
                  ? author.slice(1)
                  : author;
                onAuthorClick(authorName);
              }
            }}
            role={onAuthorClick ? "button" : undefined}
            tabIndex={onAuthorClick ? 0 : undefined}
            onKeyDown={onAuthorClick ? handleAuthorKeyDown : undefined}
            aria-label={onAuthorClick ? `View profile of ${author}` : undefined}
          >
            {author}
          </span>
        </div>
      </div>
    </div>
  );
}
