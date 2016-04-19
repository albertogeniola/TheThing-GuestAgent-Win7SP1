#include "stdafx.h"
#include "injector.h"
#include "../../InstallerAnalyzer1.1/Common/common.h"
#include <algorithm>

void Log(const char* str) {
	OutputDebugStringA(str);
	fprintf(stdout, "%s\n", str);
}

void LogError(const char* str) {
	OutputDebugStringA(str);
	fprintf(stderr, "%s\n", str);
}

HWND cwHandle;

int WINAPI WinMain(HINSTANCE hInstance,	HINSTANCE hPrevInstance,LPSTR lpCmdLine,int nCmdShow)
{
	// Variables
	STARTUPINFO si;
	PROCESS_INFORMATION pi;
	bool processCreated= false;
	//pcap_t *adhandle;
	//pcap_if_t *alldevs;
	//pcap_if_t * d;
	//char errbuf[PCAP_ERRBUF_SIZE + 1];
	FILE* f;
	DWORD res;
	int argc = 0;
	PCHAR* argv;
	LPWSTR * arglw;
	char strbuff[4096];

	arglw = CommandLineToArgvW(GetCommandLineW(), &argc);

	try{
		Log("-----> INJECTOR: Starting.");
		argv = CommandLineToArgvA(GetCommandLine(), &argc);


		// Checking arguments
		// 1. ProcessPath
		// 2. dll to inject Path
		// 3. Name of the window to PostMessages to
		if (argc != 2)
		{
			LogError("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: Invalid injector usage.");
			exit(-1);
		}
		// 1. ProcessPath: must exist
		if (fopen_s(&f, argv[1], "r") != 0)
		{
			LogError("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: ProcessPath invalid.");
			return -1;
		}
		else
		{
			// File exists, close it now
			fclose(f);
		}

		// 2. Dll to inject
		if (fopen_s(&f, DLLPATH, "r") != 0)
		{
			LogError("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: DLL Path invalid.");
			return -1;
		}
		else
		{
			// File exists, close it now
			fclose(f);
		}

		// 3. Name of the window to send message to
		cwHandle = FindWindow(NULL, GUESTCONTROLLER_WINDOW_NAME);
		// TODO: uncomment me
		if (cwHandle == NULL)
		{
			LogError("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: Cannot find the GuestController window to send message. Please check its name is ");
			return -1;
		}

		Log("[INJECTOR] Args are ok.");

		// Some processes will use DCOMLAUNCHER in order to spawn processes. If we really want to catch them, we should hook that process too. 
		if (!HookAndInjectDCOMLauncher(DCOM_DLL_PATH)) {
			LogError("XXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXX: Cannot hook DCOMLauncher");
			// We do not exit btw.
		}
		else {
			Log("[INJECTOR] DCOM Injection performed! :)");
		}

		// We might be asked to start an MSI file. In that case, we need to use MSIEXEC.
		bool isMsi = isMsiFile(argv[1]);
		std::string msiexec;
		if (isMsi) {
			// retrieve msiexec installation path
			msiexec = getMsiexecPath();
		}
		
		// Prepare arguments for create process
		ZeroMemory(&si, sizeof(STARTUPINFO));
		ZeroMemory(&pi, sizeof(PROCESS_INFORMATION));
		si.cb = sizeof(STARTUPINFO);

		processCreated = false;
		if (isMsi){
			std::string args("/passive /norestart /package ");
			args.append("\"");
			args.append(argv[1]);
			args.append("\"");
			OutputDebugStringA(args.c_str());
			processCreated = MyDetourCreateProcessWithDll(msiexec.c_str(), (char*)args.c_str(), NULL, NULL, TRUE, CREATE_DEFAULT_ERROR_MODE, NULL, NULL, &si, &pi, DLLPATH, NULL);
		} else{
			processCreated = MyDetourCreateProcessWithDll(NULL, argv[1], NULL, NULL, TRUE, CREATE_DEFAULT_ERROR_MODE, NULL, NULL, &si, &pi, DLLPATH, NULL);
		}
		
		if (!processCreated)
		{
			DWORD e = GetLastError();
			LogError("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: DetoursCreateProcessWithDll failed");
			LogError(msiexec.c_str());
			exit(-1);
		}
		

		_snprintf_s(strbuff, _countof(strbuff),"[INJECTOR] INJECTOR: child process created (%u) and DLL injected.", pi.dwProcessId);
		Log(strbuff);
		

		// Wait until the process ends
		Log("[INJECTOR] INJECTOR: Waiting for child process to exit");
		res = WaitForSingleObject(pi.hProcess, INFINITE);
		Log("[INJECTOR] INJECTOR: Child process ended.");
		CloseHandle(pi.hProcess);
		Log("[INJECTOR] INJECTOR: Child process handle closed.");
		return 0;
	}
	catch (int e){
		LogError("Unhandled exception occurred.");
		return -1;
	}
	
}

