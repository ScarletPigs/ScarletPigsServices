export type EventRecord = {
  id: number
  name: string
  creatorDiscordUsername: string
  author: string | null
  description: string | null
  createdAt: string
  lastModified: string
  startTime: string
  endTime: string
}

export type CreateEventRequest = {
  name: string
  creatorDiscordUsername: string
  author: string | null
  description: string | null
  startTime: string
  endTime: string
}

export type EditEventRequest = {
  id: number
  name?: string
  creatorDiscordUsername?: string
  author?: string | null
  description?: string | null
  startTime?: string
  endTime?: string
}

export type CurrentUser = {
  id: string
  email: string
  globalName: string
  userName: string
  avatarHash: string
  avatarUrl: string
  roles: string[]
  isAdmin: boolean
  isAllowedMissionUpload: boolean
}

export type HavocFoldersResponse = {
  targetName: string
  isConfigured: boolean
  folders: string[]
}

export type MissionUploadResponse = {
  targetName: string
  folder: string
  fileName: string
  remotePath: string
}

export type WorkshopModSummary = {
  id: string
  title: string
  sizeInBytes: number
}

export type WorkshopLookupResponse = {
  mods: WorkshopModSummary[]
}
