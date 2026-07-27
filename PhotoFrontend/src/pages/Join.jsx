import React, { useEffect } from 'react';

import { useTranslation } from 'react-i18next';

/** Session-only handoff for group invite codes between /join and /register. */
const PENDING_INVITE_KEY = 'pendingInvite';

export default function Join() {
    const { t } = useTranslation();
    // Path is /join/:inviteCode — stash briefly in sessionStorage (tab-scoped)
    // so it is not a durable localStorage secret and not re-read as a privileged URL param.

    useEffect(() => {
        const pathParts = window.location.pathname.split('/');
        // e.g. ["", "join", "uuid"]
        if (pathParts.length >= 3 && pathParts[1] === 'join') {
            const inviteCode = pathParts[2];
            if (inviteCode) {
                try {
                    sessionStorage.setItem(PENDING_INVITE_KEY, inviteCode);
                } catch {
                    // Ignore quota / private-mode failures; registration still works without invite.
                }
            }
        }

        window.location.href = '/register';
    }, []);

    return (
        <div className="flex h-screen items-center justify-center bg-background text-on-surface font-body">
            <div className="text-center">
                <span aria-hidden="true" className="material-symbols-outlined animate-spin text-4xl text-primary block mb-4">sync</span>
                <p className="text-on-surface-variant font-medium tracking-wide">{t("join.validating_invitation")}</p>
            </div>
        </div>
    );
}
