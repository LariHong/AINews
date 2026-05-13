export interface Article {
  id: string
  title: string
  summary: string | null
  sourceUrl: string
  sourceName: string
  sourceLogoUrl: string | null
  tags: string[]
  publishedAt: string
  hasAiSummary: boolean
  isBookmarked: boolean
  readTimeMinutes: number | null
}

export interface ArticleListParams {
  cursor?: string | null
  limit?: number
  keyword?: string
  tags?: string
  source?: string
  date?: string
}

export interface ArticleListResponse {
  items: Article[]
  pagination: {
    cursor: string | null
    hasMore: boolean
    totalCount: number
  }
}

export interface ApiResponse<T> {
  success: boolean
  data: T
  meta: {
    timestamp: string
    requestId: string
  }
}

export interface ApiErrorResponse {
  success: false
  error: {
    code: string
    message: string
  }
  meta: {
    timestamp: string
    requestId: string
  }
}
