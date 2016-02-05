#include "dllmain.h"

/* Global variables */
HWND cwHandle;
HMODULE ntdllmod;
HMODULE kern32dllmod;
HMODULE wsmod;
HMODULE ws2mod;

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
		
		
		tmplog.append(_T("Attached to process"));
		tmplog.append(to_string(GetCurrentProcessId()));

		//wsprintf(msgbuf, _T("Attached to process %d."), GetCurrentProcessId());
		OutputDebugString(tmplog.c_str());
		

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
		//realNtClose = (pNtClose)(GetProcAddress(ntdllmod, "NtClose"));
		realCreateProcessA = (pCreateProcessA)(GetProcAddress(kern32dllmod, "CreateProcessA"));
		realCreateProcessW = (pCreateProcessA)(GetProcAddress(kern32dllmod, "CreateProcessW"));
		realNtQueryInformationProcess = (pNtQueryInformationProcess)(GetProcAddress(ntdllmod, "NtQueryInformationProcess"));
		realExitProcess = (pExitProcess)ExitProcess;
		/*
		// WinSock 1
		realConnect = (pConnect)(GetProcAddress(wsmod, "connect"));
		realSend = (pSend)(GetProcAddress(wsmod, "send"));
		// WinSock 2
		realWSARecv = (pWSARecv)(GetProcAddress(ws2mod, "WSARecv"));
		realWSASend = (pWSASend)(GetProcAddress(ws2mod, "WSASend"));
		realWSAConnect = (pWSAConnect)(GetProcAddress(ws2mod,"WSAConnect"));
		realWSAConnectByName = (pWSAConnectByName)(GetProcAddress(ws2mod, "WSAConnectByName"));
		*/

		// Read from shared memory the name of the window to send message to
		if (!configureWindowName())
		{
			OutputDebugString(_T("It was impossible to read get the window name to which send messages. I will terminate."));
			exit(-1);
		}
		
		DisableThreadLibraryCalls(hDLL);
		
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
		/*
		// NtClose
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtClose, MyNtClose);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtClose not derouted correctly"));
		else
			OutputDebugString(_T("NtClose successfully"));
			*/
		// CreateProcessA
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realCreateProcessA, MyCreateProcessA);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("CreateProcessA not derouted correctly"));
		else
			OutputDebugString(_T("CreateProcessA successful"));

		// CreateProcessW
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realCreateProcessW, MyCreateProcessW);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("CreateProcessW not derouted correctly"));
		else
			OutputDebugString(_T("CreateProcessW successful"));

		// ExitProcess
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realExitProcess, MyExitProcess);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("ExitProcess not derouted correctly"));
		else
			OutputDebugString(_T("ExitProcess successful"));
		


		/*
		// Winsock Connect
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realConnect, MyConnect);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock Connect not derouted correctly"));
		else
			OutputDebugString(_T("Winsock Connect derouted successfully"));

		// Winsock2 WSAConnect
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realWSAConnect, MyWSAConnect);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock2 WSAConnect not derouted correctly"));
		else
			OutputDebugString(_T("Winsock2 WSAConnect derouted successfully"));

		// Winsock2 WSAConnectByName
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realWSAConnectByName, MyWSAConnectByName);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock2 WSAConnectByName not derouted correctly"));
		else
			OutputDebugString(_T("Winsock2 WSAConnectByName derouted successfully"));

		// Winsock2 WSASend
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realWSASend, MyWSASend);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock WSASend not derouted correctly"));
		else
			OutputDebugString(_T("Winsock WSASend derouted successfully"));

		// Winsock2 WSARecv
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realWSARecv, MyWSARecv);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock WSARecv not derouted correctly"));
		else
			OutputDebugString(_T("Winsock WSARecv derouted successfully"));

		// Winsock Send
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realSend, MySend);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock Send not derouted correctly"));
		else
			OutputDebugString(_T("Winsock Send derouted successfully"));
		*/

		break;

	case DLL_PROCESS_DETACH:
		OutputDebugString(_T("PROCESS DETACHED FROM DLL."));
		
		DisableThreadLibraryCalls(hDLL);
		
		// NtClose
		/*
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realNtClose, MyNtClose);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtClose not detached correctly"));
		else
			OutputDebugString(_T("NtClose detached successfully"));
			*/
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

		// CreateProcessA
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realCreateProcessA, MyCreateProcessA);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("CreateProcessA not detached correctly"));
		else
			OutputDebugString(_T("CreateProcessA detached successfully"));

		// CreateProcessW
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realCreateProcessW, MyCreateProcessW);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("CreateProcessW not detached correctly"));
		else
			OutputDebugString(_T("CreateProcessW detached successfully"));


		/*
		// Sock connect
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realConnect, MyConnect);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock Connect not detached correctly"));
		else
			OutputDebugString(_T("Winsock Connect detached successfully"));

		// Sock2 WSAConnect
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realWSAConnect, MyWSAConnect);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock2 WSAConnect not detached correctly"));
		else
			OutputDebugString(_T("Winsock2 WSAConnect detached successfully"));

		// Sock2 WSAConnectByName
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realWSAConnectByName, MyWSAConnectByName);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock2 WSAConnectByName not detached correctly"));
		else
			OutputDebugString(_T("Winsock2 WSAConnectByName detached successfully"));

		// Sock2 WSARecv
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realWSARecv, MyWSARecv);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock2 WSARecv not detached correctly"));
		else
			OutputDebugString(_T("Winsock2 WSARecv detached successfully"));


		// Sock WSASend
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realWSASend, MyWSASend);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock WSASend not detached correctly"));
		else
			OutputDebugString(_T("Winsock WSASend detached successfully"));

		// Sock Send
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realSend, MySend);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("Winsock Send not detached correctly"));
		else
			OutputDebugString(_T("Winsock Send detached successfully"));

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
	
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtCreateFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock, AllocationSize, FileAttributes, ShareAccess, CreateDisposition, CreateOptions, EaBuffer, EaLength);
	
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;
	pugi::xml_node element = doc.append_child(_T("NtCreateFile"));
	
	// Write Access Mask: parse the flags
	string s = string();
	FileAccessMaskToString(DesiredAccess, &s);
	element.addAttribute(_T("AccessMask"), s.c_str());
	
	// >>>>>>>>>>>>>>> ObjectAttributes (File Path) <<<<<<<<<<<<<<<
	if (ObjectAttributes->RootDirectory != NULL)
	{
		// The path specified in ObjectName is relative to the directory handle. Get the path of that directory
		s.clear();
		GetHandleFileName(ObjectAttributes->RootDirectory, &s);
		element.addAttribute(_T("DirPath"), s.c_str());
		s.clear();
		from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
		element.addAttribute(_T("Path"), s.c_str());
	}
	else
	{
		// The objectname contains a full path to the file
		s.clear();
		from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
		element.addAttribute(_T("Path"), s.c_str());
	}
	
	
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
		wsprintf(buff, _T("%p"), *FileHandle);
		element.addAttribute(_T("Handle"), buff);
	}
	

	log(&element);

	return res;
	
}
NTSTATUS WINAPI MyNtOpenFile(PHANDLE FileHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PIO_STATUS_BLOCK IoStatusBlock, ULONG ShareAccess, ULONG OpenOptions)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock, ShareAccess, OpenOptions);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtOpenFile"));

	string s = string();
	// >>>>>>>>>>>>>>> File Path <<<<<<<<<<<<<<<
	if (ObjectAttributes->RootDirectory != NULL)
	{
		// The path specified in ObjectName is relative to the directory handle. Get the path of that directory
		s.clear();
		GetHandleFileName(ObjectAttributes->RootDirectory, &s);
		element.addAttribute(_T("DirPath"), s.c_str());
		s.clear();
		from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
		element.addAttribute(_T("Path"), s.c_str());
	}
	else
	{
		// The objectname contains a full path to the file
		s.clear();
		from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
		element.addAttribute(_T("Path"), s.c_str());
	}

	
	// >>>>>>>>>>>>>>> DESIRED ACCESS <<<<<<<<<<<<<<<
	s.clear();
	FileAccessMaskToString(DesiredAccess, &s);
	element.addAttribute(_T("DesiredAccess"), s.c_str());

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
		wsprintf(buff, _T("%p"), *FileHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtDeleteFile(POBJECT_ATTRIBUTES ObjectAttributes)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtDeleteFile(ObjectAttributes);

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

	return res;

}
NTSTATUS WINAPI MyNtOpenDirectoryObject(PHANDLE DirectoryObject, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenDirectoryObject(DirectoryObject, DesiredAccess, ObjectAttributes);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtOpenDirectoryObject"));
	
	string s = string();

	// >>>>>>>>>>>>>>> File Path <<<<<<<<<<<<<<<
	s.clear();
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
	element.addAttribute(_T("Path"), s.c_str());

	// >>>>>>>>>>>>>>> DESIRED ACCESS <<<<<<<<<<<<<<<
	s.clear();
	DirectoryAccessMaskToString(DesiredAccess, &s);
	element.addAttribute(_T("DesiredAccess"), s.c_str());
	
	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	s.clear();
	NtStatusToString(res, &s);
	element.addAttribute(_T("Result"), s.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("%p"), *DirectoryObject);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtOpenKey(PHANDLE KeyHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenKey(KeyHandle, DesiredAccess, ObjectAttributes);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtOpenKey"));
	string w = string();

	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	w.clear();
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &w);
	element.addAttribute(_T("Path"), w.c_str());

	// >>>>>>>>>>>>>>> Access Mask <<<<<<<<<<<<<<<
	w.clear();
	KeyAccessMaskToString(DesiredAccess, &w);
	element.addAttribute(_T("DesiredAccess"), w.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w.clear();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("%p"), *KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtCreateKey(PHANDLE KeyHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, ULONG TitleIndex, PUNICODE_STRING Class, ULONG CreateOptions, PULONG Disposition)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtCreateKey(KeyHandle, DesiredAccess, ObjectAttributes, TitleIndex, Class, CreateOptions, Disposition);
	
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtCreateKey"));
	string w = string();
	
	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &w);
	element.addAttribute(_T("Path"), w.c_str());
	
	// >>>>>>>>>>>>>>> Access Mask <<<<<<<<<<<<<<<
	w.clear();
	KeyAccessMaskToString(DesiredAccess, &w);
	element.addAttribute(_T("DesiredAccess"), w.c_str());
	
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
		wsprintf(buff, _T("%p"), *KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);
	
	return res;	
}
NTSTATUS WINAPI MyNtQueryKey(HANDLE KeyHandle, KEY_INFORMATION_CLASS KeyInformationClass, PVOID KeyInformation, ULONG Length, PULONG ResultLength)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryKey(KeyHandle,KeyInformationClass,KeyInformation,Length,ResultLength);

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
		wsprintf(buff, _T("%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	return res;

}
NTSTATUS WINAPI MyNtDeleteKey(HANDLE KeyHandle)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtDeleteKey(KeyHandle);

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
		wsprintf(buff, _T("%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtDeleteValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName){

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtDeleteValueKey(KeyHandle, ValueName);

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
		wsprintf(buff, _T("%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);
	return res;
}
NTSTATUS WINAPI MyNtEnumerateKey(HANDLE KeyHandle, ULONG Index, KEY_INFORMATION_CLASS KeyInformationClass, PVOID KeyInformation, ULONG Length, PULONG ResultLength)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtEnumerateKey(KeyHandle,Index,KeyInformationClass,KeyInformation,Length,ResultLength);

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
		wsprintf(buff, _T("%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtEnumerateValueKey(HANDLE KeyHandle, ULONG Index, KEY_VALUE_INFORMATION_CLASS KeyValueInformationClass, PVOID KeyValueInformation, ULONG Length, PULONG ResultLength)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtEnumerateValueKey(KeyHandle, Index, KeyValueInformationClass, KeyValueInformation, Length, ResultLength);

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
		wsprintf(buff, _T("%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);
	return res;
}
NTSTATUS WINAPI MyNtLockFile(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine, PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, PLARGE_INTEGER ByteOffset, PLARGE_INTEGER Length, ULONG Key, BOOLEAN FailImmediately, BOOLEAN ExclusiveLock)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtLockFile(FileHandle,Event,ApcRoutine,ApcContext,IoStatusBlock,ByteOffset,Length,Key,FailImmediately,ExclusiveLock);

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
		wsprintf(buff, _T("%p"), FileHandle);
		element.addAttribute(_T("Handle"),buff);
	}

	log(&element);

	return res;
}
/*
NTSTATUS WINAPI MyNtOpenProcess(PHANDLE ProcessHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PCLIENT_ID ClientId)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenProcess(ProcessHandle,DesiredAccess,ObjectAttributes,ClientId);
	
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
	

	log(&element);
	
	return res;
}
*/
NTSTATUS WINAPI MyNtQueryDirectoryFile(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine, PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, PVOID FileInformation, ULONG Length, FILE_INFORMATION_CLASS FileInformationClass, BOOLEAN ReturnSingleEntry, PUNICODE_STRING FileName, BOOLEAN RestartScan)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryDirectoryFile(FileHandle,Event,ApcRoutine, ApcContext, IoStatusBlock, FileInformation, Length,FileInformationClass,ReturnSingleEntry,FileName,RestartScan);

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
		wsprintf(buff, _T("%p"), FileHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtQueryFullAttributesFile(POBJECT_ATTRIBUTES ObjectAttributes, PFILE_NETWORK_OPEN_INFORMATION FileInformation)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryFullAttributesFile(ObjectAttributes, FileInformation);

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

	return res;
}
NTSTATUS WINAPI MyNtQueryValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName, KEY_VALUE_INFORMATION_CLASS KeyValueInformationClass, PVOID KeyValueInformation, ULONG Length, PULONG ResultLength)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryValueKey(KeyHandle, ValueName, KeyValueInformationClass, KeyValueInformation, Length, ResultLength);
	
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
		wsprintf(buff, _T("%p"), KeyHandle);
		element.addAttribute(_T("Handle"), buff);
	}

	log(&element);
	
	return res;
}
NTSTATUS WINAPI MyNtSetInformationFile(HANDLE FileHandle, PIO_STATUS_BLOCK IoStatusBlock, PVOID FileInformation, ULONG Length, FILE_INFORMATION_CLASS FileInformationClass)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtSetInformationFile(FileHandle,IoStatusBlock,FileInformation, Length,FileInformationClass);
	
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
		wsprintf(buff, _T("%p"), FileHandle);
		element.addAttribute(_T("Handle"),buff);
	}

	log(&element);
	return res;
}
NTSTATUS WINAPI MyNtSetValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName, ULONG TitleIndex, ULONG Type, PVOID Data, ULONG DataSize)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtSetValueKey(KeyHandle,ValueName,TitleIndex,Type,Data,DataSize);

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
		wsprintf(buff, _T("%p"), KeyHandle);
		element.addAttribute(_T("Handle"),buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtTerminateProcess(HANDLE ProcessHandle, NTSTATUS ExitStatus)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtTerminateProcess(ProcessHandle,ExitStatus);

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
		wsprintf(buff, _T("%p"), ProcessHandle);
		element.addAttribute(_T("Handle"),buff);
	}

	log(&element);

	return res;
}
/*NTSTATUS WINAPI MyNtClose(HANDLE Handle)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtClose(Handle);
	
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(_T("NtClose")); 

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	string w = string();
	NtStatusToString(res, &w);
	element.addAttribute(_T("Result"), w);
	

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, _T("%p"), Handle);
		element.addAttribute(_T("Handle"), string(buff));
	}

	log(&element);
	
	return res;
}*/


