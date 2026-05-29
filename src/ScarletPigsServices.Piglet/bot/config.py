from dotenv import load_dotenv
import os

load_dotenv()

DLC_REFRESH_DEBOUNCE_SECONDS = 5

DISCORD_TOKEN = os.getenv("DISCORD_TOKEN")
GITHUB_TOKEN = os.getenv("GITHUB_TOKEN")
CREATOR_ID = os.getenv("CREATOR_ID")
SERVER_IP = os.getenv("SERVER_IP")
SERVER_PORT = os.getenv("SERVER_PORT")
SCARLETPIGS_API_URL = os.getenv("SCARLETPIGS_API") or 'None'

PRIVATE_KEY = os.getenv('PRIVATE_KEY')
if PRIVATE_KEY is None:
    raise ValueError("PRIVATE_KEY environment variable is not set.")

GOOGLE_SHEET_NAME = os.getenv("GOOGLE_SHEET_NAME")
if GOOGLE_SHEET_NAME is None:
    raise ValueError("GOOGLE_SHEET_NAME environment variable is not set.")

GOOGLE_KEY = {
    "type": os.getenv('TYPE'),
    "project_id": os.getenv('PROJECT_ID'),
    "private_key_id": os.getenv('PRIVATE_KEY_ID'),
    "private_key": PRIVATE_KEY.replace('\\n', '\n'),
    "client_email": os.getenv('CLIENT_EMAIL'),
    "client_id": os.getenv('CLIENT_ID'),
    "auth_uri": os.getenv('AUTH_URI'),
    "token_uri": os.getenv('TOKEN_URI'),
    "auth_provider_x509_cert_url": os.getenv('AUTH_PROVIDER_X509_CERT_URL'),
    "client_x509_cert_url": os.getenv('CLIENT_X509_CERT_URL')
}

GOOGLE_SCOPE = [
    'https://www.googleapis.com/auth/spreadsheets',
    'https://www.googleapis.com/auth/drive.file',
    'https://www.googleapis.com/auth/drive',
]