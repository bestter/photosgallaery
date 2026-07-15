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

## 2026-05-30 - Native tooltips for icon-only buttons
**Learning:** In `Dashboard.jsx`, icon-only pagination buttons had `aria-label`s for screen readers but lacked `title` attributes. Adding the `title` attribute provides a native hover tooltip, which is crucial for sighted users to immediately understand the function of an icon if it is not universally recognizable.
**Action:** When implementing icon-only interactive elements, ensure they include both a localized `aria-label` for screen readers and a matching `title` attribute for visual hover context.

## 2026-05-31 - Localized Loading State for 'Load More'
**Learning:** In `PhotoFrontend`, when implementing pagination features like a "Load More" button that appends items to an existing list, using a global `isLoading` state causes the entire existing data set to visually unmount and flash a full-page spinner. This is jarring and destroys context.
**Action:** Always use a dedicated localized loading state (e.g., `isFetchingMore`) for appending data. This localized state should be used to display an inline spinner inside the button itself, ensuring a smooth and non-disruptive user experience.

## 2024-05-31 - Avoid breaking translations by flattening objects
**Learning:** In the `PhotoFrontend` app, when replacing hardcoded text with translations, ensure you add the new translation keys to the existing translation objects appropriately without nesting existing keys or changing their structural location. In my previous attempt, I inadvertently moved `admin.moderation.title` to `admin.moderation.reports.title` creating an object where a string previously was. This creates a high risk of breaking other components that expect the string at that location.
**Action:** When adding new translations using `react-i18next`, append the keys thoughtfully without nesting/moving existing translation nodes, to guarantee backwards compatibility.

## 2024-06-05 - Native tooltips and ARIA localization for icon-only action buttons
**Learning:** Found an icon-only delete button in `AdminGroups.jsx` with hardcoded French `aria-label` and `title` attributes (e.g., `title="Supprimer ce groupe"`). This violates localization standards for multi-language apps, causing screen readers to read French text for English users and tooltips to be similarly hardcoded. Also discovered the "Remove member" button had an `aria-label` with hardcoded French text alongside its visible "Retirer" label.
**Action:** Always replace hardcoded string literals in `aria-label` and `title` attributes on icon-only interactive elements with localized equivalents using `t()` from `react-i18next`. Additionally, ensure visible button text is also translated correctly. Ensure you update both the `fr` and `en` `translation.json` files.

## 2026-06-09 - Standardizing Loading States for Async Actions
**Learning:** Found a table in `Dashboard.jsx` using the hardcoded string "Chargement..." to indicate a loading state, instead of the standardized animated spinner + translated text pattern used elsewhere in the application. This violates localization standards and provides inconsistent visual feedback for async operations. Hardcoded text should be avoided, and visual feedback using loading spinners ensures clear feedback to users.
**Action:** Always replace hardcoded loading string literals (like "Chargement...") with an accessible animated spinner using the Material Symbols `sync` icon and the Tailwind `animate-spin` class, alongside translated text for consistency and accessibility.

## 2026-06-11 - Hardcoded ARIA labels for dynamic objects
**Learning:** Found several dynamic elements like the 'open photo' button in `Gallery.jsx` and the 'accept/reject' buttons in `AdminGroupRequests.jsx` using template literals with hardcoded French text for their `aria-label` and `alt` attributes (e.g. ``aria-label={`Ouvrir la photo ${photo.title}`}``). This violates localization standards, as non-French screen reader users would hear French text, causing massive confusion. It also breaks the site's `react-i18next` internationalization setup.
**Action:** Always replace hardcoded template strings in `aria-label` and `alt` attributes with proper localization functions. For dynamic content, pass the variables into the translation function (e.g. `t("gallery.open_photo", { title: photo.title })`) and define the corresponding parameter blocks in both English and French `translation.json` files.

## 2026-06-11 - Explicit "Required" indicators on Form Labels
**Learning:** Sighted and screen reader users need to be explicitly informed about which fields are required before submitting forms. Relying purely on the submit validation or native `required` attribute isn't enough for sighted users if there is no visual indicator. Using a red asterisk is a common and understood pattern, but we must be careful to make it `aria-hidden="true"` so screen readers don't announce "star" (since the `required` attribute is already read out by screen readers).
**Action:** When creating forms with required fields, always append `<span className="text-red-500" aria-hidden="true">*</span>` or similar indicator to the label text. Ensure the input itself has the `required` attribute for functional and programmatic accessibility.
## 2026-06-21 - Native tooltips and ARIA localization for icon-only action buttons
**Learning:** Found an icon-only 'Retour aux groupes' button in `AdminGroups.jsx` with hardcoded French text alongside the icon, but missing localized `aria-label` and `title` attributes. This violates localization standards for multi-language apps and lacks proper tooltip support for sighted users.
**Action:** Always replace hardcoded string literals with localized equivalents using `t()` from `react-i18next` for both visible text and ARIA labels. Ensure icon-only interactive elements also receive a `title` attribute containing the localized text to provide a native hover tooltip.

## 2024-06-27 - Required Field Indicators in Modals
**Learning:** Adding visual required indicators (like asterisks) to mandatory form fields is a crucial UX pattern. However, for accessibility, these visual cues must be hidden from screen readers (using `aria-hidden="true"`) if the input itself already uses the HTML5 `required` attribute. This prevents redundant announcements while maintaining clear visual guidance for sighted users.
**Action:** Always pair visual required indicators (e.g., `<span aria-hidden="true">*</span>`) with the semantic `required` attribute on the corresponding input element.

## 2024-05-18 - Avoid CSS Hover Dropdowns
**Learning:** Hover-only dropdowns (e.g., using `group-hover:opacity-100`) completely break keyboard navigation because they cannot be reliably triggered by focusing the toggle button. They also create a poor user experience on touch devices where hover states are unpredictable.
**Action:** Always implement dropdowns using click/keyboard-driven state (like a React `isOpen` state) rather than CSS-only hover, and ensure the toggle button uses `aria-expanded` and `aria-haspopup` for proper accessibility across all devices.

## 2024-07-07 - Semantic Elements for Interactive Actions
**Learning:** Found non-interactive elements like `<h3>` and `<span>` using `onClick` handlers and `cursor-pointer` to trigger in-page actions (e.g., author clicks and tag clicks in `ImageModal.jsx`). This degrades keyboard accessibility because these elements cannot be focused via the Tab key and screen readers do not announce them as actionable controls.
**Action:** Always use semantic `<button type="button">` elements for interactive on-page actions, rather than attaching `onClick` events to text or container elements like `div`, `span`, or `h3`.

## 2024-07-13 - Fix visually hidden interactive elements during keyboard navigation
**Learning:** Using `opacity-0 group-hover:opacity-100` on interactive elements or their containers makes them inaccessible to keyboard users because elements remain visually hidden even when receiving keyboard focus.
**Action:** Always complement `group-hover:opacity-100` with `focus-within:opacity-100` (and `group-focus-visible:opacity-100` if the container itself is focusable) to ensure interactive elements are visible when they or their children receive keyboard focus.
