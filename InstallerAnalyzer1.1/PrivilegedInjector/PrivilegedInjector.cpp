// PrivilegedInjector.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <Windows.h>
#define DCOM_LAUNCH_SERVICE_NAME "DcomLaunch"
#define DLL_PATH L"C:\\users\\bot\\desktop\\CHookingDll.dll"

HANDLE MyCreateRemoteThread(HANDLE hProcess, LPVOID lpRemoteThreadStart, LPVOID lpRemoteCallback);


typedef NTSTATUS(WINAPI *PFN_NtCreateThreadEx)
(
OUT PHANDLE hThread,
IN ACCESS_MASK DesiredAccess,
IN LPVOID ObjectAttributes,
IN HANDLE ProcessHandle,
IN LPTHREAD_START_ROUTINE lpStartAddress,
IN LPVOID lpParameter,
IN BOOL CreateSuspended,
IN ULONG StackZeroBits,
IN ULONG SizeOfStackCommit,
IN ULONG SizeOfStackReserve,
OUT LPVOID lpBytesBuffer
);

BOOL setDebugPrivileges();


struct NtCreateThreadExBuffer
{
	ULONG Size;
	ULONG Unknown1;
	ULONG Unknown2;
	PULONG Unknown3;
	ULONG Unknown4;
	ULONG Unknown5;
	ULONG Unknown6;
	PULONG Unknown7;
	ULONG Unknown8;
};

void Log(const char* str) {
	OutputDebugStringA(str);
	fprintf(stdout, "%s\n", str);
}

void LogError(const char* str) {
	OutputDebugStringA(str);
	fprintf(stderr, "%s\n", str);
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	CHAR   DllPath[MAX_PATH] = { 0 };
	HANDLE hProcess = NULL;
	PWSTR allocatedAddr = NULL;
	HANDLE hThread = NULL;
	DWORD dwProcessId; /*TODOOOOO*/
	SC_HANDLE schSCManager;
	SC_HANDLE dcomLauncherService;
	SERVICE_STATUS_PROCESS srvStatus;
	DWORD dwBytesNeeded;
	char strbuff[256];

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
		TEXT("DcomLaunch"),           // name of service 
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
	setDebugPrivileges();

	sprintf_s(strbuff, sizeof(strbuff), "Process ID of dcomLauncher is %d", dwProcessId);
	Log(strbuff);

	// Get the handle of the running dcomlauncher process
	hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwProcessId);
	if (hProcess == INVALID_HANDLE_VALUE) {
		LogError("XXXXXX Injector Error: Cannot open DCOMLauncher process");
		return FALSE;
	}

	// Calculate the numebr of bytes needed for the dll string length
	int cch = 1 + lstrlenW(DLL_PATH);
	int cb = cch * sizeof(wchar_t);

	// Allocate spece into the target process
	allocatedAddr = (PWSTR)VirtualAllocEx(hProcess, NULL, cb, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
	if (allocatedAddr == NULL) {
		LogError("XXXXXX Injector Error: Cannot allocate memory on DCOMLauncher process");
		return FALSE;
	}

	// Now copy the name of the dll into the allocated space
	if (!WriteProcessMemory(hProcess, allocatedAddr, (PVOID)DLL_PATH, cb, NULL)){
		LogError("XXXXXX Injector Error: Cannot copy the DLL path into DCOMLauncher process addressspace");
		return FALSE;
	}

	// Retrieve the address of LoadLibraryW in Kernel32
	PTHREAD_START_ROUTINE pfnThreadRtn = (PTHREAD_START_ROUTINE)GetProcAddress(GetModuleHandle(TEXT("Kernel32")), "LoadLibraryW");
	if (pfnThreadRtn == NULL){
		LogError("XXXXXX Injector Error: Cannot retrieve LoadLibraryW address");
		return FALSE;
	}

	// Create the remote thread into the DCOMLauncher process. Unfortunately the standard Win32 API CreateRemoteThread doesn't work with privileged processes (such as DCOMLaunch services).
	// For this reason I'm using low level API, ntdll api.
	hThread = MyCreateRemoteThread(hProcess, pfnThreadRtn, allocatedAddr);
	//hThread = CreateRemoteThread(hProcess, NULL, 0, pfnThreadRtn, allocatedAddr, 0, NULL);
	if (hThread == NULL){
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "XXXXXX Injector Error: Cannot create remote thread, error: %d", err);
		LogError(strbuff);
		return FALSE;
	}

	WaitForSingleObject(hThread, INFINITE);

	return TRUE;



	return 0;


}

