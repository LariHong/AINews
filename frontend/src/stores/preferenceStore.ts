import { defineStore } from 'pinia'

import { fetchHiddenArticles, restoreHiddenArticle } from '@/services/apiClient'
import type { Article } from '@/types/article'

export const usePreferenceStore = defineStore('preferenceStore', {
  state: () => ({
    hiddenArticles: [] as Article[],
    isLoadingHiddenArticles: false,
    hiddenArticlesErrorMessage: '',
  }),
  actions: {
    async loadHiddenArticles(): Promise<void> {
      this.isLoadingHiddenArticles = true
      this.hiddenArticlesErrorMessage = ''

      try {
        this.hiddenArticles = await fetchHiddenArticles()
      } catch (error) {
        this.hiddenArticlesErrorMessage = error instanceof Error ? error.message : 'Unable to load hidden articles'
      } finally {
        this.isLoadingHiddenArticles = false
      }
    },
    async restoreArticle(articleId: string): Promise<void> {
      const previous = [...this.hiddenArticles]
      this.hiddenArticles = this.hiddenArticles.filter((article) => article.id !== articleId)

      try {
        await restoreHiddenArticle(articleId)
      } catch (error) {
        this.hiddenArticles = previous
        this.hiddenArticlesErrorMessage = error instanceof Error ? error.message : 'Unable to restore article'
      }
    },
  },
})
