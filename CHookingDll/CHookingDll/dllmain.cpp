#include "stdafx.h"
#include "dllmain.h"
#include "HandleMap.h"

/* Global variables. 
 * Each Porcess that loads this DLL will have its own copy of these. 
 * However we should recall that each thread shares access to those variables
 * so it cruacial that we keep all of them in READ ONLY.
 */
HWND cwHandle;
HMODULE ntdllmod;
HMODULE kern32dllmod;
HMODULE wsmod;
HMODULE ws2mod;
HandleMap handleMap;

// Index of TLS used by threads in order to retrieve the hooking count.
// This has to be global so that any thread in this Process can refer to that.
static DWORD dwTlsIndex; 

// The following switch enables/disables SYSCALL logging/notification to GuestController.
#undef SYSCALL_LOG

template <typename Type>
bool Hook(Type* realFunction, void* hookingFunction, HMODULE module, const char* function_name) {
	char buff[512];

	// Assign the pointer to the real function
	(*realFunction) = (Type)(GetProcAddress(module, function_name));
	
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourAttach(&(PVOID&)(*realFunction), hookingFunction);
	if (DetourTransactionCommit() != NO_ERROR) {
		sprintf_s(buff, "[CHOOKING DLL] %s not derouted correctly", function_name);
		OutputDebugStringA(buff);
		return false;
	}
	else {
		sprintf_s(buff, "[CHOOKING DLL] %s atteched OK", function_name);
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
		sprintf_s(buff, "[CHOOKING DLL] %s not detached correctly", function_name);
		OutputDebugStringA(buff);
		return false;
	}
	else {
		sprintf_s(buff, "[CHOOKING DLL] %s detached OK", function_name);
		OutputDebugStringA(buff);
		return true;
	}
}

/*
 * This is the Main DLL Entry. Detours will execute this code after the DLL has been injected.
 * In this function we will get real function addresses and store them into relatives static
 * variables. After that, we will setup the new derouted addresses.
 * When DLL is going to be unloaded, we will unload the hooks.
 */
