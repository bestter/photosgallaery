2024-05-18 - Async Action Status Announcements
Learning: When adding background async actions to buttons (like downloading or liking an image), standard text changes (e.g. replacing an icon with a spinner) are not always announced clearly by screen readers since focus is already on the button and the DOM change is subtle.
Action: Apply `aria-busy={isProcessing}` to interactive controls that trigger asynchronous tasks without navigating away. This informs screen readers that the element is currently busy processing the request.

2024-05-18 - Loading State Announcers for Large Media
Learning: When dynamically loading high-resolution images within modals or overlays, users dependent on screen readers (and sighted users on slow connections) need explicit feedback that the media is fetching to avoid perceiving a broken state.
Action: Add a dedicated loading container with `role="status"` and visually hidden text (`<span className="sr-only">Loading...</span>`) behind the image, hiding it once the image's `onLoad` event fires.