HANDLE MyCreateRemoteThread(HANDLE hProcess, LPVOID pfnThreadRun, LPVOID remoteArg)
{
	char strbuff[256];
	HANDLE hThread;
	PFN_NtCreateThreadEx pfnNtCreateThreadEx = (PFN_NtCreateThreadEx)GetProcAddress(GetModuleHandle(TEXT("ntdll.dll")), "NtCreateThreadEx");
	if (pfnNtCreateThreadEx == NULL) {
		sprintf_s(strbuff, sizeof(strbuff), "The token does not have the specified privilege. \n");
		LogError(strbuff);
		return INVALID_HANDLE_VALUE;
	}
	NtCreateThreadExBuffer ntCTB;
	memset(&ntCTB, 0, sizeof(NtCreateThreadExBuffer));
	DWORD temp1 = 0;
	DWORD temp2 = 0;

	ntCTB.Size = sizeof(NtCreateThreadExBuffer);
	ntCTB.Unknown1 = 0x10003;
	ntCTB.Unknown2 = 0x8;
	ntCTB.Unknown3 = &temp2;
	ntCTB.Unknown4 = 0;
	ntCTB.Unknown5 = 0x10004;
	ntCTB.Unknown6 = 4;
	ntCTB.Unknown7 = &temp1;
	ntCTB.Unknown8 = 0;

	NTSTATUS status = pfnNtCreateThreadEx(
		&hThread,
		0x1FFFFF,
		NULL,
		hProcess,
		(LPTHREAD_START_ROUTINE)pfnThreadRun,
		remoteArg,
		FALSE,
		NULL,
		NULL,
		NULL,
		&ntCTB);
	WaitForSingleObject(hThread, INFINITE);
	return hThread;


	/*
	HANDLE hThread = NULL;

	HMODULE modNtDll = GetModuleHandle("ntdll.dll");
	if (!modNtDll)
	{
	LogError("Cannot load ntdll module");
	return NULL;
	}

	LPFUN_NtCreateThreadEx funNtCreateThreadEx = (LPFUN_NtCreateThreadEx)GetProcAddress(modNtDll, "NtCreateThreadEx");
	if (!funNtCreateThreadEx)
	{
	LogError("Cannot load NtCreateThreadEx() address");
	return NULL;
	}

	//setup and initialize the buffer
	NtCreateThreadExBuffer ntbuffer;

	memset(&ntbuffer, 0, sizeof(NtCreateThreadExBuffer));
	DWORD temp1 = 0;
	DWORD temp2 = 0;

	ntbuffer.Size = sizeof(NtCreateThreadExBuffer);
	ntbuffer.Unknown1 = 0x10003;
	ntbuffer.Unknown2 = 0x8;
	ntbuffer.Unknown3 = &temp2;
	ntbuffer.Unknown4 = 0;
	ntbuffer.Unknown5 = 0x10004;
	ntbuffer.Unknown6 = 4;
	ntbuffer.Unknown7 = &temp1;
	ntbuffer.Unknown8 = 0;

	NTSTATUS status = funNtCreateThreadEx(
	&hThread,
	0x1FFFFF,
	NULL,
	hProcess,
	(LPTHREAD_START_ROUTINE)addressOfFunction,
	functionStringAddress,
	FALSE, //start instantly
	NULL,
	NULL,
	NULL,
	&ntbuffer
	);

	if (hThread == NULL)
	{
	LogError("Cannot create remote thread.");
	return NULL;
	}

	//Wait for thread to complete....
	WaitForSingleObject(hThread, INFINITE);

	//Check the return code from remote thread function
	int dwExitCode;
	if (GetExitCodeThread(hThread, (DWORD*)&dwExitCode))
	{
	printf("\n Remote thread returned with status = %d", dwExitCode);
	}

	CloseHandle(hThread);

	return NULL;
	*/
}

BOOL setDebugPrivileges()
{
	HANDLE hToken;
	TOKEN_PRIVILEGES tp;
	LUID luid;
	char strbuff[256];


	if (!OpenProcessToken(
		GetCurrentProcess(),
		TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES,
		&hToken))
	{
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "OpenProcessToken error : %u", err);
		LogError(strbuff);
		return FALSE;
	}

	if (!LookupPrivilegeValue(
		NULL,            // lookup privilege on local system
		SE_DEBUG_NAME,   // privilege to lookup
		&luid))        // receives LUID of privilege
	{
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "LookupPrivilegeValue error : %u", err);
		LogError(strbuff);
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
		DWORD err = GetLastError();
		sprintf_s(strbuff, sizeof(strbuff), "AdjustTokenPrivileges error : %u", err);
		LogError(strbuff);
		return FALSE;
	}

	if (GetLastError() == ERROR_NOT_ALL_ASSIGNED)

	{
		sprintf_s(strbuff, sizeof(strbuff), "The token does not have the specified privilege. \n");
		LogError(strbuff);
		return FALSE;
	}

	return TRUE;
}