## 2024-05-19 - [Missing tooltips on icon-only password toggles]
**Learning:** Icon-only password visibility toggles were missing native hover tooltips, making their function less discoverable for sighted mouse users. Providing just an `aria-label` only benefits screen-reader users.
**Action:** Always pair `aria-label` with a matching `title` on icon-only buttons to ensure both screen reader and mouse users get context.
