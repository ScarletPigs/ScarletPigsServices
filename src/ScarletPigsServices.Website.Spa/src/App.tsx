import { startTransition, useEffect, useState } from 'react'
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  AppBar,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Checkbox,
  CircularProgress,
  Container,
  CssBaseline,
  Divider,
  FormControl,
  FormControlLabel,
  FormGroup,
  IconButton,
  InputLabel,
  List,
  ListItem,
  ListItemText,
  Menu,
  MenuItem,
  Select,
  Stack,
  TextField,
  ThemeProvider,
  Toolbar,
  Typography,
  createTheme,
} from '@mui/material'
import AddCircleOutlineRoundedIcon from '@mui/icons-material/AddCircleOutlineRounded'
import CalendarMonthRoundedIcon from '@mui/icons-material/CalendarMonthRounded'
import ContentCopyRoundedIcon from '@mui/icons-material/ContentCopyRounded'
import DeleteOutlineRoundedIcon from '@mui/icons-material/DeleteOutlineRounded'
import DesignServicesRoundedIcon from '@mui/icons-material/DesignServicesRounded'
import DownloadRoundedIcon from '@mui/icons-material/DownloadRounded'
import ExpandMoreRoundedIcon from '@mui/icons-material/ExpandMoreRounded'
import OpenInNewRoundedIcon from '@mui/icons-material/OpenInNewRounded'
import PublishRoundedIcon from '@mui/icons-material/PublishRounded'
import TerminalRoundedIcon from '@mui/icons-material/TerminalRounded'
import type { SelectChangeEvent } from '@mui/material/Select'
import { BrowserRouter, Link as RouterLink, Navigate, Route, Routes, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from 'react-oidc-context'
import dayjs from 'dayjs'
import type { ReactNode } from 'react'
import { ScarletPigsAuthProvider } from './auth'
import { createApiClient } from './api'
import { appConfig } from './config'
import {
  availableDlcs,
  buildCommandLine,
  buildKeycloakAccountUrl,
  createRole,
  createRoleSheetDocument,
  createSquad,
  defaultServerCommandLine,
  findDlcIds,
  getMissingMods,
  getReadableSize,
  parsePresetContent,
  parseWorkshopIds,
  type RoleSheetDocument,
  type RoleSheetRole,
  type RoleSheetSquad,
} from './tools'
import type { CreateEventRequest, CurrentUser, EditEventRequest, EventRecord, WorkshopModSummary } from './types'

const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: {
      main: '#d43b2a',
    },
    secondary: {
      main: '#c8a157',
    },
    background: {
      default: '#140b0a',
      paper: 'rgba(33, 18, 16, 0.78)',
    },
  },
  shape: {
    borderRadius: 20,
  },
  typography: {
    fontFamily: '"Segoe UI Variable", "Trebuchet MS", sans-serif',
    h3: {
      fontWeight: 800,
      letterSpacing: '-0.04em',
    },
    h4: {
      fontWeight: 700,
    },
    h5: {
      fontWeight: 700,
    },
  },
  components: {
    MuiCard: {
      styleOverrides: {
        root: {
          backdropFilter: 'blur(16px)',
          border: '1px solid rgba(255, 255, 255, 0.07)',
          boxShadow: '0 24px 80px rgba(0, 0, 0, 0.28)',
        },
      },
    },
  },
})

const toolLinks = [
  {
    title: 'Build-A-Role-Sheet',
    href: '/build-a-role-sheet',
    description: 'Squad planning workspace for recreating the role composition tool.',
    icon: <DesignServicesRoundedIcon fontSize="small" />,
  },
  {
    title: 'Command Line Generator',
    href: '/command-line-generator',
    description: 'Operational launch arguments and preset composition.',
    icon: <TerminalRoundedIcon fontSize="small" />,
  },
  {
    title: 'Upload Mission',
    href: '/upload-mission',
    description: 'Mission upload flow wired to the live API and folder lookup.',
    icon: <PublishRoundedIcon fontSize="small" />,
  },
] as const

type EventFormState = {
  name: string
  author: string
  description: string
  startTime: string
  endTime: string
}

const emptyEventForm: EventFormState = {
  name: '',
  author: '',
  description: '',
  startTime: dayjs().minute(0).second(0).millisecond(0).format('YYYY-MM-DDTHH:mm'),
  endTime: dayjs().add(2, 'hour').minute(0).second(0).millisecond(0).format('YYYY-MM-DDTHH:mm'),
}

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <ScarletPigsAuthProvider>
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </ScarletPigsAuthProvider>
    </ThemeProvider>
  )
}

function AppRoutes() {
  const auth = useAuth()
  const api = createApiClient({ accessToken: auth.user?.access_token })
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null)
  const [userError, setUserError] = useState<string | null>(null)

  useEffect(() => {
    let isCancelled = false

    if (!auth.isAuthenticated) {
      setCurrentUser(null)
      setUserError(null)
      return undefined
    }

    api
      .getCurrentUser()
      .then((user) => {
        if (!isCancelled) {
          setCurrentUser(user)
          setUserError(null)
        }
      })
      .catch((error: unknown) => {
        if (!isCancelled) {
          setUserError(error instanceof Error ? error.message : 'Unable to load the current user.')
        }
      })

    return () => {
      isCancelled = true
    }
  }, [auth.isAuthenticated, auth.user?.access_token])

  return (
    <Box
      sx={{
        minHeight: '100vh',
        background:
          'radial-gradient(circle at top, rgba(212, 59, 42, 0.26), transparent 32%), linear-gradient(180deg, #25100d 0%, #120707 100%)',
      }}
    >
      <TopBar currentUser={currentUser} />
      <Container maxWidth="lg" sx={{ pb: 8, pt: { xs: 12, md: 16 } }}>
        {userError ? <Alert sx={{ mb: 3 }} severity="warning">{userError}</Alert> : null}
        <Routes>
          <Route path="/" element={<HomePage currentUser={currentUser} />} />
          <Route path="/auth/callback" element={<AuthCallbackPage />} />
          <Route path="/events" element={<EventsPage />} />
          <Route path="/events/new" element={<ProtectedRoute><CreateEventPage currentUser={currentUser} /></ProtectedRoute>} />
          <Route path="/events/:eventId" element={<EventDetailsPage currentUser={currentUser} />} />
          <Route path="/events/:eventId/edit" element={<ProtectedRoute><EditEventPage currentUser={currentUser} /></ProtectedRoute>} />
          <Route path="/build-a-role-sheet" element={<BuildRoleSheetPage />} />
          <Route path="/command-line-generator" element={<CommandLineGeneratorPage />} />
          <Route path="/upload-mission" element={<ProtectedRoute><UploadMissionPage currentUser={currentUser} /></ProtectedRoute>} />
          <Route path="/account" element={<ProtectedRoute><AccountPage currentUser={currentUser} /></ProtectedRoute>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Container>
    </Box>
  )
}

