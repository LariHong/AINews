<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

import AiSummaryPanel from '@/components/ai/AiSummaryPanel.vue'
import ArticleFeed from '@/components/article/ArticleFeed.vue'
import TagFilter from '@/components/common/TagFilter.vue'
import { useArticleStore } from '@/stores/articleStore'
import type { Article } from '@/types/article'

const articleStore = useArticleStore()
const selectedArticleId = ref<string>('')
const theme = ref<'dark' | 'light'>('dark')

const selectedArticle = computed(() => {
  return articleStore.articles.find((article) => article.id === selectedArticleId.value) ?? articleStore.articles[0]
})

const bookmarkedArticles = computed(() => {
  const saved = articleStore.articles.filter((article) => article.isBookmarked)
  return saved.length > 0 ? saved : articleStore.articles.slice(0, 3)
})

const tagStats = computed(() => {
  if (articleStore.dashboardStats) {
    const max = Math.max(1, ...articleStore.dashboardStats.tagBreakdown.map((item) => item.count))
    return articleStore.dashboardStats.tagBreakdown
      .slice(0, 4)
      .map((item) => ({
        tag: item.name,
        count: item.count,
        width: `${Math.max(12, Math.round((item.count / max) * 100))}%`,
      }))
  }

  const counts = articleStore.articles.reduce<Record<string, number>>((acc, article) => {
    article.tags.forEach((tag) => {
      acc[tag] = (acc[tag] ?? 0) + 1
    })
    return acc
  }, {})

  const max = Math.max(1, ...Object.values(counts))
  return Object.entries(counts)
    .sort(([, a], [, b]) => b - a)
    .slice(0, 4)
    .map(([tag, count]) => ({ tag, count, width: `${Math.max(12, Math.round((count / max) * 100))}%` }))
})

const sourceStats = computed(() => {
  if (articleStore.dashboardStats) {
    return articleStore.dashboardStats.topSources
      .slice(0, 5)
      .map((item) => ({ source: item.name, count: item.count }))
  }

  const counts = articleStore.articles.reduce<Record<string, number>>((acc, article) => {
    acc[article.sourceName] = (acc[article.sourceName] ?? 0) + 1
    return acc
  }, {})

  return Object.entries(counts)
    .sort(([, a], [, b]) => b - a)
    .slice(0, 5)
    .map(([source, count]) => ({ source, count }))
})

const totalArticles = computed(() => articleStore.dashboardStats?.totalArticles ?? articleStore.totalCount)

const aiSummarizedCount = computed(() => {
  return articleStore.dashboardStats?.aiSummarizedCount ?? articleStore.articles.filter((article) => article.hasAiSummary).length
})

const statsUpdatedLabel = computed(() => {
  if (articleStore.isStatsLoading) {
    return 'Syncing sources...'
  }

  if (articleStore.statsErrorMessage) {
    return 'Stats temporarily unavailable'
  }

  const updatedAt = articleStore.dashboardStats?.updatedAt
  if (!updatedAt) {
    return 'No updates yet today'
  }

  const elapsedMinutes = Math.max(0, Math.round((Date.now() - new Date(updatedAt).getTime()) / 60000))
  if (elapsedMinutes < 1) {
    return 'Updated just now'
  }

  if (elapsedMinutes < 60) {
    return `Updated ${elapsedMinutes} min ago`
  }

  const elapsedHours = Math.round(elapsedMinutes / 60)
  return `Updated ${elapsedHours} hr ago`
})

const syncStatusLabel = computed(() => {
  if (articleStore.isFeedSyncing || articleStore.feedSyncMessage) {
    return articleStore.feedSyncMessage
  }

  if (articleStore.feedSyncErrorMessage) {
    return articleStore.feedSyncErrorMessage
  }

  const status = articleStore.dashboardStats?.syncStatus
  if (!status) {
    return articleStore.statsErrorMessage ? 'Using article list fallback' : 'Source status pending'
  }

  if (status.isSyncing) {
    return 'Syncing sources...'
  }

  if (status.sourceFailures > 0) {
    return `${status.sourcesSynced} sources synced · ${status.sourceFailures} source failed`
  }

  return status.message
})

const feedStateLabel = computed(() => {
  switch (articleStore.feedSyncViewState) {
    case 'empty_fresh_start':
      return "Fetching today's AI news..."
    case 'stale_with_data':
      return "Updating today's feed in the background..."
    case 'sync_failed':
      return 'Feed sync failed'
    case 'ready':
      return 'Feed ready'
    default:
      return 'Feed waiting'
  }
})

const todayLabel = new Intl.DateTimeFormat('en', {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
}).format(new Date())

function selectArticle(article: Article): void {
  selectedArticleId.value = article.id
}

function toggleTheme(): void {
  theme.value = theme.value === 'dark' ? 'light' : 'dark'
}

watch(
  () => articleStore.articles,
  (articles) => {
    if (articles.length > 0 && !articles.some((article) => article.id === selectedArticleId.value)) {
      selectedArticleId.value = articles[0].id
    }
  },
)

onMounted(() => {
  void articleStore.ensureTodayFeed()
})
</script>