INT APIENTRY DllMain(HMODULE hDLL, DWORD Reason, LPVOID Reserved)
{
	// Get the module in which there is the function to deroute, which is NTDLL.DLL
	ntdllmod = GetModuleHandle(TEXT("ntdll.dll"));
	kern32dllmod = GetModuleHandle(TEXT("kernel32.dll"));
	wsmod = LoadLibrary(TEXT("wsock32.dll"));
	ws2mod = LoadLibrary(TEXT("ws2_32.dll"));
	
	string tmplog;

	// Now, according to the reason this method has been called, register or unregister the DLL
	switch (Reason)
	{
	case DLL_THREAD_ATTACH:
		OutputDebugString(_T("THREAD ATTACHED TO DLL."));
		break;
	case DLL_THREAD_DETACH:
		OutputDebugString(_T("THREAD DETACHED FROM DLL."));
		break;
	case DLL_PROCESS_ATTACH:
		
		// Initialize the TLS index. Every allocated slot is guaranteed to be initialized to 0. So we don't need to initialize them.
		if ((dwTlsIndex = TlsAlloc()) == TLS_OUT_OF_INDEXES)
			return FALSE;

		tmplog.append(_T("Attached to process"));
		tmplog.append(to_string(GetCurrentProcessId()));

		DisableThreadLibraryCalls(hDLL);

		//wsprintf(msgbuf, _T("Attached to process %d."), GetCurrentProcessId());
		OutputDebugString(tmplog.c_str());
		
		// Set the error mode to NONE so we do not get annoying UI
		SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX);
		_set_abort_behavior(0, _WRITE_ABORT_MSG);

		// The following is needed for performing the lookup of opened query keys
		realNtQueryKey = (pNtQueryKey)(GetProcAddress(ntdllmod, "NtQueryKey"));

		Hook(&realNtCreateFile, MyNtCreateFile, ntdllmod, "NtCreateFile");
		Hook(&realNtOpenFile,MyNtOpenFile,ntdllmod, "NtOpenFile");
		Hook(&realNtDeleteFile,MyNtDeleteFile,ntdllmod, "NtDeleteFile");
		Hook(&realNtCreateKey, MyNtCreateKey,ntdllmod, "NtCreateKey");
		Hook(&realNtOpenKey, MyNtOpenKey,ntdllmod, "NtOpenKey");
		Hook(&realNtSetInformationFile, MyNtSetInformationFile,ntdllmod, "NtSetInformationFile");
		Hook(&realNtClose, MyNtClose,ntdllmod, "NtClose");
		Hook(&realCreateProcessInternalW, MyCreateProcessInternalW,kern32dllmod, "CreateProcessInternalW");
		Hook(&realExitProcess, MyExitProcess, kern32dllmod, "ExitProcess");

		
		// Supplementay Hooks
		//Hook(&realNtOpenDirectoryObject,MyNtOpenDirectoryObject,ntdllmod, "NtOpenDirectoryObject");
		//Hook(&realNtDeleteKey, MyNtDeleteKey,ntdllmod, "NtDeleteKey");
		//Hook(&realNtQueryKey, MyNtQueryKey,ntdllmod, "NtQueryKey");
		//Hook(&realNtDeleteValueKey, MyNtDeleteValueKey,ntdllmod, "NtDeleteValueKey");
		//Hook(&realNtEnumerateKey, MyNtEnumerateKey,ntdllmod, "NtEnumerateKey");
		//Hook(&realNtEnumerateValueKey, MyNtEnumerateValueKey,ntdllmod, "NtEnumerateValueKey");
		//Hook(&realNtLockFile, MyNtLockFile,ntdllmod, "NtLockFile");
		//Hook(&realNtQueryDirectoryFile, MyNtQueryDirectoryFile,ntdllmod, "NtQueryDirectoryFile");
		//Hook(&realNtQueryFullAttributesFile, MyNtQueryFullAttributesFile,ntdllmod, "NtQueryFullAttributesFile");
		//Hook(&realNtQueryValueKey, MyNtQueryValueKey,ntdllmod, "NtQueryValueKey");
		//Hook(&realNtSetValueKey, MyNtSetValueKey,ntdllmod, "NtSetValueKey");
		//Hook(&realNtTerminateProcess, MyNtTerminateProcess,ntdllmod, "NtTerminateProcess");
		//Hook(&realNtOpenProcess,MyNtOpenProcess,ntdllmod, "NtOpenProcess");
		//realCreateProcessInternalA = (pCreateProcessInternalA,kern32dllmod, "CreateProcessInternalA"));
		//Hook(&realNtQueryInformationProcess = (pNtQueryInformationProcess,ntdllmod, "NtQueryInformationProcess"));

		// Old code
		/*
		// Assign the address location of the function to the static pointer
		realNtCreateFile = (pNtCreateFile)(GetProcAddress(ntdllmod, "NtCreateFile"));
		realNtOpenFile = (pNtOpenFile)(GetProcAddress(ntdllmod, "NtOpenFile"));
		realNtDeleteFile = (pNtDeleteFile)(GetProcAddress(ntdllmod, "NtDeleteFile"));
		realNtOpenDirectoryObject = (pNtOpenDirectoryObject)(GetProcAddress(ntdllmod, "NtOpenDirectoryObject"));
		realNtCreateKey = (pNtCreateKey)(GetProcAddress(ntdllmod, "NtCreateKey"));
		realNtOpenKey = (pNtOpenKey)(GetProcAddress(ntdllmod, "NtOpenKey"));
		realNtDeleteKey = (pNtDeleteKey)(GetProcAddress(ntdllmod, "NtDeleteKey"));
		realNtQueryKey = (pNtQueryKey)(GetProcAddress(ntdllmod, "NtQueryKey"));
		realNtDeleteValueKey = (pNtDeleteValueKey)(GetProcAddress(ntdllmod, "NtDeleteValueKey"));
		realNtEnumerateKey = (pNtEnumerateKey)(GetProcAddress(ntdllmod, "NtEnumerateKey"));
		realNtEnumerateValueKey = (pNtEnumerateValueKey)(GetProcAddress(ntdllmod, "NtEnumerateValueKey"));
		realNtLockFile = (pNtLockFile)(GetProcAddress(ntdllmod, "NtLockFile"));
		//realNtOpenProcess = (pNtOpenProcess)(GetProcAddress(ntdllmod, "NtOpenProcess"));
		realNtQueryDirectoryFile = (pNtQueryDirectoryFile)(GetProcAddress(ntdllmod, "NtQueryDirectoryFile"));
		realNtQueryFullAttributesFile = (pNtQueryFullAttributesFile)(GetProcAddress(ntdllmod, "NtQueryFullAttributesFile"));
		realNtQueryValueKey = (pNtQueryValueKey)(GetProcAddress(ntdllmod, "NtQueryValueKey"));
		realNtSetInformationFile = (pNtSetInformationFile)(GetProcAddress(ntdllmod, "NtSetInformationFile"));
		realNtSetValueKey = (pNtSetValueKey)(GetProcAddress(ntdllmod, "NtSetValueKey"));
		realNtTerminateProcess = (pNtTerminateProcess)(GetProcAddress(ntdllmod, "NtTerminateProcess"));
		realNtClose = (pNtClose)(GetProcAddress(ntdllmod, "NtClose"));

		realCreateProcessInternalW = (pCreateProcessInternalW)(GetProcAddress(kern32dllmod, "CreateProcessInternalW"));
		//realCreateProcessInternalA = (pCreateProcessInternalA)(GetProcAddress(kern32dllmod, "CreateProcessInternalA"));

		realNtQueryInformationProcess = (pNtQueryInformationProcess)(GetProcAddress(ntdllmod, "NtQueryInformationProcess"));
		realExitProcess = (pExitProcess)ExitProcess;

		// Hook the functions now!
		// NtCreateFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtCreateFile, MyNtCreateFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtCreateFile not derouted correctly"));
		else
			OutputDebugString(_T("NtCreateFile successful"));

		// NtOpenFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtOpenFile, MyNtOpenFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtOpenFile not derouted correctly"));
		else
			OutputDebugString(_T("NtOpenFile successful"));

		// NtDeleteFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtDeleteFile, MyNtDeleteFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtDeleteFile not derouted correctly"));
		else
			OutputDebugString(_T("NtDeleteFile successful"));

		// NtDeleteFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtOpenDirectoryObject, MyNtOpenDirectoryObject);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtOpenDirectoryObject not derouted correctly"));
		else
			OutputDebugString(_T("NtOpenDirectoryObject successful"));

		// NtCreateKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtCreateKey, MyNtCreateKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtCreateKey not derouted correctly"));
		else
			OutputDebugString(_T("NtCreateKey successful"));

		// NtOpenKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtOpenKey, MyNtOpenKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtOpenKey not derouted correctly"));
		else
			OutputDebugString(_T("NtOpenKey successful"));

		// NtDeleteKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtDeleteKey, MyNtDeleteKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtDeleteKey not derouted correctly"));
		else
			OutputDebugString(_T("NtDeleteKey successful"));

		// NtQueryKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtQueryKey, MyNtQueryKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryKey not derouted correctly"));
		else
			OutputDebugString(_T("NtQueryKey successful"));

		// NtDeleteValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtDeleteValueKey, MyNtDeleteValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtDeleteValueKey not derouted correctly"));
		else
			OutputDebugString(_T("NtDeleteValueKey successful"));

		// NtEnumerateKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtEnumerateKey, MyNtEnumerateKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtEnumerateKey not derouted correctly"));
		else
			OutputDebugString(_T("NtEnumerateKey successful"));

		// NtEnumerateValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtEnumerateValueKey, MyNtEnumerateValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtEnumerateValueKey not derouted correctly"));
		else
			OutputDebugString(_T("NtEnumerateValueKey successful"));

		// NtLockFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtLockFile, MyNtLockFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtLockFile not derouted correctly"));
		else
			OutputDebugString(_T("NtLockFile successful"));

		//DetourAttach(&(PVOID&)realNtOpenProcess, MyNtOpenProcess);

		// NtQueryDirectoryFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtQueryDirectoryFile, MyNtQueryDirectoryFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryDirectoryFile not derouted correctly"));
		else
			OutputDebugString(_T("NtQueryDirectoryFile successful"));

		// NtQueryFullAttributesFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtQueryFullAttributesFile, MyNtQueryFullAttributesFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryFullAttributesFile not derouted correctly"));
		else
			OutputDebugString(_T("NtQueryFullAttributesFile successful"));

		// NtQueryValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtQueryValueKey, MyNtQueryValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryValueKey not derouted correctly"));
		else
			OutputDebugString(_T("NtQueryValueKey successful"));

		// NtSetInformationFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtSetInformationFile, MyNtSetInformationFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtSetInformationFile not derouted correctly"));
		else
			OutputDebugString(_T("NtSetInformationFile successful"));

		// NtSetValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtSetValueKey, MyNtSetValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtSetValueKey not derouted correctly"));
		else
			OutputDebugString(_T("NtSetValueKey successful"));

		// NtTerminateProcess
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtTerminateProcess, MyNtTerminateProcess);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtTerminateProcess not derouted correctly"));
		else
			OutputDebugString(_T("NtTerminateProcess successful"));

		// NtClose
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtClose, MyNtClose);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtClose not derouted correctly"));
		else
			OutputDebugString(_T("NtClose successfully"));

		// CreateProcessInternalW
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realCreateProcessInternalW, MyCreateProcessInternalW);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("CreateProcessInternalW not derouted correctly"));
		else
			OutputDebugString(_T("CreateProcessInternalW successful"));

		// ExitProcess
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realExitProcess, MyExitProcess);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("ExitProcess not derouted correctly"));
		else
			OutputDebugString(_T("ExitProcess successful"));
		*/

		notifyNewPid(0, GetCurrentProcessId());
		

		break;
	case DLL_PROCESS_DETACH:
		OutputDebugString(_T("PROCESS DETACHED FROM DLL."));
		
		DisableThreadLibraryCalls(hDLL);
		
		UnHook(&realNtCreateFile, MyNtCreateFile, "NtCreateFile");
		UnHook(&realNtOpenFile, MyNtOpenFile, "NtOpenFile");
		UnHook(&realNtDeleteFile, MyNtDeleteFile, "NtDeleteFile");
		UnHook(&realNtCreateKey, MyNtCreateKey,  "NtCreateKey");
		UnHook(&realNtOpenKey, MyNtOpenKey,  "NtOpenKey");
		UnHook(&realNtSetInformationFile, MyNtSetInformationFile, "NtSetInformationFile");
		UnHook(&realNtClose, MyNtClose, "NtClose");
		UnHook(&realCreateProcessInternalW, MyCreateProcessInternalW, "CreateProcessInternalW");
		UnHook(&realExitProcess, MyExitProcess, "ExitProcess");

		// Supplementay Hooks
		//UnHook(&realNtSetValueKey, MyNtSetValueKey, "NtSetValueKey");
		//UnHook(&realNtTerminateProcess, MyNtTerminateProcess, "NtTerminateProcess");
		//UnHook(&realNtOpenDirectoryObject, MyNtOpenDirectoryObject, "NtOpenDirectoryObject");
		//UnHook(&realNtDeleteKey, MyNtDeleteKey,  "NtDeleteKey");
		//UnHook(&realNtQueryKey, MyNtQueryKey,  "NtQueryKey");
		//UnHook(&realNtDeleteValueKey, MyNtDeleteValueKey,  "NtDeleteValueKey");
		//UnHook(&realNtEnumerateKey, MyNtEnumerateKey,  "NtEnumerateKey");
		//UnHook(&realNtEnumerateValueKey, MyNtEnumerateValueKey,  "NtEnumerateValueKey");
		//UnHook(&realNtLockFile, MyNtLockFile, "NtLockFile");
		//UnHook(&realNtQueryDirectoryFile, MyNtQueryDirectoryFile,  "NtQueryDirectoryFile");
		//UnHook(&realNtQueryFullAttributesFile, MyNtQueryFullAttributesFile, "NtQueryFullAttributesFile");
		//UnHook(&realNtQueryValueKey, MyNtQueryValueKey, "NtQueryValueKey");

		// Old Code
		/*
		// NtClose
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtClose, MyNtClose);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtClose not detached correctly"));
		else
			OutputDebugString(_T("NtClose detached successfully"));
		
		// NtCreateFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtCreateFile, MyNtCreateFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtCreateFile not detached correctly"));
		else
			OutputDebugString(_T("NtCreateFile detached successfully"));

		// NtOpenFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtOpenFile, MyNtOpenFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtOpenFile not detached correctly"));
		else
			OutputDebugString(_T("NtOpenFile detached successfully"));


		// DeleteFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtDeleteFile, MyNtDeleteFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtDeleteFile not detached correctly"));
		else
			OutputDebugString(_T("NtDeleteFile detached successfully"));


		// NtOpenDirectoryObject
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtOpenDirectoryObject, MyNtOpenDirectoryObject);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtOpenDirectoryObject not detached correctly"));
		else
			OutputDebugString(_T("NtOpenDirectoryObject detached successfully"));

		
		// NtCreateKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtCreateKey, MyNtCreateKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtCreateKey not detached correctly"));
		else
			OutputDebugString(_T("NtCreateKey detached successfully"));

		
		// NtOpenKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtOpenKey, MyNtOpenKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtOpenKey not detached correctly"));
		else
			OutputDebugString(_T("NtOpenKey detached successfully"));

		
		// NtDeleteKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtDeleteKey, MyNtDeleteKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtDeleteKey not detached correctly"));
		else
			OutputDebugString(_T("NtDeleteKey detached successfully"));

		
				
		// NtQueryKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtQueryKey, MyNtQueryKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryKey not detached correctly"));
		else
			OutputDebugString(_T("NtQueryKey detached successfully"));

		
		// NtDeleteValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtDeleteValueKey, MyNtDeleteValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtDeleteValueKey not detached correctly"));
		else
			OutputDebugString(_T("NtDeleteValueKey detached successfully"));

		
		
		// NtEnumerateKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtEnumerateKey, MyNtEnumerateKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtEnumerateKey not detached correctly"));
		else
			OutputDebugString(_T("NtEnumerateKey detached successfully"));

		
		
		
		// NtEnumerateValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtEnumerateValueKey, MyNtEnumerateValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtEnumerateValueKey not detached correctly"));
		else
			OutputDebugString(_T("NtEnumerateValueKey detached successfully"));
		
		
		// NtLockFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtLockFile, MyNtLockFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtLockFile not detached correctly"));
		else
			OutputDebugString(_T("NtLockFile detached successfully"));

		//DetourDetach(&(PVOID&)realNtOpenProcess, MyNtOpenProcess);
		
		
		// NtQueryDirectoryFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtQueryDirectoryFile, MyNtQueryDirectoryFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryDirectoryFile not detached correctly"));
		else
			OutputDebugString(_T("NtQueryDirectoryFile detached successfully"));

		
		// NtQueryFullAttributesFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtQueryFullAttributesFile, MyNtQueryFullAttributesFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryFullAttributesFile not detached correctly"));
		else
			OutputDebugString(_T("NtQueryFullAttributesFile detached successfully"));

		// NtQueryValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtQueryValueKey, MyNtQueryValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryValueKey not detached correctly"));
		else
			OutputDebugString(_T("NtQueryValueKey detached successfully"));

		// NtSetInformationFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtSetInformationFile, MyNtSetInformationFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtSetInformationFile not detached correctly"));
		else
			OutputDebugString(_T("NtSetInformationFile detached successfully"));

		// NtSetValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtSetValueKey, MyNtSetValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtSetValueKey not detached correctly"));
		else
			OutputDebugString(_T("NtSetValueKey detached successfully"));


		// NtTerminateProcess
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtTerminateProcess, MyNtTerminateProcess);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtTerminateProcess not detached correctly"));
		else
			OutputDebugString(_T("NtTerminateProcess detached successfully"));

		// CreateProcessInternalW
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realCreateProcessInternalW, MyCreateProcessInternalW);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("CreateProcessInternalW not detached correctly"));
		else
			OutputDebugString(_T("CreateProcessInternalW detached successfully"));
		*/
		break;
	}

	return TRUE;
}

