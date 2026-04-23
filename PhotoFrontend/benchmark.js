import React, { useMemo, useState } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';

function generateData(userCount, memberCount) {
    const allUsers = [];
    for (let i = 0; i < userCount; i++) {
        allUsers.push({ id: `user_${i}`, username: `User ${i}`, email: `user${i}@example.com` });
    }
    const members = [];
    for (let i = 0; i < memberCount; i++) {
        members.push({ userId: `user_${i}`, role: 1 });
    }
    return { allUsers, members };
}

function OldSelect({ allUsers, members }) {
    return React.createElement(
        'select',
        null,
        allUsers.filter(u => !members.some(m => (m.userId || m.UserId) === (u.id || u.Id))).map(user =>
            React.createElement('option', { key: user.id, value: user.id }, user.username)
        )
    );
}

function NewSelect({ allUsers, members }) {
    const memberIds = useMemo(() => new Set(members.map(m => m.userId || m.UserId)), [members]);
    const availableUsers = useMemo(() => allUsers.filter(u => !memberIds.has(u.id || u.Id)), [allUsers, memberIds]);

    return React.createElement(
        'select',
        null,
        availableUsers.map(user =>
            React.createElement('option', { key: user.id, value: user.id }, user.username)
        )
    );
}

const { allUsers, members } = generateData(5000, 1000);

console.time('Old rendering loop');
for (let i = 0; i < 50; i++) {
    renderToStaticMarkup(React.createElement(OldSelect, { allUsers, members }));
}
console.timeEnd('Old rendering loop');

console.time('New rendering loop');
for (let i = 0; i < 50; i++) {
    renderToStaticMarkup(React.createElement(NewSelect, { allUsers, members }));
}
console.timeEnd('New rendering loop');