std::string getMsiexecPath() {
	HKEY hKey;
	DWORD type;
	DWORD cbData;

	if (RegOpenKeyExA(HKEY_LOCAL_MACHINE, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer", 0, KEY_READ, &hKey) != ERROR_SUCCESS)
		throw "Could not open registry key";
	
	if (RegQueryValueExA(hKey, "InstallerLocation", NULL, &type, NULL, &cbData) != ERROR_SUCCESS)
	{
		RegCloseKey(hKey);
		throw "Could not read registry value";
	}

	if (type != REG_SZ)
	{
		RegCloseKey(hKey);
		throw "Incorrect registry value type";
	}

	BYTE *buffer = (BYTE*)LocalAlloc(LPTR, cbData);
	if (RegQueryValueExA(hKey, "InstallerLocation", NULL, NULL, buffer, &cbData) != ERROR_SUCCESS)
	{
		RegCloseKey(hKey);
		throw "Could not read registry value";
	}

	RegCloseKey(hKey);

	std::string result((char*)buffer);
	result.append("msiexec.exe");
	LocalFree(buffer);

	return result;
}

bool isMsiFile(char* filepath) {
	std::string ext = std::string(filepath);
	int pos = ext.find_last_of('.');
	if (pos != std::string::npos) {
		
		// Found an extension. Extract the extension. This will clear the previous value
		ext = ext.substr(pos);
		std::transform(ext.begin(), ext.end(), ext.begin(), ::tolower);
		// Check if the extension matches MSI
		return ext.compare(".msi") == 0;
	}

	return false;
}

int injectIntoPID(int process, const char* dll)
{
	HANDLE hThread;
	DWORD pid = (DWORD)process;
	char strbuff[512];

	//Gets the process handle for the target process
	HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
	if (OpenProcess == NULL)
	{
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "XXXXXX Injector Error: cannot find PID %d.", process);
		LogError(strbuff);
		return FALSE;
	}

	//Retrieves kernel32.dll module handle for getting loadlibrary base address
	HMODULE hModule = GetModuleHandle("kernel32.dll");
	if (hModule == NULL){
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "XXXXXX Injector Error: cannot get module Kernel32.dll.");
		LogError(strbuff);
		return FALSE;
	}

	//Gets address for LoadLibraryA in kernel32.dll
	LPVOID lpBaseAddress = (LPVOID)GetProcAddress(hModule, "LoadLibraryA");
	if (lpBaseAddress == NULL)
	{
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "Unable to locate LoadLibraryA.");
		LogError(strbuff);
		return FALSE;
	}

	//Allocates space inside for inject.dll to our target process
	LPVOID lpSpace = (LPVOID)VirtualAllocEx(hProcess, NULL, strlen(dll), MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
	if (lpSpace == NULL)
	{
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "Could not allocate memory in process %u", (int)process);
		LogError(strbuff);
		return FALSE;
	}

	//Write inject.dll to memory of process
	int n = WriteProcessMemory(hProcess, lpSpace, dll, strlen(dll), NULL);
	if (n == 0)
	{
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "Could not write to process's address space");
		LogError(strbuff);
		return FALSE;
	}


	hThread = RtlCreateUserThread(hProcess, lpBaseAddress, lpSpace);
	if (hThread == NULL)
	{
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "Invocation of RtlCreateUserThread to start the injected lib failed with err: %u",err);
		LogError(strbuff);
		return FALSE;
	}
	else
	{
		DWORD threadId = GetThreadId(hThread);
		DWORD processId = GetProcessIdOfThread(hThread);
		sprintf_s(strbuff, sizeof(strbuff), "Injected thread id: %u for pid: %u", threadId, processId);
		Log(strbuff);
		CloseHandle(hProcess);
		return TRUE;
	}
}