function TopBar({ currentUser }: { currentUser: CurrentUser | null }) {
  const auth = useAuth()
  const [toolsAnchor, setToolsAnchor] = useState<HTMLElement | null>(null)
  const [profileAnchor, setProfileAnchor] = useState<HTMLElement | null>(null)

  return (
    <AppBar
      position="fixed"
      color="transparent"
      elevation={0}
      sx={{
        backdropFilter: 'blur(24px)',
        borderBottom: '1px solid rgba(255,255,255,0.08)',
        backgroundColor: 'rgba(17, 8, 8, 0.55)',
      }}
    >
      <Toolbar sx={{ display: 'grid', gap: 2, gridTemplateColumns: '1fr auto 1fr', minHeight: 88 }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
          <Button component={RouterLink} to="/" color="inherit">Home</Button>
          <Button component={RouterLink} to="/events" color="inherit" startIcon={<CalendarMonthRoundedIcon />}>Events</Button>
        </Stack>
        <Box component={RouterLink} to="/" sx={{ alignItems: 'center', display: 'inline-flex', justifyContent: 'center' }}>
          <Box component="img" src="/LogoScarletPigs.svg" alt="Scarlet Pigs" sx={{ width: { xs: 140, md: 180 }, filter: 'drop-shadow(0 10px 22px rgba(0,0,0,0.35))' }} />
        </Box>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', justifyContent: 'flex-end' }}>
          <Button color="inherit" onClick={(event) => setToolsAnchor(event.currentTarget)}>Tools</Button>
          <Menu anchorEl={toolsAnchor} open={Boolean(toolsAnchor)} onClose={() => setToolsAnchor(null)}>
            {toolLinks.map((tool) => (
              <MenuItem component={RouterLink} to={tool.href} key={tool.href} onClick={() => setToolsAnchor(null)}>
                <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
                  {tool.icon}
                  <Box>
                    <Typography sx={{ fontWeight: 700 }}>{tool.title}</Typography>
                    <Typography variant="body2" color="text.secondary">{tool.description}</Typography>
                  </Box>
                </Stack>
              </MenuItem>
            ))}
          </Menu>
          {auth.isAuthenticated ? (
            <>
              <Button color="inherit" onClick={(event) => setProfileAnchor(event.currentTarget)} sx={{ minWidth: 0, p: 0.75 }}>
                <Avatar src={currentUser?.avatarUrl || undefined} alt={currentUser?.globalName ?? 'Account'} />
              </Button>
              <Menu anchorEl={profileAnchor} open={Boolean(profileAnchor)} onClose={() => setProfileAnchor(null)}>
                <MenuItem component={RouterLink} to="/account" onClick={() => setProfileAnchor(null)}>Account</MenuItem>
                <MenuItem
                  onClick={() => {
                    setProfileAnchor(null)
                    void auth.signoutRedirect()
                  }}
                >
                  Logout
                </MenuItem>
              </Menu>
            </>
          ) : (
            <Button color="primary" variant="contained" onClick={() => void auth.signinRedirect()}>
              Login (Discord)
            </Button>
          )}
        </Stack>
      </Toolbar>
    </AppBar>
  )
}

function ProtectedRoute({ children }: { children: ReactNode }) {
  const auth = useAuth()

  if (auth.isLoading) {
    return <PageFrame title="Authenticating"><LoadingState label="Checking your session..." /></PageFrame>
  }

  if (!auth.isAuthenticated) {
    return (
      <PageFrame title="Login required" eyebrow="Access control">
        <Alert severity="info" sx={{ mb: 3 }}>
          This area is role-gated in the current website. Sign in through Keycloak to continue.
        </Alert>
        <Button variant="contained" onClick={() => void auth.signinRedirect()}>
          Continue to login
        </Button>
      </PageFrame>
    )
  }

  return <>{children}</>
}

function HomePage({ currentUser }: { currentUser: CurrentUser | null }) {
  const auth = useAuth()
  const api = createApiClient({ accessToken: auth.user?.access_token })
  const [events, setEvents] = useState<EventRecord[]>([])
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isCancelled = false

    setIsLoading(true)
    api
      .getEvents()
      .then((items) => {
        if (!isCancelled) {
          setEvents(items)
          setError(null)
        }
      })
      .catch((loadError: unknown) => {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : 'Unable to load events.')
        }
      })
      .finally(() => {
        if (!isCancelled) {
          setIsLoading(false)
        }
      })

    return () => {
      isCancelled = true
    }
  }, [auth.user?.access_token])

  const upcomingEvents = [...events]
    .sort((left, right) => dayjs(left.startTime).valueOf() - dayjs(right.startTime).valueOf())
    .slice(0, 4)

  return (
    <Stack spacing={3}>
      <HeroCard currentUser={currentUser} />
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={3}>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Typography variant="h5" gutterBottom>
              Next operations
            </Typography>
            <Typography color="text.secondary" sx={{ mb: 3 }}>
              The Blazor home calendar is being ported into a pure SPA. This first slice already reads live event data from the API.
            </Typography>
            {error ? <Alert severity="warning">{error}</Alert> : null}
            {isLoading ? <LoadingState label="Loading events..." /> : <EventList events={upcomingEvents} emptyMessage="No upcoming events are scheduled." />}
          </CardContent>
        </Card>
        <Card sx={{ flex: 1 }}>
          <CardContent>
            <Typography variant="h5" gutterBottom>
              Migration status
            </Typography>
            <List disablePadding>
              <StatusLine label="App shell" value="React + MUI live" />
              <StatusLine label="Keycloak" value="OIDC browser flow wired" />
              <StatusLine label="Events" value="Read, create, edit, delete routes started" />
              <StatusLine label="Mission upload" value="API-backed form in place" />
              <StatusLine label="Tools" value="Route parity started" />
            </List>
          </CardContent>
        </Card>
      </Stack>
    </Stack>
  )
}

function HeroCard({ currentUser }: { currentUser: CurrentUser | null }) {
  return (
    <Card sx={{ overflow: 'hidden', position: 'relative' }}>
      <Box
        sx={{
          inset: 0,
          opacity: 0.24,
          position: 'absolute',
          background:
            'linear-gradient(125deg, rgba(212,59,42,0.65) 0%, rgba(200,161,87,0.38) 48%, rgba(19,8,8,0.2) 100%)',
        }}
      />
      <CardContent sx={{ position: 'relative', py: { xs: 4, md: 6 } }}>
        <Stack spacing={2}>
          <Chip label="Scarlet Pigs React SPA" color="primary" sx={{ alignSelf: 'flex-start' }} />
          <Typography variant="h3">Operational dashboard, rebuilt as a TypeScript SPA.</Typography>
          <Typography color="text.secondary" sx={{ maxWidth: 720 }}>
            This frontend keeps the original site’s structure, navigation, and Material-style language while moving auth and API access into the browser.
          </Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
            <Button component={RouterLink} to="/events" size="large" variant="contained">
              Explore events
            </Button>
            <Button component={RouterLink} to="/upload-mission" size="large" variant="outlined" color="secondary">
              Mission upload
            </Button>
          </Stack>
          {currentUser ? (
            <Typography color="text.secondary">
              Signed in as {currentUser.globalName} with {currentUser.roles.length} active role{currentUser.roles.length === 1 ? '' : 's'}.
            </Typography>
          ) : null}
        </Stack>
      </CardContent>
    </Card>
  )
}

