<script setup lang="ts">
const tags = [
  { value: 'model', label: 'Model' },
  { value: 'product', label: 'Product' },
  { value: 'research', label: 'Research' },
  { value: 'safety', label: 'Safety' },
  { value: 'agent', label: 'Agent' },
]

const model = defineModel<string>({ default: '' })
</script>

<template>
  <div class="tag-list" aria-label="Article tag filter">
    <button
      class="tag-pill"
      :class="{ active: model === '' }"
      type="button"
      @click="model = ''"
    >
      All
    </button>
    <button
      v-for="tag in tags"
      :key="tag.value"
      class="tag-pill"
      :class="{ active: model === tag.value }"
      type="button"
      @click="model = tag.value"
    >
      {{ tag.label }}
    </button>
  </div>
</template>
