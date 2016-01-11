#include "dllmain.h"
using namespace std;

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

		// Configure Pcap

		OutputDebugString(_T("PROCESS ATTACHED TO DLL."));
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
			OutputDebugString(_T("NtCreateFile successfully"));

		// NtOpenFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtOpenFile, MyNtOpenFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtOpenFile not derouted correctly"));
		else
			OutputDebugString(_T("NtOpenFile successfully"));

		// NtDeleteFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtDeleteFile, MyNtDeleteFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtDeleteFile not derouted correctly"));
		else
			OutputDebugString(_T("NtDeleteFile successfully"));

		// NtDeleteFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtOpenDirectoryObject, MyNtOpenDirectoryObject);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtOpenDirectoryObject not derouted correctly"));
		else
			OutputDebugString(_T("NtOpenDirectoryObject successfully"));

		// NtCreateKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtCreateKey, MyNtCreateKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtCreateKey not derouted correctly"));
		else
			OutputDebugString(_T("NtCreateKey successfully"));

		// NtOpenKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtOpenKey, MyNtOpenKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtOpenKey not derouted correctly"));
		else
			OutputDebugString(_T("NtOpenKey successfully"));

		// NtDeleteKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtDeleteKey, MyNtDeleteKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtDeleteKey not derouted correctly"));
		else
			OutputDebugString(_T("NtDeleteKey successfully"));

		// NtQueryKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtQueryKey, MyNtQueryKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryKey not derouted correctly"));
		else
			OutputDebugString(_T("NtQueryKey successfully"));

		// NtDeleteValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtDeleteValueKey, MyNtDeleteValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtDeleteValueKey not derouted correctly"));
		else
			OutputDebugString(_T("NtDeleteValueKey successfully"));

		// NtEnumerateKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtEnumerateKey, MyNtEnumerateKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtEnumerateKey not derouted correctly"));
		else
			OutputDebugString(_T("NtEnumerateKey successfully"));

		// NtEnumerateValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtEnumerateValueKey, MyNtEnumerateValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtEnumerateValueKey not derouted correctly"));
		else
			OutputDebugString(_T("NtEnumerateValueKey successfully"));

		// NtLockFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtLockFile, MyNtLockFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtLockFile not derouted correctly"));
		else
			OutputDebugString(_T("NtLockFile successfully"));

		//DetourAttach(&(PVOID&)realNtOpenProcess, MyNtOpenProcess);

		// NtQueryDirectoryFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtQueryDirectoryFile, MyNtQueryDirectoryFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryDirectoryFile not derouted correctly"));
		else
			OutputDebugString(_T("NtQueryDirectoryFile successfully"));

		// NtQueryFullAttributesFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtQueryFullAttributesFile, MyNtQueryFullAttributesFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryFullAttributesFile not derouted correctly"));
		else
			OutputDebugString(_T("NtQueryFullAttributesFile successfully"));

		// NtQueryValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtQueryValueKey, MyNtQueryValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtQueryValueKey not derouted correctly"));
		else
			OutputDebugString(_T("NtQueryValueKey successfully"));

		// NtSetInformationFile
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtSetInformationFile, MyNtSetInformationFile);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtSetInformationFile not derouted correctly"));
		else
			OutputDebugString(_T("NtSetInformationFile successfully"));

		// NtSetValueKey
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtSetValueKey, MyNtSetValueKey);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtSetValueKey not derouted correctly"));
		else
			OutputDebugString(_T("NtSetValueKey successfully"));

		// NtTerminateProcess
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realNtTerminateProcess, MyNtTerminateProcess);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("NtTerminateProcess not derouted correctly"));
		else
			OutputDebugString(_T("NtTerminateProcess successfully"));
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
			OutputDebugString(_T("CreateProcessA successfully"));

		// CreateProcessW
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realCreateProcessW, MyCreateProcessW);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(_T("CreateProcessW not derouted correctly"));
		else
			OutputDebugString(_T("CreateProcessW derotuersuccessfully"));
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
	pugi::xml_node element = doc.append_child(L"NtCreateFile");
	
	// Write Access Mask: parse the flags
	wstring s = wstring();
	FileAccessMaskToString(DesiredAccess, &s);
	element.addAttribute(L"AccessMask", s.c_str());
	
	// >>>>>>>>>>>>>>> ObjectAttributes (File Path) <<<<<<<<<<<<<<<
	if (ObjectAttributes->RootDirectory != NULL)
	{
		// The path specified in ObjectName is relative to the directory handle. Get the path of that directory
		s.clear();
		GetHandleFileName(ObjectAttributes->RootDirectory, &s);
		element.addAttribute(L"DirPath", s.c_str());
		s.clear();
		from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
		element.addAttribute(L"Path", s.c_str());
	}
	else
	{
		// The objectname contains a full path to the file
		s.clear();
		from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
		element.addAttribute(L"Path", s.c_str());
	}
	
	
	// >>>>>>>>>>>>>>> IO_STATUS_BLOCK <<<<<<<<<<<<<<<
	// Write IO status block: parse its 
	// value and write it to the xml node
	s.clear();
	IoStatusToString(IoStatusBlock, &s);
	element.addAttribute(L"IoStatusBlock", s.c_str());

	// >>>>>>>>>>>>>>> AllocationSize <<<<<<<<<<<<<<<
	// Skipping allocation size: I don't need it
	
	// >>>>>>>>>>>>>>> FileAttributes <<<<<<<<<<<<<<<
	s.clear();
	FileAttributesToString(FileAttributes, &s);
	element.addAttribute(L"FileAttributes", s.c_str());

	// >>>>>>>>>>>>>>> ShareAccess <<<<<<<<<<<<<<<
	s.clear();
	ShareAccessToString(ShareAccess, &s);
	element.addAttribute(L"ShareAccess", s.c_str());

	// Create Disposition
	s.clear();
	s = CreateDispositionToString(CreateDisposition);
	element.addAttribute(L"CreateDisposition", s.c_str());

	// >>>>>>>>>>>>>>> CreateOptions <<<<<<<<<<<<<<<
	s.clear();
	FileCreateOptionsToString(CreateOptions, &s);
	element.addAttribute(L"CreateOptions", s.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	s.clear();
	NtStatusToString(res, &s);
	element.addAttribute(L"Result", s.c_str());

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", *FileHandle);
		element.addAttribute(L"Handle", buff);
	}
	

	log(&element);

	return res;
	
}
NTSTATUS WINAPI MyNtOpenFile(PHANDLE FileHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PIO_STATUS_BLOCK IoStatusBlock, ULONG ShareAccess, ULONG OpenOptions)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock, ShareAccess, OpenOptions);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtOpenFile");

	wstring s = wstring();
	// >>>>>>>>>>>>>>> File Path <<<<<<<<<<<<<<<
	if (ObjectAttributes->RootDirectory != NULL)
	{
		// The path specified in ObjectName is relative to the directory handle. Get the path of that directory
		s.clear();
		GetHandleFileName(ObjectAttributes->RootDirectory, &s);
		element.addAttribute(L"DirPath", s.c_str());
		s.clear();
		from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
		element.addAttribute(L"Path", s.c_str());
	}
	else
	{
		// The objectname contains a full path to the file
		s.clear();
		from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
		element.addAttribute(L"Path", s.c_str());
	}

	
	// >>>>>>>>>>>>>>> DESIRED ACCESS <<<<<<<<<<<<<<<
	s.clear();
	FileAccessMaskToString(DesiredAccess, &s);
	element.addAttribute(L"DesiredAccess", s.c_str());

	// >>>>>>>>>>>>>>> IO STATUS BLOCK <<<<<<<<<<<<<<<
	s.clear();
	IoStatusToString(IoStatusBlock, &s);
	element.addAttribute(L"IoStatusBlock", s.c_str());
	
	// >>>>>>>>>>>>>>> SHARE ACCESS <<<<<<<<<<<<<<<
	s.clear();
	ShareAccessToString(ShareAccess, &s);
	element.addAttribute(L"ShareAccess", s.c_str());

	// >>>>>>>>>>>>>>> OPEN OPTIONS <<<<<<<<<<<<<<<
	s.clear();
	FileCreateOptionsToString(OpenOptions, &s);
	element.addAttribute(L"OpenOptions", s.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	s.clear();
	NtStatusToString(res, &s);
	element.addAttribute(L"Result", s.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", *FileHandle);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtDeleteFile(POBJECT_ATTRIBUTES ObjectAttributes)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtDeleteFile(ObjectAttributes);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtDeleteFile");

	wstring s = wstring();
	// >>>>>>>>>>>>>>> File Path <<<<<<<<<<<<<<<
	if (ObjectAttributes->RootDirectory != NULL)
	{
		// The path specified in ObjectName is relative to the directory handle. Get the path of that directory
		s.clear();
		GetHandleFileName(ObjectAttributes->RootDirectory, &s);
		element.addAttribute(L"DirPath", s.c_str());
		element.addAttribute(L"Path", ObjectAttributes->ObjectName->Buffer);
	}
	else
	{
		// The objectname contains a full path to the file
		s.clear();
		from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
		element.addAttribute(L"Path", s.c_str());
	}

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	s.clear();
	NtStatusToString(res, &s);
	element.addAttribute(L"Result", s.c_str());

	log(&element);

	return res;

}
NTSTATUS WINAPI MyNtOpenDirectoryObject(PHANDLE DirectoryObject, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenDirectoryObject(DirectoryObject, DesiredAccess, ObjectAttributes);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtOpenDirectoryObject");
	
	wstring s = wstring();

	// >>>>>>>>>>>>>>> File Path <<<<<<<<<<<<<<<
	s.clear();
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &s);
	element.addAttribute(L"Path", s.c_str());

	// >>>>>>>>>>>>>>> DESIRED ACCESS <<<<<<<<<<<<<<<
	s.clear();
	DirectoryAccessMaskToString(DesiredAccess, &s);
	element.addAttribute(L"DesiredAccess", s.c_str());
	
	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	s.clear();
	NtStatusToString(res, &s);
	element.addAttribute(L"Result", s.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", *DirectoryObject);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtOpenKey(PHANDLE KeyHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtOpenKey(KeyHandle, DesiredAccess, ObjectAttributes);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtOpenKey");
	wstring w = wstring();

	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	w.clear();
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &w);
	element.addAttribute(L"Path", w.c_str());

	// >>>>>>>>>>>>>>> Access Mask <<<<<<<<<<<<<<<
	w.clear();
	KeyAccessMaskToString(DesiredAccess, &w);
	element.addAttribute(L"DesiredAccess", w.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w.clear();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", *KeyHandle);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtCreateKey(PHANDLE KeyHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, ULONG TitleIndex, PUNICODE_STRING Class, ULONG CreateOptions, PULONG Disposition)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtCreateKey(KeyHandle, DesiredAccess, ObjectAttributes, TitleIndex, Class, CreateOptions, Disposition);
	
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtCreateKey");
	wstring w = wstring();
	
	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	from_unicode_to_wstring(ObjectAttributes->ObjectName, &w);
	element.addAttribute(L"Path", w.c_str());
	
	// >>>>>>>>>>>>>>> Access Mask <<<<<<<<<<<<<<<
	w.clear();
	KeyAccessMaskToString(DesiredAccess, &w);
	element.addAttribute(L"DesiredAccess", w.c_str());
	
	// >>>>>>>>>>>>>>> Class <<<<<<<<<<<<<<<
	if (Class != NULL)
	{
		from_unicode_to_wstring(Class, &w);
		element.addAttribute(L"Class", w.c_str());
	}
		
	
	// >>>>>>>>>>>>>>> Create Options <<<<<<<<<<<<<<<
	w.clear();
	KeyCreateOptionsToString(CreateOptions, &w);
	element.addAttribute(L"CreateOptions", w.c_str());
	
	// >>>>>>>>>>>>>>> Disposition <<<<<<<<<<<<<<<
	if (Disposition != NULL)
	{
		if (*Disposition == REG_CREATED_NEW_KEY)
			element.addAttribute(L"Disposition", L"REG_CREATED_NEW_KEY");
		else if (*Disposition == REG_OPENED_EXISTING_KEY)
			element.addAttribute(L"Disposition", L"REG_OPENED_EXISTING_KEY");
	}
	else
		element.addAttribute(L"Disposition", L"N/A");
	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w.clear();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", *KeyHandle);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);
	
	return res;	
}
NTSTATUS WINAPI MyNtQueryKey(HANDLE KeyHandle, KEY_INFORMATION_CLASS KeyInformationClass, PVOID KeyInformation, ULONG Length, PULONG ResultLength)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryKey(KeyHandle,KeyInformationClass,KeyInformation,Length,ResultLength);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtQueryKey");

	wstring w = wstring();
	GetKeyPathFromKKEY(KeyHandle, &w);

	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	element.addAttribute(L"Path", w.c_str());

	// >>>>>>>>>>>>>>> Key Information Class <<<<<<<<<<<<<<<
	w = KeyInformationClassToString(KeyInformationClass);
	element.addAttribute(L"KeyInformationClass", w.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", KeyHandle);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);

	return res;

}
NTSTATUS WINAPI MyNtDeleteKey(HANDLE KeyHandle)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtDeleteKey(KeyHandle);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtDeleteKey");

	wstring w = wstring();

	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	GetKeyPathFromKKEY(KeyHandle, &w);
	element.addAttribute(L"Path", w.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w.clear();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", KeyHandle);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtDeleteValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName){

	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtDeleteValueKey(KeyHandle, ValueName);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtDeleteValueKey");

	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	wstring w = wstring();
	GetKeyPathFromKKEY(KeyHandle, &w);
	element.addAttribute(L"Path", w.c_str());

	// >>>>>>>>>>>>>>> Value <<<<<<<<<<<<<<<
	w.clear();
	from_unicode_to_wstring(ValueName, &w);
	element.addAttribute(L"ValueName", w.c_str());

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	w.clear();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", KeyHandle);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);
	return res;
}
NTSTATUS WINAPI MyNtEnumerateKey(HANDLE KeyHandle, ULONG Index, KEY_INFORMATION_CLASS KeyInformationClass, PVOID KeyInformation, ULONG Length, PULONG ResultLength)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtEnumerateKey(KeyHandle,Index,KeyInformationClass,KeyInformation,Length,ResultLength);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtEnumerateKey");

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());

	// >>>>>>>>>>>>>>> KeyPath <<<<<<<<<<<<<<<
	GetKeyPathFromKKEY(KeyHandle,&w);
	element.addAttribute(L"KeyPath", w.c_str());

	// >>>>>>>>>>>>>>> Key Information Class <<<<<<<<<<<<<<<
	w = KeyInformationClassToString(KeyInformationClass);
	element.addAttribute(L"KeyInformationClass", w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", KeyHandle);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtEnumerateValueKey(HANDLE KeyHandle, ULONG Index, KEY_VALUE_INFORMATION_CLASS KeyValueInformationClass, PVOID KeyValueInformation, ULONG Length, PULONG ResultLength)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtEnumerateValueKey(KeyHandle, Index, KeyValueInformationClass, KeyValueInformation, Length, ResultLength);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtEnumerateValueKey");

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());

	// >>>>>>>>>>>>>>> KeyPath <<<<<<<<<<<<<<<
	GetKeyPathFromKKEY(KeyHandle,&w);
	element.addAttribute(L"KeyPath", w.c_str());

	// >>>>>>>>>>>>>>> SubKey <<<<<<<<<<<<<<<
	element.addAttribute(L"SubKeyIndex", to_wstring(Index).c_str());

	// >>>>>>>>>>>>>>> Key Value Information Class <<<<<<<<<<<<<<<
	w = KeyValueInformationClassToString(KeyValueInformationClass);
	element.addAttribute(L"KeyValueInformationClass", w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", KeyHandle);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);
	return res;
}
NTSTATUS WINAPI MyNtLockFile(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine, PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, PLARGE_INTEGER ByteOffset, PLARGE_INTEGER Length, ULONG Key, BOOLEAN FailImmediately, BOOLEAN ExclusiveLock)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtLockFile(FileHandle,Event,ApcRoutine,ApcContext,IoStatusBlock,ByteOffset,Length,Key,FailImmediately,ExclusiveLock);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtLockFile");

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());

	// >>>>>>>>>>>>>>> FilePath <<<<<<<<<<<<<<<
	GetFileNameFromHandle(FileHandle,&w);
	element.addAttribute(L"KeyPath", w.c_str());
	

	// >>>>>>>>>>>>>>> Byte offset <<<<<<<<<<<<<<<
	w.clear();
	element.addAttribute(L"LockFrom", to_wstring(ByteOffset->QuadPart).c_str());

	// >>>>>>>>>>>>>>> Length to lock <<<<<<<<<<<<<<<
	w.clear();
	element.addAttribute(L"LengthToLock", to_wstring(Length->QuadPart).c_str());

	// >>>>>>>>>>>>>>> Fail Immediately? <<<<<<<<<<<<<<<
	w.clear();
	element.addAttribute(L"FailImmediately", to_wstring(FailImmediately).c_str());

	// >>>>>>>>>>>>>>> Exclusive Lock <<<<<<<<<<<<<<<
	w.clear();
	element.addAttribute(L"ExclusiveLock", to_wstring(ExclusiveLock).c_str());

	// >>>>>>>>>>>>>>> IO STATUS BLOCK <<<<<<<<<<<<<<<
	w.clear();
	IoStatusToString(IoStatusBlock, &w);
	element.addAttribute(L"IoStatusBlock", w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", FileHandle);
		element.addAttribute(L"Handle",buff);
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
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtOpenProcess");
	
	
	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w);

	// >>>>>>>>>>>>>>> DESIRED ACCESS <<<<<<<<<<<<<<<
	w.clear();
	FileAccessMaskToString(DesiredAccess, &w);
	element.addAttribute(L"DesiredAccess", w);
	
	// The objectname contains a full path to the file
	// The ObjectName might be null: http://msdn.microsoft.com/en-us/library/windows/hardware/ff567022(v=vs.85).aspx
	if (ObjectAttributes->ObjectName == NULL)
	{
		//TODO
	}
	else
	{
		// If it is not null, calcualte the path
		element.addAttribute(L"Path", ObjectAttributes->ObjectName->Buffer);
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
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtQueryDirectoryFile");

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());

	// >>>>>>>>>>>>>>> IO STATUS BLOCK <<<<<<<<<<<<<<<
	w.clear();
	IoStatusToString(IoStatusBlock, &w);
	element.addAttribute(L"IoStatusBlock", w.c_str());

	// >>>>>>>>>>>>>>> FILE Name <<<<<<<<<<<<<<<
	if (FileName != NULL)
	{
		w.clear();
		element.addAttribute(L"FileName", FileName->Buffer);
	}
	

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", FileHandle);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtQueryFullAttributesFile(POBJECT_ATTRIBUTES ObjectAttributes, PFILE_NETWORK_OPEN_INFORMATION FileInformation)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryFullAttributesFile(ObjectAttributes, FileInformation);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtQueryFullAttributesFile");

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());

	// >>>>>>>>>>>>>>> File Path <<<<<<<<<<<<<<<
	if (ObjectAttributes->RootDirectory != NULL)
	{
		// The path specified in ObjectName is relative to the directory handle. Get the path of that directory
		w.clear();
		GetHandleFileName(ObjectAttributes->RootDirectory, &w);
		element.addAttribute(L"DirPath", w.c_str());
		element.addAttribute(L"Path", ObjectAttributes->ObjectName->Buffer);
	}
	else
	{
		// The objectname contains a full path to the file
		element.addAttribute(L"Path", ObjectAttributes->ObjectName->Buffer);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtQueryValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName, KEY_VALUE_INFORMATION_CLASS KeyValueInformationClass, PVOID KeyValueInformation, ULONG Length, PULONG ResultLength)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtQueryValueKey(KeyHandle, ValueName, KeyValueInformationClass, KeyValueInformation, Length, ResultLength);
	
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtQueryValueKey");

	// >>>>>>>>>>>>>>> Result <<<<<<<<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());
	
	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	w.clear();
	GetKeyPathFromKKEY(KeyHandle,&w);
	element.addAttribute(L"KeyPath", w.c_str());
	
	// >>>>>>>>>>>>>>> ValueName: could be not null terminated, so set the terminator! <<<<<<<<<<<<<<<
	w.clear();
	from_unicode_to_wstring(ValueName, &w);
	element.addAttribute(L"ValueName", w.c_str());
	
	// >>>>>>>>>>>>>>> Key Value Information Class <<<<<<<<<<<<<<<
	w = KeyValueInformationClassToString(KeyValueInformationClass);
	element.addAttribute(L"KeyValueInformationClass", w.c_str());
	

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", KeyHandle);
		element.addAttribute(L"Handle", buff);
	}

	log(&element);
	
	return res;
}
NTSTATUS WINAPI MyNtSetInformationFile(HANDLE FileHandle, PIO_STATUS_BLOCK IoStatusBlock, PVOID FileInformation, ULONG Length, FILE_INFORMATION_CLASS FileInformationClass)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtSetInformationFile(FileHandle,IoStatusBlock,FileInformation, Length,FileInformationClass);
	
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtSetInformationFile");

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());
	
	// >>>>>>>>>>>>>>> FileInformationClass <<<<<<<<<<<<<<<
	w.clear();
	//element.addAttribute(L"FileInformationClass", FileInformationClassToString(FileInformationClass));
	element.addAttribute(L"FileInformationClass", to_wstring(FileInformationClass).c_str());

	// TODO: parse the class result. It depends on the class type

	// >>>>>>>>>>>>>>> IO STATUS BLOCK <<<<<<<<<<<<<<<
	w.clear();
	IoStatusToString(IoStatusBlock, &w);
	element.addAttribute(L"IoStatusBlock", w.c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", FileHandle);
		element.addAttribute(L"Handle",buff);
	}

	log(&element);
	return res;
}
NTSTATUS WINAPI MyNtSetValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName, ULONG TitleIndex, ULONG Type, PVOID Data, ULONG DataSize)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtSetValueKey(KeyHandle,ValueName,TitleIndex,Type,Data,DataSize);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtSetValueKey");

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());
	
	// >>>>>>>>>>>>>>> Key Path <<<<<<<<<<<<<<<
	w.clear();
	GetKeyPathFromKKEY(KeyHandle,&w);
	element.addAttribute(L"KeyPath", w.c_str());

	// >>>>>>>>>>>>>>> ValueName <<<<<<<<<<<<<<<
	w.clear();
	from_unicode_to_wstring(ValueName, &w);
	element.addAttribute(L"ValueName", w.c_str());

	// >>>>>>>>>>>>>>> TitleIndex <<<<<<<<<<<<<<<
	element.addAttribute(L"TitleIndex", to_wstring(TitleIndex).c_str());
	
	// SKIPPING NOW THE TYPE OF THE DATA TO BE WRITTEN: (lately TODO?)


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", KeyHandle);
		element.addAttribute(L"Handle",buff);
	}

	log(&element);

	return res;
}
NTSTATUS WINAPI MyNtTerminateProcess(HANDLE ProcessHandle, NTSTATUS ExitStatus)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtTerminateProcess(ProcessHandle,ExitStatus);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtTerminateProcess");

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());

	// >>>>>>>>>>>>>>> Process Handle <<<<<<<<<<<<<<<

	// >>>>>>>>>>>>>>> ExitStatus <<<<<<<<<<<<<<<
	element.addAttribute(L"ExitStatus", to_wstring(ExitStatus).c_str());


	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", ProcessHandle);
		element.addAttribute(L"Handle",buff);
	}

	log(&element);

	return res;
}
/*NTSTATUS WINAPI MyNtClose(HANDLE Handle)
{
	// Call first because we want to store the result to the call too.
	NTSTATUS res = realNtClose(Handle);
	
	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"NtClose"); 

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w);
	

	if (NT_SUCCESS(res))
	{
		wchar_t buff[32];
		wsprintf(buff, L"%p", Handle);
		element.addAttribute(L"Handle", wstring(buff));
	}

	log(&element);
	
	return res;
}*/
BOOL WINAPI MyCreateProcessA(LPCTSTR lpApplicationName, LPTSTR lpCommandLine, LPSECURITY_ATTRIBUTES lpProcessAttributes, LPSECURITY_ATTRIBUTES lpThreadAttributes, BOOL bInheritHandles, DWORD dwCreationFlags, LPVOID lpEnvironment, LPCTSTR lpCurrentDirectory, LPSTARTUPINFO lpStartupInfo, LPPROCESS_INFORMATION lpProcessInformation)
{
	CHAR   DllPath[MAX_PATH] = { 0 };
	GetModuleFileNameA((HINSTANCE)&__ImageBase, DllPath, _countof(DllPath));

	// Use directly the Detours API
	BOOL res = DetourCreateProcessWithDll(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation,DllPath,realCreateProcessA);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"CreateProcess");

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());

	
	log(&element);

	return res;
}
BOOL WINAPI MyCreateProcessW(LPCWSTR lpApplicationName, LPWSTR lpCommandLine, LPSECURITY_ATTRIBUTES lpProcessAttributes, LPSECURITY_ATTRIBUTES lpThreadAttributes, BOOL bInheritHandles, DWORD dwCreationFlags, LPVOID lpEnvironment, LPCWSTR lpCurrentDirectory, LPSTARTUPINFO lpStartupInfo, LPPROCESS_INFORMATION lpProcessInformation)
{
	char   DllPath[MAX_PATH] = { 0 };
	GetModuleFileNameA((HINSTANCE)&__ImageBase, DllPath, _countof(DllPath));

	// Use directly the Detours API
	BOOL res = DetourCreateProcessWithDllW(lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation, DllPath, realCreateProcessW);

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc;pugi::xml_node element = doc.append_child(L"CreateProcess");

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	wstring w = wstring();
	NtStatusToString(res, &w);
	element.addAttribute(L"Result", w.c_str());


	log(&element);

	return res;
}
/*
// Winsock
int WINAPI MySend(SOCKET s, const char *buf, int len, int flags)
{
	//OutputDebugString(L"SEEEEEND!");
	return realSend(s, buf, len, flags);
}

int WINAPI MyWSAConnect(SOCKET s, const struct sockaddr* name, int namelen, LPWSABUF lpCallerData, LPWSABUF lpCalleeData, LPQOS lpSQOS, LPQOS lpGQOS)
{
	OutputDebugStringW(L"WSAConnect!!!!!");
	return realWSAConnect(s, name, namelen, lpCallerData, lpCalleeData, lpSQOS, lpGQOS);
}

int WINAPI MyConnect(SOCKET s, const struct sockaddr* name, int namelen)
{
	OutputDebugStringW(L"Connect!!!!!");
	return realConnect(s, name, namelen);
}

BOOL WINAPI MyWSAConnectByName(SOCKET s, LPTSTR nodename, LPTSTR servicename, LPDWORD LocalAddressLength, LPSOCKADDR LocalAddress, LPDWORD RemoteAddressLength, LPSOCKADDR RemoteAddress, const struct timeval *timeout, LPWSAOVERLAPPED Reserved)
{
	OutputDebugStringW(L"MyWSAConnectByName!!!!!");
	return realWSAConnectByName(s, nodename,servicename, LocalAddressLength,LocalAddress, RemoteAddressLength, RemoteAddress, timeout, Reserved);
}

int WINAPI MyWSASend(SOCKET s, LPWSABUF lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesSent, DWORD dwFlags, LPWSAOVERLAPPED lpOverlapped, LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine)
{
	OutputDebugStringW(L"WSAsend!!!!");
	return realWSASend(s, lpBuffers,dwBufferCount, lpNumberOfBytesSent, dwFlags, lpOverlapped, lpCompletionRoutine);
}

int WINAPI MyWSARecv(SOCKET s, LPWSABUF lpBuffers, DWORD dwBufferCount, LPDWORD lpNumberOfBytesRecvd, LPDWORD lpFlags, LPWSAOVERLAPPED lpOverlapped, LPWSAOVERLAPPED_COMPLETION_ROUTINE lpCompletionRoutine)
{
	OutputDebugStringW(L"WSARecv!!!!");
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

	element->append_attribute(L"ThreadId") = to_string(GetCurrentThreadId()).c_str();
	element->append_attribute(L"PId") = to_string(GetCurrentProcessId()).c_str();

	std::wstringstream ss;
	
	element->print(ss, L"", pugi::format_no_declaration|pugi::format_raw);
	
	
	// Create a std::string and copy your document data in to the string    
	std::wstring str = ss.str();

	ds.dwData = 0;
	ds.cbData = str.size()*sizeof(wchar_t);
	ds.lpData = (wchar_t*)str.c_str();
		
	// Send message...
	SendMessage(cwHandle, WM_COPYDATA, 0, (LPARAM)&ds);

}


/* 
	>>>>>>>>>>>>>>> Parsing functions <<<<<<<<<<<<<<< 
*/
void FileAttributesToString(ULONG FileAttributes, wstring* s)
{
	if ((FileAttributes & FILE_ATTRIBUTE_ARCHIVE) == FILE_ATTRIBUTE_ARCHIVE)
		s->append(L"|FILE_ATTRIBUTE_ARCHIVE");
	if ((FileAttributes & FILE_ATTRIBUTE_ENCRYPTED) == FILE_ATTRIBUTE_ENCRYPTED)
		s->append(L"|FILE_ATTRIBUTE_ENCRYPTED");
	if ((FileAttributes & FILE_ATTRIBUTE_HIDDEN) == FILE_ATTRIBUTE_HIDDEN)
		s->append(L"|FILE_ATTRIBUTE_HIDDEN");
	if ((FileAttributes & FILE_ATTRIBUTE_NORMAL) == FILE_ATTRIBUTE_NORMAL)
		s->append(L"|FILE_ATTRIBUTE_NORMAL");
	if ((FileAttributes & FILE_ATTRIBUTE_OFFLINE) == FILE_ATTRIBUTE_OFFLINE)
		s->append(L"|FILE_ATTRIBUTE_OFFLINE");
	if ((FileAttributes & FILE_ATTRIBUTE_READONLY) == FILE_ATTRIBUTE_READONLY)
		s->append(L"|FILE_ATTRIBUTE_READONLY");
	if ((FileAttributes & FILE_ATTRIBUTE_SYSTEM) == FILE_ATTRIBUTE_SYSTEM)
		s->append(L"|FILE_ATTRIBUTE_SYSTEM");
	if ((FileAttributes & FILE_ATTRIBUTE_TEMPORARY) == FILE_ATTRIBUTE_TEMPORARY)
		s->append(L"|FILE_ATTRIBUTE_TEMPORARY");

	if (s->length() > 0)
		(*s) = s->substr(1, s->length() - 1);
	else
		(*s) = L"NA";
}

