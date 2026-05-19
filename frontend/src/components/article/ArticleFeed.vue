<script setup lang="ts">
import ArticleCard from '@/components/article/ArticleCard.vue'
import type { Article } from '@/types/article'

defineProps<{
  articles: Article[]
  isLoading: boolean
  errorMessage: string
  hasMore: boolean
  selectedArticleId?: string
}>()

const emit = defineEmits<{
  (event: 'load-more'): void
  (event: 'select', article: Article): void
  (event: 'bookmark', article: Article, isBookmarked: boolean): void
}>()
</script>

<template>
  <section class="article-feed" aria-label="Latest AI articles">
    <div v-if="errorMessage" class="feed-state feed-state--error">{{ errorMessage }}</div>
    <div v-else-if="isLoading && articles.length === 0" class="feed-state">Loading articles...</div>
    <div v-else-if="articles.length === 0" class="feed-state">No articles match these filters.</div>
    <template v-else>
      <ArticleCard
        v-for="article in articles"
        :key="article.id"
        :article="article"
        :selected="article.id === selectedArticleId"
        @select="emit('select', article)"
        @bookmark="(article, isBookmarked) => emit('bookmark', article, isBookmarked)"
      />
      <button
        v-if="hasMore"
        class="article-feed-more"
        type="button"
        :disabled="isLoading"
        @click="emit('load-more')"
      >
        {{ isLoading ? 'Loading...' : 'Load more' }}
      </button>
    </template>
  </section>
</template>
