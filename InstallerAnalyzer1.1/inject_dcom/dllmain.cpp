/* 
	This DLL is in charge of being injected into the DCOM Process Launcher service to log process being created by the service on behalf of
	connected clients. To inject this DLL into the DCOM service, advanced DLL injection techniques are needed: Windows CreateRemoteThread() function 
	does not allow thread creation if the remote process is running on a different session. For this reason, a dedicated injection tecnique using RtlCreateThread 
	has to be used. That function is undocumented and hard to maintain.
	Once injected, this DLL has two main tasks achieved by hooking the CreateProcessInternalW():
	-> Inject the CHookingDll into the new procees.
	-> Communicate to the GuestController the new process being spawned. 
	
	
	The first task is easy to accomplish: we simply use the same injection technique offered by Detours. However, a problem raises when dealing with windows services. 
	Windows Services running as LOCAL_SYSTEM are privileded, therefore they are not allowed to talk directly with desktop apps using the classic message pump. So,
	we cannot use the POSTMESSAGE / FINDWINDOW and other communication system to notify our GuestController about the new info. 
	For this reason we need to create a simple named-pipe communication system between the DCOM Process Launcher and the GuestController. Here we create the PipeServer, that will
	work as follows.
	
	1. GuestController is in charge of allocating a NamedPipe \\.\pipe\dcom_hook_pipe
	2. Whenever CreateProcessInteralW is intercepted, we spawnt the process as "SUSPENDED" and we inject the CHookingDll into it. 
	3. We connect to the pipe and send a message reporting the process being spawned
	4. We wait synch until the GuestController sends an ACK (it means he's now monitoring that PID)
	5. We proceed to start the thread and close disconnect from the pipe.
	
*/

// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"
#include <stdlib.h>
#include <Windows.h>
#include <detours.h>
#include <tchar.h>
#include <iostream>     // std::cout
#include <sstream>      // std::stringstream
#include <stdio.h>
#include "../../InstallerAnalyzer1.1/Common/common.h"
#include "pugixml.hpp"

#define SHMEMSIZE 4096

#pragma comment(lib, "detours.lib")	// Nedded for DTOURS

typedef __success(return >= 0) long NTSTATUS;
EXTERN_C IMAGE_DOS_HEADER __ImageBase;

// Windows UNICODE PAIN
#ifdef UNICODE
typedef std::wstring string;
typedef std::wstringstream stringstream;

template <typename T>string to_string(T a) {
	return std::to_wstring(a);
}
#endif


/* >>>>>>>>>>>>>> ExitProcess <<<<<<<<<<<<<<< */
typedef VOID(WINAPI * pExitProcess)(UINT uExitCode);
VOID WINAPI MyExitProcess(UINT uExitCode);
static pExitProcess realExitProcess;

/* >>>>>>>>>>>>>> CreateProcessInternalW <<<<<<<<<<<<<<< */
typedef BOOL(WINAPI * pCreateProcessInternalW)(HANDLE hToken,
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
	PHANDLE hNewToken);

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
	PHANDLE hNewToken);

static pCreateProcessInternalW realCreateProcessInternalW;

HMODULE kern32dllmod = NULL;


void NtStatusToString(NTSTATUS status, string* s)
{
	s->clear();
	*s = to_string(status);
}


BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
	)
{
	HMODULE ntdll = GetModuleHandleA("ntdll.dll");
	kern32dllmod = kern32dllmod = GetModuleHandle(TEXT("kernel32.dll"));


	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		OutputDebugStringA("[DCOM INJECTOR] injection started. Time to hook! - XXXXX");
		realCreateProcessInternalW = (pCreateProcessInternalW)(GetProcAddress(kern32dllmod, "CreateProcessInternalW"));
		realExitProcess = (pExitProcess)(GetProcAddress(kern32dllmod, "ExitProcess"));

		DisableThreadLibraryCalls(hModule);

		// CreateProcessInternalW
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realCreateProcessInternalW, MyCreateProcessInternalW);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(TEXT("[DCOM INJECTOR] CreateProcessInternalW not derouted correctly"));
		else
			OutputDebugString(TEXT("[DCOM INJECTOR] CreateProcessInternalW successful"));

		// ExitProcess
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realExitProcess, MyExitProcess);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(TEXT("[DCOM INJECTOR] ExitProcess not derouted correctly"));
		else
			OutputDebugString(TEXT("[DCOM INJECTOR] ExitProcess successful"));


		break;
	case DLL_THREAD_ATTACH:
		break;
	case DLL_THREAD_DETACH:
		break;
	case DLL_PROCESS_DETACH:

		// CreateProcessInternalW
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realCreateProcessInternalW, MyCreateProcessInternalW);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(TEXT("[DCOM INJECTOR] CreateProcessInternalW not detached correctly"));
		else
			OutputDebugString(TEXT("[DCOM INJECTOR] CreateProcessInternalW detached successfully"));

		// ExitProcess
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realExitProcess, MyExitProcess);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(TEXT("[DCOM INJECTOR] ExitProcess not detached correctly"));
		else
			OutputDebugString(TEXT("[DCOM INJECTOR] ExitProcess detached successfully"));

		break;
	}
	return TRUE;
}