/*
	>>>>>>>>>>>>>>> HOOKs <<<<<<<<<<<<<<<
*/
NTSTATUS WINAPI MyNtCreateFile(PHANDLE FileHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PIO_STATUS_BLOCK IoStatusBlock, PLARGE_INTEGER AllocationSize, ULONG FileAttributes, ULONG ShareAccess, ULONG CreateDisposition, ULONG CreateOptions, PVOID EaBuffer, ULONG EaLength)
{
	if (!shouldIntercept())
		return realNtCreateFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock, AllocationSize, FileAttributes, ShareAccess, CreateDisposition, CreateOptions, EaBuffer, EaLength);

	incHookingDepth();

	// If the handle has write access, it is a good idea to calculate the hash before the attached thread changes the file itself.
	// To do so, we send a message to the GuestController everytime we see an NtOpenFile with write access. The Guest controller
	// will then try to open the file and store, in its report, the hash of the file before any modification. After that, the Guest
	// controller will answer to the SendMessage and this thread will continue (yeah, SendMessage blocks until the sender eats the message from the pump).
	string s = GetFullPathByObjectAttributes(ObjectAttributes);
	if (IsRequestingWriteAccess(DesiredAccess))
		NotifyFileAccess(s, WK_FILE_CREATED);
		
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtCreateFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock, AllocationSize, FileAttributes, ShareAccess, CreateDisposition, CreateOptions, EaBuffer, EaLength);
	
	// If the file has been created successfully, keep track of it.
	if (res == 0)
		handleMap.Insert(*FileHandle, s);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;
	pugi::xml_node element = doc.append_child(_T("NtCreateFile"));
	
	// >>>>>>>>>>>>>>> ObjectAttributes (File Path) <<<<<<<<<<<<<<<
	s.clear();
	element.addAttribute(_T("Path"), s.c_str());

	// >>>>>>>>>>>>>>> AccessMask (File Path) <<<<<<<<<<<<<<<
	element.addAttribute(_T("AccessMask"), StandardAccessMaskToString(DesiredAccess).c_str());
	
	// >>>>>>>>>>>>>>> IO_STATUS_BLOCK <<<<<<<<<<<<<<<
	// Write IO status block: parse its 
	// value and write it to the xml node
	s.clear();
	IoStatusToString(IoStatusBlock, &s);
	element.addAttribute(_T("IoStatusBlock"), s.c_str());

	// >>>>>>>>>>>>>>> AllocationSize <<<<<<<<<<<<<<<
	// Skipping allocation size: I don't need it
	
	// >>>>>>>>>>>>>>> FileAttributes <<<<<<<<<<<<<<<
	s.clear();
	FileAttributesToString(FileAttributes, &s);
	element.addAttribute(_T("FileAttributes"), s.c_str());

	// >>>>>>>>>>>>>>> ShareAccess <<<<<<<<<<<<<<<
	s.clear();
	ShareAccessToString(ShareAccess, &s);
	element.addAttribute(_T("ShareAccess"), s.c_str());

	// Create Disposition
	s.clear();
	s = CreateDispositionToString(CreateDisposition);
	element.addAttribute(_T("CreateDisposition"), s.c_str());

	// >>>>>>>>>>>>>>> CreateOptions <<<<<<<<<<<<<<<
	s.clear();
	FileCreateOptionsToString(CreateOptions, &s);
	element.addAttribute(_T("CreateOptions"), s.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	s.clear();
	NtStatusToString(res, &s);
	element.addAttribute(_T("Result"), s.c_str());

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), *FileHandle);
		element.addAttribute(_T("Handle"), buff);
	}
	
	log(&element);
	#endif

	decHookingDepth();

	return res;
	
}
NTSTATUS WINAPI MyNtOpenFile(PHANDLE FileHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PIO_STATUS_BLOCK IoStatusBlock, ULONG ShareAccess, ULONG OpenOptions)
{
	if (!shouldIntercept())
		return realNtOpenFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock, ShareAccess, OpenOptions);

	incHookingDepth();

	// If the handle has write access, it is a good idea to calculate the hash before the attached thread changes the file itself.
	// To do so, we send a message to the GuestController everytime we see an NtOpenFile with write access. The Guest controller
	// will then try to open the file and store, in its report, the hash of the file before any modification. After that, the Guest
	// controller will answer to the SendMessage and this thread will continue (yeah, SendMessage blocks until the sender eats the message from the pump).
	string s = GetFullPathByObjectAttributes(ObjectAttributes);
	if (IsRequestingWriteAccess(DesiredAccess))
		NotifyFileAccess(s, WK_FILE_OPENED);
	
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock, ShareAccess, OpenOptions);

	if (res == 0)
		handleMap.Insert(*FileHandle, s);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtOpenFile"));

	// >>>>>>>>>>>>>>> File Path <<<<<<<<<<<<<<<
	// The objectname contains a full path to the file
	element.addAttribute(_T("Path"), s.c_str());
	
	// >>>>>>>>>>>>>>> DESIRED ACCESS <<<<<<<<<<<<<<<
	element.addAttribute(_T("AccessMask"), StandardAccessMaskToString(DesiredAccess).c_str());

	// >>>>>>>>>>>>>>> IO STATUS BLOCK <<<<<<<<<<<<<<<
	s.clear();
	IoStatusToString(IoStatusBlock, &s);
	element.addAttribute(_T("IoStatusBlock"), s.c_str());
	
	// >>>>>>>>>>>>>>> SHARE ACCESS <<<<<<<<<<<<<<<
	s.clear();
	ShareAccessToString(ShareAccess, &s);
	element.addAttribute(_T("ShareAccess"), s.c_str());

	// >>>>>>>>>>>>>>> OPEN OPTIONS <<<<<<<<<<<<<<<
	s.clear();
	FileCreateOptionsToString(OpenOptions, &s);
	element.addAttribute(_T("OpenOptions"), s.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	s.clear();
	NtStatusToString(res, &s);
	element.addAttribute(_T("Result"), s.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), *FileHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	#endif

	decHookingDepth();

	return res;
}
NTSTATUS WINAPI MyNtDeleteFile(POBJECT_ATTRIBUTES ObjectAttributes)
{
	if (!shouldIntercept())
		return realNtDeleteFile(ObjectAttributes);

	incHookingDepth();

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtDeleteFile(ObjectAttributes);

	// Ok we notify our component only after the call has happened, so the GuestController will understand file has been deleted
	NotifyFileAccess(GetFullPathByObjectAttributes(ObjectAttributes), WK_FILE_DELETED);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtDeleteFile"));

	string s = string();
	// >>>>>>>>>>>>>>> File Path <<<<<<<<<<<<<<<
	if (ObjectAttributes->RootDirectory != NULL)
	{
		// The path specified in ObjectName is relative to the directory handle. Get the path of that directory
		s.clear();
		GetHandleFileName(ObjectAttributes->RootDirectory, &s);
		element.addAttribute(_T("DirPath"), s.c_str());
		element.addAttribute(_T("Path"), ObjectAttributes->ObjectName->Buffer);
	}
	else
	{
		// The objectname contains a full path to the file
		s.clear();
		from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
		element.addAttribute(_T("Path"), s.c_str());
	}

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	s.clear();
	NtStatusToString(res, &s);
	element.addAttribute(_T("Result"), s.c_str());

	log(&element);
	#endif

	decHookingDepth();

	return res;

}
NTSTATUS WINAPI MyNtOpenDirectoryObject(PHANDLE DirectoryObject, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes)
{
	if (!shouldIntercept())
		return realNtOpenDirectoryObject(DirectoryObject, DesiredAccess, ObjectAttributes);

	incHookingDepth();

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenDirectoryObject(DirectoryObject, DesiredAccess, ObjectAttributes);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtOpenDirectoryObject"));
	
	string s = string();

	// >>>>>>>>>>>>>>> File Path <<<<<<<<<<<<<<<
	s.clear();
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
	element.addAttribute(_T("Path"), s.c_str());

	// >>>>>>>>>>>>>>> DESIRED ACCESS <<<<<<<<<<<<<<<
	/*
	s.clear();
	DirectoryAccessMaskToString(DesiredAccess, &s);
	element.addAttribute(_T("DesiredAccess"), s.c_str());
	*/
	element.addAttribute(_T("DesiredAccess"), StandardAccessMaskToString(DesiredAccess).c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	s.clear();
	NtStatusToString(res, &s);
	element.addAttribute(_T("Result"), s.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), *DirectoryObject);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);
	#endif

	decHookingDepth();

	return res;
}
NTSTATUS WINAPI MyNtOpenKey(PHANDLE KeyHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes)
{
	if (!shouldIntercept())
		return realNtOpenKey(KeyHandle, DesiredAccess, ObjectAttributes);

	incHookingDepth();

	// We need to notify the GuestController that this process is going to manipulate, somehow, this key. By sending a synch. notification
	// before any operation happens on the key, we give a chance to the GuestController to retrieve the original value of the key before any
	// change happens. When the process is done, the GuestController will compare original values with the final ones.
	// For this reason we just need to notify it when we open Keys with write mode.
	string s = GetKeyPathFromOA(ObjectAttributes);
	if (IsRequestingRegistryWriteAccess(DesiredAccess)) {
		OutputDebugStringW(ObjectAttributes->ObjectName->Buffer);
		NotifyRegistryAccess(s, WK_KEY_OPENED);
	}

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenKey(KeyHandle, DesiredAccess, ObjectAttributes);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtOpenKey"));
	string w = string();

	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	w.clear();
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &w);
	element.addAttribute(_T("Path"), w.c_str());

	// >>>>>>>>>>>>>>> Access Mask <<<<<<<<<<<<<<<
	/*
	w.clear();
	KeyAccessMaskToString(DesiredAccess, &w);
	element.addAttribute(_T("DesiredAccess"), w.c_str());
	*/
	element.addAttribute(_T("DesiredAccess"), StandardAccessMaskToString(DesiredAccess).c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w.clear();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), *KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);
	#endif

	decHookingDepth();

	return res;
}
NTSTATUS WINAPI MyNtCreateKey(PHANDLE KeyHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, ULONG TitleIndex, PUNICODE_STRING fullpath, ULONG CreateOptions, PULONG Disposition)
{
	if (!shouldIntercept())
		return realNtCreateKey(KeyHandle, DesiredAccess, ObjectAttributes, TitleIndex, fullpath, CreateOptions, Disposition);

	incHookingDepth();

	// Notify the GuestController the process wants to create a key
	/*
	string s = GetFullPathByObjectAttributes(ObjectAttributes);
	if (IsRequestingRegistryWriteAccess(DesiredAccess))
		NotifyRegistryAccess(s, WK_KEY_CREATED);
	*/
	if (IsRequestingRegistryWriteAccess(DesiredAccess)) {
		std::wstring s;
		s = GetKeyPathFromOA(ObjectAttributes);
		NotifyRegistryAccess(s, WK_KEY_CREATED);
	}

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtCreateKey(KeyHandle, DesiredAccess, ObjectAttributes, TitleIndex, fullpath, CreateOptions, Disposition);
	
	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtCreateKey"));
	string w = string();
	
	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &w);
	element.addAttribute(_T("Path"), w.c_str());
	
	// >>>>>>>>>>>>>>> Access Mask <<<<<<<<<<<<<<<
	/*
	w.clear();
	KeyAccessMaskToString(DesiredAccess, &w);
	element.addAttribute(_T("DesiredAccess"), w.c_str());
	*/
	element.addAttribute(_T("DesiredAccess"), StandardAccessMaskToString(DesiredAccess).c_str());

	// >>>>>>>>>>>>>>> Class <<<<<<<<<<<<<<<
	if (Class != NULL)
	{
		from_unicode_to_wstring(Class, &w);
		element.addAttribute(_T("Class"), w.c_str());
	}
		
	
	// >>>>>>>>>>>>>>> Create Options <<<<<<<<<<<<<<<
	w.clear();
	KeyCreateOptionsToString(CreateOptions, &w);
	element.addAttribute(_T("CreateOptions"), w.c_str());
	
	// >>>>>>>>>>>>>>> Disposition <<<<<<<<<<<<<<<
	if (Disposition != NULL)
	{
		if (*Disposition == REG_CREATED_NEW_KEY)
			element.addAttribute(_T("Disposition"), _T("REG_CREATED_NEW_KEY"));
		else if (*Disposition == REG_OPENED_EXISTING_KEY)
			element.addAttribute(_T("Disposition"), _T("REG_OPENED_EXISTING_KEY"));
	}
	else
		element.addAttribute(_T("Disposition"), _T("N/A"));
	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w.clear();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), *KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);
	
	#endif

	decHookingDepth();

	return res;	
}
NTSTATUS WINAPI MyNtQueryKey(HANDLE KeyHandle, KEY_INFORMATION_CLASS KeyInformationClass, PVOID KeyInformation, ULONG Length, PULONG ResultLength)
{
	if (!shouldIntercept())
		return realNtQueryKey(KeyHandle, KeyInformationClass, KeyInformation, Length, ResultLength);

	incHookingDepth();

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryKey(KeyHandle,KeyInformationClass,KeyInformation,Length,ResultLength);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtQueryKey"));

	string w = string();
	GetKeyPathFromKKEY(KeyHandle, &w);

	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	element.addAttribute(_T("Path"), w.c_str());

	// >>>>>>>>>>>>>>> Key Information Class <<<<<<<<<<<<<<<
	w = KeyInformationClassToString(KeyInformationClass);
	element.addAttribute(_T("KeyInformationClass"), w.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);
	#endif

	decHookingDepth();

	return res;

}
NTSTATUS WINAPI MyNtDeleteKey(HANDLE KeyHandle)
{
	if (!shouldIntercept())
		return realNtDeleteKey(KeyHandle);

	incHookingDepth();

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtDeleteKey(KeyHandle);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtDeleteKey"));

	string w = string();

	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	GetKeyPathFromKKEY(KeyHandle, &w);
	element.addAttribute(_T("Path"), w.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w.clear();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	#endif

	decHookingDepth();

	return res;
}
NTSTATUS WINAPI MyNtDeleteValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName){
	
	if (!shouldIntercept())
		return realNtDeleteValueKey(KeyHandle, ValueName);

	incHookingDepth();

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtDeleteValueKey(KeyHandle, ValueName);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtDeleteValueKey"));

	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	string w = string();
	GetKeyPathFromKKEY(KeyHandle, &w);
	element.addAttribute(_T("Path"), w.c_str());

	// >>>>>>>>>>>>>>> Value <<<<<<<<<<<<<<<
	w.clear();
	from_unicode_to_wstring(ValueName, &w);
	element.addAttribute(_T("ValueName"), w.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w.clear();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	#endif

	decHookingDepth();

	return res;
}
NTSTATUS WINAPI MyNtEnumerateKey(HANDLE KeyHandle, ULONG Index, KEY_INFORMATION_CLASS KeyInformationClass, PVOID KeyInformation, ULONG Length, PULONG ResultLength)
{
	if (!shouldIntercept())
		return realNtEnumerateKey(KeyHandle, Index, KeyInformationClass, KeyInformation, Length, ResultLength);

	incHookingDepth();

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtEnumerateKey(KeyHandle,Index,KeyInformationClass,KeyInformation,Length,ResultLength);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtEnumerateKey"));

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());

	// >>>>>>>>>>>>>>> KeyPath <<<<<<<<<<<<<<<
	GetKeyPathFromKKEY(KeyHandle,&w);
	element.addAttribute(_T("KeyPath"), w.c_str());

	// >>>>>>>>>>>>>>> Key Information Class <<<<<<<<<<<<<<<
	w = KeyInformationClassToString(KeyInformationClass);
	element.addAttribute(_T("KeyInformationClass"), w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	#endif

	decHookingDepth();

	return res;
}
NTSTATUS WINAPI MyNtEnumerateValueKey(HANDLE KeyHandle, ULONG Index, KEY_VALUE_INFORMATION_CLASS KeyValueInformationClass, PVOID KeyValueInformation, ULONG Length, PULONG ResultLength)
{
	if (!shouldIntercept())
		return realNtEnumerateValueKey(KeyHandle, Index, KeyValueInformationClass, KeyValueInformation, Length, ResultLength);

	incHookingDepth();

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtEnumerateValueKey(KeyHandle, Index, KeyValueInformationClass, KeyValueInformation, Length, ResultLength);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtEnumerateValueKey"));

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());

	// >>>>>>>>>>>>>>> KeyPath <<<<<<<<<<<<<<<
	GetKeyPathFromKKEY(KeyHandle,&w);
	element.addAttribute(_T("KeyPath"), w.c_str());

	// >>>>>>>>>>>>>>> SubKey <<<<<<<<<<<<<<<
	element.addAttribute(_T("SubKeyIndex"), to_string(Index).c_str());

	// >>>>>>>>>>>>>>> Key Value Information Class <<<<<<<<<<<<<<<
	w = KeyValueInformationClassToString(KeyValueInformationClass);
	element.addAttribute(_T("KeyValueInformationClass"), w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);
	#endif

	decHookingDepth();

	return res;
}
NTSTATUS WINAPI MyNtLockFile(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine, PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, PLARGE_INTEGER ByteOffset, PLARGE_INTEGER Length, ULONG Key, BOOLEAN FailImmediately, BOOLEAN ExclusiveLock)
{
	if (!shouldIntercept())
		return realNtLockFile(FileHandle, Event, ApcRoutine, ApcContext, IoStatusBlock, ByteOffset, Length, Key, FailImmediately, ExclusiveLock);

	incHookingDepth();

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtLockFile(FileHandle,Event,ApcRoutine,ApcContext,IoStatusBlock,ByteOffset,Length,Key,FailImmediately,ExclusiveLock);

	#ifdef SYSCALL_LOG

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtLockFile"));

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());

	// >>>>>>>>>>>>>>> FilePath <<<<<<<<<<<<<<<
	GetFileNameFromHandle(FileHandle,&w);
	element.addAttribute(_T("KeyPath"), w.c_str());
	

	// >>>>>>>>>>>>>>> Byte offset <<<<<<<<<<<<<<<
	w.clear();
	element.addAttribute(_T("LockFrom"), to_string(ByteOffset->QuadPart).c_str());

	// >>>>>>>>>>>>>>> Length to lock <<<<<<<<<<<<<<<
	w.clear();
	element.addAttribute(_T("LengthToLock"), to_string(Length->QuadPart).c_str());

	// >>>>>>>>>>>>>>> Fail Immediately? <<<<<<<<<<<<<<<
	w.clear();
	element.addAttribute(_T("FailImmediately"), to_string(FailImmediately).c_str());

	// >>>>>>>>>>>>>>> Exclusive Lock <<<<<<<<<<<<<<<
	w.clear();
	element.addAttribute(_T("ExclusiveLock"), to_string(ExclusiveLock).c_str());

	// >>>>>>>>>>>>>>> IO STATUS BLOCK <<<<<<<<<<<<<<<
	w.clear();
	IoStatusToString(IoStatusBlock, &w);
	element.addAttribute(_T("IoStatusBlock"), w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), FileHandle);
		element.addAttribute(_T("Handle"),buff);
	}

	log(&element);

	#endif

	decHookingDepth();

	return res;
}
/*
NTSTATUS WINAPI MyNtOpenProcess(PHANDLE ProcessHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PCLIENT_ID ClientId)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenProcess(ProcessHandle,DesiredAccess,ObjectAttributes,ClientId);
	
	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtOpenProcess"));
	
	
	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w);

	// >>>>>>>>>>>>>>> DESIRED ACCESS <<<<<<<<<<<<<<<
	w.clear();
	FileAccessMaskToString(DesiredAccess, &w);
	element.addAttribute(_T("DesiredAccess"), w);
	
	// The objectname contains a full path to the file
	// The ObjectName might be null: http://msdn.microsoft.com/en-us/library/windows/hardware/ff567022(v=vs.85).aspx
	if (ObjectAttributes->ObjectName == NULL)
	{
		//TODO
	}
	else
	{
		// If it is not null, calcualte the path
		element.addAttribute(_T("Path"), ObjectAttributes->ObjectName->Buffer);
	}
	
	#endif

	log(&element);
	
	return res;
}
*/
NTSTATUS WINAPI MyNtQueryDirectoryFile(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine, PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, PVOID FileInformation, ULONG Length, FILE_INFORMATION_CLASS FileInformationClass, BOOLEAN ReturnSingleEntry, PUNICODE_STRING FileName, BOOLEAN RestartScan)
{
	if (!shouldIntercept())
		return realNtQueryDirectoryFile(FileHandle, Event, ApcRoutine, ApcContext, IoStatusBlock, FileInformation, Length, FileInformationClass, ReturnSingleEntry, FileName, RestartScan);
	incHookingDepth();
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryDirectoryFile(FileHandle,Event,ApcRoutine, ApcContext, IoStatusBlock, FileInformation, Length,FileInformationClass,ReturnSingleEntry,FileName,RestartScan);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtQueryDirectoryFile"));

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());

	// >>>>>>>>>>>>>>> IO STATUS BLOCK <<<<<<<<<<<<<<<
	w.clear();
	IoStatusToString(IoStatusBlock, &w);
	element.addAttribute(_T("IoStatusBlock"), w.c_str());

	// >>>>>>>>>>>>>>> FILE Name <<<<<<<<<<<<<<<
	if (FileName != NULL)
	{
		w.clear();
		element.addAttribute(_T("FileName"), FileName->Buffer);
	}
	

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), FileHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);
	#endif

	decHookingDepth();
	return res;
}
NTSTATUS WINAPI MyNtQueryFullAttributesFile(POBJECT_ATTRIBUTES ObjectAttributes, PFILE_NETWORK_OPEN_INFORMATION FileInformation)
{
	if (!shouldIntercept())
		return realNtQueryFullAttributesFile(ObjectAttributes, FileInformation);
	incHookingDepth();
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryFullAttributesFile(ObjectAttributes, FileInformation);

	#ifdef SYSCALL_LOG

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtQueryFullAttributesFile"));

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());

	// >>>>>>>>>>>>>>> File Path <<<<<<<<<<<<<<<
	if (ObjectAttributes->RootDirectory != NULL)
	{
		// The path specified in ObjectName is relative to the directory handle. Get the path of that directory
		w.clear();
		GetHandleFileName(ObjectAttributes->RootDirectory, &w);
		element.addAttribute(_T("DirPath"), w.c_str());
		element.addAttribute(_T("Path"), ObjectAttributes->ObjectName->Buffer);
	}
	else
	{
		// The objectname contains a full path to the file
		element.addAttribute(_T("Path"), ObjectAttributes->ObjectName->Buffer);
	}

	log(&element);

	#endif

	decHookingDepth();
	return res;
}
NTSTATUS WINAPI MyNtQueryValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName, KEY_VALUE_INFORMATION_CLASS KeyValueInformationClass, PVOID KeyValueInformation, ULONG Length, PULONG ResultLength)
{
	if (!shouldIntercept())
		return realNtQueryValueKey(KeyHandle, ValueName, KeyValueInformationClass, KeyValueInformation, Length, ResultLength);
	incHookingDepth();
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryValueKey(KeyHandle, ValueName, KeyValueInformationClass, KeyValueInformation, Length, ResultLength);
	
	#ifdef SYSCALL_LOG

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtQueryValueKey"));

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());
	
	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	w.clear();
	GetKeyPathFromKKEY(KeyHandle,&w);
	element.addAttribute(_T("KeyPath"), w.c_str());
	
	// >>>>>>>>>>>>>>> ValueName: could be not null terminated, so set the terminator! <<<<<<<<<<<<<<<
	w.clear();
	from_unicode_to_wstring(ValueName, &w);
	element.addAttribute(_T("ValueName"), w.c_str());
	
	// >>>>>>>>>>>>>>> Key Value Information Class <<<<<<<<<<<<<<<
	w = KeyValueInformationClassToString(KeyValueInformationClass);
	element.addAttribute(_T("KeyValueInformationClass"), w.c_str());
	

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	#endif

	decHookingDepth();
	return res;
}
NTSTATUS WINAPI MyNtSetInformationFile(HANDLE FileHandle, PIO_STATUS_BLOCK IoStatusBlock, PVOID FileInformation, ULONG Length, FILE_INFORMATION_CLASS FileInformationClass)
{
	if (!shouldIntercept())
		return realNtSetInformationFile(FileHandle, IoStatusBlock, FileInformation, Length, FileInformationClass);
	incHookingDepth();
	// _FILE_INFORMATION_CLASS::FileRenameInformation = 10 /*0xA*/
	// FILE_INFORMATION_CLASS::FileRenameInformationBypassAccessCheck => only valid in kernel mode and for Windows 8. So we do not care about this right now.
	bool isRename = (FileInformationClass == 0xA);
	string oldPath;
	bool pathFound = false;

	// We need to save current file path in case this is a rename
	if (isRename) {
		pathFound = handleMap.Lookup(FileHandle, oldPath);
	}
		
	// Immediately perform the call to the real API
	NTSTATUS res = realNtSetInformationFile(FileHandle, IoStatusBlock, FileInformation, Length, FileInformationClass);
	
	// This API has the capability of renaming a file. For us, it is crucial to detect this so we can keep track of the history of a file.
	// Only notify the GuestController IF the operation was a RENAME and only if it was successful.
	if (res == STATUS_SUCCESS && isRename) {
		// Information about this operation is given by the FIleInformation buffer, that has to be casted to a FILE_RENAME_INFORMATION struct
		PFILE_RENAME_INFORMATION info = ((PFILE_RENAME_INFORMATION)FileInformation);
		
		// Build info about the new path. If the path is relative to a directory, prepend the directory path.
		string newPath;
		if (info->RootDirectory != NULL) {
			// The file name is not absolute, but depends on the directory
			GetHandleFileName(info->RootDirectory, &newPath);
		}
		
		// Put now the relative path.
		int bufflen = info->FileNameLength / sizeof(WCHAR) + 1;
		WCHAR * tmp = new WCHAR[ bufflen ];
		memcpy(tmp, info->FileName, info->FileNameLength);
		tmp[bufflen -1] = '\0';
			
		// This will copy the memory so we can get rid of tmp buffer
		newPath.append(string(tmp));
		delete[] tmp;

		// The old path may be unknown for some reason (implementation bug?). In that case, simulate a create file behaviour, so we don't loose any info
		if (!pathFound) {
			NotifyFileAccess(newPath, WK_FILE_CREATED);
		}
		else {
			// Otherwise trigger a rename
			NotifyFileRename(oldPath, newPath);
		}
	}

	#ifdef SYSCALL_LOG

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtSetInformationFile"));

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());
	
	// >>>>>>>>>>>>>>> FileInformationClass <<<<<<<<<<<<<<<
	w.clear();
	//element.addAttribute(_T("FileInformationClass"), FileInformationClassToString(FileInformationClass));
	element.addAttribute(_T("FileInformationClass"), to_string(FileInformationClass).c_str());

	// TODO: parse the class result. It depends on the class type

	// >>>>>>>>>>>>>>> IO STATUS BLOCK <<<<<<<<<<<<<<<
	w.clear();
	IoStatusToString(IoStatusBlock, &w);
	element.addAttribute(_T("IoStatusBlock"), w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), FileHandle);
		element.addAttribute(_T("Handle"),buff);
	}

	log(&element);

	#endif

	decHookingDepth();
	return res;
}
NTSTATUS WINAPI MyNtSetValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName, ULONG TitleIndex, ULONG Type, PVOID Data, ULONG DataSize)
{
	if (!shouldIntercept())
		return realNtSetValueKey(KeyHandle, ValueName, TitleIndex, Type, Data, DataSize);
	incHookingDepth();
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtSetValueKey(KeyHandle,ValueName,TitleIndex,Type,Data,DataSize);

	#ifdef SYSCALL_LOG

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtSetValueKey"));

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());
	
	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	w.clear();
	GetKeyPathFromKKEY(KeyHandle,&w);
	element.addAttribute(_T("KeyPath"), w.c_str());

	// >>>>>>>>>>>>>>> ValueName <<<<<<<<<<<<<<<
	w.clear();
	from_unicode_to_wstring(ValueName, &w);
	element.addAttribute(_T("ValueName"), w.c_str());

	// >>>>>>>>>>>>>>> TitleIndex <<<<<<<<<<<<<<<
	element.addAttribute(_T("TitleIndex"), to_string(TitleIndex).c_str());
	
	// SKIPPING NOW THE TYPE OF THE DATA TO BE WRITTEN: (lately TODO?)


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), KeyHandle);
		element.addAttribute(_T("Handle"),buff);
	}

	log(&element);

	#endif

	decHookingDepth();
	return res;
}
NTSTATUS WINAPI MyNtTerminateProcess(HANDLE ProcessHandle, NTSTATUS ExitStatus)
{
	if (!shouldIntercept())
		return realNtTerminateProcess(ProcessHandle, ExitStatus);
	incHookingDepth();
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtTerminateProcess(ProcessHandle,ExitStatus);

	#ifdef SYSCALL_LOG

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtTerminateProcess"));

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());

	// >>>>>>>>>>>>>>> Process Handle <<<<<<<<<<<<<<<

	// >>>>>>>>>>>>>>> ExitStatus <<<<<<<<<<<<<<<
	element.addAttribute(_T("ExitStatus"), to_string(ExitStatus).c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("0x%p"), ProcessHandle);
		element.addAttribute(_T("Handle"),buff);
	}

	log(&element);

	#endif

	decHookingDepth();
	return res;
}
NTSTATUS WINAPI MyNtClose(HANDLE Handle)
{
	if (!shouldIntercept())
		return realNtClose(Handle);
	incHookingDepth();
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtClose(Handle);
	
	string path;
	bool pathFound = handleMap.Lookup(Handle, path);

	// Unfortunately, we cannot rely on the NtClose for IO-flushing. NtClose does not guarantee file will be written 
	// right after the close. For this reason we just recalculate all file hases only when the entire process is dead
	// and this is done by the GuestController, which can see this process/DLL terminating.
	// Thus, the HandleTable would not be needed. For now I'll leve it where it is.
	//if (pathFound)
	//	NotifyFileAccess(path, COPYDATA_FILE_CLOSED);

	#ifdef SYSCALL_LOG
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtClose")); 

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());
	
	// >>>>>>>>>>>>>>> Path (only if it was a file) <<<<<< <<<<<<<<<
	if (pathFound)
		element.addAttribute(_T("Path"), path.c_str());

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		buff[0] = '\0';
		wsprintf(buff, _T("0x%p"), Handle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	#endif

	decHookingDepth();
	return res;
}


