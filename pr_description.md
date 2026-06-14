💡 What:
Added `aria-hidden="true"` to the mandatory field indicators (red asterisk `*`) for the Name, Email, Subject, and Message fields in the Contact form.

🎯 Why:
To prevent screen readers from redundantly announcing "star" for every required field. Since the associated `<input>` and `<textarea>` elements already use the native HTML5 `required` attribute, the screen reader will automatically announce that the fields are mandatory. Hiding the visual indicator cleans up the audio experience for visually impaired users.

📸 Before/After:
Before: `<span className="text-red-500 ml-1">*</span>`
After: `<span className="text-red-500 ml-1" aria-hidden="true">*</span>`

♿ Accessibility:
Improves screen reader experience by removing redundant announcements of decorative/visual required indicators, relying instead on programmatic semantic HTML (`required` attribute).
