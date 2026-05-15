🎯 What
Optimized the tag filtering rendering overhead in `Gallery.jsx`.

💡 Why
During the render phase, string representations of tags (`photoTags`) were computed dynamically for each photo on every re-render by iterating over `photo.tags` arrays, checking translations, mapping, and filtering. This caused significant O(n*m) blocking overhead on the React main thread when typing in the search box, even though the main array `filteredPhotos` was memoized using `useMemo`.

✅ Verification
- Ensured the `Gallery.jsx` filters and search still work.
- Ran `vitest` in the `PhotoFrontend` directory which confirmed `PhotoCard.test.jsx` still passes.
- Linting checks passed via `pnpm lint`.
- Safe extraction of string formatting logic out of the JSX render iteration mapping.

✨ Result
Display tag calculations (`_displayTags`) are now computed once during the `useMemo` block execution, removing O(N) mapping loops from the rendering phase and reducing blocking time on the UI thread when search inputs trigger rendering.