// TODO: NtCreateProcess?
/*
NTSTATUS WINAPI MyNtCreateUserProcess(
_Out_ PHANDLE ProcessHandle,
_Out_ PHANDLE ThreadHandle,
_In_ ACCESS_MASK ProcessDesiredAccess,
_In_ ACCESS_MASK ThreadDesiredAccess,
_In_opt_ POBJECT_ATTRIBUTES ProcessObjectAttributes,
_In_opt_ POBJECT_ATTRIBUTES ThreadObjectAttributes,
_In_ ULONG ProcessFlags, // PROCESS_CREATE_FLAGS_*
_In_ ULONG ThreadFlags, // THREAD_CREATE_FLAGS_*
_In_opt_ PVOID ProcessParameters, // PRTL_USER_PROCESS_PARAMETERS
_Inout_ PPS_CREATE_INFO CreateInfo,
_In_opt_ PPS_ATTRIBUTE_LIST AttributeList)
{

BOOL res = DetourCreateProcessWithDll(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation, DllPath, realCreateProcessA);
if (!res)
OutputDebugString(_T("There was a problem when injecting the DLL to the new process via MyCreateProcessA()."));
else{
notifyNewPid(cwHandle, lpProcessInformation->dwProcessId);
OutputDebugString(_T("New process created and dll injected via MyCreateProcessA()."));
}

// TODO
}
*/
//TODO...