void FileAccessMaskToString(ACCESS_MASK DesiredAccess, wstring* s)
{
	bool mustCut = false;
	StandardAccessMaskToString(DesiredAccess, s);

	if (s->length() == 0)
		mustCut = true;

	if ((DesiredAccess & FILE_READ_DATA) == FILE_READ_DATA)
		s->append(L"|FILE_READ_DATA");
	if ((DesiredAccess & FILE_READ_ATTRIBUTES) == FILE_READ_ATTRIBUTES)
		s->append(L"|FILE_READ_ATTRIBUTES");
	if ((DesiredAccess & FILE_READ_EA) == FILE_READ_EA)
		s->append(L"|FILE_READ_EA");
	if ((DesiredAccess & FILE_WRITE_DATA) == FILE_WRITE_DATA)
		s->append(L"|FILE_WRITE_DATA");
	if ((DesiredAccess & FILE_WRITE_ATTRIBUTES) == FILE_WRITE_ATTRIBUTES)
		s->append(L"|FILE_WRITE_ATTRIBUTES");
	if ((DesiredAccess & FILE_WRITE_EA) == FILE_WRITE_EA)
		s->append(L"|FILE_WRITE_EA");
	if ((DesiredAccess & FILE_APPEND_DATA) == FILE_APPEND_DATA)
		s->append(L"|FILE_APPEND_DATA");
	if ((DesiredAccess & FILE_EXECUTE) == FILE_EXECUTE)
		s->append(L"|FILE_EXECUTE");
	if ((DesiredAccess & FILE_LIST_DIRECTORY) == FILE_LIST_DIRECTORY)
		s->append(L"|FILE_LIST_DIRECTORY");
	if ((DesiredAccess & FILE_TRAVERSE) == FILE_TRAVERSE)
		s->append(L"|FILE_TRAVERSE");

	if (s->length() > 0 && mustCut)
	{
		(*s) = s->substr(1, s->length() - 1);
	}
}