// TODO: NtCreateProcess?

BOOL WINAPI MyCreateProcessA(LPCTSTR lpApplicationName, LPTSTR lpCommandLine, LPSECURITY_ATTRIBUTES lpProcessAttributes, LPSECURITY_ATTRIBUTES lpThreadAttributes, BOOL bInheritHandles, DWORD dwCreationFlags, LPVOID lpEnvironment, LPCTSTR lpCurrentDirectory, LPSTARTUPINFO lpStartupInfo, LPPROCESS_INFORMATION lpProcessInformation)
{
	CHAR   DllPath[MAX_PATH] = { 0 };
	GetModuleFileNameA((HINSTANCE)&__ImageBase, DllPath, _countof(DllPath));

	// Use directly the Detours API
	BOOL res = DetourCreateProcessWithDll(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation,DllPath,realCreateProcessA);
	if (!res) 
		OutputDebugString(_T("There was a problem when injecting the DLL to the new process via MyCreateProcessA()."));
	else{
		notifyNewPid(lpProcessInformation->dwProcessId);
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
	GetModuleFileNameA((HINSTANCE)&__ImageBase, DllPath, _countof(DllPath));

	// Use directly the Detours API
	BOOL res = DetourCreateProcessWithDll(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation, DllPath, realCreateProcessW);
	if (!res)
		OutputDebugString(_T("There was a problem when injecting the DLL to the new process via MyCreateProcessW()."));
	else {
		notifyNewPid(lpProcessInformation->dwProcessId);
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

VOID WINAPI MyExitProcess(UINT uExitCode)
{
	// We hook this to let the GuestController know about our intention to terminate
	notifyRemovedPid(GetCurrentProcessId());
	return realExitProcess(uExitCode);
}

/*
// Winsock
int WINAPI MySend(SOCKET s, const char *buf, int len, int flags)
{
	//OutputDebugString(_T("SEEEEEND!"));
	return realSend(s, buf, len, flags);
}

int WINAPI MyWSAConnect(SOCKET s, const struct sockaddr* name, int namelen, LPWSABUF lpCallerData, LPWSABUF lpCalleeData, LPQOS lpSQOS, LPQOS lpGQOS)
{
	OutputDebugString(_T("WSAConnect!!!!!"));
	return realWSAConnect(s, name, namelen, lpCallerData, lpCalleeData, lpSQOS, lpGQOS);
}

int WINAPI MyConnect(SOCKET s, const struct sockaddr* name, int namelen)
{
	OutputDebugString(_T("Connect!!!!!"));
	return realConnect(s, name, namelen);
}

BOOL WINAPI MyWSAConnectByName(SOCKET s, LPTSTR nodename, LPTSTR servicename, LPDWORD LocalAddressLength, LPSOCKADDR LocalAddress, LPDWORD RemoteAddressLength, LPSOCKADDR RemoteAddress, const struct timeval *timeout, LPWSAOVERLAPPED Reserved)
{
	OutputDebugString(_T("MyWSAConnectByName!!!!!"));
	return realWSAConnectByName(s, nodename,servicename, LocalAddressLength,LocalAddress, RemoteAddressLength, RemoteAddress, timeout, Reserved);
}

int WINAPI MyWSASend(SOCKET s, LPWSABUF lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesSent, DWORD dwFlags, LPWSAOVERLAPPED lpOverlapped, LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine)
{
	OutputDebugString(_T("WSAsend!!!!"));
	return realWSASend(s, lpBuffers,dwBufferCount, lpNumberOfBytesSent, dwFlags, lpOverlapped, lpCompletionRoutine);
}

int WINAPI MyWSARecv(SOCKET s, LPWSABUF lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesRecvd, LPDWORD lpFlags, LPWSAOVERLAPPED lpOverlapped, LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine)
{
	OutputDebugString(_T("WSARecv!!!!"));
	return realWSARecv(s,lpBuffers,dwBufferCount,lpNumberOfBytesRecvd,lpFlags,lpOverlapped,lpCompletionRoutine);
}
*/
extern "C" __declspec(dllexport)VOID NullExport(VOID)
{
}


void log(pugi::xml_node *element)
{
	DWORD res = 0;
	COPYDATASTRUCT ds;

	element->append_attribute(_T("ThreadId")) = to_string(GetCurrentThreadId()).c_str();
	element->append_attribute(_T("PId")) = to_string(GetCurrentProcessId()).c_str();

	/*
	std::wstringstream ss;
	element->print(ss, _T(""), pugi::format_no_declaration|pugi::format_raw);
	
	
	// Create a string and copy your document data in to the string    
	string str = ss.str();

	ds.dwData = 0;
	ds.cbData = str.size()*sizeof(wchar_t);
	ds.lpData = (wchar_t*)str.c_str();
		
	// Send message...
	SendMessage(cwHandle, WM_COPYDATA, 0, (LPARAM)&ds);
	*/

	// We use a wchart_t type for buffer
	
	std::wostringstream ss;
	element->print(ss, _T(""), pugi::format_no_declaration | pugi::format_raw,pugi::xml_encoding::encoding_utf16_le);
	
	std::wstring s = ss.str();
	const wchar_t* str = s.c_str();

	ds.dwData = COPYDATA_LOG;
	ds.cbData = s.length()*sizeof(wchar_t); 
	ds.lpData = (PVOID)str;

	// Send message...
	SendMessage(cwHandle, WM_COPYDATA, 0, (LPARAM)&ds);
	
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

void notifyRemovedPid(DWORD pid)
{
	DWORD res = 0;
	COPYDATASTRUCT ds;

	ds.dwData = COPYDATA_PROC_DIED;
	ds.cbData = sizeof(DWORD);
	ds.lpData = (PVOID)&pid;

	// Send message...
	SendMessage(cwHandle, WM_COPYDATA, 0, (LPARAM)&ds);

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

void FileAccessMaskToString(ACCESS_MASK DesiredAccess, string* s)
{
	bool mustCut = false;
	StandardAccessMaskToString(DesiredAccess, s);

	if (s->length() == 0)
		mustCut = true;

	if ((DesiredAccess & FILE_READ_DATA) == FILE_READ_DATA)
		s->append(_T("|FILE_READ_DATA"));
	if ((DesiredAccess & FILE_READ_ATTRIBUTES) == FILE_READ_ATTRIBUTES)
		s->append(_T("|FILE_READ_ATTRIBUTES"));
	if ((DesiredAccess & FILE_READ_EA) == FILE_READ_EA)
		s->append(_T("|FILE_READ_EA"));
	if ((DesiredAccess & FILE_WRITE_DATA) == FILE_WRITE_DATA)
		s->append(_T("|FILE_WRITE_DATA"));
	if ((DesiredAccess & FILE_WRITE_ATTRIBUTES) == FILE_WRITE_ATTRIBUTES)
		s->append(_T("|FILE_WRITE_ATTRIBUTES"));
	if ((DesiredAccess & FILE_WRITE_EA) == FILE_WRITE_EA)
		s->append(_T("|FILE_WRITE_EA"));
	if ((DesiredAccess & FILE_APPEND_DATA) == FILE_APPEND_DATA)
		s->append(_T("|FILE_APPEND_DATA"));
	if ((DesiredAccess & FILE_EXECUTE) == FILE_EXECUTE)
		s->append(_T("|FILE_EXECUTE"));
	if ((DesiredAccess & FILE_LIST_DIRECTORY) == FILE_LIST_DIRECTORY)
		s->append(_T("|FILE_LIST_DIRECTORY"));
	if ((DesiredAccess & FILE_TRAVERSE) == FILE_TRAVERSE)
		s->append(_T("|FILE_TRAVERSE"));

	if (s->length() > 0 && mustCut)
	{
		(*s) = s->substr(1, s->length() - 1);
	}
}

void DirectoryAccessMaskToString(ACCESS_MASK DesiredAccess, string* s)
{
	bool mustCut = false;
	StandardAccessMaskToString(DesiredAccess, s);

	if (s->length() == 0)
		mustCut = true;

	if ((DesiredAccess & 0x0001) == 0x0001)
		s->append(_T("|DIRECTORY_QUERY"));
	if ((DesiredAccess & FILE_READ_DATA) == FILE_READ_DATA)
		s->append(_T("|DIRECTORY_TRAVERSE"));

	if ((DesiredAccess & 0x0002) == 0x0002)
		s->append(_T("|FILE_READ_ATTRIBUTES"));

	if ((DesiredAccess & 0x0004) == 0x0004)
		s->append(_T("|DIRECTORY_CREATE_OBJECT"));

	if ((DesiredAccess & 0x0008) == 0x0008)
		s->append(_T("|DIRECTORY_CREATE_SUBDIRECTORY"));

	if ((DesiredAccess & (STANDARD_RIGHTS_REQUIRED | 0xF)) == (STANDARD_RIGHTS_REQUIRED | 0xF))
		s->append(_T("|DIRECTORY_ALL_ACCESS"));

	if (s->length() > 0 && mustCut)
	{
		(*s) = s->substr(1, s->length() - 1);
	}
}

void KeyAccessMaskToString(ACCESS_MASK DesiredAccess, string* s)
{
	bool mustCut = false;
	StandardAccessMaskToString(DesiredAccess, s);

	if (s->length() == 0)
		mustCut = true;

	if ((DesiredAccess & KEY_QUERY_VALUE) == KEY_QUERY_VALUE)
		s->append(_T("|KEY_QUERY_VALUE"));
	if ((DesiredAccess & KEY_ENUMERATE_SUB_KEYS) == KEY_ENUMERATE_SUB_KEYS)
		s->append(_T("|KEY_ENUMERATE_SUB_KEYS"));
	if ((DesiredAccess & KEY_NOTIFY) == KEY_NOTIFY)
		s->append(_T("|KEY_NOTIFY"));
	if ((DesiredAccess & KEY_SET_VALUE) == KEY_SET_VALUE)
		s->append(_T("|KEY_SET_VALUE"));
	if ((DesiredAccess & KEY_CREATE_SUB_KEY) == KEY_CREATE_SUB_KEY)
		s->append(_T("|KEY_CREATE_SUB_KEY"));
	if ((DesiredAccess & KEY_CREATE_LINK) == KEY_CREATE_LINK)
		s->append(_T("|KEY_CREATE_LINK"));

	if (s->length() > 0 && mustCut)
	{
		(*s) = s->substr(1, s->length() - 1);
	}

}

void StandardAccessMaskToString(ACCESS_MASK DesiredAccess, string* s)
{
	s->clear();

	if ((DesiredAccess & DELETE) == DELETE)
		s->append(_T("|DELETE"));

	if ((DesiredAccess & READ_CONTROL) == READ_CONTROL)
		s->append(_T("|READ_CONTROL"));

	if ((DesiredAccess & SYNCHRONIZE) == SYNCHRONIZE)
		s->append(_T("|SYNCHRONIZE"));

	if ((DesiredAccess & WRITE_DAC) == WRITE_DAC)
		s->append(_T("|WRITE_DAC"));

	if ((DesiredAccess & WRITE_OWNER) == WRITE_OWNER)
		s->append(_T("|WRITE_OWNER"));

	if ((DesiredAccess & GENERIC_READ) == GENERIC_READ)
		s->append(_T("|GENERIC_READ"));

	if ((DesiredAccess & GENERIC_WRITE) == GENERIC_WRITE)
		s->append(_T("|GENERIC_WRITE"));

	if ((DesiredAccess & GENERIC_EXECUTE) == GENERIC_EXECUTE)
		s->append(_T("|GENERIC_EXECUTE"));

	if ((DesiredAccess & GENERIC_ALL) == GENERIC_ALL)
		s->append(_T("|GENERIC_ALL"));

	if (s->length() > 0)
		(*s) = s->substr(1, s->length() - 1);
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
	*s = to_string((unsigned long)status);	
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
/*
const wchar_t* FileInformationClassToString(FILE_INFORMATION_CLASS FileInformationClass)
{
	switch (FileInformationClass)
	{
	case FileBasicInformation:
		return _T("FileBasicInformation";
	case FileDispositionInformation:
		return _T("FileDispositionInformation";
	case FileEndOfFileInformation:
		return _T("FileEndOfFileInformation";
	case FileIoPriorityHintInformation:
		return _T("FileIoPriorityHintInformation";
	case FileLinkInformation:
		return _T("FileLinkInformation";
	case FilePositionInformation:
		return _T("FilePositionInformation";
	case FileRenameInformation:
		return _T("FileRenameInformation";
	case FileShortNameInformation:
		return _T("FileShortNameInformation";
	case FileValidDataLengthInformation:
		return _T("FileValidDataLengthInformation";
	case FileReplaceCompletionInformation:
		return _T("FileReplaceCompletionInformation";
	default:
		return _T("N/A";
	}
}
*/

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

bool configureWindowName()
{
	TCHAR buf[SHMEMSIZE];
	buf[0] = '\0';

	cwHandle = FindWindow(NULL, GUESTCONTROLLER_WINDOW_NAME);

	if (cwHandle == NULL)
	{
		return false;
	}

	_stprintf_s(buf, _T("Sending events to window name: %s"), GUESTCONTROLLER_WINDOW_NAME);

	OutputDebugString(buf);

	return true;
}