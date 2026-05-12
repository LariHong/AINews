import type { ApiResponse, ArticleListParams, ArticleListResponse } from '@/types/article'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api/v1'

function buildQuery(params: ArticleListParams): string {
  const query = new URLSearchParams()

  if (params.cursor) query.set('cursor', params.cursor)
  if (params.limit) query.set('limit', String(params.limit))
  if (params.keyword) query.set('keyword', params.keyword)
  if (params.tags) query.set('tags', params.tags)
  if (params.source) query.set('source', params.source)
  if (params.date) query.set('date', params.date)

  const value = query.toString()
  return value ? `?${value}` : ''
}

export async function fetchArticles(params: ArticleListParams): Promise<ArticleListResponse> {
  const response = await fetch(`${API_BASE_URL}/articles${buildQuery(params)}`)

  if (!response.ok) {
    throw new Error(`Article API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<ArticleListResponse>
  return payload.data
}
