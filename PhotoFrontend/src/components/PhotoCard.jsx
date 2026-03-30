import React from 'react';

export default function PhotoCard({ src, alt, author, onClick, onAuthorClick }) {
    return (
        <div className="masonry-item relative group cursor-pointer" onClick={onClick}>
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
                        className={`text-white text-xs font-medium ${onAuthorClick ? 'hover:text-primary transition-colors hover:underline z-10' : ''}`}
                        onClick={(e) => {
                            if (onAuthorClick) {
                                e.stopPropagation();
                                const authorName = author.startsWith('@') ? author.slice(1) : author;
                                onAuthorClick(authorName);
                            }
                        }}
                    >
                        {author}
                    </span>
                </div>
            </div>
        </div>
    );
}