function EventsPage() {
  const auth = useAuth()
  const api = createApiClient({ accessToken: auth.user?.access_token })
  const [events, setEvents] = useState<EventRecord[]>([])
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isCancelled = false

    api
      .getEvents()
      .then((items) => {
        if (!isCancelled) {
          setEvents(items)
          setError(null)
        }
      })
      .catch((loadError: unknown) => {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : 'Unable to load events.')
        }
      })
      .finally(() => {
        if (!isCancelled) {
          setIsLoading(false)
        }
      })

    return () => {
      isCancelled = true
    }
  }, [auth.user?.access_token])

  return (
    <PageFrame title="Events" eyebrow="Live API">
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ justifyContent: 'space-between', mb: 3 }}>
        <Typography color="text.secondary">
          Event read paths are already connected to the existing ASP.NET API.
        </Typography>
        <Button component={RouterLink} to="/events/new" variant="contained">
          Create event
        </Button>
      </Stack>
      {error ? <Alert severity="warning" sx={{ mb: 3 }}>{error}</Alert> : null}
      {isLoading ? <LoadingState label="Loading events..." /> : <EventList events={events} emptyMessage="No events were returned by the API." />}
    </PageFrame>
  )
}

function EventDetailsPage({ currentUser }: { currentUser: CurrentUser | null }) {
  const auth = useAuth()
  const api = createApiClient({ accessToken: auth.user?.access_token })
  const { eventId } = useParams()
  const navigate = useNavigate()
  const [eventRecord, setEventRecord] = useState<EventRecord | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isDeleting, setIsDeleting] = useState(false)

  useEffect(() => {
    let isCancelled = false

    if (!eventId) {
      setError('The requested event was not found.')
      setIsLoading(false)
      return undefined
    }

    api
      .getEvent(Number(eventId))
      .then((item) => {
        if (!isCancelled) {
          setEventRecord(item)
          setError(null)
        }
      })
      .catch((loadError: unknown) => {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : 'Unable to load the event.')
        }
      })
      .finally(() => {
        if (!isCancelled) {
          setIsLoading(false)
        }
      })

    return () => {
      isCancelled = true
    }
  }, [auth.user?.access_token, eventId])

  const handleDelete = async () => {
    if (!eventRecord) {
      return
    }

    setIsDeleting(true)

    try {
      await api.deleteEvent(eventRecord.id)
      startTransition(() => navigate('/events'))
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : 'Unable to delete the event.')
    } finally {
      setIsDeleting(false)
    }
  }

  if (isLoading) {
    return <PageFrame title="Event details"><LoadingState label="Loading event..." /></PageFrame>
  }

  if (!eventRecord) {
    return <PageFrame title="Event details"><Alert severity="warning">{error ?? 'Event not found.'}</Alert></PageFrame>
  }

  return (
    <PageFrame title={eventRecord.name} eyebrow="Event details">
      {error ? <Alert severity="warning" sx={{ mb: 3 }}>{error}</Alert> : null}
      <Stack spacing={3}>
        <Card>
          <CardContent>
            <Stack spacing={2}>
              <Typography color="text.secondary">Created by {eventRecord.creatorDiscordUsername}</Typography>
              <Typography>{eventRecord.description || 'No event description was provided.'}</Typography>
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                <MetricCard label="Start" value={formatDateTime(eventRecord.startTime)} />
                <MetricCard label="End" value={formatDateTime(eventRecord.endTime)} />
                <MetricCard label="Author" value={eventRecord.author || 'Unspecified'} />
              </Stack>
            </Stack>
          </CardContent>
        </Card>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <Button component={RouterLink} to={`/events/${eventRecord.id}/edit`} variant="contained">
            Edit event
          </Button>
          {currentUser?.isAdmin ? (
            <Button color="error" disabled={isDeleting} onClick={() => void handleDelete()} variant="outlined">
              {isDeleting ? 'Deleting...' : 'Delete event'}
            </Button>
          ) : null}
        </Stack>
      </Stack>
    </PageFrame>
  )
}

function CreateEventPage({ currentUser }: { currentUser: CurrentUser | null }) {
  const auth = useAuth()
  const api = createApiClient({ accessToken: auth.user?.access_token })
  const navigate = useNavigate()
  const [form, setForm] = useState<EventFormState>(emptyEventForm)
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSaving(true)
    setError(null)

    const payload: CreateEventRequest = {
      name: form.name,
      creatorDiscordUsername: currentUser?.userName ?? 'unknown',
      author: nullable(form.author),
      description: nullable(form.description),
      startTime: new Date(form.startTime).toISOString(),
      endTime: new Date(form.endTime).toISOString(),
    }

    try {
      const created = await api.createEvent(payload)
      startTransition(() => navigate(`/events/${created.id}`))
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'Unable to create the event.')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <PageFrame title="Create event" eyebrow="Event operations">
      {error ? <Alert severity="warning" sx={{ mb: 3 }}>{error}</Alert> : null}
      <EventForm form={form} onChange={setForm} onSubmit={handleSubmit} submitLabel={isSaving ? 'Creating...' : 'Create event'} />
    </PageFrame>
  )
}

function EditEventPage({ currentUser }: { currentUser: CurrentUser | null }) {
  const auth = useAuth()
  const api = createApiClient({ accessToken: auth.user?.access_token })
  const navigate = useNavigate()
  const { eventId } = useParams()
  const [form, setForm] = useState<EventFormState>(emptyEventForm)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    let isCancelled = false

    if (!eventId) {
      setError('The requested event was not found.')
      setIsLoading(false)
      return undefined
    }

    api
      .getEvent(Number(eventId))
      .then((item) => {
        if (!isCancelled) {
          setForm({
            name: item.name,
            author: item.author ?? '',
            description: item.description ?? '',
            startTime: toLocalDateTime(item.startTime),
            endTime: toLocalDateTime(item.endTime),
          })
          setError(null)
        }
      })
      .catch((loadError: unknown) => {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : 'Unable to load the event.')
        }
      })
      .finally(() => {
        if (!isCancelled) {
          setIsLoading(false)
        }
      })

    return () => {
      isCancelled = true
    }
  }, [auth.user?.access_token, eventId])

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!eventId) {
      return
    }

    setIsSaving(true)
    setError(null)

    const payload: EditEventRequest = {
      id: Number(eventId),
      name: form.name,
      creatorDiscordUsername: currentUser?.userName,
      author: nullable(form.author),
      description: nullable(form.description),
      startTime: new Date(form.startTime).toISOString(),
      endTime: new Date(form.endTime).toISOString(),
    }

    try {
      await api.updateEvent(payload)
      startTransition(() => navigate(`/events/${eventId}`))
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'Unable to update the event.')
    } finally {
      setIsSaving(false)
    }
  }

  if (isLoading) {
    return <PageFrame title="Edit event"><LoadingState label="Loading event..." /></PageFrame>
  }

  return (
    <PageFrame title="Edit event" eyebrow="Event operations">
      {error ? <Alert severity="warning" sx={{ mb: 3 }}>{error}</Alert> : null}
      <EventForm form={form} onChange={setForm} onSubmit={handleSubmit} submitLabel={isSaving ? 'Saving...' : 'Save changes'} />
    </PageFrame>
  )
}