<template>
  <div class="app-shell" :data-theme="theme">
    <nav class="topnav">
      <RouterLink class="brand" :to="{ name: 'dashboard' }">
        <span class="brand-dot"></span>
        AI Daily
      </RouterLink>
      <span class="live-badge">LIVE</span>

      <span class="nav-spacer"></span>
      <span class="nav-date">{{ todayLabel }}</span>
      <button class="nav-btn" type="button">Saved</button>
      <button class="theme-toggle" type="button" :aria-label="`Switch to ${theme === 'dark' ? 'light' : 'dark'} theme`" @click="toggleTheme">
        {{ theme === 'dark' ? 'L' : 'D' }}
      </button>
    </nav>

    <form class="toolbar" @submit.prevent="articleStore.applyFilters">
      <div class="search-wrap">
        <span class="search-icon">/</span>
        <input
          v-model="articleStore.filters.keyword"
          class="search-input"
          type="search"
          placeholder="Search models, research, sources..."
        />
      </div>

      <TagFilter v-model="articleStore.filters.tags" @update:model-value="articleStore.applyFilters" />

      <span class="toolbar-spacer"></span>

      <input v-model="articleStore.filters.source" class="source-select" type="search" placeholder="Source" />
      <input v-model="articleStore.filters.date" class="source-select source-select--date" type="date" />
      <button class="nav-btn nav-btn--accent" type="submit">Apply</button>
      <button
        class="nav-btn"
        type="button"
        :disabled="articleStore.isFeedSyncing"
        @click="articleStore.syncTodayFeed"
      >
        {{ articleStore.isFeedSyncing ? 'Syncing' : 'Refresh' }}
      </button>
    </form>

    <main class="main-grid">
      <section class="feed">
        <div class="stats-row">
          <div class="stat-card">
            <div class="stat-num">{{ totalArticles }}</div>
            <div class="stat-lbl">Articles</div>
          </div>
          <div class="stat-card">
            <div class="stat-num">{{ aiSummarizedCount }}</div>
            <div class="stat-lbl">AI Briefs</div>
          </div>
          <div class="stat-card">
            <div class="stat-num">{{ sourceStats.length }}</div>
            <div class="stat-lbl">Sources</div>
          </div>
          <div class="stat-card">
            <div class="stat-num">{{ bookmarkedArticles.length }}</div>
            <div class="stat-lbl">Saved</div>
          </div>
        </div>

        <div class="dashboard-status">
          <span>{{ statsUpdatedLabel }}</span>
          <span>{{ feedStateLabel }}</span>
          <span>{{ syncStatusLabel }}</span>
        </div>

        <div
          v-if="articleStore.feedSyncViewState === 'stale_with_data' || articleStore.feedSyncViewState === 'sync_failed'"
          class="sync-banner"
          :class="{ 'sync-banner--error': articleStore.feedSyncViewState === 'sync_failed' }"
        >
          <span>{{ articleStore.feedSyncViewState === 'sync_failed' ? articleStore.feedSyncErrorMessage : articleStore.feedSyncMessage }}</span>
          <button
            v-if="articleStore.feedSyncViewState === 'sync_failed'"
            class="btn-sm"
            type="button"
            @click="articleStore.syncTodayFeed"
          >
            Retry
          </button>
        </div>

        <div v-if="articleStore.feedSyncViewState === 'empty_fresh_start'" class="cold-start-panel">
          <div class="cold-start-copy">
            <strong>Fetching today's AI news...</strong>
            <span>Sources are syncing now. Fresh articles will appear here when the run finishes.</span>
          </div>
          <div class="skeleton-list" aria-hidden="true">
            <span></span>
            <span></span>
            <span></span>
          </div>
        </div>

        <div class="section-heading">Latest Signal</div>

        <ArticleFeed
          :articles="articleStore.articles"
          :is-loading="articleStore.isLoading || articleStore.isFeedSyncing"
          :error-message="articleStore.errorMessage"
          :has-more="articleStore.hasMore"
          :selected-article-id="selectedArticle?.id"
          @select="selectArticle"
          @load-more="articleStore.loadArticles(false)"
        />
      </section>

      <aside class="side-panel" aria-label="Article insights">
        <section class="panel-section">
          <div class="panel-label"><span class="panel-label-dot"></span> AI Quick Summary</div>
          <AiSummaryPanel :article="selectedArticle ?? null" compact />
        </section>

        <section class="panel-section">
          <div class="panel-label"><span class="panel-label-dot"></span> Bookmarks</div>
          <div class="bookmark-list">
            <button
              v-for="article in bookmarkedArticles"
              :key="article.id"
              class="bookmark-item"
              type="button"
              @click="selectArticle(article)"
            >
              <span class="bm-dot"></span>
              <span class="bm-title">{{ article.title }}</span>
            </button>
          </div>
        </section>

        <section class="panel-section">
          <div class="panel-label"><span class="panel-label-dot"></span> Topic Mix</div>
          <div class="tag-stat-list">
            <div v-for="stat in tagStats" :key="stat.tag" class="tag-stat-row">
              <span class="tag-stat-name">{{ stat.tag }}</span>
              <div class="tag-stat-bar-bg">
                <div class="tag-stat-bar" :style="{ width: stat.width }"></div>
              </div>
              <span class="tag-stat-count">{{ stat.count }}</span>
            </div>
          </div>
        </section>

        <section class="panel-section">
          <div class="panel-label"><span class="panel-label-dot"></span> Top Sources</div>
          <div class="source-list">
            <div v-for="(stat, index) in sourceStats" :key="stat.source" class="source-item">
              <span class="source-rank">{{ String(index + 1).padStart(2, '0') }}</span>
              <span class="source-item-name">{{ stat.source }}</span>
              <span class="source-count">{{ stat.count }}</span>
            </div>
          </div>
        </section>
      </aside>
    </main>
  </div>
</template>
