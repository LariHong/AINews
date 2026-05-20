import type { AiReport, AiReportStreamEvent, AiSummary } from '@/types/aiSummary'
import type { ApiErrorResponse, ApiResponse } from '@/types/article'
import { localUserHeaders } from '@/services/apiClient'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api/v1'

export async function fetchAiSummary(articleId: string): Promise<AiSummary> {
  const response = await fetch(`${API_BASE_URL}/articles/${encodeURIComponent(articleId)}/ai-summary`)

  if (!response.ok) {
    throw await createApiError(response, `AI summary API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<AiSummary>
  return payload.data
}

export async function generateAiSummary(articleId: string, force = false): Promise<AiSummary> {
  const query = force ? '?force=true' : ''
  const response = await fetch(`${API_BASE_URL}/articles/${encodeURIComponent(articleId)}/ai-summary${query}`, {
    method: 'POST',
  })

  if (!response.ok) {
    throw await createApiError(response, `AI summary generation failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<AiSummary>
  return payload.data
}

export async function fetchAiReport(articleId: string): Promise<AiReport> {
  const response = await fetch(`${API_BASE_URL}/articles/${encodeURIComponent(articleId)}/ai-report`)

  if (!response.ok) {
    throw await createApiError(response, `AI report API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<AiReport>
  return payload.data
}

export async function generateAiReport(
  articleId: string,
  onEvent: (event: AiReportStreamEvent) => void,
  force = false,
): Promise<void> {
  const query = force ? '?force=true' : ''
  const response = await fetch(`${API_BASE_URL}/articles/${encodeURIComponent(articleId)}/ai-summary/generate${query}`, {
    method: 'POST',
    headers: {
      Accept: 'text/event-stream',
      ...localUserHeaders(),
    },
  })

  if (!response.ok || !response.body) {
    throw await createApiError(response, `AI report generation failed with ${response.status}`)
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  while (true) {
    const { value, done } = await reader.read()
    if (done) break

    buffer += decoder.decode(value, { stream: true })
    const chunks = splitSseChunks(buffer)
    buffer = chunks.pop() ?? ''

    for (const chunk of chunks) {
      const event = parseAiReportStreamEvent(chunk)
      if (!event) continue

      onEvent(event)

      if (event.type === 'error') {
        const error = new Error(event.message || 'AI report generation failed')
        error.name = event.code || 'AI_REPORT_GENERATION_FAILED'
        throw error
      }
    }
  }
}

export function parseAiReportStreamEvent(chunk: string): AiReportStreamEvent | null {
  const data = chunk
    .replace(/\r\n/g, '\n')
    .split('\n')
    .filter((line) => line.startsWith('data:'))
    .map((line) => line.slice(5).trimStart())
    .join('\n')

  if (!data) return null

  return JSON.parse(data) as AiReportStreamEvent
}

function splitSseChunks(buffer: string): string[] {
  return buffer.replace(/\r\n/g, '\n').split('\n\n')
}

async function createApiError(response: Response, fallbackMessage: string): Promise<Error> {
  try {
    const payload = (await response.json()) as ApiErrorResponse
    const error = new Error(payload.error?.message || fallbackMessage)
    error.name = payload.error?.code || 'API_ERROR'
    return error
  } catch {
    return new Error(fallbackMessage)
  }
}