void DirectoryAccessMaskToString(ACCESS_MASK DesiredAccess, wstring* s)
{
	bool mustCut = false;
	StandardAccessMaskToString(DesiredAccess, s);

	if (s->length() == 0)
		mustCut = true;

	if ((DesiredAccess & 0x0001) == 0x0001)
		s->append(L"|DIRECTORY_QUERY");
	if ((DesiredAccess & FILE_READ_DATA) == FILE_READ_DATA)
		s->append(L"|DIRECTORY_TRAVERSE");

	if ((DesiredAccess & 0x0002) == 0x0002)
		s->append(L"|FILE_READ_ATTRIBUTES");

	if ((DesiredAccess & 0x0004) == 0x0004)
		s->append(L"|DIRECTORY_CREATE_OBJECT");

	if ((DesiredAccess & 0x0008) == 0x0008)
		s->append(L"|DIRECTORY_CREATE_SUBDIRECTORY");

	if ((DesiredAccess & (STANDARD_RIGHTS_REQUIRED | 0xF)) == (STANDARD_RIGHTS_REQUIRED | 0xF))
		s->append(L"|DIRECTORY_ALL_ACCESS");

	if (s->length() > 0 && mustCut)
	{
		(*s) = s->substr(1, s->length() - 1);
	}
}

