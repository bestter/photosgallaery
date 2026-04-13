# Cyanide Glass: Design System

### 1. Overview & Creative North Star
**Creative North Star: The Neon Brutalist**
Cyanide Glass is a high-contrast, editorial-inspired design system that balances the clinical precision of a dark-mode administrative dashboard with the vibrant energy of tech-noir aesthetics. It rejects the traditional "boxy" layout in favor of deep tonal layering and atmospheric depth.

The system uses a signature electric cyan (#00ced1) as a piercing accent against a sophisticated palette of deep teal-greys and slates. Asymmetry is used intentionally in navigation sidebars and header blur effects to create a "liquid" feel that guides the eye toward critical data points.

### 2. Colors
The palette is built on a foundation of "Deep Sea" neutrals, punctuated by "Cyber Cyan" primaries.

- **The "No-Line" Rule:** Sectioning must be achieved through background shifts (e.g., transitioning from `surface` to `surface_container_low`) rather than 1px solid borders wherever possible. In cases where structural separation is required, use `outline_variant` at 40% opacity.
- **Surface Hierarchy & Nesting:** Depth is created by "stacking" lighter teal shades. The main workspace sits on `background`, while widgets and cards use `surface_container_low`. Overlays and active states use `surface_container_high`.
- **The "Glass & Gradient" Rule:** Navigation headers and floating menus utilize `backdrop-blur-md` with a 50% transparent background to maintain a sense of environmental continuity.
- **Signature Textures:** Primary CTAs should use a solid `#00ced1` fill, while secondary indicators (like notifications or active nav states) use a 20% alpha wash of the primary color (`rgba(0, 206, 209, 0.2)`).

### 3. Typography
Cyanide Glass relies on **Inter** to provide a clean, hyper-readable foundation that scales from dense data tables to bold editorial headings.

**Ground Truth Typography Scale:**
- **Display/Hero:** `1.875rem` (30px) - Extrabold, tracking-tight. Used for page titles.
- **Headline:** `1.25rem` (20px) - Bold. Used for section headers.
- **Title:** `1.125rem` (18px) - Bold. Used for card titles.
- **Body:** `0.875rem` (14px) - Regular to Medium. Standard for all UI text and data.
- **Label/Small:** `0.75rem` (12px) - Semibold. Used for metadata and button text.
- **Micro-Label:** `10px` - Bold, uppercase, tracking-widest. Used for sidebar category headers.

The typographic rhythm is intentionally tight, using font-weight (from 400 to 900) rather than size to establish hierarchy.

### 4. Elevation & Depth
Elevation is handled via tonal density and translucency rather than heavy dropshadows.

- **The Layering Principle:** 
    - Base: `background` (#0f2323)
    - Level 1: `surface_container_low` (Cards/Lists)
    - Level 2: `surface_container_high` (Hover states)
- **Ambient Shadows:** While the screen relies primarily on color shifts, any floating modals should use a "Zero-Gravity" shadow: `0 20px 25px -5px rgba(0, 0, 0, 0.3)`.
- **Glassmorphism:** The header uses a `white/50` (or `surface/50`) blend with a `backdrop-blur-md` to simulate a frosted glass sheet hovering over the content.

### 5. Components
- **Buttons:**
    - *Primary:* Solid Cyan (#00ced1) with dark text. 
    - *Secondary:* Ghosted with a 1px `primary/50` border or a `slate-800` fill.
    - *Destructive:* Transparent background with red text and 10% red alpha hover.
- **Active Navigation:** Uses a left-aligned 4px "Cyanide" stripe and a 20% alpha primary background.
- **Status Chips:** High-contrast pill shapes. Admin roles use emerald-500/20 backgrounds, while creators use primary/20.
- **Inputs:** Clean, borderless boxes (`bg-slate-800`) that activate with a 2px `primary` ring on focus.

### 6. Do's and Don'ts
**Do:**
- Use heavy font weights (800-900) for page headers to create an editorial impact.
- Use `backdrop-blur` for any element that scrolls over content.
- Rely on high-contrast labels (White on Dark Teal) for readability.

**Don't:**
- Do not use pure black (#000) or pure white (#FFF) for surfaces; always use the teal-shifted slates.
- Do not use rounded corners larger than `1rem` for cards; keep the look professional and "precision-engineered."
- Avoid traditional shadow-based elevation; stick to tonal shifts between `surface` tiers.