function UploadMissionPage({ currentUser }: { currentUser: CurrentUser | null }) {
  const auth = useAuth()
  const api = createApiClient({ accessToken: auth.user?.access_token })
  const [target, setTarget] = useState('server')
  const [folders, setFolders] = useState<string[]>([])
  const [folder, setFolder] = useState('/')
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoadingFolders, setIsLoadingFolders] = useState(true)
  const [isUploading, setIsUploading] = useState(false)

  useEffect(() => {
    let isCancelled = false

    setIsLoadingFolders(true)
    api
      .getHavocFolders(target)
      .then((response) => {
        if (!isCancelled) {
          setFolders(response.folders)
          setFolder(response.folders[0] ?? '/')
          setError(response.isConfigured ? null : `${response.targetName} is not configured.`)
        }
      })
      .catch((loadError: unknown) => {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : 'Unable to load mission folders.')
        }
      })
      .finally(() => {
        if (!isCancelled) {
          setIsLoadingFolders(false)
        }
      })

    return () => {
      isCancelled = true
    }
  }, [auth.user?.access_token, target])

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!selectedFile) {
      setError('Choose a mission file before uploading.')
      return
    }

    setIsUploading(true)
    setError(null)
    setMessage(null)

    try {
      const response = await api.uploadMission(selectedFile.name, selectedFile, folder, target)
      setMessage(`Uploaded ${response.fileName} to ${response.remotePath}.`)
    } catch (uploadError) {
      setError(uploadError instanceof Error ? uploadError.message : 'Mission upload failed.')
    } finally {
      setIsUploading(false)
    }
  }

  if (!currentUser?.isAllowedMissionUpload) {
    return (
      <PageFrame title="Upload mission" eyebrow="Permissions required">
        <Alert severity="warning">Your current roles do not allow mission uploads.</Alert>
      </PageFrame>
    )
  }

  return (
    <PageFrame title="Upload mission" eyebrow="Live API">
      {error ? <Alert severity="warning" sx={{ mb: 3 }}>{error}</Alert> : null}
      {message ? <Alert severity="success" sx={{ mb: 3 }}>{message}</Alert> : null}
      <Card>
        <CardContent>
          <Box component="form" onSubmit={handleSubmit}>
            <Stack spacing={3}>
              <Typography color="text.secondary">
                This flow already calls the existing files API and respects the mission upload permission gate.
              </Typography>
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                <FormControl fullWidth>
                  <InputLabel id="target-label">Target</InputLabel>
                  <Select label="Target" labelId="target-label" onChange={(event: SelectChangeEvent) => setTarget(event.target.value)} value={target}>
                    <MenuItem value="server">Server</MenuItem>
                    <MenuItem value="headless">Headless</MenuItem>
                  </Select>
                </FormControl>
                <FormControl fullWidth disabled={isLoadingFolders}>
                  <InputLabel id="folder-label">Folder</InputLabel>
                  <Select label="Folder" labelId="folder-label" onChange={(event: SelectChangeEvent) => setFolder(event.target.value)} value={folder}>
                    {folders.map((item) => (
                      <MenuItem key={item} value={item}>{item}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Stack>
              <Button component="label" variant="outlined">
                {selectedFile ? selectedFile.name : 'Choose mission file'}
                <input hidden onChange={(event) => setSelectedFile(event.target.files?.[0] ?? null)} type="file" />
              </Button>
              <Button disabled={isUploading} type="submit" variant="contained">
                {isUploading ? 'Uploading...' : 'Upload mission'}
              </Button>
            </Stack>
          </Box>
        </CardContent>
      </Card>
    </PageFrame>
  )
}

function BuildRoleSheetPage() {
  const [document, setDocument] = useState<RoleSheetDocument>(() => createRoleSheetDocument())
  const [message, setMessage] = useState<string | null>(null)

  const updateSquad = (squadId: string, updater: (squad: RoleSheetSquad) => RoleSheetSquad) => {
    setDocument((current) => ({
      ...current,
      squads: updateSquadTree(current.squads, squadId, updater),
    }))
  }

  const updateRole = (squadId: string, roleId: string, updater: (role: RoleSheetRole) => RoleSheetRole) => {
    updateSquad(squadId, (squad) => ({
      ...squad,
      roles: squad.roles.map((role) => (role.id === roleId ? updater(role) : role)),
    }))
  }

  const exportJson = () => {
    downloadTextFile(`${slugify(document.title) || 'role-sheet'}.json`, JSON.stringify(document, null, 2), 'application/json')
    setMessage('Downloaded the role-sheet document as JSON.')
  }

  return (
    <PageFrame title="Build-A-Role-Sheet" eyebrow="Operations planning">
      {message ? <Alert severity="success" sx={{ mb: 1 }}>{message}</Alert> : null}
      <Stack direction={{ xs: 'column', xl: 'row' }} spacing={3} sx={{ alignItems: 'stretch' }}>
        <Card sx={{ flex: 1.1 }}>
          <CardContent>
            <Stack spacing={3}>
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ justifyContent: 'space-between' }}>
                <Box>
                  <Typography variant="h6">Editable composition</Typography>
                  <Typography color="text.secondary">
                    This ports the old MudBlazor tool into typed React state so the preview updates immediately as squads and assignments change.
                  </Typography>
                </Box>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                  <Button onClick={exportJson} startIcon={<DownloadRoundedIcon />} variant="outlined">Download JSON</Button>
                  <Button onClick={() => window.print()} variant="contained">Print preview</Button>
                </Stack>
              </Stack>

              <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                <TextField fullWidth label="Title" onChange={(event) => setDocument((current) => ({ ...current, title: event.target.value }))} value={document.title} />
                <TextField fullWidth label="Author" onChange={(event) => setDocument((current) => ({ ...current, author: event.target.value }))} value={document.author} />
              </Stack>
              <TextField fullWidth label="Description" minRows={2} multiline onChange={(event) => setDocument((current) => ({ ...current, description: event.target.value }))} value={document.description} />
              <TextField fullWidth label="Join order comments" minRows={2} multiline onChange={(event) => setDocument((current) => ({ ...current, joinOrderComments: event.target.value }))} value={document.joinOrderComments} />
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                <TextField fullWidth label="Background color" onChange={(event) => setDocument((current) => ({ ...current, bgColor: event.target.value }))} value={document.bgColor} />
                <TextField fullWidth label="Text accent color" onChange={(event) => setDocument((current) => ({ ...current, textColor: event.target.value }))} value={document.textColor} />
              </Stack>

              <Stack spacing={2}>
                {document.squads.map((squad) => (
                  <SquadEditorCard
                    key={squad.id}
                    depth={0}
                    onAddChild={(parentId) => updateSquad(parentId, (current) => ({ ...current, squads: [...current.squads, createSquad()] }))}
                    onAddRole={(squadId) => updateSquad(squadId, (current) => ({ ...current, roles: [...current.roles, createRole()] }))}
                    onRemoveRole={(squadId, roleId) => updateSquad(squadId, (current) => ({ ...current, roles: current.roles.filter((role) => role.id !== roleId) }))}
                    onRemoveSquad={(squadId) => setDocument((current) => ({ ...current, squads: removeSquadTree(current.squads, squadId) }))}
                    onUpdateRole={updateRole}
                    onUpdateSquad={updateSquad}
                    squad={squad}
                  />
                ))}
                <Button onClick={() => setDocument((current) => ({ ...current, squads: [...current.squads, createSquad()] }))} startIcon={<AddCircleOutlineRoundedIcon />} variant="outlined">
                  Add squad
                </Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        <Card sx={{ flex: 1 }}>
          <CardContent sx={{ p: 0 }}>
            <Box sx={{ bgcolor: '#000', color: '#fff', display: 'grid', gap: 2, gridTemplateColumns: 'auto 1fr auto', p: 3 }}>
              <Box sx={{ alignItems: 'center', bgcolor: 'rgba(255,255,255,0.06)', borderRadius: 2, display: 'flex', height: 72, justifyContent: 'center', width: 112 }}>
                <Typography color="secondary.main" sx={{ fontWeight: 700, letterSpacing: '0.08em' }} variant="body2">
                  OP ART
                </Typography>
              </Box>
              <Box>
                <Typography sx={{ fontWeight: 800 }} variant="h4">{document.title || 'Untitled operation'}</Typography>
                <Typography sx={{ opacity: 0.82 }}>{document.description || 'Describe the operation and intent here.'}</Typography>
              </Box>
              <Box sx={{ alignSelf: 'end', textAlign: 'right' }}>
                <Typography sx={{ opacity: 0.7 }} variant="body2">by</Typography>
                <Typography sx={{ fontWeight: 700 }}>{document.author || 'Unknown author'}</Typography>
              </Box>
            </Box>

            <Box sx={{ backgroundColor: document.bgColor, p: 3 }}>
              <Stack spacing={2}>
                {document.squads.map((squad) => (
                  <SquadPreview key={squad.id} squad={squad} textColor={document.textColor} />
                ))}
              </Stack>
            </Box>

            <Box sx={{ bgcolor: '#000', color: '#fff', display: 'grid', gap: 2, gridTemplateColumns: '1fr auto', p: 3 }}>
              <Box>
                <Typography sx={{ fontStyle: 'italic' }}>All assignments are subject to change depending on attendance.</Typography>
                <Typography sx={{ fontStyle: 'italic' }}>Be flexible if slots move around as the server fills.</Typography>
                {document.joinOrderComments ? <Typography sx={{ fontStyle: 'italic' }}>Join order: {document.joinOrderComments}</Typography> : null}
              </Box>
              <Box sx={{ backgroundColor: document.textColor, borderRadius: 1, minHeight: 72, width: 44 }} />
            </Box>
          </CardContent>
        </Card>
      </Stack>
    </PageFrame>
  )
}

