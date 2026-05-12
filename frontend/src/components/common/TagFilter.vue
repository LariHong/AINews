<script setup lang="ts">
const tags = ['model', 'product', 'research', 'safety', 'agent']

const model = defineModel<string>({ default: '' })
</script>

<template>
  <div class="tag-filter" aria-label="Article tag filter">
    <button
      class="tag-filter__option"
      :class="{ 'tag-filter__option--active': model === '' }"
      type="button"
      @click="model = ''"
    >
      All
    </button>
    <button
      v-for="tag in tags"
      :key="tag"
      class="tag-filter__option"
      :class="{ 'tag-filter__option--active': model === tag }"
      type="button"
      @click="model = tag"
    >
      {{ tag }}
    </button>
  </div>
</template>
