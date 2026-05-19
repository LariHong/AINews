import { defineStore } from 'pinia'

import { addBookmark, deleteBookmark, fetchArticle, fetchArticles, fetchTodayStats, runTodayFeedCrawl } from '@/services/apiClient'
import type { Article, ArticleListParams, DashboardStats, FeedSyncViewState } from '@/types/article'

export const useArticleStore = defineStore('articleStore', {
  state: () => ({
    articles: [] as Article[],
    cursor: null as string | null,
    hasMore: false,
    totalCount: 0,
    selectedArticle: null as Article | null,
    dashboardStats: null as DashboardStats | null,
    isDetailLoading: false,
    isStatsLoading: false,
    isFeedSyncing: false,
    detailErrorCode: '',
    statsErrorMessage: '',
    feedSyncMessage: '',
    feedSyncErrorMessage: '',
    feedSyncViewState: 'idle' as FeedSyncViewState,
    isLoading: false,
    errorMessage: '',
    filters: {
      keyword: '',
      tags: '',
      source: '',
      date: '',
    },
  }),
  actions: {
    async loadArticles(reset = true): Promise<void> {
      this.isLoading = true
      this.errorMessage = ''

      try {
        const params: ArticleListParams = {
          limit: 20,
          cursor: reset ? null : this.cursor,
          keyword: this.filters.keyword || undefined,
          tags: this.filters.tags || undefined,
          source: this.filters.source || undefined,
          date: this.filters.date || undefined,
        }
        const result = await fetchArticles(params)

        this.articles = reset ? result.items : [...this.articles, ...result.items]
        this.cursor = result.pagination.cursor
        this.hasMore = result.pagination.hasMore
        this.totalCount = result.pagination.totalCount
        this.updateFeedSyncViewState()
      } catch (error) {
        this.errorMessage = error instanceof Error ? error.message : 'Unable to load articles'
      } finally {
        this.isLoading = false
      }
    },
    async applyFilters(): Promise<void> {
      this.cursor = null
      await this.loadArticles(true)
    },
    async syncTodayFeed(): Promise<void> {
      this.isFeedSyncing = true
      this.feedSyncErrorMessage = ''
      this.feedSyncMessage = this.articles.length === 0
        ? "Fetching today's AI news..."
        : "Updating today's feed in the background..."
      this.feedSyncViewState = this.articles.length === 0 ? 'empty_fresh_start' : 'stale_with_data'

      try {
        const result = await runTodayFeedCrawl()
        this.feedSyncMessage = result.sourceFailures > 0
          ? `${result.sourcesVisited - result.sourceFailures} sources synced; ${result.sourceFailures} source failed`
          : `${result.sourcesVisited} sources synced`
        await this.loadArticles(true)
        await this.loadTodayStats()
      } catch (error) {
        this.feedSyncErrorMessage = error instanceof Error ? error.message : 'Unable to sync feed'
        this.feedSyncViewState = 'sync_failed'
      } finally {
        this.isFeedSyncing = false
        this.updateFeedSyncViewState()
      }
    },
    async ensureTodayFeed(): Promise<void> {
      await this.loadArticles(true)

      if (this.articles.length === 0 && !this.errorMessage) {
        await this.syncTodayFeed()
      } else {
        await this.loadTodayStats()
      }
    },
    async loadTodayStats(): Promise<void> {
      this.isStatsLoading = true
      this.statsErrorMessage = ''

      try {
        this.dashboardStats = await fetchTodayStats()
      } catch (error) {
        this.dashboardStats = null
        this.statsErrorMessage = error instanceof Error ? error.message : 'Unable to load dashboard stats'
      } finally {
        this.isStatsLoading = false
      }
    },
    async loadArticle(id: string): Promise<void> {
      this.isDetailLoading = true
      this.detailErrorCode = ''
      this.errorMessage = ''

      try {
        const cachedArticle = this.articles.find((article) => article.id === id)
        if (cachedArticle) this.selectedArticle = cachedArticle
        this.selectedArticle = await fetchArticle(id)
      } catch (error) {
        this.selectedArticle = null
        this.detailErrorCode = error instanceof Error ? error.name : 'API_ERROR'
        this.errorMessage = error instanceof Error ? error.message : 'Unable to load article'
      } finally {
        this.isDetailLoading = false
      }
    },
    async setBookmark(article: Article, isBookmarked: boolean): Promise<void> {
      const previous = article.isBookmarked
      this.patchBookmarkState(article.id, isBookmarked)

      try {
        if (isBookmarked) {
          await addBookmark(article.id)
        } else {
          await deleteBookmark(article.id)
        }
      } catch (error) {
        this.patchBookmarkState(article.id, previous)
        this.errorMessage = error instanceof Error ? error.message : 'Unable to update bookmark'
      }
    },
    patchBookmarkState(articleId: string, isBookmarked: boolean): void {
      this.articles = this.articles.map((item) =>
        item.id === articleId ? { ...item, isBookmarked } : item)

      if (this.selectedArticle?.id === articleId) {
        this.selectedArticle = { ...this.selectedArticle, isBookmarked }
      }
    },
    updateFeedSyncViewState(): void {
      if (this.isFeedSyncing) {
        this.feedSyncViewState = this.articles.length === 0 ? 'empty_fresh_start' : 'stale_with_data'
        return
      }

      if (this.feedSyncErrorMessage) {
        this.feedSyncViewState = 'sync_failed'
        return
      }

      this.feedSyncViewState = this.articles.length > 0 ? 'ready' : 'idle'
    },
  },
})