function CommandLineGeneratorPage() {
  const auth = useAuth()
  const api = createApiClient({ accessToken: auth.user?.access_token })
  const [customIdText, setCustomIdText] = useState('')
  const [presetName, setPresetName] = useState('')
  const [mods, setMods] = useState<WorkshopModSummary[]>([])
  const [selectedDlcIds, setSelectedDlcIds] = useState<string[]>([])
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [comparisonWarning, setComparisonWarning] = useState<string | null>(null)
  const [missingServerMods, setMissingServerMods] = useState<string[]>([])
  const [missingHeadlessMods, setMissingHeadlessMods] = useState<string[]>([])
  const [isBusy, setIsBusy] = useState(false)

  const hasResults = mods.length > 0 || selectedDlcIds.length > 0
  const commandLine = buildCommandLine(mods, selectedDlcIds)
  const totalSize = getReadableSize(mods.reduce((sum, mod) => sum + mod.sizeInBytes, 0))

  useEffect(() => {
    let isCancelled = false

    if (mods.length === 0) {
      setMissingServerMods([])
      setMissingHeadlessMods([])
      setComparisonWarning(null)
      return undefined
    }

    ;(async () => {
      try {
        const [serverFolders, headlessFolders] = await Promise.all([
          api.getHavocFolders('server'),
          api.getHavocFolders('headless'),
        ])

        if (isCancelled) {
          return
        }

        const warnings: string[] = []
        setMissingServerMods(serverFolders.isConfigured ? getMissingMods(mods, serverFolders.folders) : [])
        setMissingHeadlessMods(headlessFolders.isConfigured ? getMissingMods(mods, headlessFolders.folders) : [])

        if (!serverFolders.isConfigured) {
          warnings.push('Server FTP target is not configured, so server parity checks are unavailable.')
        }

        if (!headlessFolders.isConfigured) {
          warnings.push('Headless FTP target is not configured, so HC parity checks are unavailable.')
        }

        setComparisonWarning(warnings.length > 0 ? warnings.join(' ') : null)
      } catch (comparisonError) {
        if (!isCancelled) {
          setComparisonWarning(comparisonError instanceof Error ? comparisonError.message : 'Failed to compare installed mods.')
          setMissingServerMods([])
          setMissingHeadlessMods([])
        }
      }
    })()

    return () => {
      isCancelled = true
    }
  }, [auth.user?.access_token, mods])

  const applyPreset = (name: string, nextMods: WorkshopModSummary[], nextDlcIds: string[], successMessage: string) => {
    setPresetName(name)
    setMods(nextMods)
    setSelectedDlcIds(nextDlcIds)
    setError(null)
    setMessage(successMessage)
  }

  const handleFileSelected = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]

    if (!file) {
      return
    }

    setIsBusy(true)
    setError(null)
    setMessage(null)

    try {
      const parsed = parsePresetContent(file.name, await file.text())

      if (parsed.mods.length === 0 && parsed.dlcIds.length === 0) {
        throw new Error('No mod entries were found in the selected preset file.')
      }

      applyPreset(parsed.name, parsed.mods, parsed.dlcIds, `Loaded preset ${parsed.name}.`)
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Failed to parse the selected preset file.')
    } finally {
      event.target.value = ''
      setIsBusy(false)
    }
  }

  const handleGenerateFromIds = async () => {
    const ids = parseWorkshopIds(customIdText)

    if (ids.length === 0) {
      setError('Paste one or more Steam workshop ids to generate a command line.')
      return
    }

    setIsBusy(true)
    setError(null)
    setMessage(null)

    try {
      const response = await api.lookupWorkshopMods(ids)
      const dlcIds = findDlcIds(ids)

      if (response.mods.length === 0 && dlcIds.length === 0) {
        throw new Error('No Arma 3 workshop entries were found for those ids.')
      }

      applyPreset('Ad-hoc workshop selection', response.mods, dlcIds, `Loaded ${response.mods.length} workshop entries.`)
    } catch (lookupError) {
      setError(lookupError instanceof Error ? lookupError.message : 'Failed to generate the command line from workshop ids.')
    } finally {
      setIsBusy(false)
    }
  }

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(commandLine)
      setMessage('Command line copied to the clipboard.')
    } catch (copyError) {
      setError(copyError instanceof Error ? copyError.message : 'Copy to clipboard failed.')
    }
  }

  return (
    <PageFrame title="Command Line Generator" eyebrow="Operations tooling">
      {error ? <Alert severity="warning">{error}</Alert> : null}
      {message ? <Alert severity="success">{message}</Alert> : null}
      {comparisonWarning ? <Alert severity="info">{comparisonWarning}</Alert> : null}

      <Card>
        <CardContent>
          <Stack spacing={3}>
            <Typography color="text.secondary">
              Generate a launcher command line from an exported HTML preset or directly from workshop ids, then compare it against the configured server and headless mod folders.
            </Typography>

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <Button component="label" disabled={isBusy} sx={{ minHeight: 88 }} variant="outlined">
                {isBusy ? 'Working...' : 'Choose preset file (.html or .txt)'}
                <input accept=".html,.txt" hidden onChange={handleFileSelected} type="file" />
              </Button>
              <TextField fullWidth label="Workshop ids" minRows={3} multiline onChange={(event) => setCustomIdText(event.target.value)} placeholder="Paste workshop ids, Steam URLs, or mixed text here." value={customIdText} />
            </Stack>

            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField fullWidth label="Default server command line" value={defaultServerCommandLine} />
              <Button disabled={isBusy} onClick={handleGenerateFromIds} variant="contained">
                Generate from ids
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      {hasResults ? (
        <Stack spacing={3}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <MetricCard label="Preset" value={presetName || 'Ad-hoc selection'} />
            <MetricCard label="Mods" value={`${mods.length}`} />
            <MetricCard label="Approx. size" value={totalSize} />
          </Stack>

          <Card>
            <CardContent>
              <Stack spacing={2.5}>
                <Box>
                  <Typography variant="h6">Required DLCs</Typography>
                  <Typography color="text.secondary">Mirror the original generator by toggling supported Arma 3 CDLCs into the final command line.</Typography>
                </Box>
                <FormGroup>
                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} sx={{ flexWrap: 'wrap' }} useFlexGap>
                    {availableDlcs.map((dlc) => (
                      <FormControlLabel
                        control={<Checkbox checked={selectedDlcIds.includes(dlc.id)} onChange={(event) => setSelectedDlcIds((current) => event.target.checked ? [...current, dlc.id] : current.filter((item) => item !== dlc.id))} />}
                        key={dlc.id}
                        label={dlc.label}
                      />
                    ))}
                  </Stack>
                </FormGroup>
                <TextField fullWidth label="Generated command line" multiline minRows={3} value={commandLine} />
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                  <Button onClick={handleCopy} startIcon={<ContentCopyRoundedIcon />} variant="contained">Copy command line</Button>
                  <Button onClick={() => downloadTextFile(`${slugify(presetName) || 'command-line'}.txt`, commandLine, 'text/plain')} startIcon={<DownloadRoundedIcon />} variant="outlined">
                    Download text
                  </Button>
                </Stack>
              </Stack>
            </CardContent>
          </Card>

          {missingServerMods.length > 0 ? <Alert severity="warning">Missing mods on server: {missingServerMods.join(', ')}</Alert> : null}
          {missingHeadlessMods.length > 0 ? <Alert severity="warning">Missing mods on HC: {missingHeadlessMods.join(', ')}</Alert> : null}

          <Card>
            <CardContent>
              <Typography gutterBottom variant="h6">Resolved workshop entries</Typography>
              <List disablePadding>
                {mods.map((mod) => (
                  <StatusLine key={mod.id} label={mod.title} value={`${mod.id} • ${getReadableSize(mod.sizeInBytes)}`} />
                ))}
              </List>
            </CardContent>
          </Card>
        </Stack>
      ) : null}
    </PageFrame>
  )
}