void notifyPidEvent(DWORD pid, DWORD evt){
	bool connected = false;
	int attempts = 0;
	bool ok = false;
	char buff[512];

	DWORD info[2];
	info[0] = pid;
	info[1] = evt;

	while (!connected && attempts<5){
		// Connect to the named pipe
		DWORD ack;
		DWORD read;
		if (!CallNamedPipe(TEXT(DCOM_HOOK_PIPE), &info, sizeof(info), &ack, sizeof(ack), &read, 3000)){
			DWORD error = GetLastError();
			// An error has occurred
			sprintf_s(buff, "[DCOM INJECTOR] notify error pid error: Cannot write on the pipe. Error %u XXXX", error);
			OutputDebugStringA(buff);
			return;
		}
		else {
			if (read == sizeof(DWORD)) {
				sprintf_s(buff, "[DCOM INJECTOR] Pipe, received: %u.", ack);
				OutputDebugStringA(buff);
				return;
			}
		}
	}
}


VOID WINAPI MyExitProcess(UINT uExitCode)
{
	// We hook this to let the GuestController know about our intention to terminate
	notifyPidEvent(GetCurrentProcessId(), PROC_EXITING);
	return realExitProcess(uExitCode);
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
	PHANDLE hNewToken) {
	
	OutputDebugString(TEXT("[DCOM INJECTOR] Spawning one process!"));
	BOOL processCreated = FALSE;
	char buff[512];
	char* dllpath = NULL;

	// Save the previous value of the creation flags and make sure we add the create suspended BIT
	DWORD originalFlags = dwCreationFlags;
	dwCreationFlags = dwCreationFlags | CREATE_SUSPENDED;
	processCreated = realCreateProcessInternalW(hToken, lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation, hNewToken);
	if (processCreated) {
		
		// Now we need to detect if the new process is running on session 0 (i.e. service) or greater (interactive).
		// This is something that's gonna work only on win 7/Vista. Probably for windows 8 things will change.
		DWORD sessionId = -1;
		if (!ProcessIdToSessionId(lpProcessInformation->dwProcessId, &sessionId)){
			DWORD err = GetLastError();
			// Something went wrong. Do not inject anything
			_snprintf_s(buff,sizeof(buff), "[DCOM INJECTOR] Cannot retrieve Session ID for pid %u. Error: %u. XXXX NOT INJECTING! XXXX", lpProcessInformation->dwProcessId,err);
			OutputDebugStringA(buff);
			// Do not inject anything
			return processCreated;
		}
		else {
			_snprintf_s(buff, sizeof(buff), "[DCOM INJECTOR] PID %u has session ID %u.", lpProcessInformation->dwProcessId, sessionId);
			OutputDebugStringA(buff);
		}
		
		if (sessionId == 0) {
			// Inject myself
			//OutputDebugStringA("This process has session ID 0, I won't inject myself.");
			//dllpath = NULL;
			dllpath = DCOM_DLL_PATH;
		} else {
			dllpath = DLLPATH;
		}
		
		if (dllpath != NULL){
			_snprintf_s(buff, sizeof(buff), "[DCOM INJECTOR] Injecting DLL %s into PID %u", dllpath, lpProcessInformation->dwProcessId);
			OutputDebugStringA(buff);

			// Allocate enough memory on the new process
			LPVOID baseAddress = (LPVOID)VirtualAllocEx(lpProcessInformation->hProcess, NULL, strlen(dllpath) + 1, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);

			// Copy the code to be injected
			WriteProcessMemory(lpProcessInformation->hProcess, baseAddress, dllpath, strlen(dllpath), NULL);

			OutputDebugStringA("[DCOM INJECTOR] DLL copied into host process memory space");

			// Notify the HostController that a new process has been created
			notifyPidEvent(lpProcessInformation->dwProcessId, PROC_SPAWNING);
			kern32dllmod = GetModuleHandle(TEXT("kernel32.dll"));
			HANDLE loadLibraryAddress = GetProcAddress(kern32dllmod, "LoadLibraryA");
			if (loadLibraryAddress == NULL)
			{
				OutputDebugStringW(TEXT("[DCOM INJECTOR] LOADLIB IS NULL - XXXX"));
				//error
				return 0;
			}
			else {
				OutputDebugStringW(TEXT("[DCOM INJECTOR] LOAD LIB OK"));
			}

			// Create a remote thread the remote thread
			HANDLE  threadHandle = CreateRemoteThread(lpProcessInformation->hProcess, NULL, 0, (LPTHREAD_START_ROUTINE)loadLibraryAddress, baseAddress, NULL, 0);
			if (threadHandle == NULL) {
				OutputDebugStringW(TEXT("[DCOM INJECTOR] REMTOE THREAD NOT OK XXXXX"));
			}
			else {
				OutputDebugStringW(TEXT("[DCOM INJECTOR] Remote thread created"));
				WaitForSingleObject(threadHandle, INFINITE);
			}
			
		}
		// Check if the process was meant to be stopped. If not, resume it now
		if ((originalFlags & CREATE_SUSPENDED) != CREATE_SUSPENDED) {
			// need to start it right away
			ResumeThread(lpProcessInformation->hThread);
			OutputDebugStringA("[DCOM INJECTOR] Thread resumed");
		}
	}
	else {
		DWORD error = GetLastError();
		_snprintf_s(buff, sizeof(buff), "[DCOM INJECTOR] Error creating process: %u. XXXX",error);
		OutputDebugStringA(buff);
	}
	return processCreated;
}