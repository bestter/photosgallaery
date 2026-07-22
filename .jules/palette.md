## 2024-05-18 - Keyboard accessibility for CSS-only hover tooltips
**Learning:** Found that custom tooltips using `group-hover:block` (like the Groups tooltip in Dashboard) were completely inaccessible to keyboard users because the trigger was not focusable and lacked focus-based display rules.
**Action:** Always add `tabIndex={0}` to the trigger element and include focus utility classes (like `group-focus:block` or `focus-within:block`) alongside the hover classes so the tooltip appears when navigating via keyboard.
