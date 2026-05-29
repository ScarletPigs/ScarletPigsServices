import type { WorkshopModSummary } from './types'

export type RoleSheetRole = {
  id: string
  name: string
  description: string
  icon: string
  assignedPlayer: string
}

export type RoleSheetSquad = {
  id: string
  callsign: string
  descriptiveName: string
  description: string
  srRadioChannels: [string, string, string]
  lrRadioChannels: [string, string, string]
  roles: RoleSheetRole[]
  squads: RoleSheetSquad[]
}

export type RoleSheetDocument = {
  title: string
  description: string
  author: string
  joinOrderComments: string
  bgColor: string
  textColor: string
  squads: RoleSheetSquad[]
}

export type ParsedPreset = {
  name: string
  mods: WorkshopModSummary[]
  dlcIds: string[]
}

export type DlcOption = {
  id: string
  label: string
  commandLineName: string
}

export const defaultServerCommandLine = '@Advanced Towing;@Advanced Sling Loading;@ocap;'

export const availableDlcs: DlcOption[] = [
  { id: '1681170', label: 'Western Sahara', commandLineName: 'ws' },
  { id: '1175380', label: 'Spearhead 1944', commandLineName: 'spe' },
  { id: '1021790', label: 'Contact', commandLineName: '' },
  { id: '1042220', label: 'Global Mobilization', commandLineName: 'gm' },
  { id: '1227700', label: 'S.O.G. Prairie Fire', commandLineName: 'vn' },
  { id: '1294440', label: 'CSLA Iron Curtain', commandLineName: 'csla' },
  { id: '2647760', label: 'Reaction Forces', commandLineName: 'rf' },
] as const

const dlcById = new Map(availableDlcs.map((item) => [item.id, item]))

export function createRole(): RoleSheetRole {
  return {
    id: createId(),
    name: '',
    description: '',
    icon: '',
    assignedPlayer: '',
  }
}

export function createSquad(): RoleSheetSquad {
  return {
    id: createId(),
    callsign: '',
    descriptiveName: '',
    description: '',
    srRadioChannels: ['', '', ''],
    lrRadioChannels: ['', '', ''],
    roles: [createRole()],
    squads: [],
  }
}

export function createRoleSheetDocument(): RoleSheetDocument {
  return {
    title: 'OP Title',
    description: 'Operation overview',
    author: '',
    joinOrderComments: '',
    bgColor: '#a7a9ac',
    textColor: '#f8b133',
    squads: [createSquad()],
  }
}

export function parseWorkshopIds(text: string) {
  return Array.from(new Set(text.match(/\d+/g)?.filter((value) => value !== '0') ?? []))
}

export function findDlcIds(ids: string[]) {
  return ids.filter((id) => dlcById.has(id))
}

export function parsePresetContent(fileName: string, text: string): ParsedPreset {
  const parser = new DOMParser()
  const htmlDocument = parser.parseFromString(text, 'text/html')
  const xmlDocument = parser.parseFromString(text, 'application/xml')
  const document = htmlDocument.querySelectorAll('tr').length > 0 ? htmlDocument : xmlDocument

  const name = document.querySelector('meta[name="arma:PresetName"]')?.getAttribute('content')?.trim() || fileName

  const mods = Array.from(document.querySelectorAll('tr[data-type="ModContainer"]'))
    .map((row) => {
      const cells = row.querySelectorAll('td')
      return {
        id: extractIdFromUrl(cells[2]?.querySelector('a')?.getAttribute('href')),
        title: cells[0]?.textContent?.trim() ?? '',
        sizeInBytes: 0,
      }
    })
    .filter((item) => item.id && item.title) as WorkshopModSummary[]

  const dlcIds = Array.from(document.querySelectorAll('tr[data-type="DlcContainer"]'))
    .map((row) => {
      const cells = row.querySelectorAll('td')
      return extractIdFromUrl(cells[1]?.querySelector('a')?.getAttribute('href'))
    })
    .filter((value): value is string => typeof value === 'string' && dlcById.has(value))

  return {
    name,
    mods: uniqueMods(mods.sort((left, right) => left.title.localeCompare(right.title))),
    dlcIds: Array.from(new Set(dlcIds)),
  }
}

export function buildCommandLine(mods: WorkshopModSummary[], dlcIds: string[]) {
  return [...dlcIds.map((id) => dlcById.get(id)?.commandLineName ?? ''), ...mods.map(getCommandLineName)]
    .filter((value) => value.length > 0)
    .join(';')
}

export function getMissingMods(mods: WorkshopModSummary[], installedFolders: string[]) {
  const installedSet = new Set(installedFolders.filter(Boolean).map((item) => item.toLowerCase()))

  return mods
    .map((mod) => getCommandLineName(mod))
    .filter((item) => item.length > 0 && !installedSet.has(item.toLowerCase()))
}

export function getReadableSize(sizeInBytes: number) {
  if (sizeInBytes <= 0) {
    return 'Unknown size'
  }

  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  let currentSize = sizeInBytes
  let unitIndex = 0

  while (currentSize >= 1024 && unitIndex < units.length - 1) {
    currentSize /= 1024
    unitIndex += 1
  }

  return `${currentSize.toFixed(currentSize >= 10 || unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`
}

export function buildKeycloakAccountUrl(authority: string) {
  return `${authority.replace(/\/+$/, '')}/account`
}

function getCommandLineName(mod: WorkshopModSummary) {
  const dlc = dlcById.get(mod.id)
  if (dlc) {
    return dlc.commandLineName ? `@${dlc.commandLineName}` : ''
  }

  const normalizedTitle = mod.id === '894678801' ? mod.title.replace(/[()]+/g, '') : mod.title
  const cleaned = normalizedTitle
    .replace(/[^a-zA-Z0-9' +\-@_\[\]]+/g, '')
    .replace(/\s{2,}/g, ' ')
    .trim()
    .replace(/^@+/, '')

  return cleaned ? `@${cleaned}` : ''
}

function extractIdFromUrl(url: string | null | undefined) {
  if (!url) {
    return null
  }

  try {
    const parsedUrl = new URL(url)

    if (parsedUrl.hostname.includes('steampowered.com')) {
      const segments = parsedUrl.pathname.split('/').filter(Boolean)
      return segments.at(-1) ?? null
    }

    return parsedUrl.searchParams.get('id')
  } catch {
    return null
  }
}

function uniqueMods(mods: WorkshopModSummary[]) {
  const seen = new Set<string>()
  return mods.filter((mod) => {
    if (seen.has(mod.id)) {
      return false
    }

    seen.add(mod.id)
    return true
  })
}

function createId() {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID()
  }

  return `${Date.now()}-${Math.random().toString(36).slice(2)}`
}