import type {
  ApiErrorResponse,
  ApiResponse,
  Article,
  ArticleListParams,
  ArticleListResponse,
  BookmarkResponse,
  DashboardStats,
  FeedCrawlRunResult,
  HiddenArticleResponse,
} from '@/types/article'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api/v1'
const LOCAL_USER_STORAGE_KEY = 'ai-daily-local-user'

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
  const response = await fetch(`${API_BASE_URL}/articles${buildQuery(params)}`, {
    headers: localUserHeaders(),
  })

  if (!response.ok) {
    throw await createApiError(response, `Article API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<ArticleListResponse>
  return payload.data
}

export async function fetchArticle(id: string): Promise<Article> {
  const response = await fetch(`${API_BASE_URL}/articles/${encodeURIComponent(id)}`, {
    headers: localUserHeaders(),
  })

  if (!response.ok) {
    throw await createApiError(response, `Article detail API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<Article>
  return payload.data
}

export async function fetchTodayStats(): Promise<DashboardStats> {
  const response = await fetch(`${API_BASE_URL}/stats/today`)

  if (!response.ok) {
    throw await createApiError(response, `Stats API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<DashboardStats>
  return payload.data
}

export async function runTodayFeedCrawl(): Promise<FeedCrawlRunResult> {
  const response = await fetch(`${API_BASE_URL}/feed-crawl/run?scope=today`, {
    method: 'POST',
  })

  if (!response.ok) {
    throw await createApiError(response, `Feed sync API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<FeedCrawlRunResult>
  return payload.data
}

export async function fetchBookmarks(): Promise<Article[]> {
  const response = await fetch(`${API_BASE_URL}/bookmarks`, {
    headers: localUserHeaders(),
  })

  if (!response.ok) {
    throw await createApiError(response, `Bookmark API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<Article[]>
  return payload.data
}

export async function addBookmark(articleId: string): Promise<BookmarkResponse> {
  const response = await fetch(`${API_BASE_URL}/articles/${encodeURIComponent(articleId)}/bookmark`, {
    method: 'POST',
    headers: localUserHeaders(),
  })

  if (!response.ok) {
    throw await createApiError(response, `Bookmark API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<BookmarkResponse>
  return payload.data
}

export async function deleteBookmark(articleId: string): Promise<BookmarkResponse> {
  const response = await fetch(`${API_BASE_URL}/articles/${encodeURIComponent(articleId)}/bookmark`, {
    method: 'DELETE',
    headers: localUserHeaders(),
  })

  if (!response.ok) {
    throw await createApiError(response, `Bookmark API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<BookmarkResponse>
  return payload.data
}

export async function fetchHiddenArticles(): Promise<Article[]> {
  const response = await fetch(`${API_BASE_URL}/user-preferences/hidden-articles`, {
    headers: localUserHeaders(),
  })

  if (!response.ok) {
    throw await createApiError(response, `Hidden article API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<Article[]>
  return payload.data
}

export async function hideArticle(articleId: string, reason = 'not_interested'): Promise<HiddenArticleResponse> {
  const response = await fetch(`${API_BASE_URL}/user-preferences/hidden-articles/${encodeURIComponent(articleId)}`, {
    method: 'POST',
    headers: {
      ...localUserHeaders(),
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ reason }),
  })

  if (!response.ok) {
    throw await createApiError(response, `Hidden article API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<HiddenArticleResponse>
  return payload.data
}

export async function restoreHiddenArticle(articleId: string): Promise<HiddenArticleResponse> {
  const response = await fetch(`${API_BASE_URL}/user-preferences/hidden-articles/${encodeURIComponent(articleId)}`, {
    method: 'DELETE',
    headers: localUserHeaders(),
  })

  if (!response.ok) {
    throw await createApiError(response, `Hidden article API failed with ${response.status}`)
  }

  const payload = (await response.json()) as ApiResponse<HiddenArticleResponse>
  return payload.data
}

function localUserHeaders(): HeadersInit {
  return {
    'X-AI-Daily-Local-User': getLocalUserId(),
  }
}

function getLocalUserId(): string {
  if (typeof localStorage === 'undefined') return 'local_test_user'

  const existing = localStorage.getItem(LOCAL_USER_STORAGE_KEY)
  if (existing) return existing

  const generated = `local_${crypto.randomUUID()}`
  localStorage.setItem(LOCAL_USER_STORAGE_KEY, generated)
  return generated
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