void KeyAccessMaskToString(ACCESS_MASK DesiredAccess, wstring* s)
{
	bool mustCut = false;
	StandardAccessMaskToString(DesiredAccess, s);

	if (s->length() == 0)
		mustCut = true;

	if ((DesiredAccess & KEY_QUERY_VALUE) == KEY_QUERY_VALUE)
		s->append(L"|KEY_QUERY_VALUE");
	if ((DesiredAccess & KEY_ENUMERATE_SUB_KEYS) == KEY_ENUMERATE_SUB_KEYS)
		s->append(L"|KEY_ENUMERATE_SUB_KEYS");
	if ((DesiredAccess & KEY_NOTIFY) == KEY_NOTIFY)
		s->append(L"|KEY_NOTIFY");
	if ((DesiredAccess & KEY_SET_VALUE) == KEY_SET_VALUE)
		s->append(L"|KEY_SET_VALUE");
	if ((DesiredAccess & KEY_CREATE_SUB_KEY) == KEY_CREATE_SUB_KEY)
		s->append(L"|KEY_CREATE_SUB_KEY");
	if ((DesiredAccess & KEY_CREATE_LINK) == KEY_CREATE_LINK)
		s->append(L"|KEY_CREATE_LINK");

	if (s->length() > 0 && mustCut)
	{
		(*s) = s->substr(1, s->length() - 1);
	}

}