function AccountPage({ currentUser }: { currentUser: CurrentUser | null }) {
  const auth = useAuth()
  const accountConsoleUrl = buildKeycloakAccountUrl(appConfig.auth.authority)

  return (
    <PageFrame title="Account" eyebrow="Keycloak-backed session">
      <Stack spacing={3}>
        <Card>
          <CardContent>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={3} sx={{ alignItems: { xs: 'flex-start', md: 'center' } }}>
              <Avatar src={currentUser?.avatarUrl || undefined} sx={{ height: 72, width: 72 }} />
              <Box>
                <Typography variant="h5">{currentUser?.globalName ?? 'Unknown user'}</Typography>
                <Typography color="text.secondary">{currentUser?.email ?? 'No email available'}</Typography>
                <Typography color="text.secondary">{currentUser?.userName ?? 'No username available'}</Typography>
              </Box>
            </Stack>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Typography gutterBottom variant="h6">Roles and permissions</Typography>
            <Stack direction="row" spacing={1.25} sx={{ flexWrap: 'wrap' }} useFlexGap>
              {currentUser?.roles.length ? currentUser.roles.map((role) => <Chip key={role} label={role} />) : <Chip label="No mapped roles" variant="outlined" />}
            </Stack>
            <List disablePadding sx={{ mt: 2 }}>
              <StatusLine label="Mission uploads" value={currentUser?.isAllowedMissionUpload ? 'Allowed' : 'Not allowed'} />
              <StatusLine label="Administration" value={currentUser?.isAdmin ? 'Unit organizer privileges' : 'Standard member session'} />
              <StatusLine label="OIDC authority" value={appConfig.auth.authority} />
            </List>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Typography gutterBottom variant="h6">Session and account actions</Typography>
            <List disablePadding>
              <StatusLine label="Discord id" value={currentUser?.id || 'Unavailable'} />
              <StatusLine label="Client" value={appConfig.auth.clientId} />
              <StatusLine label="Access token" value={auth.user?.access_token ? 'Present in browser session' : 'Unavailable'} />
            </List>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ mt: 2 }}>
              <Button href={accountConsoleUrl} rel="noreferrer" startIcon={<OpenInNewRoundedIcon />} target="_blank" variant="outlined">
                Open Keycloak account
              </Button>
              <Button onClick={() => void auth.signoutRedirect()} variant="contained">
                Logout
              </Button>
            </Stack>
          </CardContent>
        </Card>
      </Stack>
    </PageFrame>
  )
}