/*
BOOL WINAPI MyCreateProcessA(LPCTSTR lpApplicationName, LPTSTR lpCommandLine, LPSECURITY_ATTRIBUTES lpProcessAttributes, LPSECURITY_ATTRIBUTES lpThreadAttributes, BOOL bInheritHandles, DWORD dwCreationFlags, LPVOID lpEnvironment, LPCTSTR lpCurrentDirectory, LPSTARTUPINFO lpStartupInfo, LPPROCESS_INFORMATION lpProcessInformation)
{
	CHAR   DllPath[MAX_PATH] = { 0 };
	OutputDebugString(_T("MyCreateProcessA"));
	GetModuleFileNameA((HINSTANCE)&__ImageBase, DllPath, _countof(DllPath));

	// Use directly the Detours API
	BOOL res = DetourCreateProcessWithDll(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation,DllPath,realCreateProcessA);
	if (!res) 
		OutputDebugString(_T("There was a problem when injecting the DLL to the new process via MyCreateProcessA()."));
	else{
		notifyNewPid(cwHandle, lpProcessInformation->dwProcessId);
		OutputDebugString(_T("New process created and dll injected via MyCreateProcessA()."));
	}
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("CreateProcess"));

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());

	
	log(&element);

	return res;
}


BOOL WINAPI MyCreateProcessW(LPCWSTR lpApplicationName, LPWSTR lpCommandLine, LPSECURITY_ATTRIBUTES lpProcessAttributes, LPSECURITY_ATTRIBUTES lpThreadAttributes, BOOL bInheritHandles, DWORD dwCreationFlags, LPVOID lpEnvironment, LPCWSTR lpCurrentDirectory, LPSTARTUPINFO lpStartupInfo, LPPROCESS_INFORMATION lpProcessInformation)
{
	CHAR   DllPath[MAX_PATH] = { 0 };
	OutputDebugString(_T("MyCreateProcessW"));
	GetModuleFileNameA((HINSTANCE)&__ImageBase, DllPath, _countof(DllPath));

	// Use directly the Detours API
	BOOL res = DetourCreateProcessWithDll(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation, DllPath, realCreateProcessW);
	if (!res)
		OutputDebugString(_T("There was a problem when injecting the DLL to the new process via MyCreateProcessW()."));
	else {
		notifyNewPid(cwHandle, lpProcessInformation->dwProcessId);
		OutputDebugString(_T("New process created and dll injected via MyCreateProcessW()."));
	}

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("CreateProcess"));

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());


	log(&element);

	return res;
}
*/


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
	if (!shouldIntercept())
		return realCreateProcessInternalW(hToken, lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation, hNewToken);
	incHookingDepth();
	// This is the API that gets eventually called by all the others. Ansi params get converted into wide characters, so the A version is useless.
	CHAR   DllPath[MAX_PATH] = { 0 };
	OutputDebugString(_T("MyCreateProcessInternalW"));
	GetModuleFileNameA((HINSTANCE)&__ImageBase, DllPath, _countof(DllPath));
	BOOL processCreated;

	// Save the previous value of the creation flags and make sure we add the create suspended BIT
	DWORD originalFlags = dwCreationFlags;
	dwCreationFlags = dwCreationFlags | CREATE_SUSPENDED;
	processCreated = realCreateProcessInternalW(hToken, lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation, hNewToken);
	if (processCreated) {
		// Allocate enough memory on the new process
		LPVOID baseAddress = (LPVOID)VirtualAllocEx(lpProcessInformation->hProcess, NULL, strlen(DllPath)+1, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);

		// Copy the code to be injected
		WriteProcessMemory(lpProcessInformation->hProcess, baseAddress, DllPath, strlen(DllPath), NULL);

		OutputDebugStringA("-----> INJECTOR: DLL copied into host process memory space");

		// Notify the HostController that a new process has been created
		notifyNewPid(GetCurrentProcessId(), lpProcessInformation->dwProcessId);
		kern32dllmod = GetModuleHandle(TEXT("kernel32.dll"));
		HANDLE loadLibraryAddress = GetProcAddress(kern32dllmod, "LoadLibraryA");
		if (loadLibraryAddress == NULL)
		{
			OutputDebugStringW(_T("!!!!!LOADLIB IS NULL"));
			//error
			return 0;
		}
		else {
			OutputDebugStringW(_T("LOAD LIB OK"));
		}

		// Create a remote thread the remote thread
		HANDLE  threadHandle = CreateRemoteThread(lpProcessInformation->hProcess, NULL, 0, (LPTHREAD_START_ROUTINE)loadLibraryAddress, baseAddress, NULL, 0);
		if (threadHandle == NULL) {
			OutputDebugStringW(_T("!!!!REMTOE THREAD NOT OK"));
		}
		else {
			OutputDebugStringW(_T("!!!!REMTOE OK"));
		}
		OutputDebugStringA("-----> INJECTOR: Remote thread created");

		
		// Check if the process was meant to be stopped. If not, resume it now
		if ((originalFlags & CREATE_SUSPENDED) != CREATE_SUSPENDED) {
			// need to start it right away
			ResumeThread(lpProcessInformation->hThread);
			OutputDebugStringA("-----> INJECTOR: Thread resumed");
		}
	}

	#ifdef SYSCALL_LOG	
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc; pugi::xml_node element = doc.append_child(_T("CreateProcessInternalW"));

	
	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	string w = string();
	NtStatusToString(processCreated, &w);
	element.addAttribute(_T("Result"), w.c_str());


	log(&element);

	#endif

	decHookingDepth();
	return processCreated;
	
}


