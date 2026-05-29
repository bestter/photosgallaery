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

## 2025-02-28 - Add standard loading states for async actions
**Learning:** Found multiple modals (GroupRequestModal, InviteModal) implementing async actions that updated button text (e.g. 'Sending...') but lacked a standardized visual loading indicator (spinner). This inconsistency breaks the user expectation for async feedback (visual feedback like a spinner) and violates the memory's Frontend UX standard.
**Action:** Always add a loading spinner using the 'sync' material symbol icon along with standard text for long-running asynchronous actions in modals and ensure flexbox styles are used for proper alignment.

## 2024-05-13 - Add `aria-label` to clear search button in Moderation dashboard
**Learning:** Found a search input in the Moderation dashboard without a clear button.
**Action:** Always ensure that search inputs have an accessible clear button with `aria-label` attribute and proper focus styles.

## 2024-05-25 - Improve Loading Feedback in Modals
**Learning:** For asynchronous loading states on action buttons in PhotoFrontend, replacing standard icons with a Material Symbols 'sync' icon utilizing Tailwind's animate-spin utility (e.g., `<span className="material-symbols-outlined animate-spin" aria-hidden="true">sync</span>`) provides much clearer visual feedback during form submissions (like the Report Modal). This prevents users from repeatedly clicking the submit button, reducing frustration and potential API duplicate errors. The flexbox classes `flex items-center justify-center gap-2` or `space-x-2` ensure proper alignment with text.
**Action:** Always include an explicit animated visual indicator and disabled state for all buttons that trigger asynchronous API actions. Wrap the text in `t()` function for translations.

## 2024-05-28 - Hardcoded ARIA labels in Pagination
**Learning:** Pagination buttons in components like `Dashboard.jsx` had hardcoded French `aria-label`s (e.g., "Page précédente", "Aller à la page 2"). This violates accessibility guidelines for localized apps where the screen reader needs to announce text matching the current locale.
**Action:** Use translation functions (e.g., `t('common.previous_page')`) to dynamically localize `aria-label` attributes on pagination controls, ensuring they adapt correctly to the user's selected language.

## 2024-05-29 - Icon-only buttons tooltip with `title` attribute
**Learning:** Icon-only buttons with an `aria-hidden` span for the icon should not only have an `aria-label` for screen readers but also a `title` attribute. The `title` attribute acts as a native hover tooltip, making the button's action explicitly clear for sighted users who might not immediately recognize the icon.
**Action:** When creating or fixing icon-only buttons, always ensure that both `aria-label` and `title` attributes are present and contain the same translated text (using the `t()` function).
