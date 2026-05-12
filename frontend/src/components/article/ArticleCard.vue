<script setup lang="ts">
import type { Article } from '@/types/article'

defineProps<{
  article: Article
}>()

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('en', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}
</script>

<template>
  <article class="article-card">
    <div class="article-card__meta">
      <span>{{ article.sourceName }}</span>
      <span>{{ formatDate(article.publishedAt) }}</span>
      <span v-if="article.readTimeMinutes">{{ article.readTimeMinutes }} min</span>
    </div>
    <h2 class="article-card__title">
      <a :href="article.sourceUrl" target="_blank" rel="noreferrer">{{ article.title }}</a>
    </h2>
    <p class="article-card__summary">{{ article.summary ?? 'No summary available yet.' }}</p>
    <div class="article-card__footer">
      <div class="article-card__tags">
        <span v-for="tag in article.tags" :key="tag" class="article-card__tag">{{ tag }}</span>
      </div>
      <div class="article-card__flags">
        <span v-if="article.hasAiSummary" class="article-card__pill">AI summary</span>
        <span v-if="article.isBookmarked" class="article-card__pill">Saved</span>
      </div>
    </div>
  </article>
</template>
