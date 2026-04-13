import React from 'react';

const GroupSelector = ({ groups, activeGroupId, onGroupSelect }) => {
    if (!groups || groups.length === 0) {
        return null; // Pas de groupe disponible
    }

    const activeGroup = groups.find(g => (g.id || g.Id) === activeGroupId) || groups[0];

    if (groups.length === 1) {
        return (
            <div className="flex items-center gap-2 text-slate-400 font-medium py-1 cursor-default">
                <span className="material-symbols-outlined text-[18px]">groups</span>
                <span>{activeGroup.name || activeGroup.Name}</span>
            </div>
        );
    }

    return (
        <div className="relative group">
            <button className="flex items-center gap-2 text-slate-400 hover:text-slate-100 transition-colors duration-200 py-1">
                <span className="material-symbols-outlined text-[18px]">groups</span>
                <span>{activeGroup.name || activeGroup.Name}</span>
                <span className="material-symbols-outlined text-[16px]">keyboard_arrow_down</span>
            </button>
            {/* Dropdown Menu */}
            <div className="absolute left-0 top-full mt-2 w-56 bg-surface-container-high rounded-lg shadow-2xl border border-outline-variant/40 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-200 overflow-hidden">
                <div className="px-3 py-2 text-[10px] font-bold uppercase tracking-widest text-slate-500 bg-surface-container-highest/50">
                    Your Collectives
                </div>
                {groups.map((group) => {
                    const groupId = group.id || group.Id;
                    const groupName = group.name || group.Name;
                    const isActive = groupId === activeGroupId;

                    return (
                        <button
                            key={groupId}
                            onClick={() => onGroupSelect(groupId)}
                            className={`w-full flex items-center gap-3 px-4 py-3 text-sm text-left transition-colors ${
                                isActive 
                                    ? 'text-cyan-400 bg-cyan-400/10' 
                                    : 'text-slate-300 hover:bg-cyan-400/10 hover:text-cyan-400'
                            }`}
                        >
                            <span 
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
