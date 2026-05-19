## 2024-05-18 - Hardcoded ARIA labels vs Translatable text
**Learning:** Hardcoded `aria-label` attributes on buttons that already contain visible text violate WCAG 2.5.3 (Label in Name), especially in a multi-language app (react-i18next). If a user switches to English but the `aria-label` remains "Télécharger l'image" (French), screen readers will announce the French text instead of the visible English text, causing severe confusion.
**Action:** Never use `aria-label` on buttons that have visible, descriptive text content unless you are specifically appending context that is visually hidden. If you must use `aria-label` to override or add to text, it must contain the visible text, and it must be translated using the `t()` function.

## 2025-02-12 - Material Symbols Font Ligature Accessibility
**Learning:** In the `PhotoFrontend` app, Material Symbols are implemented as a ligature font (e.g. `<span>person</span>`). If these spans lack `aria-hidden="true"`, screen readers announce the literal text "person", leading to confusing and redundant readouts, especially when placed near text inputs or other interactive elements.
**Action:** Always verify that `aria-hidden="true"` is applied to icon spans using ligature fonts (e.g. `className="material-symbols-outlined"`), and ensure that interactive elements have descriptive accessible names (like `aria-label`).
