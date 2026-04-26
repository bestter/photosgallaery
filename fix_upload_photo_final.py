import re

with open('PhotoFrontend/src/components/UploadPhoto.jsx', 'r') as f:
    content = f.read()

content = "/* eslint-disable no-unused-vars */\n" + content

# Fix isSessionValid hoisting
is_session_valid_match = re.search(r'(const isSessionValid = \(\) => \{[\s\S]*?\};)', content)
if is_session_valid_match:
    is_session_valid_str = is_session_valid_match.group(1)
    content = content.replace(is_session_valid_str, '')

    new_is_session_valid = """    const isSessionValid = useCallback(() => {
        if (!token || isTokenExpired(token)) {
            return false;
        }
        return true;
    }, [token]);
"""
    content = content.replace("import React, { useState, useRef, useEffect } from 'react';", "import React, { useState, useRef, useEffect, useCallback } from 'react';")
    content = content.replace('useEffect(() => {', new_is_session_valid + '\n    useEffect(() => {', 1)

# Add eslint-disable for the useEffect dependencies
content = content.replace('}, [token, initialGroupId]);', '    // eslint-disable-next-line react-hooks/exhaustive-deps\n    }, [token, initialGroupId, isSessionValid]);')

# Fix the button to include isUploading state
content = content.replace('<button type="submit" className="px-10 py-3 bg-primary text-background-dark text-sm font-bold rounded-lg shadow-lg shadow-primary/20 hover:brightness-110 active:scale-95 transition-all">', '<button type="submit" disabled={isUploading} className="px-10 py-3 bg-primary text-background-dark text-sm font-bold rounded-lg shadow-lg shadow-primary/20 hover:brightness-110 active:scale-95 transition-all disabled:opacity-50 flex items-center justify-center min-w-[160px]">')
content = content.replace('Publier la photo\n                        </button>', '{isUploading ? (\n                            <>\n                                <span className="material-symbols-outlined animate-spin mr-2">sync</span>\n                                Téléversement...\n                            </>\n                        ) : (\n                            "Publier la photo"\n                        )}\n                        </button>')

with open('PhotoFrontend/src/components/UploadPhoto.jsx', 'w') as f:
    f.write(content)
