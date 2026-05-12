<script setup lang="ts">
import ArticleCard from '@/components/article/ArticleCard.vue'
import type { Article } from '@/types/article'

defineProps<{
  articles: Article[]
  isLoading: boolean
  errorMessage: string
  hasMore: boolean
}>()

const emit = defineEmits<{
  (event: 'load-more'): void
}>()
</script>

<template>
  <section class="article-feed">
    <div v-if="errorMessage" class="article-feed__error">{{ errorMessage }}</div>
    <div v-else-if="isLoading && articles.length === 0" class="article-feed__state">Loading articles...</div>
    <div v-else-if="articles.length === 0" class="article-feed__state">No articles match these filters.</div>
    <template v-else>
      <ArticleCard v-for="article in articles" :key="article.id" :article="article" />
      <button
        v-if="hasMore"
        class="article-feed__more"
        type="button"
        :disabled="isLoading"
        @click="emit('load-more')"
      >
        {{ isLoading ? 'Loading...' : 'Load more' }}
      </button>
    </template>
  </section>
</template>