void StandardAccessMaskToString(ACCESS_MASK DesiredAccess, wstring* s)
{
	s->clear();

	if ((DesiredAccess & DELETE) == DELETE)
		s->append(L"|DELETE");

	if ((DesiredAccess & READ_CONTROL) == READ_CONTROL)
		s->append(L"|READ_CONTROL");

	if ((DesiredAccess & SYNCHRONIZE) == SYNCHRONIZE)
		s->append(L"|SYNCHRONIZE");

	if ((DesiredAccess & WRITE_DAC) == WRITE_DAC)
		s->append(L"|WRITE_DAC");

	if ((DesiredAccess & WRITE_OWNER) == WRITE_OWNER)
		s->append(L"|WRITE_OWNER");

	if ((DesiredAccess & GENERIC_READ) == GENERIC_READ)
		s->append(L"|GENERIC_READ");

	if ((DesiredAccess & GENERIC_WRITE) == GENERIC_WRITE)
		s->append(L"|GENERIC_WRITE");

	if ((DesiredAccess & GENERIC_EXECUTE) == GENERIC_EXECUTE)
		s->append(L"|GENERIC_EXECUTE");

	if ((DesiredAccess & GENERIC_ALL) == GENERIC_ALL)
		s->append(L"|GENERIC_ALL");

	if (s->length() > 0)
		(*s) = s->substr(1, s->length() - 1);
}

