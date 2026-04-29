## 2025-04-26 - Icon-Only Button Accessibility Pattern
**Learning:** When using Material Symbols ligatures (like `close`, `chevron_left`) inside `<span>` tags for icon-only buttons, screen readers attempt to read the ligature text, which can be confusing (e.g., "chevron underscore left button").
**Action:** Always add `aria-hidden="true"` to the `<span>` containing the material symbol ligature, and ensure the parent `<button>` has a descriptive `aria-label` and `title` to provide accessible context to screen readers and tooltips to sighted users.## 2026-04-28 - Explicit Form Control Linking
**Learning:** React/JSX form components sometimes lack explicit linkage between `<label>` elements and their corresponding input fields, relying only on visual proximity or implicit structure. This is a critical accessibility failure as screen readers cannot announce the field's purpose correctly.
**Action:** Always ensure that form labels contain an `htmlFor` attribute that exactly matches the `id` attribute of the associated `<input>`, `<textarea>`, or `<select>` element. This guarantees screen reader compatibility and enhances usability by enabling click-to-focus behavior.

## 2024-05-18 - Keyboard Navigation in Modals
**Learning:** Adding explicit keyboard navigation shortcuts (e.g., ArrowLeft, ArrowRight) in media viewers drastically improves accessibility and UX for keyboard power users. Relying solely on on-screen buttons creates friction.
**Action:** Always implement ArrowLeft/ArrowRight listeners for any component presenting a horizontal sequence of items (like a carousel or a modal gallery). Ensure that aria-labels clearly mention these shortcuts to users relying on assistive technology.
