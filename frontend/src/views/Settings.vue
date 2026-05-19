<script setup lang="ts">
import { onMounted } from 'vue'

import ThemeToggle from '@/components/common/ThemeToggle.vue'
import { usePreferenceStore } from '@/stores/preferenceStore'
import { useThemeStore } from '@/stores/themeStore'

const preferenceStore = usePreferenceStore()
const themeStore = useThemeStore()

onMounted(() => {
  void preferenceStore.loadHiddenArticles()
})
</script>

<template>
  <div class="app-shell bookmarks-shell" :data-theme="themeStore.resolvedTheme">
    <nav class="topnav">
      <RouterLink class="brand" :to="{ name: 'dashboard' }">
        <span class="brand-dot"></span>
        AI Daily
      </RouterLink>
      <span class="nav-spacer"></span>
      <RouterLink class="nav-btn" :to="{ name: 'dashboard' }">Dashboard</RouterLink>
      <RouterLink class="nav-btn" :to="{ name: 'bookmarks' }">Bookmarks</RouterLink>
      <ThemeToggle />
    </nav>

    <main class="bookmark-page">
      <div class="section-heading">Settings</div>
      <section class="settings-panel">
        <div>
          <h1 class="settings-title">Theme</h1>
          <p class="settings-copy">Current preference: {{ themeStore.preference }}</p>
        </div>
        <div class="settings-actions">
          <button class="nav-btn" type="button" @click="themeStore.setPreference('system')">System</button>
          <button class="nav-btn" type="button" @click="themeStore.setPreference('light')">Light</button>
          <button class="nav-btn" type="button" @click="themeStore.setPreference('dark')">Dark</button>
        </div>
      </section>

      <section class="settings-panel">
        <div>
          <h1 class="settings-title">Hidden Articles</h1>
          <p class="settings-copy">
            {{ preferenceStore.hiddenArticles.length }} hidden from your normal feed.
          </p>
        </div>
        <div class="bookmark-list">
          <div v-if="preferenceStore.isLoadingHiddenArticles" class="feed-state">Loading hidden articles...</div>
          <div v-else-if="preferenceStore.hiddenArticlesErrorMessage" class="feed-state feed-state--error">
            {{ preferenceStore.hiddenArticlesErrorMessage }}
          </div>
          <div v-else-if="preferenceStore.hiddenArticles.length === 0" class="feed-state">
            No hidden articles.
          </div>
          <template v-else>
            <button
              v-for="article in preferenceStore.hiddenArticles"
              :key="article.id"
              class="bookmark-item"
              type="button"
              @click="preferenceStore.restoreArticle(article.id)"
            >
              <span class="bm-dot"></span>
              <span class="bm-title">{{ article.title }}</span>
              <span class="source-count">Restore</span>
            </button>
          </template>
        </div>
      </section>
    </main>
  </div>
</template>
