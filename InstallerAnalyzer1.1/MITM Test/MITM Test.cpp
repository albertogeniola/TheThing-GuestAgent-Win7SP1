#include "stdafx.h"
#define WK_MITM_EVENT _T("wk_mitm_event")
#define REQUEST_FILE_NAME _T("mitm.log")
#define LOCAL_LOG_FILE_NAME _T("local_info.log")
#define SIMPLE_MODE_SWITCH _T("--WK_SIMPLE_MODE")

static std::wstring simple_switch = std::wstring(SIMPLE_MODE_SWITCH);

BOOL IsElevated() {
	BOOL fRet = FALSE;
	HANDLE hToken = NULL;
	if (OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken)) {
		TOKEN_ELEVATION Elevation;
		DWORD cbSize = sizeof(TOKEN_ELEVATION);
		if (GetTokenInformation(hToken, TokenElevation, &Elevation, sizeof(Elevation), &cbSize)) {
			fRet = Elevation.TokenIsElevated;
		}
	}
	if (hToken) {
		CloseHandle(hToken);
	}
	return fRet;
}

int _tmain(int argc, _TCHAR* argv[]) {
	/*
	 This executable is padded with optional data. At the very end of it, a 4 bytes unsigned integer is added.
	 That integer represents the number of bytes, corresponding to UTF-8 encoding, that have beed aded to the executable.

	 | PE-Data | xxxxx utf-8 data xxxxx | 4 bytes unsinged integer |

	 By reading that binary data it is possible to extract extra data we added.

	*/

	TCHAR module_path[MAX_PATH];
	TCHAR tempdir[MAX_PATH];
	FILE* f = NULL;
	char * data = NULL;
	unsigned __int32 data_len = 0;
	unsigned int file_len = 0;
	int i = 0;
	bool simple_mode = false;
	TCHAR request_file_path[MAX_PATH];

	i = GetModuleFileName(NULL, module_path, MAX_PATH);
	module_path[i] = '\0';

	i = GetTempPath(MAX_PATH, tempdir);
	tempdir[i] = '\0';

	/*
	std::wstring t = std::wstring();
	t.append(std::to_wstring(GetCurrentProcessId()));	
	MessageBox(NULL, t.c_str(), L"TEST", MB_OK);
	*/

	// Check if the "simple" switch was specified
	for (int j = 1; j < argc; j++) {
		std::wstring input = std::wstring(argv[j]);

		if (input.compare(simple_switch)==0) {
			simple_mode = true;
		}
	}

	if (!simple_mode) {
		

		f = _tfopen(module_path, _T("rb"));
		if (f == NULL) {
			printf("Error, cannot open myself. Code %i", GetLastError());
			system("pause");
			return -1;
		}

		// Now seek to the last 4 bytes of the stream and read the int value
		if (fseek(f, -4, SEEK_END) != 0) {
			printf("Error, seek to last 4 bytes of file. Code %i", GetLastError());
			system("pause");
			return -1;
		}

		file_len = ftell(f);

		if (fread(&data_len, 4, 1, f) != 1) {
			printf("Error, cannot read data_len. Code %i", GetLastError());
			system("pause");
			return -1;
		}

		// At this point "extract" data from executable and write it to external file
		data = new char[data_len + 1];
		fseek(f, file_len - data_len, SEEK_SET);
		if (fread(data, data_len, 1, f) != 1) {
			printf("Error, cannot read data. Code %i", GetLastError());
			system("pause");
			return -1;
		}
		data[data_len] = '\0';
		fclose(f);

		// Now just write the buffer into a local file that will be used by GuestController as "source of info"
		PathCombine(request_file_path, tempdir, REQUEST_FILE_NAME);
		f = _tfopen(request_file_path, _T("wb"));
		fwrite(data, data_len, 1, f);
		fflush(f);
		fclose(f);

		delete data;
	}

	// Now fill other info, such as permissions owned by this process and other stuff.
	request_file_path[0] = '\0';
	PathCombine(request_file_path, tempdir, LOCAL_LOG_FILE_NAME);
	f = _tfopen(request_file_path, _T("wb"));
	std::wofstream ofs = std::wofstream(request_file_path, std::ofstream::out);
	// First line: get our current pid
	ofs << std::to_wstring(GetCurrentProcessId()) << std::endl;
	// Second line: check whether we are running under elevated privileges.
	ofs << IsElevated() << std::endl;
	// Third line: filename/path used by the executable
	ofs << module_path << std::endl;
	ofs.close();

	// Finally notify the GuestController that we are done, by simply rising the relative AutoresetEvent.
	HANDLE evt = NULL;
	evt = OpenEvent(EVENT_MODIFY_STATE | SYNCHRONIZE, FALSE, WK_MITM_EVENT);
	
	if (evt == NULL) {
		printf("Error OpenEvent, code %i", GetLastError());
		system("pause");
		return -1;
	}

	if (SetEvent(evt) == 0) {
		printf("Error SetEvent, code %i", GetLastError());
		system("pause");
		return -2;
	}

	return 0;
}