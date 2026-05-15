## 2024-05-15 - [Gallery UI Accessibility and ARIA Attributes]
**Learning:** Adding an `aria-label` to an interactive element that already contains visible text replaces the visible text completely for screen readers, violating WCAG 2.5.3 (Label in Name).
**Action:** When adding an `aria-label` to clarify an action, ensure the visually-presented text is included or seamlessly integrated into the `aria-label` value. Also, if a nested interactive visual button element is used, ensuring it has `tabIndex={-1}` and `aria-hidden="true"` effectively removes redundant elements.
