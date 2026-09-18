import React, { StrictMode, Suspense } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import './i18n'
import { initializeGoogleAnalytics } from './analytics.js'

initializeGoogleAnalytics(import.meta.env.VITE_GOOGLE_ANALYTICS_ID)

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <Suspense fallback={<div>Loading...</div>}>
      <App />
    </Suspense>
  </StrictMode>,
)
