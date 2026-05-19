import { createRouter, createWebHistory } from 'vue-router'

import Dashboard from '@/views/Dashboard.vue'
import Report from '@/views/Report.vue'
import Bookmarks from '@/views/Bookmarks.vue'
import Settings from '@/views/Settings.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: Dashboard,
    },
    {
      path: '/report/:id',
      name: 'report',
      component: Report,
      props: true,
    },
    {
      path: '/bookmarks',
      name: 'bookmarks',
      component: Bookmarks,
    },
    {
      path: '/settings',
      name: 'settings',
      component: Settings,
    },
  ],
})

export default router
