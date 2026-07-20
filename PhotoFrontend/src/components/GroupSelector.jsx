import React, { useState, useRef, useEffect, useMemo } from 'react';
import { useTranslation } from 'react-i18next';

const GroupSelector = ({ groups, activeGroupId, onGroupSelect }) => {
    const { t } = useTranslation();
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef(null);

    useEffect(() => {
        const handleClickOutside = (event) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsOpen(false);
            }
        };
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    // ⚡ Bolt: Memoize the active group lookup to avoid O(N) array scans on every render cycle
    const activeGroup = useMemo(() => {
        if (!groups || groups.length === 0) return null;
        return groups.find(g => (g.id || g.Id) === activeGroupId) || groups[0];
    }, [groups, activeGroupId]);

    if (!groups || groups.length === 0) {
        return null;
    }

    if (groups.length === 1) {
        return (
            <div className="flex items-center gap-2 text-slate-400 font-medium py-1 cursor-default">
                <span aria-hidden="true" className="material-symbols-outlined text-[18px]">groups</span>
                <span>{activeGroup.name || activeGroup.Name}</span>
            </div>
        );
    }

    return (
        <div className="relative" ref={dropdownRef}>
            <button
                onClick={() => setIsOpen(!isOpen)}
                aria-expanded={isOpen}
                aria-label={t("components.group_selector.toggle_aria", { name: activeGroup.name || activeGroup.Name, defaultValue: "Select a group: {{name}}" })}
                title={t("components.group_selector.toggle_title", "Select a group")}
                aria-haspopup="menu"
                className="flex items-center gap-2 text-slate-400 hover:text-slate-100 transition-colors duration-200 py-1 focus:outline-none focus-visible:ring-2 focus-visible:ring-cyan-400 rounded"
            >
                <span aria-hidden="true" className="material-symbols-outlined text-[18px]">groups</span>
                <span>{activeGroup.name || activeGroup.Name}</span>
                <span aria-hidden="true" className={`material-symbols-outlined text-[16px] transition-transform duration-200 ${isOpen ? 'rotate-180' : ''}`}>keyboard_arrow_down</span>
            </button>
            {/* Dropdown Menu */}
            <div
                className={`absolute left-0 top-full mt-2 w-56 bg-surface-container-high rounded-lg shadow-2xl border border-outline-variant/40 transition-all duration-200 overflow-hidden z-50 ${
                    isOpen ? 'opacity-100 visible translate-y-0' : 'opacity-0 invisible -translate-y-2'
                }`}
                role="menu"
            >
                <div className="px-3 py-2 text-[10px] font-bold uppercase tracking-widest text-slate-500 bg-surface-container-highest/50">
                    {t("components.group_selector.your_collectives")}
                </div>
                {groups.map((group) => {
                    const groupId = group.id || group.Id;
                    const groupName = group.name || group.Name;
                    const isActive = groupId === activeGroupId;

                    return (
                        <button
                            key={groupId}
                            role="menuitem"
                            onClick={() => {
                                onGroupSelect(groupId);
                                setIsOpen(false);
                            }}
                            className={`w-full flex items-center gap-3 px-4 py-3 text-sm text-left transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-cyan-400 focus-visible:ring-inset ${
                                isActive 
                                    ? 'text-cyan-400 bg-cyan-400/10' 
                                    : 'text-slate-300 hover:bg-cyan-400/10 hover:text-cyan-400'
                            }`}
                        >
                            <span aria-hidden="true"
                                className="material-symbols-outlined text-[18px]"
                                style={isActive ? { fontVariationSettings: "'FILL' 1" } : {}}
                            >
                                {isActive ? 'stars' : 'landscape'}
                            </span>
                            {groupName}
                        </button>
                    );
                })}
            </div>
        </div>
    );
};

export default GroupSelector;
