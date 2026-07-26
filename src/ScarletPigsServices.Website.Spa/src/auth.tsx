import type { PropsWithChildren } from 'react'
import { AuthProvider } from 'react-oidc-context'
import { WebStorageStateStore } from 'oidc-client-ts'
import { appConfig } from './config'

const clearCallbackPath = () => {
  const url = new URL(window.location.href)

  if (url.pathname === '/auth/callback') {
    window.history.replaceState({}, document.title, '/')
  }
}

export function ScarletPigsAuthProvider({ children }: PropsWithChildren) {
  return (
    <AuthProvider
      authority={appConfig.auth.authority}
      client_id={appConfig.auth.clientId}
      redirect_uri={`${window.location.origin}/auth/callback`}
      post_logout_redirect_uri={window.location.origin}
      response_type="code"
      scope={appConfig.auth.scope}
      loadUserInfo
      automaticSilentRenew={false}
      monitorSession={false}
      userStore={new WebStorageStateStore({ store: window.localStorage })}
      onSigninCallback={clearCallbackPath}
    >
      {children}
    </AuthProvider>
  )
}