VOID WINAPI MyExitProcess(UINT uExitCode)
{
	if (!shouldIntercept())
		return realExitProcess(uExitCode);
	incHookingDepth();
	
	// We hook this to let the GuestController know about our intention to terminate
	notifyRemovedPid(cwHandle, GetCurrentProcessId());
	
	decHookingDepth();
	realExitProcess(uExitCode);
}


void incHookingDepth() {
	DWORD depth = (DWORD)TlsGetValue(dwTlsIndex);
	depth++;
	TlsSetValue(dwTlsIndex, (LPVOID)depth);
}

void decHookingDepth() {
	DWORD depth = (DWORD)TlsGetValue(dwTlsIndex);
	depth--;
	TlsSetValue(dwTlsIndex, (LPVOID)depth);
}

bool shouldIntercept() {
	DWORD depth = (DWORD)TlsGetValue(dwTlsIndex);
	return depth == 0;
}

extern "C" __declspec(dllexport)VOID NullExport(VOID)
{
}

void NotifyFileAccess(std::wstring fullPath, const wchar_t* mode) {
	
	pugi::xml_document doc;
	pugi::xml_node element = doc.append_child(WK_FILE_EVENT);
	element.addAttribute(WK_FILE_EVENT_MODE, mode);
	element.addAttribute(WK_FILE_EVENT_PATH, fullPath.c_str());

	sendToEventPipe(&element);

}

void NotifyFileRename(std::wstring oldPath, std::wstring newPath) {
	
	pugi::xml_document doc;
	pugi::xml_node element = doc.append_child(WK_FILE_EVENT);
	element.addAttribute(WK_FILE_EVENT_MODE, WK_FILE_RENAMED);
	element.addAttribute(WK_FILE_EVENT_OLD_PATH, oldPath.c_str());
	element.addAttribute(WK_FILE_EVENT_NEW_PATH, newPath.c_str());

	sendToEventPipe(&element);
}

void NotifyRegistryAccess(std::wstring fullPath, const wchar_t* mode) {

	pugi::xml_document doc;
	pugi::xml_node element = doc.append_child(WK_REGISTRY_EVENT);
	element.addAttribute(WK_REGISTRY_EVENT_MODE, mode);
	element.addAttribute(WK_REGISTRY_EVENT_PATH, fullPath.c_str());

	sendToEventPipe(&element);

}


void log(pugi::xml_node *element) {
	
	element->append_attribute(_T("ThreadId")) = to_string(GetCurrentThreadId()).c_str();
	element->append_attribute(_T("PId")) = to_string(GetCurrentProcessId()).c_str();

	// We have now to send the data on using the named pipe.
	try{
		sendToLogPipe(element);
	}
	catch (int e) {
	}
}


HANDLE connectToPipe(char* pipeName) {
	char buff[512];
	bool firstConnection = false;
	HANDLE pipeh = INVALID_HANDLE_VALUE;

	// Check if the handle has been already initialized.
	while (pipeh == INVALID_HANDLE_VALUE) {
		// The pipe is disconnected, we need to connect.
		firstConnection = true;
		pipeh = CreateFileA(
			pipeName,   // pipe name 
			GENERIC_READ |  // read and write access 
			GENERIC_WRITE,
			0,              // no sharing 
			NULL,           // default security attributes
			OPEN_EXISTING,  // opens existing pipe 
			0,              // default attributes 
			NULL);          // no template file 

		if (pipeh != INVALID_HANDLE_VALUE) {
			break;
		}
		DWORD error = GetLastError();
		// If the error is different from PIPE_BUSY, we have to give up and return
		if (error != ERROR_PIPE_BUSY)
		{
			// An error has occurred
			sprintf_s(buff, "[CHOOKING DLL] Cannot open pipe %s. Error %u XXXX", pipeName, error);
			OutputDebugStringA(buff);
			return INVALID_HANDLE_VALUE;
		}

		// If the control reaches this point, it means all the pipe instances are busy. Wait up to 20 seconds and try again.
		if (!WaitNamedPipeA(pipeName, NMPWAIT_WAIT_FOREVER))
		{
			sprintf_s(buff, "[CHOOKING DLL] Timeout waiting for pipe %s. Giving up.", pipeName);
			OutputDebugStringA(buff);
			return INVALID_HANDLE_VALUE;
		}
	}
	
	// The pipe connected; change to message-read mode. 
	DWORD dwMode = PIPE_READMODE_MESSAGE;
	if (!SetNamedPipeHandleState(
		pipeh,    // pipe handle 
		&dwMode,  // new pipe mode 
		NULL,     // don't set maximum bytes 
		NULL))    // don't set maximum time 
	{
		DWORD error = GetLastError();
		// An error has occurred
		sprintf_s(buff, "[CHOOKING DLL] Cannot set message mode on pipe %s. Error %u XXXX", pipeName, error);
		OutputDebugStringA(buff);
		return INVALID_HANDLE_VALUE;
	}
	
	// At this point we are sure the pipe is connected and read for messages
	return pipeh;
	
}

bool sendMessageToPipe(HANDLE hPipe, std::wstring msg) {
	char buff[512];
	DWORD ack;
	DWORD read;
	BOOL fSuccess;
	DWORD wrt;
	DWORD rd;
	// Dimension of the string to send
	const wchar_t* str = msg.c_str();
	DWORD bytelen = sizeof(wchar_t)* msg.size();
	
	// Write data to the pipe
	if (!WriteFile(
		hPipe,          // pipe handle 
		str,            // message 
		bytelen,        // message length 
		&wrt,           // bytes written 
		NULL))          // not overlapped 
	{
		DWORD error = GetLastError();
		// An error has occurred
		sprintf_s(buff, "[CHOOKING DLL] Cannot write on pipe (handle %u). Error %u XXXX", hPipe, error);
		OutputDebugStringA(buff);
		return false;
	}

	// Message has been sent, receive the ack
	fSuccess = ReadFile(
		hPipe,			// pipe handle 
		&ack,			// buffer to receive reply 
		sizeof(DWORD),  // size of buffer 
		&rd,			// number of bytes read 
		NULL);			// not overlapped 

	if (!fSuccess && GetLastError() != ERROR_MORE_DATA) {
		return false;
	}
	
	return true;
}

