import { describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

import { useAiSummaryStore } from '@/stores/aiSummaryStore'
import { useArticleStore } from '@/stores/articleStore'
import { fetchTodayStats } from '@/services/apiClient'

vi.mock('@/services/apiClient', () => ({
  fetchArticles: vi.fn(async () => ({
    items: [
      {
        id: 'art_01',
        title: 'AI release',
        summary: 'A short summary',
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
}))

vi.mock('@/services/aiSummaryApi', () => ({
  fetchAiSummary: vi.fn(async () => ({
    articleId: 'art_01',
    highlights: ['A useful highlight'],
    impactScope: 'Developer tools',
    controversy: 'Permissions need care',
    editorView: 'Worth tracking',
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

  it('loads an AI summary preview into state', async () => {
    setActivePinia(createPinia())
    const store = useAiSummaryStore()

    await store.loadSummary('art_01')

    expect(store.byArticleId.art_01.highlights).toContain('A useful highlight')
    expect(store.errorByArticleId.art_01).toBeUndefined()
  })
})
