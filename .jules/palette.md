## 2025-02-28 - Empty State with Call-to-Action
**Learning:** When users encounter an empty gallery or zero search results, a generic message like "Aucune image pour le moment" leaves them stuck. Providing a context-aware message (differentiating between "no uploads yet" and "no search results") alongside a relevant call-to-action (like an "Upload" button) significantly improves onboarding and feature discovery.
**Action:** Always design empty states as opportunities for action rather than dead ends, ensuring they have helpful guidance and accessible, relevant buttons (using aria-hidden="true" on decorative icons).
## 2026-05-05 - Explicit Disabled States on Action Buttons
**Learning:** Relying solely on CSS (e.g., `disabled:opacity-50`) or logical checks within a submit handler isn't enough for good UX or accessibility. Form submission buttons should visually and functionally communicate when they cannot be clicked, especially for required fields like file selection.
**Action:** Always add explicit `disabled` attributes linked to form validation state on critical action buttons. Additionally, use `title` (or `aria-label`) tooltips on disabled buttons to give users immediate, context-aware feedback on *why* the button is disabled and what they need to do to proceed.

## 2025-05-06 - Search Input Accessibility
**Learning:** Search inputs (especially on mobile layouts without visible labels) often lack adequate context for screen readers. While placeholders provide visual hints, they are not a substitute for proper ARIA labels.
**Action:** Always add an explicit `aria-label` to search inputs, ensuring that users navigating with screen readers understand the input's purpose.
## 2026-05-07 - Explicit Disabled States on Authentication Forms\n**Learning:** Implementing disabled states and tooltips based on form validation enhances user experience and accessibility, but requires escaping special characters in JSX (like apostrophes with \&apos;) to pass strict linting rules.\n**Action:** Apply disabled states using form validation logic, provide descriptive tooltips, and always resolve 'react/no-unescaped-entities' lint errors.
