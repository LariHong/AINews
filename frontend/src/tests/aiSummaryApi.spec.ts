import { beforeEach, describe, expect, it, vi } from 'vitest'

import { fetchAiSummary, generateAiSummary, parseAiReportStreamEvent } from '@/services/aiSummaryApi'

const fetchMock = vi.fn()

vi.stubGlobal('fetch', fetchMock)

describe('aiSummaryApi SSE parser', () => {
  beforeEach(() => {
    fetchMock.mockReset()
  })

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

  it('loads an AI summary payload', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({
      success: true,
      data: {
        articleId: 'art_01',
        highlights: ['A useful highlight'],
        impactScope: 'Developer tools',
        controversy: 'Permissions need care',
        editorView: 'Worth tracking',
        provider: 'seed',
        promptVersion: 'quick-summary-seed-v1',
        generatedAt: '2026-05-12T08:15:00Z',
      },
      meta: {
        timestamp: '2026-05-12T08:15:00Z',
        requestId: 'req_01',
      },
    }))

    const summary = await fetchAiSummary('art_01')

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/articles/art_01/ai-summary')
    expect(summary.provider).toBe('seed')
  })

  it('generates an AI summary with force refresh', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({
      success: true,
      data: {
        articleId: 'art_01',
        highlights: ['A refreshed highlight'],
        impactScope: 'Developer tools',
        controversy: 'Permissions need care',
        editorView: 'Worth refreshing',
        provider: 'stub',
        promptVersion: 'quick-summary-v1',
        generatedAt: '2026-05-12T08:20:00Z',
      },
      meta: {
        timestamp: '2026-05-12T08:20:00Z',
        requestId: 'req_02',
      },
    }))

    const summary = await generateAiSummary('art_01', true)

    expect(fetchMock).toHaveBeenCalledWith('/api/v1/articles/art_01/ai-summary?force=true', {
      method: 'POST',
    })
    expect(summary.provider).toBe('stub')
  })

  it('maps API errors onto Error name and message', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({
      success: false,
      error: {
        code: 'AI_PROVIDER_RATE_LIMITED',
        message: 'AI summary provider rate limit or quota was reached.',
      },
      meta: {
        timestamp: '2026-05-12T08:20:00Z',
        requestId: 'req_03',
      },
    }, 502))

    await expect(generateAiSummary('art_01', true)).rejects.toMatchObject({
      name: 'AI_PROVIDER_RATE_LIMITED',
      message: 'AI summary provider rate limit or quota was reached.',
    })
  })
})

function jsonResponse(payload: unknown, status = 200): Response {
  return new Response(JSON.stringify(payload), {
    status,
    headers: {
      'Content-Type': 'application/json',
    },
  })
}
