import { describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

import { useAiSummaryStore } from '@/stores/aiSummaryStore'
import { useArticleStore } from '@/stores/articleStore'
import { useBookmarkStore } from '@/stores/bookmarkStore'
import { useThemeStore } from '@/stores/themeStore'
import { addBookmark, deleteBookmark, fetchArticles, fetchTodayStats, runTodayFeedCrawl } from '@/services/apiClient'

vi.mock('@/services/apiClient', () => ({
  fetchArticles: vi.fn(async () => ({
    items: [
      {
        id: 'art_01',
        title: 'AI release',
        summary: 'A short summary',
        content: 'A short summary',
        contentText: 'A short summary',
        contentStatus: 'summary_fallback',
        contentExtractedAt: null,
        sourceUrl: 'https://example.com',
        sourceName: 'Example',
        sourceLogoUrl: null,
        tags: ['model'],
        publishedAt: '2026-05-12T06:00:00Z',
        hasAiSummary: true,
        isBookmarked: false,
        readTimeMinutes: 5,
      },
    ],
    pagination: {
      cursor: null,
      hasMore: false,
      totalCount: 1,
    },
  })),
  fetchArticle: vi.fn(async () => ({
    id: 'art_01',
    title: 'AI release',
    summary: 'A short summary',
    content: 'A short summary',
    contentText: 'A short summary',
    contentStatus: 'summary_fallback',
    contentExtractedAt: null,
    sourceUrl: 'https://example.com',
    sourceName: 'Example',
    sourceLogoUrl: null,
    tags: ['model'],
    publishedAt: '2026-05-12T06:00:00Z',
    hasAiSummary: true,
    isBookmarked: false,
    readTimeMinutes: 5,
  })),
  fetchTodayStats: vi.fn(async () => ({
    totalArticles: 2,
    aiSummarizedCount: 1,
    tagBreakdown: [{ name: 'model', count: 2 }],
    topSources: [{ name: 'Example', count: 2 }],
    updatedAt: '2026-05-13T06:00:00Z',
    syncStatus: {
      isSyncing: false,
      sourcesSynced: 1,
      sourceFailures: 0,
      message: '1 sources synced',
    },
  })),
  runTodayFeedCrawl: vi.fn(async () => ({
    scope: 'today',
    status: 'completed',
    sourcesVisited: 1,
    articlesPersisted: 1,
    sourceFailures: 0,
    logs: ['Crawled Example: 1 RSS items read.'],
    completedAt: '2026-05-13T06:01:00Z',
  })),
  fetchBookmarks: vi.fn(async () => [
    {
      id: 'art_saved',
      title: 'Saved AI release',
      summary: 'A saved summary',
      content: 'A saved summary',
      contentText: 'A saved summary',
      contentStatus: 'summary_fallback',
      contentExtractedAt: null,
      sourceUrl: 'https://example.com/saved',
      sourceName: 'Example',
      sourceLogoUrl: null,
      tags: ['model'],
      publishedAt: '2026-05-12T06:00:00Z',
      hasAiSummary: true,
      isBookmarked: true,
      readTimeMinutes: 5,
    },
  ]),
  addBookmark: vi.fn(async (articleId: string) => ({
    articleId,
    isBookmarked: true,
  })),
  deleteBookmark: vi.fn(async (articleId: string) => ({
    articleId,
    isBookmarked: false,
  })),
}))

vi.mock('@/services/aiSummaryApi', () => ({
  fetchAiSummary: vi.fn(async () => ({
    articleId: 'art_01',
    highlights: ['A useful highlight'],
    impactScope: 'Developer tools',
    controversy: 'Permissions need care',
    editorView: 'Worth tracking',
    provider: 'seed',
    promptVersion: 'quick-summary-seed-v1',
    generatedAt: '2026-05-12T08:15:00Z',
  })),
  generateAiSummary: vi.fn(async () => ({
    articleId: 'art_02',
    highlights: ['Generated highlight'],
    impactScope: 'Research teams',
    controversy: 'Summary fallback needs care',
    editorView: 'Generated from summary/source metadata',
    provider: 'stub',
    promptVersion: 'quick-summary-v1',
    generatedAt: '2026-05-12T08:15:00Z',
  })),
}))

describe('articleStore', () => {
  it('loads articles into state', async () => {
    setActivePinia(createPinia())
    const store = useArticleStore()

    await store.loadArticles(true)

    expect(store.articles).toHaveLength(1)
    expect(store.totalCount).toBe(1)
    expect(store.errorMessage).toBe('')
  })

  it('loads a selected article by id', async () => {
    setActivePinia(createPinia())
    const store = useArticleStore()

    await store.loadArticle('art_01')

    expect(store.selectedArticle?.id).toBe('art_01')
    expect(store.detailErrorCode).toBe('')
  })

  it('loads dashboard stats into independent state', async () => {
    setActivePinia(createPinia())
    const store = useArticleStore()

    await store.loadTodayStats()

    expect(store.dashboardStats?.totalArticles).toBe(2)
    expect(store.dashboardStats?.tagBreakdown[0].name).toBe('model')
    expect(store.statsErrorMessage).toBe('')
  })

  it('keeps article state usable when stats fail', async () => {
    vi.mocked(fetchTodayStats).mockRejectedValueOnce(new Error('Stats down'))
    setActivePinia(createPinia())
    const store = useArticleStore()

    await store.loadArticles(true)
    await store.loadTodayStats()

    expect(store.articles).toHaveLength(1)
    expect(store.dashboardStats).toBeNull()
    expect(store.statsErrorMessage).toBe('Stats down')
  })

  it('runs explicit feed sync only after a cold empty article list', async () => {
    vi.mocked(fetchArticles)
      .mockResolvedValueOnce({
        items: [],
        pagination: {
          cursor: null,
          hasMore: false,
          totalCount: 0,
        },
      })
      .mockResolvedValueOnce({
        items: [
          {
            id: 'art_synced',
            title: 'Synced AI story',
            summary: 'Fetched from RSS',
            content: null,
            contentText: null,
            contentStatus: 'summary_fallback',
            contentExtractedAt: null,
            sourceUrl: 'https://example.com/rss',
            sourceName: 'Example',
            sourceLogoUrl: null,
            tags: ['rss'],
            publishedAt: '2026-05-13T06:00:00Z',
            hasAiSummary: false,
            isBookmarked: false,
            readTimeMinutes: null,
          },
        ],
        pagination: {
          cursor: null,
          hasMore: false,
          totalCount: 1,
        },
      })
    setActivePinia(createPinia())
    const store = useArticleStore()

    await store.ensureTodayFeed()

    expect(runTodayFeedCrawl).toHaveBeenCalledTimes(1)
    expect(store.articles).toHaveLength(1)
    expect(store.feedSyncViewState).toBe('ready')
  })

  it('loads an AI summary preview into state', async () => {
    setActivePinia(createPinia())
    const store = useAiSummaryStore()

    await store.loadSummary('art_01')

    expect(store.byArticleId.art_01.highlights).toContain('A useful highlight')
    expect(store.byArticleId.art_01.provider).toBe('seed')
    expect(store.errorByArticleId.art_01).toBeUndefined()
  })

  it('generates a missing AI summary into state', async () => {
    setActivePinia(createPinia())
    const store = useAiSummaryStore()

    await store.generateSummary('art_02')

    expect(store.byArticleId.art_02.highlights).toContain('Generated highlight')
    expect(store.byArticleId.art_02.promptVersion).toBe('quick-summary-v1')
    expect(store.errorByArticleId.art_02).toBeUndefined()
  })

  it('updates bookmark state optimistically', async () => {
    setActivePinia(createPinia())
    const store = useArticleStore()

    await store.loadArticles(true)
    await store.setBookmark(store.articles[0], true)

    expect(addBookmark).toHaveBeenCalledWith('art_01')
    expect(store.articles[0].isBookmarked).toBe(true)

    await store.setBookmark(store.articles[0], false)

    expect(deleteBookmark).toHaveBeenCalledWith('art_01')
    expect(store.articles[0].isBookmarked).toBe(false)
  })

  it('loads bookmark list into state', async () => {
    setActivePinia(createPinia())
    const store = useBookmarkStore()

    await store.loadBookmarks()

    expect(store.articles).toHaveLength(1)
    expect(store.articles[0].isBookmarked).toBe(true)
  })

  it('persists theme preference in the theme store', () => {
    setActivePinia(createPinia())
    const store = useThemeStore()

    store.setPreference('light')

    expect(store.preference).toBe('light')
    expect(store.resolvedTheme).toBe('light')
  })
})
