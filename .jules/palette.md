## 2026-07-30 - ARIA Tooltip Accessibility
**Learning:** When creating CSS-only informational tooltips, using `group-focus:block` might trigger the tooltip when the parent receives focus, but it lacks semantic meaning. Relying solely on `aria-label` on the parent to duplicate the tooltip text flattens the content and can lead to poor screen reader experiences. Using proper ARIA attributes like `role="tooltip"` with a unique ID and linking it via `aria-describedby` on the focusable trigger provides a robust and semantically correct experience.
**Action:** Always implement semantic ARIA attributes (`role="tooltip"`, `aria-describedby`) for custom tooltips, even when using CSS-only hover/focus states to reveal them.
## 2026-08-02 - Accessible Search Inputs & Filter Contrast
**Learning:** Relying solely on `aria-label` for search inputs can reduce screen reader compatibility compared to using explicit `htmlFor`/`id` bindings. Additionally, using `hover:text-white` on close icons inside bright background buttons (like Cyan 400) causes severe WCAG contrast failures (~1.5:1).
**Action:** Always pair search inputs with explicit, visually hidden `<label className="sr-only">` elements and `id` bindings. For bright active filter buttons, maintain dark text color on hover and rely on CSS transforms (e.g., `hover:scale-125`) or parent background brightness changes for interactive feedback.

## 2026-08-06 - ImageModal Action Buttons Accessibility
**Learning:** Icon-only or minimally labeled buttons within modals (like "Download", "Share") often lack sufficient context for screen readers when they rely only on adjacent tiny text or icons.
**Action:** Always ensure critical action buttons in media viewers have explicit `aria-label` attributes and `title` tooltips, leveraging existing translation dictionaries to provide localized context for both screen reader users and mouse hover interactions.
2025-02-15 - Stretched Native Button over Clickable Card
Learning: Using `<div role="button">` for complex cards flattens their semantics for screen readers and requires manual keyboard handlers. Overlaid native `<button>` combined with CSS `absolute inset-0` and correct z-index layering preserves semantics and provides native keyboard operability.
Action: Use absolute positioned native `<button>` elements overlaid on relative card containers instead of `role="button"` on `div` wrappers.

2025-02-15 - React Modal Accessibility Without Native Dialog
Learning: When building custom modals in React using absolute positioned `div` overlays, failing to include `role="dialog"` and `aria-modal="true"` renders the modal structurally invisible to screen readers, meaning they won't trap virtual focus and will allow users to read the obscured background content.
Action: Always add `role="dialog"` and `aria-modal="true"` to the outermost container of any custom modal overlay, along with an accessible name via `aria-label` or `aria-labelledby`.