void IoStatusToString(IO_STATUS_BLOCK* IoStatusBlock, wstring* s)
{
	s->clear();
	switch (IoStatusBlock->Status)
	{
	case FILE_CREATED:
		(*s) = L"FILE_CREATED";
		break;

	case FILE_OPENED:
		(*s) = L"FILE_OPENED";
		break;

	case FILE_OVERWRITTEN:
		(*s) = L"FILE_OVERWRITTEN";
		break;
	case FILE_SUPERSEDED:
		(*s) = L"FILE_SUPERSEDED";
		break;

	case FILE_EXISTS:
		(*s) = L"FILE_EXISTS";
		break;

	case FILE_DOES_NOT_EXIST:
		(*s) = L"FILE_DOES_NOT_EXIST";
		break;

	default:
		(*s) = L"NA"; // Should never happen...
	}
}

void ShareAccessToString(ULONG ShareAccess, wstring* s)
{
	s->clear();
	if ((ShareAccess & FILE_SHARE_READ) == FILE_SHARE_READ)
		s->append(L"|FILE_SHARE_READ");
	if ((ShareAccess & FILE_SHARE_WRITE) == FILE_SHARE_WRITE)
		s->append(L"|FILE_SHARE_WRITE");
	if ((ShareAccess & FILE_SHARE_DELETE) == FILE_SHARE_DELETE)
		s->append(L"|FILE_SHARE_DELETE");
	if (s->length() > 0)
		(*s) = s->substr(1, s->length() - 1);
	else
		(*s) = L"NA";
}

