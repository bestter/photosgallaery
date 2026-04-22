---
name: Cyanide Glass
colors:
  surface: '#0e1515'
  surface-dim: '#0e1515'
  surface-bright: '#333b3a'
  surface-container-lowest: '#090f0f'
  surface-container-low: '#161d1d'
  surface-container: '#1a2121'
  surface-container-high: '#242b2b'
  surface-container-highest: '#2f3636'
  on-surface: '#dde4e3'
  on-surface-variant: '#bac9c9'
  inverse-surface: '#dde4e3'
  inverse-on-surface: '#2b3232'
  outline: '#859493'
  outline-variant: '#3b4949'
  surface-tint: '#2ddbde'
  primary: '#47eaed'
  on-primary: '#003738'
  primary-container: '#00ced1'
  on-primary-container: '#005354'
  inverse-primary: '#00696b'
  secondary: '#b9c8de'
  on-secondary: '#233143'
  secondary-container: '#39485a'
  on-secondary-container: '#a7b6cc'
  tertiary: '#60eeb1'
  on-tertiary: '#003824'
  tertiary-container: '#3dd197'
  on-tertiary-container: '#005539'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#5af8fb'
  primary-fixed-dim: '#2ddbde'
  on-primary-fixed: '#002020'
  on-primary-fixed-variant: '#004f51'
  secondary-fixed: '#d4e4fa'
  secondary-fixed-dim: '#b9c8de'
  on-secondary-fixed: '#0d1c2d'
  on-secondary-fixed-variant: '#39485a'
  tertiary-fixed: '#6ffbbe'
  tertiary-fixed-dim: '#4edea3'
  on-tertiary-fixed: '#002113'
  on-tertiary-fixed-variant: '#005236'
  background: '#0e1515'
  on-background: '#dde4e3'
  surface-variant: '#2f3636'
typography:
  display-xl:
    fontFamily: Inter
    fontSize: 1.25rem
    fontWeight: '800'
    lineHeight: 1.75rem
    letterSpacing: -0.05em
  brand-logo:
    fontFamily: Inter
    fontSize: 0.75rem
    fontWeight: '900'
    lineHeight: 1rem
    letterSpacing: 0.15em
  table-header:
    fontFamily: Inter
    fontSize: 0.75rem
    fontWeight: '600'
    lineHeight: 1rem
    letterSpacing: 0.05em
  body-sm:
    fontFamily: Inter
    fontSize: 0.875rem
    fontWeight: '400'
    lineHeight: 1.25rem
  caption-bold:
    fontFamily: Inter
    fontSize: 10px
    fontWeight: '700'
    lineHeight: 1rem
    letterSpacing: 0.1em
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  container-margin: 2rem
  gutter: 1.5rem
  row-padding: 1rem 1.5rem
  nav-width: 16rem
  header-height: 4rem
---

## Brand & Style
The brand identity is a fusion of **Cyberpunk Minimalism** and **Glassmorphism**. It evokes a high-stakes, technical atmosphere suitable for administrative control systems and network moderation. The aesthetic is "Electronic Noir"—dark, moody, and sharp, utilizing high-contrast accents against deep slate and teal foundations. 

Visual signals should communicate precision, authority, and real-time activity. The style employs translucent layers, subtle neon glows (cyanide), and crisp typography to create a sophisticated, data-driven environment that feels both futuristic and functionally rigorous.

## Colors
The palette is dominated by "Deep Ocean" neutrals and "Electric Cyan" accents. 

- **Primary (#00ced1):** Used for critical actions, active states, and high-priority indicators. It should often be accompanied by a soft outer glow or high-saturation text shadows.
- **Surface Strategy:** Layers are built using varying depths of teal-tinted blacks and slate. The sidebar uses a true dark slate-950 (#020617) to provide a structural anchor, while main content areas use a slightly lighter teal-black (#0f2323).
- **Semantic Accents:** Tertiary green is used for production-related indicators, while a bright vibrant red is reserved strictly for destructive "Reject" or "Error" states.

## Typography
The system relies exclusively on **Inter** to maintain a systematic, utilitarian feel. 

Hierarchy is established through extreme variations in weight (from 400 to 900) and letter spacing rather than large scale changes. Brand elements and technical labels utilize heavy tracking (letter-spacing) and uppercase transformations to mimic terminal displays. Headlines should feel compressed and "heavy" (extra-bold/black), while body text remains clean and legible with standard spacing.

## Layout & Spacing
The layout uses a **Fixed Sidebar / Fluid Content** model. 

- **Sidebar:** A narrow 64 unit (16rem) column that remains locked to the left.
- **Navigation:** Vertical rhythm is tight, using 4px (1 unit) increments. 
- **Main Canvas:** Content is centered within a max-width container (7xl) with generous outer padding (32px) to prevent cognitive overload.
- **Data Grids:** Tables use comfortable horizontal padding (px-6) but tight vertical padding (py-4) to maximize information density.

## Elevation & Depth
Depth is achieved through **Luminescence** and **Translucency** rather than traditional physical shadows.

- **The Glass Effect:** The Top Navigation bar uses a backdrop blur (blur-md) and 50% opacity slate background to suggest a semi-transparent layer floating above the content.
- **Outer Glows:** Primary buttons and active indicators use colored drop shadows (e.g., `rgba(0, 206, 209, 0.3)`) to create a "neon" effect that makes them appear to emit light.
- **Border-based Tiers:** Containers are defined by 1px borders with low-opacity cyan tints (`cyan-400/10`). This creates a "wireframe" aesthetic that feels structural without being heavy.
- **Shadows:** Only high-level containers (like the main data table) use a `shadow-2xl` with high opacity black to separate them from the base background.

## Shapes
The shape language is **Technical and Precise**. 

A base roundedness of 4px (0.25rem) is used for most UI elements like buttons, input fields, and small cards, giving them a slightly softened but still industrial feel. User avatars are the primary exception, utilizing full circles (pill-shaped) to provide a organic contrast to the rigid grid. Table containers and large modules use an 8px (0.5rem) radius for structural stability.

## Components
- **Primary Buttons:** High-contrast background (#00ced1) with dark text. Must feature a subtle cyan glow. Transitions should include a slight brightness increase on hover.
- **Ghost/Secondary Buttons:** Border-only or transparent background with muted text. Use a background tint (e.g., `primary/10`) on hover.
- **Navigation Links:** Active state is marked by a 4px left-border and a semi-transparent background fill of the primary color. Non-active states should be muted slate.
- **Data Tables:** Use `divide-y` with low-opacity borders. Header rows should have a slightly darker, distinct background. Row hover states must be reactive, slightly increasing brightness or adding a subtle background tint.
- **Badges/Chips:** Small, uppercase text with a circular color dot. The dot color should correspond to the category (Primary for Creative, Tertiary for Technical).
- **Search Inputs:** Borderless or subtle background-filled rectangles. The focus state should involve a 2px primary color ring.