void disconnectPipe(HANDLE hPipe) {
	CloseHandle(hPipe);
}

void sendToLogPipe(pugi::xml_node* node){
	HANDLE pipe;
	bool done = false;
	int attempts = 0;

	// We use a wchart_t type for buffer
	std::wostringstream ss;
	node->print(ss, _T(""), pugi::format_raw, pugi::xml_encoding::encoding_utf16_le);
	std::wstring s = ss.str();

	while (!done) {
		pipe = connectToPipe(LOG_PIPE);
		if (pipe != INVALID_HANDLE_VALUE) {
			done = sendMessageToPipe(pipe, s);
			disconnectPipe(pipe);
			attempts++;
		}
		else {
			// We had a severe error and we cannot try again.
			break;
		}
	}

	if (done) {
		if (attempts > 1) {
			char buff[512];
			sprintf_s(buff, "[CHOOKING DLL] Sending message on LOG PIPE required %d attempts.", attempts);
			OutputDebugStringA(buff);
		}
	} else {
		// In case of failure let it be known
		char buff[512];
		sprintf_s(buff, "[CHOOKING DLL] Sending message on LOG EVENT FAILED after %d attempts.", attempts);
		OutputDebugStringA(buff);
	}
}

void sendToEventPipe(pugi::xml_node* node) {
	bool done = false;
	HANDLE pipe;
	int attempts = 0;

	// We use a wchart_t type for buffer
	std::wostringstream ss;
	node->print(ss, _T(""), pugi::format_raw, pugi::xml_encoding::encoding_utf16_le);
	std::wstring s = ss.str();

	while (!done) {
		pipe = connectToPipe(EVENT_PIPE);
		if (pipe != INVALID_HANDLE_VALUE) {
			done = sendMessageToPipe(pipe, s);
			disconnectPipe(pipe);
			attempts++;
		}
		else {
			// We had a severe error and we cannot try again.
			break;
		}
	}

	if (done) {
		// Only notify if we had delays
		if (attempts > 1) {
			char buff[512];
			sprintf_s(buff, "[CHOOKING DLL] Sending message on EVENT PIPE required %d attempts.", attempts);
			OutputDebugStringA(buff);
		}
	}
	else {
		// In case of failure let it be known
		char buff[512];
		sprintf_s(buff, "[CHOOKING DLL] Sending message on LOG EVENT FAILED after %d attempts.", attempts);
		OutputDebugStringA(buff);
	}
}

bool IsRequestingWriteAccess(ACCESS_MASK DesiredAccess) {
	bool notification = false;
	// Check if we really need to continue. We keep going only if the file access is in write mode. 
	for (int i = 0; i < sizeof(WRITE_FLAGS); i++) {
		if (((DesiredAccess & (WRITE_FLAGS[i])) == (WRITE_FLAGS[i]))){
			notification = true; break;
		}
	}

	return notification;
}

bool IsRequestingRegistryWriteAccess(ACCESS_MASK DesiredAccess) {
	bool notification = false;
	// Check if we really need to continue. We keep going only if the file access is in write mode. 
	for (int i = 0; i < sizeof(REGISTRY_WRITE_FLAGS); i++) {
		if (((DesiredAccess & (REGISTRY_WRITE_FLAGS[i])) == (REGISTRY_WRITE_FLAGS[i]))){
			notification = true; break;
		}
	}

	return notification;
}

std::wstring GetKeyPathFromOA(POBJECT_ATTRIBUTES ObjectAttributes) {
	std::wstring w = std::wstring();
	if (ObjectAttributes->RootDirectory != nullptr) {
		// Resolve the root directory handle
		GetKeyPathFromKKEY(ObjectAttributes->RootDirectory, &w);
		w.append(L"\\");
	}

	// Populate the rest of the path.
	std::wstring s = std::wstring();
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
	w.append(s);

	return w;
}


std::wstring GetFullPathByObjectAttributes(POBJECT_ATTRIBUTES ObjectAttributes) {
	// Time to build the filepath. As stated here https://msdn.microsoft.com/en-us/library/windows/hardware/ff557749(v=vs.85).aspx, the ObjectAttribute
	// structure contains a couple of information. The full file path can be either relative to a folder or be full qualifield. In both cases we need
	// to construct a unique string pointing to the actual file. So, if the RootDirectory attribute of the struct is not null, we need to resolve the directory
	// handle and compose the full path.
	// Populate the root dir path, if needed.
	std::wstring w = std::wstring();
	if (ObjectAttributes->RootDirectory != nullptr) {
		// Resolve the root directory handle
		GetFileNameFromHandle(ObjectAttributes->RootDirectory, &w);
	}

	// Populate the rest of the path.
	std::wstring s = std::wstring();
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
	w.append(s);

	return w;
}

/* 
	>>>>>>>>>>>>>>> Parsing functions <<<<<<<<<<<<<<< 
*/
void FileAttributesToString(ULONG FileAttributes, string* s)
{
	if ((FileAttributes & FILE_ATTRIBUTE_ARCHIVE) == FILE_ATTRIBUTE_ARCHIVE)
		s->append(_T("|FILE_ATTRIBUTE_ARCHIVE"));
	if ((FileAttributes & FILE_ATTRIBUTE_ENCRYPTED) == FILE_ATTRIBUTE_ENCRYPTED)
		s->append(_T("|FILE_ATTRIBUTE_ENCRYPTED"));
	if ((FileAttributes & FILE_ATTRIBUTE_HIDDEN) == FILE_ATTRIBUTE_HIDDEN)
		s->append(_T("|FILE_ATTRIBUTE_HIDDEN"));
	if ((FileAttributes & FILE_ATTRIBUTE_NORMAL) == FILE_ATTRIBUTE_NORMAL)
		s->append(_T("|FILE_ATTRIBUTE_NORMAL"));
	if ((FileAttributes & FILE_ATTRIBUTE_OFFLINE) == FILE_ATTRIBUTE_OFFLINE)
		s->append(_T("|FILE_ATTRIBUTE_OFFLINE"));
	if ((FileAttributes & FILE_ATTRIBUTE_READONLY) == FILE_ATTRIBUTE_READONLY)
		s->append(_T("|FILE_ATTRIBUTE_READONLY"));
	if ((FileAttributes & FILE_ATTRIBUTE_SYSTEM) == FILE_ATTRIBUTE_SYSTEM)
		s->append(_T("|FILE_ATTRIBUTE_SYSTEM"));
	if ((FileAttributes & FILE_ATTRIBUTE_TEMPORARY) == FILE_ATTRIBUTE_TEMPORARY)
		s->append(_T("|FILE_ATTRIBUTE_TEMPORARY"));

	if (s->length() > 0)
		(*s) = s->substr(1, s->length() - 1);
	else
		(*s) = _T("NA");
}

string StandardAccessMaskToString(ACCESS_MASK DesiredAccess)
{
	// We know how long will be our result
	TCHAR mask[11]; mask[0] = '\0';
	_stprintf_s(mask, _countof(mask), TEXT("0x%X"), DesiredAccess);
	mask[8] = '\0';
	return string(mask);
}

void IoStatusToString(IO_STATUS_BLOCK* IoStatusBlock, string* s)
{
	s->clear();
	switch (IoStatusBlock->Status)
	{
	case FILE_CREATED:
		(*s) = _T("FILE_CREATED");
		break;

	case FILE_OPENED:
		(*s) = _T("FILE_OPENED");
		break;

	case FILE_OVERWRITTEN:
		(*s) = _T("FILE_OVERWRITTEN");
		break;
	case FILE_SUPERSEDED:
		(*s) = _T("FILE_SUPERSEDED");
		break;

	case FILE_EXISTS:
		(*s) = _T("FILE_EXISTS");
		break;

	case FILE_DOES_NOT_EXIST:
		(*s) = _T("FILE_DOES_NOT_EXIST");
		break;

	default:
		(*s) = _T("NA"); // Should never happen...
	}
}

void ShareAccessToString(ULONG ShareAccess, string* s)
{
	s->clear();
	if ((ShareAccess & FILE_SHARE_READ) == FILE_SHARE_READ)
		s->append(_T("|FILE_SHARE_READ"));
	if ((ShareAccess & FILE_SHARE_WRITE) == FILE_SHARE_WRITE)
		s->append(_T("|FILE_SHARE_WRITE"));
	if ((ShareAccess & FILE_SHARE_DELETE) == FILE_SHARE_DELETE)
		s->append(_T("|FILE_SHARE_DELETE"));
	if (s->length() > 0)
		(*s) = s->substr(1, s->length() - 1);
	else
		(*s) = _T("NA");
}

void FileCreateOptionsToString(ULONG OpenCreateOption, string* s)
{
	s->clear();
	if ((OpenCreateOption & FILE_DIRECTORY_FILE) == FILE_DIRECTORY_FILE)
		s->append(_T("|FILE_DIRECTORY_FILE"));
	if ((OpenCreateOption & FILE_NON_DIRECTORY_FILE) == FILE_NON_DIRECTORY_FILE)
		s->append(_T("|FILE_NON_DIRECTORY_FILE"));
	if ((OpenCreateOption & FILE_WRITE_THROUGH) == FILE_WRITE_THROUGH)
		s->append(_T("|FILE_WRITE_THROUGH"));
	if ((OpenCreateOption & FILE_SEQUENTIAL_ONLY) == FILE_SEQUENTIAL_ONLY)
		s->append(_T("|FILE_SEQUENTIAL_ONLY"));
	if ((OpenCreateOption & FILE_RANDOM_ACCESS) == FILE_RANDOM_ACCESS)
		s->append(_T("|FILE_RANDOM_ACCESS"));
	if ((OpenCreateOption & FILE_NO_INTERMEDIATE_BUFFERING) == FILE_NO_INTERMEDIATE_BUFFERING)
		s->append(_T("|FILE_NO_INTERMEDIATE_BUFFERING"));
	if ((OpenCreateOption & FILE_SYNCHRONOUS_IO_ALERT) == FILE_SYNCHRONOUS_IO_ALERT)
		s->append(_T("|FILE_SYNCHRONOUS_IO_ALERT"));
	if ((OpenCreateOption & FILE_SYNCHRONOUS_IO_NONALERT) == FILE_SYNCHRONOUS_IO_NONALERT)
		s->append(_T("|FILE_SYNCHRONOUS_IO_NONALERT"));
	if ((OpenCreateOption & FILE_CREATE_TREE_CONNECTION) == FILE_CREATE_TREE_CONNECTION)
		s->append(_T("|FILE_CREATE_TREE_CONNECTION"));
	if ((OpenCreateOption & FILE_NO_EA_KNOWLEDGE) == FILE_NO_EA_KNOWLEDGE)
		s->append(_T("|FILE_NO_EA_KNOWLEDGE"));
	if ((OpenCreateOption & FILE_OPEN_REPARSE_POINT) == FILE_OPEN_REPARSE_POINT)
		s->append(_T("|FILE_OPEN_REPARSE_POINT"));
	if ((OpenCreateOption & FILE_DELETE_ON_CLOSE) == FILE_DELETE_ON_CLOSE)
		s->append(_T("|FILE_DELETE_ON_CLOSE"));
	if ((OpenCreateOption & FILE_OPEN_BY_FILE_ID) == FILE_OPEN_BY_FILE_ID)
		s->append(_T("|FILE_OPEN_BY_FILE_ID"));
	if ((OpenCreateOption & FILE_OPEN_FOR_BACKUP_INTENT) == FILE_OPEN_FOR_BACKUP_INTENT)
		s->append(_T("|FILE_OPEN_FOR_BACKUP_INTENT"));
	if ((OpenCreateOption & FILE_RESERVE_OPFILTER) == FILE_RESERVE_OPFILTER)
		s->append(_T("|FILE_RESERVE_OPFILTER"));
	if ((OpenCreateOption & FILE_OPEN_REQUIRING_OPLOCK) == FILE_OPEN_REQUIRING_OPLOCK)
		s->append(_T("|FILE_OPEN_REQUIRING_OPLOCK"));
	if ((OpenCreateOption & FILE_COMPLETE_IF_OPLOCKED) == FILE_COMPLETE_IF_OPLOCKED)
		s->append(_T("|FILE_COMPLETE_IF_OPLOCKED"));
	if (s->length() > 0)
		(*s) = s->substr(1, s->length() - 1);
	else
		(*s) = _T("NA");
}