function SquadEditorCard({
  squad,
  depth,
  onUpdateSquad,
  onRemoveSquad,
  onAddChild,
  onAddRole,
  onUpdateRole,
  onRemoveRole,
}: {
  squad: RoleSheetSquad
  depth: number
  onUpdateSquad: (squadId: string, updater: (squad: RoleSheetSquad) => RoleSheetSquad) => void
  onRemoveSquad: (squadId: string) => void
  onAddChild: (squadId: string) => void
  onAddRole: (squadId: string) => void
  onUpdateRole: (squadId: string, roleId: string, updater: (role: RoleSheetRole) => RoleSheetRole) => void
  onRemoveRole: (squadId: string, roleId: string) => void
}) {
  return (
    <Accordion defaultExpanded={depth === 0} disableGutters sx={{ ml: depth > 0 ? 2 : 0 }}>
      <AccordionSummary expandIcon={<ExpandMoreRoundedIcon />}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', justifyContent: 'space-between', width: '100%' }}>
          <Box>
            <Typography sx={{ fontWeight: 700 }}>{squad.callsign || 'Untitled squad'}</Typography>
            <Typography color="text.secondary" variant="body2">{squad.descriptiveName || 'Define the unit name, radios, and assignments.'}</Typography>
          </Box>
          <IconButton color="error" onClick={(event) => {
            event.stopPropagation()
            onRemoveSquad(squad.id)
          }}>
            <DeleteOutlineRoundedIcon />
          </IconButton>
        </Stack>
      </AccordionSummary>
      <AccordionDetails>
        <Stack spacing={2.5}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField fullWidth label="Callsign" onChange={(event) => onUpdateSquad(squad.id, (current) => ({ ...current, callsign: event.target.value }))} value={squad.callsign} />
            <TextField fullWidth label="Descriptive name" onChange={(event) => onUpdateSquad(squad.id, (current) => ({ ...current, descriptiveName: event.target.value }))} value={squad.descriptiveName} />
          </Stack>
          <TextField fullWidth label="Description" onChange={(event) => onUpdateSquad(squad.id, (current) => ({ ...current, description: event.target.value }))} value={squad.description} />

          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <Stack spacing={1.5} sx={{ flex: 1 }}>
              <Typography variant="subtitle2">Short-range channels</Typography>
              {squad.srRadioChannels.map((channel, index) => (
                <TextField
                  key={`sr-${squad.id}-${index}`}
                  label={`SR ${index + 1}`}
                  onChange={(event) => onUpdateSquad(squad.id, (current) => ({
                    ...current,
                    srRadioChannels: current.srRadioChannels.map((item, channelIndex) => (channelIndex === index ? event.target.value : item)) as [string, string, string],
                  }))}
                  value={channel}
                />
              ))}
            </Stack>
            <Stack spacing={1.5} sx={{ flex: 1 }}>
              <Typography variant="subtitle2">Long-range channels</Typography>
              {squad.lrRadioChannels.map((channel, index) => (
                <TextField
                  key={`lr-${squad.id}-${index}`}
                  label={`LR ${index + 1}`}
                  onChange={(event) => onUpdateSquad(squad.id, (current) => ({
                    ...current,
                    lrRadioChannels: current.lrRadioChannels.map((item, channelIndex) => (channelIndex === index ? event.target.value : item)) as [string, string, string],
                  }))}
                  value={channel}
                />
              ))}
            </Stack>
          </Stack>

          <Stack spacing={1.5}>
            <Typography variant="subtitle2">Roles</Typography>
            {squad.roles.map((role) => (
              <Card key={role.id} variant="outlined">
                <CardContent>
                  <Stack spacing={1.5}>
                    <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
                      <TextField fullWidth label="Role" onChange={(event) => onUpdateRole(squad.id, role.id, (current) => ({ ...current, name: event.target.value }))} value={role.name} />
                      <TextField fullWidth label="Assigned player" onChange={(event) => onUpdateRole(squad.id, role.id, (current) => ({ ...current, assignedPlayer: event.target.value }))} value={role.assignedPlayer} />
                    </Stack>
                    <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
                      <TextField fullWidth label="Icon URL" onChange={(event) => onUpdateRole(squad.id, role.id, (current) => ({ ...current, icon: event.target.value }))} value={role.icon} />
                      <TextField fullWidth label="Notes" onChange={(event) => onUpdateRole(squad.id, role.id, (current) => ({ ...current, description: event.target.value }))} value={role.description} />
                    </Stack>
                    <Button color="error" onClick={() => onRemoveRole(squad.id, role.id)} startIcon={<DeleteOutlineRoundedIcon />} variant="text">
                      Remove role
                    </Button>
                  </Stack>
                </CardContent>
              </Card>
            ))}
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
              <Button onClick={() => onAddRole(squad.id)} startIcon={<AddCircleOutlineRoundedIcon />} variant="outlined">Add role</Button>
              <Button onClick={() => onAddChild(squad.id)} startIcon={<AddCircleOutlineRoundedIcon />} variant="outlined">Add child squad</Button>
            </Stack>
          </Stack>

          {squad.squads.length > 0 ? squad.squads.map((childSquad) => (
            <SquadEditorCard
              depth={depth + 1}
              key={childSquad.id}
              onAddChild={onAddChild}
              onAddRole={onAddRole}
              onRemoveRole={onRemoveRole}
              onRemoveSquad={onRemoveSquad}
              onUpdateRole={onUpdateRole}
              onUpdateSquad={onUpdateSquad}
              squad={childSquad}
            />
          )) : null}
        </Stack>
      </AccordionDetails>
    </Accordion>
  )
}

