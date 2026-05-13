import { defineStore } from 'pinia'

import { fetchArticle, fetchArticles } from '@/services/apiClient'
import type { Article, ArticleListParams } from '@/types/article'

export const useArticleStore = defineStore('articleStore', {
  state: () => ({
    articles: [] as Article[],
    cursor: null as string | null,
    hasMore: false,
    totalCount: 0,
    selectedArticle: null as Article | null,
    isDetailLoading: false,
    detailErrorCode: '',
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
    async loadArticle(id: string): Promise<void> {
      this.isDetailLoading = true
      this.detailErrorCode = ''
      this.errorMessage = ''

      try {
        const cachedArticle = this.articles.find((article) => article.id === id)
        this.selectedArticle = cachedArticle ?? (await fetchArticle(id))
      } catch (error) {
        this.selectedArticle = null
        this.detailErrorCode = error instanceof Error ? error.name : 'API_ERROR'
        this.errorMessage = error instanceof Error ? error.message : 'Unable to load article'
      } finally {
        this.isDetailLoading = false
      }
    },
  },
})
