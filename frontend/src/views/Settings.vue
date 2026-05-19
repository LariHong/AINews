<script setup lang="ts">
import ThemeToggle from '@/components/common/ThemeToggle.vue'
import { useThemeStore } from '@/stores/themeStore'

const themeStore = useThemeStore()
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
    </main>
  </div>
</template>