BOOL HookAndInjectDCOMLauncher(const char* dllPath){

	HANDLE hProcess = NULL;
	PWSTR allocatedAddr = NULL;
	HANDLE hThread = NULL;
	DWORD dwProcessId; /*TODOOOOO*/
	SC_HANDLE schSCManager;
	SC_HANDLE dcomLauncherService;
	SERVICE_STATUS_PROCESS srvStatus;
	DWORD dwBytesNeeded;
	char strbuff[512];
	
	// Look for the DcomLaunch process. We need its process handler in order to inject the DLL into it. 
	// We need a service manager in order to query the services.
	schSCManager = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);
	if (schSCManager == NULL) {
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "XXXXXX Injector Error: Cannot open service manager error: %d", err);
		LogError(strbuff);
		return FALSE;
	}

	dcomLauncherService = OpenService(
		schSCManager,						// SCM database 
		DCOM_LAUNCH_SERVICE_NAME,           // name of service 
		SERVICE_QUERY_STATUS);				// full access 

	if (dcomLauncherService == NULL) {
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "XXXXXX Injector Error: Cannot open service, error: %d", err);
		LogError(strbuff);
		CloseServiceHandle(schSCManager);
		return FALSE;
	}
	
	ZeroMemory(&srvStatus, sizeof(srvStatus));

	// At this point we want to know which is the PID of the process running this service.
	if (!QueryServiceStatusEx(dcomLauncherService, SC_STATUS_PROCESS_INFO, (LPBYTE)&srvStatus, sizeof(SERVICE_STATUS_PROCESS), &dwBytesNeeded)){
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "XXXXXX Injector Error: Cannot retrieve status info about dcomService, error: %d", err);
		LogError(strbuff);
		CloseServiceHandle(schSCManager);
		return FALSE;
	}

	// We finally have the pid now. Copy into a local variable.
	dwProcessId = srvStatus.dwProcessId;

	// We don't need this anymore.
	CloseServiceHandle(schSCManager);

	// We need to acquire debug permissions before starting remote thread
	if (!setDebugPrivileges()) {
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "XXXXXX Injector Error: Cannot acquire debug privileges: %d", err);
		LogError(strbuff);
		return FALSE;
	}

	sprintf_s(strbuff, sizeof(strbuff), "Process ID of dcomLauncher is %d", dwProcessId);
	Log(strbuff);

	// Get the handle of the running dcomlauncher process
	return injectIntoPID(dwProcessId, dllPath);
}

BOOL setDebugPrivileges()
{
	HANDLE hToken;
	TOKEN_PRIVILEGES tp;
	LUID luid;

	if (!OpenProcessToken(
		GetCurrentProcess(),
		TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES,
		&hToken))
	{
		LogError("OpenProcessToken error.");
		return FALSE;
	}

	if (!LookupPrivilegeValue(
		NULL,            // lookup privilege on local system
		SE_DEBUG_NAME,   // privilege to lookup
		&luid))        // receives LUID of privilege
	{
		LogError("LookupPrivilegeValue error.");
		return FALSE;
	}

	tp.PrivilegeCount = 1;
	tp.Privileges[0].Luid = luid;
	tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

	// Enable the privilege

	if (!AdjustTokenPrivileges(
		hToken,
		FALSE,
		&tp,
		sizeof(TOKEN_PRIVILEGES),
		(PTOKEN_PRIVILEGES)NULL,
		(PDWORD)NULL))
	{
		LogError("AdjustTokenPrivileges error.");
		return FALSE;
	}

	if (GetLastError() == ERROR_NOT_ALL_ASSIGNED)

	{
		LogError("The token does not have the specified privilege.");
		return FALSE;
	}

	return TRUE;
}


PCHAR*
CommandLineToArgvA(
PCHAR CmdLine,
int* _argc
)
{
	PCHAR* argv;
	PCHAR  _argv;
	ULONG   len;
	ULONG   argc;
	CHAR   a;
	ULONG   i, j;

	BOOLEAN  in_QM;
	BOOLEAN  in_TEXT;
	BOOLEAN  in_SPACE;

	len = strlen(CmdLine);
	i = ((len + 2) / 2)*sizeof(PVOID)+sizeof(PVOID);

	argv = (PCHAR*)GlobalAlloc(GMEM_FIXED,
		i + (len + 2)*sizeof(CHAR));

	_argv = (PCHAR)(((PUCHAR)argv) + i);

	argc = 0;
	argv[argc] = _argv;
	in_QM = FALSE;
	in_TEXT = FALSE;
	in_SPACE = TRUE;
	i = 0;
	j = 0;

	while (a = CmdLine[i]) {
		if (in_QM) {
			if (a == '\"') {
				in_QM = FALSE;
			}
			else {
				_argv[j] = a;
				j++;
			}
		}
		else {
			switch (a) {
			case '\"':
				in_QM = TRUE;
				in_TEXT = TRUE;
				if (in_SPACE) {
					argv[argc] = _argv + j;
					argc++;
				}
				in_SPACE = FALSE;
				break;
			case ' ':
			case '\t':
			case '\n':
			case '\r':
				if (in_TEXT) {
					_argv[j] = '\0';
					j++;
				}
				in_TEXT = FALSE;
				in_SPACE = TRUE;
				break;
			default:
				in_TEXT = TRUE;
				if (in_SPACE) {
					argv[argc] = _argv + j;
					argc++;
				}
				_argv[j] = a;
				j++;
				in_SPACE = FALSE;
				break;
			}
		}
		i++;
	}
	_argv[j] = '\0';
	argv[argc] = NULL;

	(*_argc) = argc;
	return argv;
}


