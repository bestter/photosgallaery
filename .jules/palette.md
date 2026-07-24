## 2024-05-18 - Keyboard accessibility for CSS-only hover tooltips
**Learning:** Found that custom tooltips using `group-hover:block` (like the Groups tooltip in Dashboard) were completely inaccessible to keyboard users because the trigger was not focusable and lacked focus-based display rules.
**Action:** Always add `tabIndex={0}` to the trigger element and include focus utility classes (like `group-focus:block` or `focus-within:block`) alongside the hover classes so the tooltip appears when navigating via keyboard.

## 2024-07-24 - Async Button Loading States in Modals
**Learning:** Adding inline loading spinners to interactive icon buttons inside modals improves perceived performance and provides necessary feedback for async actions (like downloading files or liking images). This also prevents duplicate submissions from impatient clicks.
**Action:** Always add an `isLiking` / `isDownloading` (or similar) state to handle UI feedback during API calls for interactive actions, disabling the button and swapping the icon for an `animate-spin` visual while processing.
