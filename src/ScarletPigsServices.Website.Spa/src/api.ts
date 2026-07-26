import { buildApiUrl } from './config'
import type {
  CreateEventRequest,
  CurrentUser,
  EditEventRequest,
  EventRecord,
  HavocFoldersResponse,
  MissionUploadResponse,
  WorkshopLookupResponse,
} from './types'

type ApiOptions = {
  accessToken?: string | null
}

async function request<T>(path: string, init: RequestInit = {}, options: ApiOptions = {}) {
  const headers = new Headers(init.headers)

  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json')
  }

  if (options.accessToken) {
    headers.set('Authorization', `Bearer ${options.accessToken}`)
  }

  const response = await fetch(buildApiUrl(path), {
    ...init,
    headers,
  })

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const createApiClient = (options: ApiOptions = {}) => ({
  getCurrentUser: () => request<CurrentUser>('/users/me', undefined, options),
  getEvents: () => request<EventRecord[]>('/events', undefined, options),
  getEvent: (id: number) => request<EventRecord>(`/events/${id}`, undefined, options),
  createEvent: (payload: CreateEventRequest) =>
    request<EventRecord>(
      '/events',
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      },
      options,
    ),
  updateEvent: (payload: EditEventRequest) =>
    request<boolean>(
      '/events',
      {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      },
      options,
    ),
  deleteEvent: (id: number) =>
    request<boolean>(
      `/events/${id}`,
      {
        method: 'DELETE',
      },
      options,
    ),
  getHavocFolders: (target = 'server') =>
    request<HavocFoldersResponse>(`/files/folders?target=${encodeURIComponent(target)}`, undefined, options),
  uploadMission: async (
    fileName: string,
    file: File,
    folder = '/',
    target = 'server',
  ) => {
    const form = new FormData()
    form.append('file', file, fileName)
    form.append('folder', folder)
    form.append('target', target)

    return request<MissionUploadResponse>(
      '/files/missions',
      {
        method: 'POST',
        body: form,
      },
      options,
    )
  },
  lookupWorkshopMods: (ids: string[]) =>
    request<WorkshopLookupResponse>(
      '/workshop/mods',
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ ids }),
      },
      options,
    ),
})
