const env = import.meta.env

const trimTrailingSlash = (value: string) => value.replace(/\/+$/, '')

const apiBaseUrl = env.VITE_API_BASE_URL ? trimTrailingSlash(env.VITE_API_BASE_URL) : ''

export const appConfig = {
  apiBaseUrl,
  auth: {
    authority:
      env.VITE_AUTH_AUTHORITY ?? 'https://keycloak.scarletpigs.com/realms/ScarletPigs',
    clientId: env.VITE_AUTH_CLIENT_ID ?? 'scarletpigsclient',
    scope: env.VITE_AUTH_SCOPE ?? 'openid profile email roles',
  },
} as const

export const buildApiUrl = (path: string) => {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`

  if (!appConfig.apiBaseUrl) {
    return normalizedPath
  }

  return `${appConfig.apiBaseUrl}${normalizedPath}`
}
