<script setup lang="ts">
import type { Article } from '@/types/article'

const props = defineProps<{
  article: Article
  selected?: boolean
}>()

const emit = defineEmits<{
  (event: 'select', article: Article): void
  (event: 'bookmark', article: Article, isBookmarked: boolean): void
}>()

function formatTimeAgo(value: string): string {
  const published = new Date(value).getTime()
  const diffHours = Math.max(1, Math.round((Date.now() - published) / 36e5))

  if (diffHours < 24) return `${diffHours}h ago`

  return new Intl.DateTimeFormat('en', {
    month: 'short',
    day: 'numeric',
  }).format(new Date(value))
}

function tagClass(tag: string): string {
  if (tag === 'research') return 'tag-research'
  if (tag === 'product' || tag === 'agent') return 'tag-product'
  if (tag === 'safety') return 'tag-safety'
  return 'tag-model'
}

function openSource(): void {
  window.open(props.article.sourceUrl, '_blank', 'noopener,noreferrer')
}
</script>

<template>
  <article class="article-card" :class="{ selected }" @click="emit('select', article)">
    <div class="card-header">
      <span v-if="article.tags[0]" class="tag-badge" :class="tagClass(article.tags[0])">
        {{ article.tags[0] }}
      </span>
      <span v-if="article.hasAiSummary" class="ai-done-badge">AI brief</span>
      <span class="source-name">{{ article.sourceName }}</span>
    </div>

    <h2 class="card-title">{{ article.title }}</h2>
    <p class="card-summary">{{ article.summary ?? 'No summary available yet.' }}</p>

    <div class="card-footer">
      <RouterLink class="btn-ai" :to="{ name: 'report', params: { id: article.id } }" @click.stop>
        Full report
      </RouterLink>
      <button class="btn-sm" type="button" @click.stop="emit('bookmark', article, !article.isBookmarked)">
        {{ article.isBookmarked ? 'Saved' : 'Save' }}
      </button>
      <button class="btn-sm" type="button" @click.stop="openSource">Source</button>
      <span v-if="article.readTimeMinutes" class="read-time">{{ article.readTimeMinutes }} min</span>
      <span class="card-time">{{ formatTimeAgo(article.publishedAt) }}</span>
    </div>
  </article>
</template>
