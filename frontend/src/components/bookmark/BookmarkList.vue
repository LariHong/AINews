<script setup lang="ts">
import type { Article } from '@/types/article'

defineProps<{
  articles: Article[]
  isLoading: boolean
  errorMessage: string
}>()

const emit = defineEmits<{
  (event: 'remove', articleId: string): void
}>()
</script>

<template>
  <section class="bookmark-page-list" aria-label="Saved articles">
    <div v-if="errorMessage" class="feed-state feed-state--error">{{ errorMessage }}</div>
    <div v-else-if="isLoading" class="feed-state">Loading bookmarks...</div>
    <div v-else-if="articles.length === 0" class="feed-state">No saved articles yet.</div>
    <template v-else>
      <article v-for="article in articles" :key="article.id" class="bookmark-card">
        <div>
          <div class="bookmark-card-source">{{ article.sourceName }}</div>
          <h2 class="bookmark-card-title">{{ article.title }}</h2>
          <p class="bookmark-card-summary">{{ article.summary ?? 'No summary available yet.' }}</p>
        </div>
        <div class="bookmark-card-actions">
          <RouterLink class="btn-ai" :to="{ name: 'report', params: { id: article.id } }">Full report</RouterLink>
          <button class="btn-sm" type="button" @click="emit('remove', article.id)">Remove</button>
        </div>
      </article>
    </template>
  </section>
</template>
