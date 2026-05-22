## 2024-05-18 - Hardcoded ARIA labels vs Translatable text
**Learning:** Hardcoded `aria-label` attributes on buttons that already contain visible text violate WCAG 2.5.3 (Label in Name), especially in a multi-language app (react-i18next). If a user switches to English but the `aria-label` remains "Télécharger l'image" (French), screen readers will announce the French text instead of the visible English text, causing severe confusion.
**Action:** Never use `aria-label` on buttons that have visible, descriptive text content unless you are specifically appending context that is visually hidden. If you must use `aria-label` to override or add to text, it must contain the visible text, and it must be translated using the `t()` function.

## 2025-02-12 - Material Symbols Font Ligature Accessibility
**Learning:** In the `PhotoFrontend` app, Material Symbols are implemented as a ligature font (e.g. `<span>person</span>`). If these spans lack `aria-hidden="true"`, screen readers announce the literal text "person", leading to confusing and redundant readouts, especially when placed near text inputs or other interactive elements.
**Action:** Always verify that `aria-hidden="true"` is applied to icon spans using ligature fonts (e.g. `className="material-symbols-outlined"`), and ensure that interactive elements have descriptive accessible names (like `aria-label`).
## 2024-05-20 - Add Clear Search Button

**Learning:** When adding a clear search button absolutely positioned within a search input in Tailwind CSS, ensure you update the input's padding (e.g., `pr-10`) to prevent text overlap.
**Action:** Always check input padding when overlaying buttons. Ensure all icon-only buttons receive `aria-label`s localized to the project's setup (e.g., using `t()`).
## 2024-05-21 - Hardcoded English ARIA labels in UI components
**Learning:** Found several generic UI buttons (like modal close buttons, carousel navigation arrows, and language selectors) that had hardcoded English `aria-label`s (e.g., "Close", "Upload a photo"). This violates the localization requirement and WCAG 2.5.3 when used by non-English users, as the screen reader text will mismatch the visual context or site language.
**Action:** Replaced hardcoded `aria-label` values with their localized equivalents using `t()` from `react-i18next`, adding translation keys to `common` scope for reuse across different components.

## 2026-05-22 - Add Loading Spinners to Form Submissions
**Learning:** In `PhotoFrontend`, providing clear visual feedback during asynchronous operations (like form submissions) improves user experience. Tailwind's `animate-spin` utility combined with the `sync` icon from Material Symbols is an effective and consistent pattern for this across the application.
**Action:** When adding asynchronous actions to buttons, implement a loading state that displays the `sync` icon with `animate-spin` and ensure it aligns nicely with text using flexbox (`flex items-center justify-center gap-2`).