void FileCreateOptionsToString(ULONG OpenCreateOption, wstring* s)
{
	s->clear();
	if ((OpenCreateOption & FILE_DIRECTORY_FILE) == FILE_DIRECTORY_FILE)
		s->append(L"|FILE_DIRECTORY_FILE");
	if ((OpenCreateOption & FILE_NON_DIRECTORY_FILE) == FILE_NON_DIRECTORY_FILE)
		s->append(L"|FILE_NON_DIRECTORY_FILE");
	if ((OpenCreateOption & FILE_WRITE_THROUGH) == FILE_WRITE_THROUGH)
		s->append(L"|FILE_WRITE_THROUGH");
	if ((OpenCreateOption & FILE_SEQUENTIAL_ONLY) == FILE_SEQUENTIAL_ONLY)
		s->append(L"|FILE_SEQUENTIAL_ONLY");
	if ((OpenCreateOption & FILE_RANDOM_ACCESS) == FILE_RANDOM_ACCESS)
		s->append(L"|FILE_RANDOM_ACCESS");
	if ((OpenCreateOption & FILE_NO_INTERMEDIATE_BUFFERING) == FILE_NO_INTERMEDIATE_BUFFERING)
		s->append(L"|FILE_NO_INTERMEDIATE_BUFFERING");
	if ((OpenCreateOption & FILE_SYNCHRONOUS_IO_ALERT) == FILE_SYNCHRONOUS_IO_ALERT)
		s->append(L"|FILE_SYNCHRONOUS_IO_ALERT");
	if ((OpenCreateOption & FILE_SYNCHRONOUS_IO_NONALERT) == FILE_SYNCHRONOUS_IO_NONALERT)
		s->append(L"|FILE_SYNCHRONOUS_IO_NONALERT");
	if ((OpenCreateOption & FILE_CREATE_TREE_CONNECTION) == FILE_CREATE_TREE_CONNECTION)
		s->append(L"|FILE_CREATE_TREE_CONNECTION");
	if ((OpenCreateOption & FILE_NO_EA_KNOWLEDGE) == FILE_NO_EA_KNOWLEDGE)
		s->append(L"|FILE_NO_EA_KNOWLEDGE");
	if ((OpenCreateOption & FILE_OPEN_REPARSE_POINT) == FILE_OPEN_REPARSE_POINT)
		s->append(L"|FILE_OPEN_REPARSE_POINT");
	if ((OpenCreateOption & FILE_DELETE_ON_CLOSE) == FILE_DELETE_ON_CLOSE)
		s->append(L"|FILE_DELETE_ON_CLOSE");
	if ((OpenCreateOption & FILE_OPEN_BY_FILE_ID) == FILE_OPEN_BY_FILE_ID)
		s->append(L"|FILE_OPEN_BY_FILE_ID");
	if ((OpenCreateOption & FILE_OPEN_FOR_BACKUP_INTENT) == FILE_OPEN_FOR_BACKUP_INTENT)
		s->append(L"|FILE_OPEN_FOR_BACKUP_INTENT");
	if ((OpenCreateOption & FILE_RESERVE_OPFILTER) == FILE_RESERVE_OPFILTER)
		s->append(L"|FILE_RESERVE_OPFILTER");
	if ((OpenCreateOption & FILE_OPEN_REQUIRING_OPLOCK) == FILE_OPEN_REQUIRING_OPLOCK)
		s->append(L"|FILE_OPEN_REQUIRING_OPLOCK");
	if ((OpenCreateOption & FILE_COMPLETE_IF_OPLOCKED) == FILE_COMPLETE_IF_OPLOCKED)
		s->append(L"|FILE_COMPLETE_IF_OPLOCKED");
	if (s->length() > 0)
		(*s) = s->substr(1, s->length() - 1);
	else
		(*s) = L"NA";
}