void KeyCreateOptionsToString(ULONG CreateOption, string* s){
	s->clear();
	if ((CreateOption & REG_OPTION_VOLATILE) == REG_OPTION_VOLATILE)
		s->append(_T("|REG_OPTION_VOLATILE"));
	if ((CreateOption & REG_OPTION_NON_VOLATILE) == REG_OPTION_NON_VOLATILE)
		s->append(_T("|REG_OPTION_NON_VOLATILE"));
	if ((CreateOption & REG_OPTION_CREATE_LINK) == REG_OPTION_CREATE_LINK)
		s->append(_T("|REG_OPTION_CREATE_LINK"));
	if ((CreateOption & REG_OPTION_BACKUP_RESTORE) == REG_OPTION_BACKUP_RESTORE)
		s->append(_T("|REG_OPTION_BACKUP_RESTORE"));

	if (s->length() > 0)
		(*s) = s->substr(1, s->length() - 1);
	else
		(*s) = _T("NA");
}

void NtStatusToString(NTSTATUS status, string* s)
{
	s->clear();
	*s = to_string(status);	
}

const wchar_t* CreateDispositionToString(ULONG CreateDisposition)
{
	switch (CreateDisposition)
	{
	case FILE_SUPERSEDE:
		return _T("FILE_SUPERSEDE");
		break;

	case FILE_CREATE:
		return _T("FILE_CREATE");
		break;

	case FILE_OPEN:
		return _T("FILE_OPEN");
		break;

	case FILE_OPEN_IF:
		return _T("FILE_OPEN_IF");
		break;

	case FILE_OVERWRITE:
		return _T("FILE_OVERWRITE");
		break;

	case FILE_OVERWRITE_IF:
		return _T("FILE_OVERWRITE_IF");
		break;

	default:
		return _T("NA"); // Should never happen...
	}
}

void GetHandleFileName(HANDLE hHandle, string* fname)
{
	FILE_FULL_DIR_INFO info;
	GetFileInformationByHandleEx(hHandle, FileFullDirectoryInfo,&info,sizeof(FILE_FULL_DIR_INFO));
	fname->clear();
	fname->append(info.FileName);
}

const wchar_t* KeyInformationClassToString(KEY_INFORMATION_CLASS keyinfo)
{
	
	switch (keyinfo)
	{
	case KeyBasicInformation:
		return _T("KeyBasicInformation");
	case KeyNodeInformation:
		return _T("KeyNodeInformation");
	case KeyFullInformation:
		return _T("KeyFullInformation");
	case KeyNameInformation:
		return _T("KeyNameInformation");
	case KeyCachedInformation:
		return _T("KeyCachedInformation");
	case KeyFlagsInformation:
		return _T("KeyFlagsInformation");
	case KeyVirtualizationInformation:
		return _T("KeyVirtualizationInformation");
	case KeyHandleTagsInformation:
		return _T("KeyHandleTagsInformation");
	case MaxKeyInfoClass:
		return _T("MaxKeyInfoClass");
	default:
		return _T("NA");
	}

}

void GetKeyPathFromKKEY(HANDLE key,string* s)
{
	if (key != NULL)
	{
		DWORD size = 0;
		DWORD result = 0;
		result = realNtQueryKey(key, KeyNameInformation, 0, 0, &size);
		if (result == STATUS_BUFFER_TOO_SMALL)
		{
			size = size + 2;
			wchar_t* buffer = new (std::nothrow) wchar_t[size / sizeof(wchar_t)]; // size is in bytes
			if (buffer != NULL)
			{
				result = realNtQueryKey(key, KeyNameInformation, buffer, size, &size);
				if (result == STATUS_SUCCESS)
				{
					buffer[size / sizeof(wchar_t)] = L'\0';
					*s = string(buffer + 2);
				}

				delete[] buffer;
			}
		}
		
	}
}

const wchar_t* KeyValueInformationClassToString(KEY_VALUE_INFORMATION_CLASS value_info_class)
{
	switch (value_info_class)
	{
	case KeyValueBasicInformation:
		return _T("KeyValueBasicInformation");
	case KeyValueFullInformation:
		return _T("KeyValueFullInformation");
	case KeyValuePartialInformation:
		return _T("KeyValuePartialInformation");
	case  KeyValueFullInformationAlign64:
		return _T("KeyValueFullInformationAlign64");
	case KeyValuePartialInformationAlign64:
		return _T("KeyValuePartialInformationAlign64");
	case MaxKeyValueInfoClass:
		return _T("MaxKeyValueInfoClass");
	default:
		return _T("N/A");
	}
}

// Taken from MICROSOFT DOCUMENTATION: https://msdn.microsoft.com/en-us/library/aa366789.aspx
BOOL GetFileNameFromHandle(HANDLE hFile, string* w)
{
	BOOL bSuccess = FALSE;
	TCHAR pszFilename[MAX_PATH + 1];
	HANDLE hFileMap;

	// Get the file size.
	DWORD dwFileSizeHi = 0;
	DWORD dwFileSizeLo = GetFileSize(hFile, &dwFileSizeHi);

	if (dwFileSizeLo == 0 && dwFileSizeHi == 0)
	{
		_tprintf(TEXT("Cannot map a file with a length of zero.\n"));
		return FALSE;
	}

	// Create a file mapping object.
	hFileMap = CreateFileMapping(hFile,
		NULL,
		PAGE_READONLY,
		0,
		1,
		NULL);

	if (hFileMap)
	{
		// Create a file mapping to get the file name.
		void* pMem = MapViewOfFile(hFileMap, FILE_MAP_READ, 0, 0, 1);

		if (pMem)
		{
			if (GetMappedFileName(GetCurrentProcess(),
				pMem,
				pszFilename,
				MAX_PATH))
			{

				// Translate path with device name to drive letters.
				TCHAR szTemp[BUFSIZE];
				szTemp[0] = '\0';

				if (GetLogicalDriveStrings(BUFSIZE - 1, szTemp))
				{
					TCHAR szName[MAX_PATH];
					TCHAR szDrive[3] = TEXT(" :");
					BOOL bFound = FALSE;
					TCHAR* p = szTemp;

					do
					{
						// Copy the drive letter to the template string
						*szDrive = *p;

						// Look up each device name
						if (QueryDosDevice(szDrive, szName, MAX_PATH))
						{
							size_t uNameLen = _tcslen(szName);

							if (uNameLen < MAX_PATH)
							{
								bFound = _tcsnicmp(pszFilename, szName, uNameLen) == 0
									&& *(pszFilename + uNameLen) == _T('\\');

								if (bFound)
								{
									// Reconstruct pszFilename using szTempFile
									// Replace device path with DOS path
									TCHAR szTempFile[MAX_PATH];
									StringCchPrintf(szTempFile,
										MAX_PATH,
										TEXT("%s%s"),
										szDrive,
										pszFilename + uNameLen);
									StringCchCopyN(pszFilename, MAX_PATH + 1, szTempFile, _tcslen(szTempFile));
								}
							}
						}

						// Go to the next NULL character.
						while (*p++);
					} while (!bFound && *p); // end of string
				}
			}
			bSuccess = TRUE;
			UnmapViewOfFile(pMem);
		}

		CloseHandle(hFileMap);
	}
	*w = string(pszFilename);
	return(bSuccess);
}

void from_unicode_to_wstring(PUNICODE_STRING u, string* w)
{
	int len = wcsnlen_s(u->Buffer, u->Length);
	w->clear();
	wchar_t* tmp = (wchar_t*)malloc(sizeof(wchar_t)*(u->Length + 1));
	if (tmp == NULL)
		throw "Error in from_unicode_to_string while allocating memory";
	for (int i = 0; i < len; i++)
	{
		tmp[i] = u->Buffer[i];
	}
	tmp[len] = L'\0';
	w->append(tmp);
	free(tmp);
}

void notifyNewPid(DWORD parentPid, DWORD childPid)
{
	pugi::xml_document doc;
	pugi::xml_node element = doc.append_child(WK_PROCESS_EVENT);
	std::wstring s;

	element.addAttribute(WK_PROCESS_EVENT_TYPE, WK_PROCESS_EVENT_TYPE_SPAWNED);
	s.clear();

	s.append(std::to_wstring(parentPid));
	element.addAttribute(WK_PROCESS_EVENT_PARENT_PID, s.c_str());
	s.clear();

	s.append(std::to_wstring(childPid));
	element.addAttribute(WK_PROCESS_EVENT_PID, s.c_str());

	sendToEventPipe(&element);
}

void notifyRemovedPid(HWND cwHandle, DWORD pid)
{
	pugi::xml_document doc;
	pugi::xml_node element = doc.append_child(WK_PROCESS_EVENT);
	std::wstring s;

	element.addAttribute(WK_PROCESS_EVENT_TYPE, WK_PROCESS_EVENT_TYPE_DEAD);
	s.clear();

	s.append(std::to_wstring(pid));
	element.addAttribute(WK_PROCESS_EVENT_PID, s.c_str());
	
	sendToEventPipe(&element);

}