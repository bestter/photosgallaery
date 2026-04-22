import React from 'react';

export default function AdminLayout({ children, title, subtitle, topActions }) {
    const currentPath = window.location.pathname;

    const handleLogout = () => {
        localStorage.removeItem('token');
        window.location.href = '/login';
    };

    const navLinks = [
        { path: '/dashboard', icon: 'person', label: 'Gestion des usagers' },
        { path: '/admin-groups', icon: 'group', label: 'Gestion des groupes' },
        { path: '/admin-group-requests', icon: 'group_add', label: 'Requêtes de Groupes', iconStyle: { fontVariationSettings: "'FILL' 1" } },
        { path: '/moderation', icon: 'shield', label: 'Modération' }
    ];

    return (
        <div className="bg-background text-on-background font-body h-screen flex overflow-hidden antialiased selection:bg-primary/30 selection:text-primary-fixed dark">
            {/* SideNavBar */}
            <aside className="fixed left-0 top-0 h-full w-64 z-50 border-r border-cyan-400/10 bg-slate-950 flex flex-col py-6">
                <div className="px-6 mb-8 flex items-center gap-3 cursor-pointer" onClick={() => window.location.href = '/'}>
                    <img alt="Cyanide Glass Logo" className="h-8 w-8 rounded object-cover" src="/Byla3.jpg" />
                    <div>
                        <h1 className="text-cyan-400 font-black uppercase tracking-widest text-xs font-['Inter']">Admin Core</h1>
                        <p className="text-slate-500 text-[10px] font-medium tracking-wider uppercase mt-0.5">System Control</p>
                    </div>
                </div>
                <nav className="flex-1 space-y-1 font-['Inter'] text-sm font-medium tracking-wide">
                    {navLinks.map((link) => {
                        const isActive = currentPath === link.path;
                        return (
                            <a 
                                key={link.path}
                                href={link.path} 
                                onClick={(e) => { e.preventDefault(); window.location.href = link.path; }}
                                className={isActive 
                                    ? "flex items-center gap-3 bg-cyan-400/20 text-cyan-400 border-l-4 border-cyan-400 px-4 py-3"
                                    : "flex items-center gap-3 text-slate-500 px-4 py-3 hover:bg-slate-800 hover:text-cyan-200 transition-all hover:translate-x-1 duration-200"}
                            >
                                <span className="material-symbols-outlined text-lg" style={link.iconStyle}>{link.icon}</span>
                                {link.label}
                            </a>
                        );
                    })}
                </nav>
                <div className="mt-auto border-t border-cyan-400/10 pt-4 space-y-1 font-['Inter'] text-sm font-medium tracking-wide bg-slate-900/40">
                    <button onClick={handleLogout} className="w-full flex items-center gap-3 text-slate-500 px-4 py-2 hover:bg-slate-800 hover:text-cyan-200 transition-all hover:translate-x-1 duration-200">
                        <span className="material-symbols-outlined text-lg">logout</span>
                        Logout
                    </button>
                </div>
            </aside>

            {/* Main Content Wrapper */}
            <div className="flex-1 ml-64 flex flex-col h-full relative">
                {/* TopNavBar */}
                <header className="fixed top-0 w-[calc(100%-16rem)] z-40 bg-slate-900/50 backdrop-blur-md border-b border-cyan-400/10 shadow-2xl shadow-black/50 flex items-center justify-between px-8 h-16 font-['Inter'] tracking-tight">
                    <div className="flex items-center gap-4 flex-1">
                        <div className="text-xl font-black tracking-tighter text-cyan-400">
                            Cyanide Glass
                        </div>
                    </div>
                    <div className="flex items-center gap-6">
                        {topActions}
                        <div className="flex items-center gap-3 cursor-pointer group">
                            <div className="text-right hidden md:block">
                                <div className="text-xs font-bold text-slate-300 group-hover:text-cyan-300 transition-colors">Admin</div>
                                <div className="text-[10px] text-primary/70 uppercase tracking-widest">SysAdmin</div>
                            </div>
                            <div className="w-8 h-8 rounded border border-cyan-400/30 group-hover:border-cyan-400 transition-colors bg-slate-800 flex items-center justify-center">
                                <span className="material-symbols-outlined text-slate-300 text-sm">person</span>
                            </div>
                        </div>
                    </div>
                </header>

                {/* Main Canvas */}
                <main className="flex-1 overflow-y-auto p-8 pt-24 pb-12 w-full max-w-7xl mx-auto">
                    {(title || subtitle) && (
                        <div className="mb-8 flex flex-col md:flex-row md:items-end justify-between gap-4">
                            <div>
                                {title && <h2 className="text-display font-display font-extrabold tracking-tight text-on-surface mb-2">{title}</h2>}
                                {subtitle && <p className="text-on-surface-variant text-sm max-w-2xl">{subtitle}</p>}
                            </div>
                        </div>
                    )}
                    {children}
                </main>
            </div>
        </div>
    );
}