BOOL WINAPI MyDetourCreateProcessWithDll(LPCSTR lpApplicationName,
	__in_z LPSTR lpCommandLine,
	LPSECURITY_ATTRIBUTES lpProcessAttributes,
	LPSECURITY_ATTRIBUTES lpThreadAttributes,
	BOOL bInheritHandles,
	DWORD dwCreationFlags,
	LPVOID lpEnvironment,
	LPCSTR lpCurrentDirectory,
	LPSTARTUPINFOA lpStartupInfo,
	LPPROCESS_INFORMATION lpProcessInformation,
	LPCSTR lpDllName,
	PDETOUR_CREATE_PROCESS_ROUTINEA pfCreateProcessA)
{
	DWORD dwMyCreationFlags = (dwCreationFlags | CREATE_SUSPENDED);
	PROCESS_INFORMATION pi;

	if (pfCreateProcessA == NULL) {
		pfCreateProcessA = CreateProcessA;
	}

	if (!pfCreateProcessA(lpApplicationName,
		lpCommandLine,
		lpProcessAttributes,
		lpThreadAttributes,
		bInheritHandles,
		dwMyCreationFlags,
		lpEnvironment,
		lpCurrentDirectory,
		lpStartupInfo,
		&pi)) {
		return FALSE;
	}

	LPCSTR rlpDlls[2];
	DWORD nDlls = 0;
	if (lpDllName != NULL) {
		rlpDlls[nDlls++] = lpDllName;
	}

	if (!DetourUpdateProcessWithDll(pi.hProcess, rlpDlls, nDlls)) {
		TerminateProcess(pi.hProcess, ~0u);
		return FALSE;
	}

	if (lpProcessInformation) {
		CopyMemory(lpProcessInformation, &pi, sizeof(pi));
	}

	// Notify the GuestController we have spawned the process
	notifyNewPid(pi.dwProcessId);

	if (!(dwCreationFlags & CREATE_SUSPENDED)) {
		ResumeThread(pi.hThread);
	}
	return TRUE;
}

void notifyNewPid(DWORD pid)
{
	DWORD res = 0;
	COPYDATASTRUCT ds;

	ds.dwData = COPYDATA_PROC_SPAWNED;
	ds.cbData = sizeof(DWORD);
	ds.lpData = (PVOID)&pid;

	// Send message...
	SendMessage(cwHandle, WM_COPYDATA, 0, (LPARAM)&ds);

}


HANDLE RtlCreateUserThread(
	HANDLE hProcess,
	LPVOID lpBaseAddress,
	LPVOID lpSpace
	)
{
	//The prototype of RtlCreateUserThread from undocumented.ntinternals.com	
	typedef DWORD(WINAPI * functypeRtlCreateUserThread)(
		HANDLE 					ProcessHandle,
		PSECURITY_DESCRIPTOR 	SecurityDescriptor,
		BOOL 					CreateSuspended,
		ULONG					StackZeroBits,
		PULONG					StackReserved,
		PULONG					StackCommit,
		LPVOID					StartAddress,
		LPVOID					StartParameter,
		HANDLE 					ThreadHandle,
		LPVOID					ClientID

		);

	//Get handle for ntdll which contains RtlCreateUserThread
	HANDLE hRemoteThread = NULL;
	HMODULE hNtDllModule = GetModuleHandle("ntdll.dll");

	if (hNtDllModule == NULL)
	{
		return NULL;
	}

	functypeRtlCreateUserThread funcRtlCreateUserThread = (functypeRtlCreateUserThread)GetProcAddress(hNtDllModule, "RtlCreateUserThread");

	if (!funcRtlCreateUserThread)
	{
		return NULL;
	}

	funcRtlCreateUserThread(hProcess, NULL, 0, 0, 0, 0, lpBaseAddress, lpSpace, &hRemoteThread, NULL);
	DWORD lastError = GetLastError();
	return hRemoteThread;
}