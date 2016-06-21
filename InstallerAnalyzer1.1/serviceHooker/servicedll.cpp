#include "servicedll.h"
#include "stdafx.h"

typedef std::wstring string;

template <typename Type>
bool Hook(Type* realFunction, void* hookingFunction, HMODULE module, const char* function_name) {
	char buff[512];

	// Assign the pointer to the real function
	(*realFunction) = (Type)(GetProcAddress(module, function_name));

	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourAttach(&(PVOID&)(*realFunction), hookingFunction);
	if (DetourTransactionCommit() != NO_ERROR) {
		sprintf_s(buff, "[SERVICES_HOOKER DLL] %s not derouted correctly", function_name);
		OutputDebugStringA(buff);
		return false;
	}
	else {
		sprintf_s(buff, "[SERVICES_HOOKER DLL] %s atteched OK", function_name);
		OutputDebugStringA(buff);
		return true;
	}
}

template <typename Type>
bool UnHook(Type* realFunction, void* hookingFunction, const char* function_name) {
	char buff[512];

	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourDetach(&(PVOID&)(*realFunction), hookingFunction);
	if (DetourTransactionCommit() != NO_ERROR) {
		sprintf_s(buff, "[SERVICES_HOOKER DLL] %s not detached correctly", function_name);
		OutputDebugStringA(buff);
		return false;
	}
	else {
		sprintf_s(buff, "[SERVICES_HOOKER DLL] %s detached OK", function_name);
		OutputDebugStringA(buff);
		return true;
	}
}

HMODULE kern32dllmod;

INT APIENTRY DllMain(HMODULE hDLL, DWORD Reason, LPVOID Reserved)
{
	
	string tmplog;

	// Now, according to the reason this method has been called, register or unregister the DLL
	switch (Reason)
	{
	case DLL_THREAD_ATTACH:
		OutputDebugString(_T("SERVICES: THREAD ATTACHED TO DLL."));
		break;
	case DLL_THREAD_DETACH:
		OutputDebugString(_T("SERVICES: THREAD DETACHED FROM DLL."));
		break;
	case DLL_PROCESS_ATTACH:

		kern32dllmod = GetModuleHandle(TEXT("kernel32.dll"));

		tmplog.append(_T("SERVICES: Attached to process"));
		tmplog.append(std::to_wstring(GetCurrentProcessId()));

		DisableThreadLibraryCalls(hDLL);

		//wsprintf(msgbuf, _T("Attached to process %d."), GetCurrentProcessId());
		OutputDebugString(tmplog.c_str());

		// Set the error mode to NONE so we do not get annoying UI
		SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX);
		_set_abort_behavior(0, _WRITE_ABORT_MSG);

		Hook(&realCreateProcessInternalW, MyCreateProcessInternalW, kern32dllmod, "CreateProcessInternalW");
		
		// Note: in the other DLL I call notifyPid(). In this case this isn't needed: we don't want to kep track of this process.

		break;
	case DLL_PROCESS_DETACH:
		OutputDebugString(_T("SERVICES: PROCESS DETACHED FROM DLL."));

		DisableThreadLibraryCalls(hDLL);

		UnHook(&realCreateProcessInternalW, MyCreateProcessInternalW, "CreateProcessInternalW");
		
		break;
	}

	return TRUE;
}

BOOL WINAPI MyCreateProcessInternalW(HANDLE hToken,
	LPCWSTR lpApplicationName,
	LPWSTR lpCommandLine,
	LPSECURITY_ATTRIBUTES lpProcessAttributes,
	LPSECURITY_ATTRIBUTES lpThreadAttributes,
	BOOL bInheritHandles,
	DWORD dwCreationFlags,
	LPVOID lpEnvironment,
	LPCWSTR lpCurrentDirectory,
	LPSTARTUPINFOW lpStartupInfo,
	LPPROCESS_INFORMATION lpProcessInformation,
	PHANDLE hNewToken)
{

	// This is the API that gets eventually called by all the others. Ansi params get converted into wide characters, so the A version is useless.
	CHAR   DllPath[MAX_PATH] = { 0 };
	OutputDebugString(_T("SERVICES: MyCreateProcessInternalW"));
	GetModuleFileNameA((HINSTANCE)&__ImageBase, DllPath, _countof(DllPath));
	BOOL processCreated;

	// Save the previous value of the creation flags and make sure we add the create suspended BIT
	DWORD originalFlags = dwCreationFlags;
	dwCreationFlags = dwCreationFlags | CREATE_SUSPENDED;
	processCreated = realCreateProcessInternalW(hToken, lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation, hNewToken);
	if (processCreated) {
		// Allocate enough memory on the new process
		LPVOID baseAddress = (LPVOID)VirtualAllocEx(lpProcessInformation->hProcess, NULL, strlen(DllPath) + 1, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);

		// Copy the code to be injected
		WriteProcessMemory(lpProcessInformation->hProcess, baseAddress, DllPath, strlen(DllPath), NULL);

		OutputDebugStringA("-----> SERVICES: DLL copied into host process memory space");

		HANDLE loadLibraryAddress = GetProcAddress(kern32dllmod, "LoadLibraryA");
		if (loadLibraryAddress == NULL)
		{
			OutputDebugStringW(_T("SERVICES: !!!!!LOADLIB IS NULL"));
			//error
			return 0;
		}
		else {
			OutputDebugStringW(_T("SERVICES: LOAD LIB OK"));
		}

		// Create a remote thread the remote thread
		HANDLE  threadHandle = CreateRemoteThread(lpProcessInformation->hProcess, NULL, 0, (LPTHREAD_START_ROUTINE)loadLibraryAddress, baseAddress, NULL, 0);
		if (threadHandle == NULL) {
			OutputDebugStringW(_T("SERVICES: !!!!REMTOE THREAD NOT OK"));
		}
		else {
			OutputDebugStringW(_T("SERVICES: !!!!REMTOE OK"));
		}
		OutputDebugStringA("-----> SERVICES: Remote thread created");


		// Check if the process was meant to be stopped. If not, resume it now
		if ((originalFlags & CREATE_SUSPENDED) != CREATE_SUSPENDED) {
			// need to start it right away
			ResumeThread(lpProcessInformation->hThread);
			OutputDebugStringA("-----> SERVICES: Thread resumed");
		}
	}

	return processCreated;

}