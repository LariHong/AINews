import { defineStore } from 'pinia'

import { fetchAiSummary, generateAiSummary } from '@/services/aiSummaryApi'
import { useArticleStore } from '@/stores/articleStore'
import type { AiSummary } from '@/types/aiSummary'

interface SummaryState {
  byArticleId: Record<string, AiSummary>
  loadingByArticleId: Record<string, boolean>
  errorByArticleId: Record<string, { code: string; message: string }>
}

export const useAiSummaryStore = defineStore('aiSummaryStore', {
  state: (): SummaryState => ({
    byArticleId: {},
    loadingByArticleId: {},
    errorByArticleId: {},
  }),
  actions: {
    async loadSummary(articleId: string, force = false): Promise<void> {
      if (!articleId) return
      if (!force && this.byArticleId[articleId]) return

      this.loadingByArticleId[articleId] = true
      delete this.errorByArticleId[articleId]

      try {
        this.byArticleId[articleId] = await fetchAiSummary(articleId)
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Unable to load AI summary'
        const code = error instanceof Error ? error.name : 'API_ERROR'
        this.errorByArticleId[articleId] = { code, message }
      } finally {
        this.loadingByArticleId[articleId] = false
      }
    },
    async generateSummary(articleId: string, force = false): Promise<void> {
      if (!articleId) return

      this.loadingByArticleId[articleId] = true
      delete this.errorByArticleId[articleId]

      try {
        this.byArticleId[articleId] = await generateAiSummary(articleId, force)
        useArticleStore().markAiSummaryAvailable(articleId)
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Unable to generate AI summary'
        const code = error instanceof Error ? error.name : 'API_ERROR'
        this.errorByArticleId[articleId] = { code, message }
      } finally {
        this.loadingByArticleId[articleId] = false
      }
    },
    async refreshSummary(articleId: string): Promise<void> {
      await this.loadSummary(articleId, true)
    },
  },
})
