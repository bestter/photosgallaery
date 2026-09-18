import { afterEach, describe, expect, it } from 'vitest'
import { initializeGoogleAnalytics } from './analytics.js'

const GOOGLE_TAG_MANAGER_SCRIPT_SELECTOR = 'script[src^="https://www.googletagmanager.com/gtag/js"]'

afterEach(() => {
  document.querySelectorAll(GOOGLE_TAG_MANAGER_SCRIPT_SELECTOR).forEach((script) => script.remove())
  delete window.dataLayer
  delete window.gtag
})

describe('initializeGoogleAnalytics', () => {
  it('loads Google Tag Manager and queues the initial events', () => {
    initializeGoogleAnalytics('G-TEST ID')

    const script = document.querySelector(GOOGLE_TAG_MANAGER_SCRIPT_SELECTOR)
    expect(script).not.toBeNull()
    expect(script.async).toBe(true)
    expect(script.src).toBe('https://www.googletagmanager.com/gtag/js?id=G-TEST%20ID')
    expect(window.dataLayer).toHaveLength(2)
    expect(window.dataLayer[0][0]).toBe('js')
    expect(window.dataLayer[0][1]).toBeInstanceOf(Date)
    expect(window.dataLayer[1]).toEqual(['config', 'G-TEST ID'])
  })

  it.each([undefined, '', '   '])('does nothing when the measurement ID is missing', (measurementId) => {
    initializeGoogleAnalytics(measurementId)

    expect(document.querySelector(GOOGLE_TAG_MANAGER_SCRIPT_SELECTOR)).toBeNull()
    expect(window.dataLayer).toBeUndefined()
    expect(window.gtag).toBeUndefined()
  })
})
