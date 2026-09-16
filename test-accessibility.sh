#!/bin/bash
echo "Testing accessible modals in Gallery..."
grep -rn "div.*fixed.*inset-0.*bg-black/50" PhotoFrontend/src/pages/Gallery.jsx
