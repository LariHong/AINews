import { computed, watch } from 'vue'

import { useArticleStore } from '@/stores/articleStore'

export function useArticle(id: () => string) {
  const articleStore = useArticleStore()

  watch(
    id,
    async (articleId) => {
      if (articleId) {
        await articleStore.loadArticle(articleId)
      }
    },
    { immediate: true },
  )

  return {
    article: computed(() => articleStore.selectedArticle),
    isLoading: computed(() => articleStore.isDetailLoading),
    errorMessage: computed(() => articleStore.errorMessage),
    isNotFound: computed(() => articleStore.detailErrorCode === 'ARTICLE_NOT_FOUND'),
  }
}
