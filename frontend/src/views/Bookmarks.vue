<script setup lang="ts">
import { onMounted } from 'vue'

import BookmarkList from '@/components/bookmark/BookmarkList.vue'
import ThemeToggle from '@/components/common/ThemeToggle.vue'
import { useBookmarkStore } from '@/stores/bookmarkStore'
import { useThemeStore } from '@/stores/themeStore'

const bookmarkStore = useBookmarkStore()
const themeStore = useThemeStore()

onMounted(() => {
  void bookmarkStore.loadBookmarks()
})
</script>

<template>
  <div class="app-shell bookmarks-shell" :data-theme="themeStore.resolvedTheme">
    <nav class="topnav">
      <RouterLink class="brand" :to="{ name: 'dashboard' }">
        <span class="brand-dot"></span>
        AI Daily
      </RouterLink>
      <span class="live-badge">SAVED</span>
      <span class="nav-spacer"></span>
      <RouterLink class="nav-btn" :to="{ name: 'dashboard' }">Dashboard</RouterLink>
      <RouterLink class="nav-btn" :to="{ name: 'settings' }">Settings</RouterLink>
      <ThemeToggle />
    </nav>

    <main class="bookmark-page">
      <div class="section-heading">Bookmarks</div>
      <BookmarkList
        :articles="bookmarkStore.articles"
        :is-loading="bookmarkStore.isLoading"
        :error-message="bookmarkStore.errorMessage"
        @remove="bookmarkStore.remove"
      />
    </main>
  </div>
</template>
