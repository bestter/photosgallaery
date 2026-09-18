const GOOGLE_TAG_MANAGER_ORIGIN = 'https://www.googletagmanager.com'

export function initializeGoogleAnalytics(measurementId) {
  const normalizedMeasurementId = measurementId?.trim()

  if (!normalizedMeasurementId) {
    return
  }

  window.dataLayer = window.dataLayer || []
  window.gtag = (...args) => window.dataLayer.push(args)
  window.gtag('js', new Date())
  window.gtag('config', normalizedMeasurementId)

  const script = document.createElement('script')
  script.async = true
  script.src = `${GOOGLE_TAG_MANAGER_ORIGIN}/gtag/js?id=${encodeURIComponent(normalizedMeasurementId)}`
  document.head.appendChild(script)
}
