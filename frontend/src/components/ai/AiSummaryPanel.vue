<script setup lang="ts">
import { computed, watch } from 'vue'

import { useAiSummaryStore } from '@/stores/aiSummaryStore'
import type { Article } from '@/types/article'

const props = defineProps<{
  article: Article | null
  compact?: boolean
}>()

const summaryStore = useAiSummaryStore()

const articleId = computed(() => props.article?.id ?? '')
const summary = computed(() => (articleId.value ? summaryStore.byArticleId[articleId.value] : null))
const error = computed(() => (articleId.value ? summaryStore.errorByArticleId[articleId.value] : null))
const isLoading = computed(() => (articleId.value ? summaryStore.loadingByArticleId[articleId.value] === true : false))
const isEmpty = computed(() => error.value?.code === 'AI_SUMMARY_NOT_FOUND')

function generateSummary() {
  if (articleId.value) {
    void summaryStore.generateSummary(articleId.value)
  }
}

watch(
  articleId,
  (id) => {
    if (id && props.article?.hasAiSummary) {
      void summaryStore.loadSummary(id)
    }
  },
  { immediate: true },
)
</script>

<template>
  <div class="ai-summary-panel" :class="{ 'ai-summary-panel--compact': compact }">
    <div v-if="!article" class="summary-state">Select an article to preview its AI brief.</div>
    <div v-else-if="isLoading" class="summary-state">Loading AI brief...</div>
    <div v-else-if="summary" class="summary-content">
      <div class="summary-title">{{ article.title }}</div>

      <div class="summary-block">
        <div class="summary-label">Highlights</div>
        <ul class="summary-list">
          <li v-for="highlight in summary.highlights" :key="highlight">{{ highlight }}</li>
        </ul>
      </div>

      <div class="summary-grid">
        <div class="summary-metric">
          <div class="summary-label">Impact scope</div>
          <p>{{ summary.impactScope }}</p>
        </div>
        <div class="summary-metric">
          <div class="summary-label">Controversy</div>
          <p>{{ summary.controversy }}</p>
        </div>
      </div>

      <div class="summary-block">
        <div class="summary-label">Editor view</div>
        <p>{{ summary.editorView }}</p>
      </div>

      <div class="summary-meta">
        {{ summary.provider }} · {{ summary.promptVersion }}
      </div>

      <RouterLink v-if="compact" class="btn-full-report" :to="{ name: 'report', params: { id: article.id } }">
        Open Full Report
      </RouterLink>
    </div>
    <div v-else-if="isEmpty || !article.hasAiSummary" class="summary-state">
      This article is waiting for an AI summary.
      <button type="button" class="btn-full-report" @click="generateSummary">Generate Summary</button>
      <RouterLink v-if="compact" class="btn-full-report" :to="{ name: 'report', params: { id: article.id } }">
        Open Full Report
      </RouterLink>
    </div>
    <div v-else-if="error" class="summary-state summary-state--error">{{ error.message }}</div>
  </div>
</template>
