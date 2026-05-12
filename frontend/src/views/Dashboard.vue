<script setup lang="ts">
import { onMounted } from 'vue'

import ArticleFeed from '@/components/article/ArticleFeed.vue'
import TagFilter from '@/components/common/TagFilter.vue'
import { useArticleStore } from '@/stores/articleStore'

const articleStore = useArticleStore()

onMounted(() => {
  void articleStore.loadArticles(true)
})
</script>

<template>
  <main class="dashboard">
    <header class="dashboard__header">
      <div>
        <p class="dashboard__eyebrow">AI Daily</p>
        <h1>Today&apos;s AI signal</h1>
      </div>
      <div class="dashboard__count">{{ articleStore.totalCount }} articles</div>
    </header>

    <form class="toolbar" @submit.prevent="articleStore.applyFilters">
      <label class="toolbar__field">
        Search
        <input v-model="articleStore.filters.keyword" type="search" placeholder="GPT, safety, robotics" />
      </label>
      <label class="toolbar__field">
        Source
        <input v-model="articleStore.filters.source" type="search" placeholder="OpenAI, DeepMind" />
      </label>
      <label class="toolbar__field">
        Date
        <input v-model="articleStore.filters.date" type="date" />
      </label>
      <button class="toolbar__submit" type="submit">Apply</button>
    </form>

    <TagFilter v-model="articleStore.filters.tags" @update:model-value="articleStore.applyFilters" />

    <ArticleFeed
      :articles="articleStore.articles"
      :is-loading="articleStore.isLoading"
      :error-message="articleStore.errorMessage"
      :has-more="articleStore.hasMore"
      @load-more="articleStore.loadArticles(false)"
    />
  </main>
</template>
