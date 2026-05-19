<script setup lang="ts">
import { useThemeStore } from '@/stores/themeStore'

const themeStore = useThemeStore()
</script>

<template>
  <button
    class="theme-toggle"
    type="button"
    :aria-label="`Switch to ${themeStore.resolvedTheme === 'dark' ? 'light' : 'dark'} theme`"
    @click="themeStore.toggleResolvedTheme"
  >
    {{ themeStore.resolvedTheme === 'dark' ? 'L' : 'D' }}
  </button>
</template>
