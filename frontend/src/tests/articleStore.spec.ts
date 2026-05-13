import { describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'

import { useArticleStore } from '@/stores/articleStore'

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
})
