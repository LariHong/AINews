import { defineStore } from 'pinia'

import {
  addBookmark,
  deleteBookmark,
  fetchBookmarks,
} from '@/services/apiClient'
import type { Article } from '@/types/article'

export const useBookmarkStore = defineStore('bookmarkStore', {
  state: () => ({
    articles: [] as Article[],
    isLoading: false,
    errorMessage: '',
  }),
  actions: {
    async loadBookmarks(): Promise<void> {
      this.isLoading = true
      this.errorMessage = ''

      try {
        this.articles = await fetchBookmarks()
      } catch (error) {
        this.errorMessage = error instanceof Error ? error.message : 'Unable to load bookmarks'
      } finally {
        this.isLoading = false
      }
    },
    async save(article: Article): Promise<void> {
      this.errorMessage = ''
      await addBookmark(article.id)
      this.upsert({ ...article, isBookmarked: true })
    },
    async remove(articleId: string): Promise<void> {
      this.errorMessage = ''
      await deleteBookmark(articleId)
      this.articles = this.articles.filter((article) => article.id !== articleId)
    },
    upsert(article: Article): void {
      const index = this.articles.findIndex((item) => item.id === article.id)
      if (index >= 0) {
        this.articles[index] = article
      } else {
        this.articles = [article, ...this.articles]
      }
    },
  },
})
