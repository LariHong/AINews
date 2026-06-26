const path = require('node:path')

const vue = require('@vitejs/plugin-vue')
const { loadEnv } = require('vite')

module.exports = ({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiProxyTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:12718'
  const devServerPort = Number(env.VITE_DEV_SERVER_PORT || 5176)

  return {
    cacheDir: path.resolve(__dirname, '../.tmp/vite-cache'),
    plugins: [vue()],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, 'src'),
      },
    },
    server: {
      port: devServerPort,
      proxy: {
        '/api': {
          target: apiProxyTarget,
          changeOrigin: true,
        },
      },
    },
  }
}
