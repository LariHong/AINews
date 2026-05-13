import type { AiSummary } from '@/types/aiSummary'
import type { ApiErrorResponse, ApiResponse } from '@/types/article'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api/v1'

export async function fetchAiSummary(articleId: string): Promise<AiSummary> {
  const response = await fetch(`${API_BASE_URL}/articles/${encodeURIComponent(articleId)}/ai-summary`)

  if (!response.ok) {
    throw await createApiError(response, `AI summary API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<AiSummary>
  return payload.data
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
