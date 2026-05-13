import { computed, ref, watch } from 'vue'

import { fetchAiReport, generateAiReport } from '@/services/aiSummaryApi'
import type { AiReport } from '@/types/aiSummary'

export function useAiReportStream(articleId: () => string) {
  const report = ref<AiReport | null>(null)
  const isLoading = ref(false)
  const isGenerating = ref(false)
  const errorMessage = ref('')
  const statusMessage = ref('')

  const hasReport = computed(() => report.value !== null)

  async function loadReport(): Promise<void> {
    const id = articleId()
    if (!id) return

    isLoading.value = true
    errorMessage.value = ''

    try {
      report.value = await fetchAiReport(id)
      statusMessage.value = 'Loaded saved AI report.'
    } catch (error) {
      if (error instanceof Error && error.name === 'AI_REPORT_NOT_FOUND') {
        report.value = null
        statusMessage.value = 'AI report is ready to generate.'
        return
      }

      errorMessage.value = error instanceof Error ? error.message : 'Unable to load AI report'
    } finally {
      isLoading.value = false
    }
  }

  async function generate(force = false): Promise<void> {
    const id = articleId()
    if (!id || isGenerating.value) return

    isGenerating.value = true
    errorMessage.value = ''
    statusMessage.value = 'Starting AI report generation.'

    try {
      await generateAiReport(id, (event) => {
        if (event.message) statusMessage.value = event.message
        if (event.report) report.value = event.report
      }, force)
    } catch (error) {
      errorMessage.value = error instanceof Error ? error.message : 'Unable to generate AI report'
    } finally {
      isGenerating.value = false
    }
  }

  watch(
    () => articleId(),
    () => {
      report.value = null
      statusMessage.value = ''
      void loadReport()
    },
    { immediate: true },
  )

  return {
    errorMessage,
    generate,
    hasReport,
    isGenerating,
    isLoading,
    loadReport,
    report,
    statusMessage,
  }
}
