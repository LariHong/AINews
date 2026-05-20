import { describe, expect, it } from 'vitest'

import { parseAiReportStreamEvent } from '@/services/aiSummaryApi'

describe('aiSummaryApi SSE parser', () => {
  it('parses the versioned MVP report event shape', () => {
    const event = parseAiReportStreamEvent(
      'event: status\r\ndata: {"type":"status","message":"Validating structured report."}\r\n',
    )

    expect(event?.type).toBe('status')
    expect(event?.message).toBe('Validating structured report.')
  })

  it('joins multi-line SSE data payloads', () => {
    const event = parseAiReportStreamEvent(
      'event: error\n' +
        'data: {"type":"error",\n' +
        'data: "code":"AI_RATE_LIMIT_EXCEEDED",\n' +
        'data: "message":"Rate limited."}\n',
    )

    expect(event?.type).toBe('error')
    expect(event?.code).toBe('AI_RATE_LIMIT_EXCEEDED')
  })

  it('ignores chunks without data fields', () => {
    expect(parseAiReportStreamEvent('event: ping\n')).toBeNull()
  })
})
