from oauth2client.service_account import ServiceAccountCredentials
import gspread

from .sheets_config import keyvar, scope, sheet_name

creds = ServiceAccountCredentials.from_json_keyfile_dict(
    keyfile_dict=keyvar, scopes=scope)  # type: ignore
client = gspread.authorize(creds)  # type: ignore

sheets = client.open(sheet_name).worksheets()
sheet1 = sheets[0]
archive_sheet = sheets[1]
dlc_sheet = sheets[2]
entire_sheet = sheet1.get_all_values()


def get_cell_entry(row: int, column: int):
    return entire_sheet[row-1][column-1]


def set_cell_entry(row: int, column: int, value: str):
    entire_sheet[row-1][column-1] = value


def update_local_sheet():
    global entire_sheet
    entire_sheet = sheet1.get_all_values()


def update_online_sheet():
    sheet1.update(entire_sheet)