function SquadPreview({ squad, textColor }: { squad: RoleSheetSquad; textColor: string }) {
  return (
    <Box sx={{ borderLeft: `4px solid ${textColor}`, pl: 2.5 }}>
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ justifyContent: 'space-between' }}>
        <Box sx={{ flex: 1 }}>
          <Typography sx={{ color: textColor, fontWeight: 800, textShadow: '0 2px 8px rgba(0,0,0,0.45)' }} variant="h5">
            {(squad.callsign || 'UNTITLED').toUpperCase()} {squad.descriptiveName ? `• ${squad.descriptiveName}` : ''}
          </Typography>
          {squad.description ? <Typography sx={{ mb: 1.5 }}>{squad.description}</Typography> : null}
          <Stack spacing={1}>
            {squad.roles.map((role) => (
              <Stack direction="row" key={role.id} spacing={1.5} sx={{ alignItems: 'center' }}>
                {role.icon ? <Box alt={role.name} component="img" src={role.icon} sx={{ borderRadius: 1, height: 24, objectFit: 'cover', width: 24 }} /> : <Box sx={{ border: '1px dashed rgba(255,255,255,0.25)', borderRadius: 1, height: 24, width: 24 }} />}
                <Typography sx={{ minWidth: 180 }}>{role.name || 'Unassigned role'}</Typography>
                <Typography color="text.secondary">{role.assignedPlayer || 'Open slot'}</Typography>
              </Stack>
            ))}
          </Stack>
        </Box>

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2.5}>
          <RadioColumn label="SR" values={squad.srRadioChannels} />
          <RadioColumn label="LR" values={squad.lrRadioChannels} />
        </Stack>
      </Stack>

      {squad.squads.length > 0 ? (
        <Stack spacing={1.5} sx={{ mt: 2.5 }}>
          {squad.squads.map((childSquad) => (
            <SquadPreview key={childSquad.id} squad={childSquad} textColor={textColor} />
          ))}
        </Stack>
      ) : null}
    </Box>
  )
}

function RadioColumn({ label, values }: { label: string; values: [string, string, string] }) {
  return (
    <Box>
      <Typography color="text.secondary" sx={{ mb: 1 }} variant="body2">{label} channels</Typography>
      <Stack spacing={0.75}>
        {values.map((value, index) => (
          <Typography key={`${label}-${index}`} variant="body2">{`${label}${index + 1}: ${value || 'TBD'}`}</Typography>
        ))}
      </Stack>
    </Box>
  )
}

function AuthCallbackPage() {
  return <PageFrame title="Finishing sign-in"><LoadingState label="Completing your Keycloak sign-in..." /></PageFrame>
}

function EventForm({
  form,
  onChange,
  onSubmit,
  submitLabel,
}: {
  form: EventFormState
  onChange: (next: EventFormState) => void
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void
  submitLabel: string
}) {
  return (
    <Card>
      <CardContent>
        <Box component="form" onSubmit={onSubmit}>
          <Stack spacing={3}>
            <TextField fullWidth label="Event name" onChange={(event) => onChange({ ...form, name: event.target.value })} required value={form.name} />
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <TextField fullWidth label="Author" onChange={(event) => onChange({ ...form, author: event.target.value })} value={form.author} />
              <TextField fullWidth label="Start time" onChange={(event) => onChange({ ...form, startTime: event.target.value })} required type="datetime-local" value={form.startTime} />
              <TextField fullWidth label="End time" onChange={(event) => onChange({ ...form, endTime: event.target.value })} required type="datetime-local" value={form.endTime} />
            </Stack>
            <TextField fullWidth label="Description" minRows={4} multiline onChange={(event) => onChange({ ...form, description: event.target.value })} value={form.description} />
            <Button type="submit" variant="contained">{submitLabel}</Button>
          </Stack>
        </Box>
      </CardContent>
    </Card>
  )
}

function EventList({ events, emptyMessage }: { events: EventRecord[]; emptyMessage: string }) {
  if (events.length === 0) {
    return <Alert severity="info">{emptyMessage}</Alert>
  }

  return (
    <Stack spacing={2}>
      {events.map((eventRecord) => (
        <Card component={RouterLink} key={eventRecord.id} to={`/events/${eventRecord.id}`} sx={{ color: 'inherit', textDecoration: 'none' }}>
          <CardContent>
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ justifyContent: 'space-between' }}>
              <Box>
                <Typography variant="h6">{eventRecord.name}</Typography>
                <Typography color="text.secondary">{eventRecord.description || 'No description provided.'}</Typography>
              </Box>
              <Stack sx={{ alignItems: { xs: 'flex-start', md: 'flex-end' } }}>
                <Chip label={formatDateTime(eventRecord.startTime)} size="small" sx={{ mb: 1 }} />
                <Typography color="text.secondary">{eventRecord.creatorDiscordUsername}</Typography>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      ))}
    </Stack>
  )
}

function PageFrame({ title, eyebrow, children }: { title: string; eyebrow?: string; children: ReactNode }) {
  return (
    <Stack spacing={3}>
      <Box>
        {eyebrow ? (
          <Typography color="secondary.main" sx={{ fontWeight: 700, letterSpacing: '0.08em', mb: 1, textTransform: 'uppercase' }} variant="body2">
            {eyebrow}
          </Typography>
        ) : null}
        <Typography variant="h4">{title}</Typography>
      </Box>
      {children}
    </Stack>
  )
}

function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <Card sx={{ flex: 1 }}>
      <CardContent>
        <Typography color="text.secondary" variant="body2">{label}</Typography>
        <Typography variant="h6">{value}</Typography>
      </CardContent>
    </Card>
  )
}

function StatusLine({ label, value }: { label: string; value: string }) {
  return (
    <>
      <ListItem disableGutters sx={{ justifyContent: 'space-between' }}>
        <ListItemText primary={label} secondary={value} />
      </ListItem>
      <Divider />
    </>
  )
}

function LoadingState({ label }: { label: string }) {
  return (
    <Stack spacing={2} sx={{ alignItems: 'center', py: 6 }}>
      <CircularProgress />
      <Typography color="text.secondary">{label}</Typography>
    </Stack>
  )
}

function formatDateTime(value: string) {
  return dayjs(value).format('ddd D MMM YYYY, HH:mm')
}

function nullable(value: string) {
  return value.trim() ? value.trim() : null
}

function toLocalDateTime(value: string) {
  return dayjs(value).format('YYYY-MM-DDTHH:mm')
}

function updateSquadTree(
  squads: RoleSheetSquad[],
  squadId: string,
  updater: (squad: RoleSheetSquad) => RoleSheetSquad,
): RoleSheetSquad[] {
  return squads.map((squad) => {
    if (squad.id === squadId) {
      return updater(squad)
    }

    if (squad.squads.length === 0) {
      return squad
    }

    return {
      ...squad,
      squads: updateSquadTree(squad.squads, squadId, updater),
    }
  })
}

function removeSquadTree(squads: RoleSheetSquad[], squadId: string): RoleSheetSquad[] {
  return squads
    .filter((squad) => squad.id !== squadId)
    .map((squad) => ({
      ...squad,
      squads: removeSquadTree(squad.squads, squadId),
    }))
}

function downloadTextFile(fileName: string, content: string, mimeType: string) {
  const blob = new Blob([content], { type: mimeType })
  const objectUrl = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = objectUrl
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(objectUrl)
}

function slugify(value: string) {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

export default App