void KeyCreateOptionsToString(ULONG CreateOption, wstring* s){
	s->clear();
	if ((CreateOption & REG_OPTION_VOLATILE) == REG_OPTION_VOLATILE)
		s->append(L"|REG_OPTION_VOLATILE");
	if ((CreateOption & REG_OPTION_NON_VOLATILE) == REG_OPTION_NON_VOLATILE)
		s->append(L"|REG_OPTION_NON_VOLATILE");
	if ((CreateOption & REG_OPTION_CREATE_LINK) == REG_OPTION_CREATE_LINK)
		s->append(L"|REG_OPTION_CREATE_LINK");
	if ((CreateOption & REG_OPTION_BACKUP_RESTORE) == REG_OPTION_BACKUP_RESTORE)
		s->append(L"|REG_OPTION_BACKUP_RESTORE");

	if (s->length() > 0)
		(*s) = s->substr(1, s->length() - 1);
	else
		(*s) = L"NA";
}

void NtStatusToString(NTSTATUS status, wstring* s)
{
	s->clear();
	*s = to_wstring((unsigned long)status);	
}

const wchar_t* CreateDispositionToString(ULONG CreateDisposition)
{
	switch (CreateDisposition)
	{
	case FILE_SUPERSEDE:
		return L"FILE_SUPERSEDE";
		break;

	case FILE_CREATE:
		return L"FILE_CREATE";
		break;

	case FILE_OPEN:
		return L"FILE_OPEN";
		break;

	case FILE_OPEN_IF:
		return L"FILE_OPEN_IF";
		break;

	case FILE_OVERWRITE:
		return L"FILE_OVERWRITE";
		break;

	case FILE_OVERWRITE_IF:
		return L"FILE_OVERWRITE_IF";
		break;

	default:
		return L"NA"; // Should never happen...
	}
}

void GetHandleFileName(HANDLE hHandle, wstring* fname)
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
		return L"KeyBasicInformation";
	case KeyNodeInformation:
		return L"KeyNodeInformation";
	case KeyFullInformation:
		return L"KeyFullInformation";
	case KeyNameInformation:
		return L"KeyNameInformation";
	case KeyCachedInformation:
		return L"KeyCachedInformation";
	case KeyFlagsInformation:
		return L"KeyFlagsInformation";
	case KeyVirtualizationInformation:
		return L"KeyVirtualizationInformation";
	case KeyHandleTagsInformation:
		return L"KeyHandleTagsInformation";
	case MaxKeyInfoClass:
		return L"MaxKeyInfoClass";
	default:
		return L"NA";
	}

}

void GetKeyPathFromKKEY(HANDLE key,wstring* s)
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
					*s = std::wstring(buffer + 2);
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
		return L"KeyValueBasicInformation";
	case KeyValueFullInformation:
		return L"KeyValueFullInformation";
	case KeyValuePartialInformation:
		return L"KeyValuePartialInformation";
	case  KeyValueFullInformationAlign64:
		return L"KeyValueFullInformationAlign64";
	case KeyValuePartialInformationAlign64:
		return L"KeyValuePartialInformationAlign64";
	case MaxKeyValueInfoClass:
		return L"MaxKeyValueInfoClass";
	default:
		return L"N/A";
	}
}

BOOL GetFileNameFromHandle(HANDLE hFile, wstring* w)
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
	*w = wstring(pszFilename);
	return(bSuccess);
}
/*
const wchar_t* FileInformationClassToString(FILE_INFORMATION_CLASS FileInformationClass)
{
	switch (FileInformationClass)
	{
	case FileBasicInformation:
		return L"FileBasicInformation";
	case FileDispositionInformation:
		return L"FileDispositionInformation";
	case FileEndOfFileInformation:
		return L"FileEndOfFileInformation";
	case FileIoPriorityHintInformation:
		return L"FileIoPriorityHintInformation";
	case FileLinkInformation:
		return L"FileLinkInformation";
	case FilePositionInformation:
		return L"FilePositionInformation";
	case FileRenameInformation:
		return L"FileRenameInformation";
	case FileShortNameInformation:
		return L"FileShortNameInformation";
	case FileValidDataLengthInformation:
		return L"FileValidDataLengthInformation";
	case FileReplaceCompletionInformation:
		return L"FileReplaceCompletionInformation";
	default:
		return L"N/A";
	}
}
*/

void from_unicode_to_wstring(PUNICODE_STRING u, wstring* w)
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
	HANDLE hMapObject;
	LPVOID lpvMem;
	char buf[SHMEMSIZE];

	// Open the shared memory mapping
	hMapObject = OpenFileMapping(FILE_MAP_READ, FALSE, TEXT(SHARED_MEM_NAME));
	lpvMem = MapViewOfFile(
		hMapObject,     // object to map view of
		FILE_MAP_READ, // read/write access
		0,              // high offset:  map from
		0,              // low offset:   beginning
		0);             // default: map entire file
	
	// If error occurred, return false
	if (lpvMem == NULL)
		return false;

	// Otherwise read the name of the window
	sprintf_s(buf, SHMEMSIZE,"%s",(char*)lpvMem);
	cwHandle = FindWindowA(NULL, buf);

	if (cwHandle == NULL)
	{
		return false;
	}
	
	// Done, dispose everything
	CloseHandle(hMapObject);
	UnmapViewOfFile(lpvMem);

	OutputDebugStringA("Window name...");
	OutputDebugStringA(buf);

	return true;
}