export interface Article {
  id: string
  title: string
  summary: string | null
  content: string | null
  contentText: string | null
  contentStatus: 'full_content_ready' | 'extracting_source' | 'summary_fallback' | 'extraction_failed'
  contentExtractedAt: string | null
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

export interface StatsBreakdownItem {
  name: string
  count: number
}

export interface DashboardStats {
  totalArticles: number
  aiSummarizedCount: number
  tagBreakdown: StatsBreakdownItem[]
  topSources: StatsBreakdownItem[]
  updatedAt: string | null
  syncStatus: {
    isSyncing: boolean
    sourcesSynced: number
    sourceFailures: number
    message: string
  }
}

export type FeedSyncViewState = 'idle' | 'empty_fresh_start' | 'stale_with_data' | 'ready' | 'sync_failed'

export interface FeedCrawlRunResult {
  scope: 'today'
  status: 'completed' | 'already_running' | 'failed'
  sourcesVisited: number
  articlesPersisted: number
  sourceFailures: number
  logs: string[]
  completedAt: string
}

export interface BookmarkResponse {
  articleId: string
  isBookmarked: boolean
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
