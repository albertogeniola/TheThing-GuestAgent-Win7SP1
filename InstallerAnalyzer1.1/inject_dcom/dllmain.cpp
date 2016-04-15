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
		OutputDebugStringA("XXXXX DCOM INJECTOR: injection started. Time to hook! XXXXX");
		realCreateProcessInternalW = (pCreateProcessInternalW)(GetProcAddress(kern32dllmod, "CreateProcessInternalW"));

		DisableThreadLibraryCalls(hModule);

		// CreateProcessInternalW
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realCreateProcessInternalW, MyCreateProcessInternalW);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(TEXT("CreateProcessInternalW not derouted correctly"));
		else
			OutputDebugString(TEXT("CreateProcessInternalW successful"));


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
			OutputDebugString(TEXT("CreateProcessInternalW not detached correctly"));
		else
			OutputDebugString(TEXT("CreateProcessInternalW detached successfully"));

		break;
	}
	return TRUE;
}


void notifyNewPid(DWORD pid){
	bool connected = false;
	int attempts = 0;
	bool ok = false;

	while (!connected && attempts<5){
		// Connect to the named pipe
		HANDLE hPipe = CreateFile(
			TEXT(DCOM_HOOK_PIPE),   // pipe name 
			GENERIC_READ |  // read and write access 
			GENERIC_WRITE,
			0,              // no sharing 
			NULL,           // default security attributes
			OPEN_EXISTING,  // opens existing pipe 
			0,              // default attributes 
			NULL);          // no template file 

		if (hPipe == INVALID_HANDLE_VALUE) {
			DWORD err = GetLastError();
			// There may be a couple of reasons why that method failed. If the pipe is currently busy, we have to retry later. 
			// otherwise there may be none listening for it, so we can skip all of this.
			if (err == ERROR_PIPE_BUSY) {
				OutputDebugString(TEXT("DCOM_INJECTOR: Pipe is busy. Waiting for it to become free..."));
				
				// Wait up to 5 seconds before retrying
				if (!WaitNamedPipe(TEXT(DCOM_HOOK_PIPE), 1000)){
					OutputDebugString(TEXT("DCOM_INJECTOR: Pipe is still busy."));
					connected = false;
					attempts++;
				}
			}
			else {
				char buff[512];
				sprintf_s(buff, "DCOM_INJECTOR: XXX Error opening dcom_hook_pipe. Error code: %u XXX", err);
				OutputDebugStringA(buff);
				// Some severe error occurred. We can't recover, so skip the notification
				connected = false;
				break;
			}
		}
		else {
			connected = true;
		}

		

		// Check if the connection went ok.
		if (!connected) {
			OutputDebugString(TEXT("DCOM_INJECTOR: Could not notify the GuestController because pipe connection failed."));
			CloseHandle(hPipe);
			return;
		}

		// If ok, send the data and wait for the response
		DWORD dwMode = PIPE_READMODE_MESSAGE; // Our messages are only pids, so It's ok for us to send them as messages instead of byte stream.
		if (!SetNamedPipeHandleState(
			hPipe,    // pipe handle 
			&dwMode,  // new pipe mode 
			NULL,     // don't set maximum bytes 
			NULL)    // don't set maximum time 
			){
				
			OutputDebugString(TEXT("DCOM_INJECTOR: Could not set the pipe mode. Cannot notify the Guestcontroller."));
			CloseHandle(hPipe);
			return;
		}

		int cbToWrite = sizeof(DWORD);
		
		DWORD cbWritten = 0;
		if (!WriteFile(
			hPipe,                  // pipe handle 
			&pid,		             // message 
			cbToWrite,              // message length 
			&cbWritten,             // bytes written 
			NULL)                  // not overlapped 
			)
		{
			OutputDebugString(TEXT("DCOM_INJECTOR: Could not write message on the pipe. Cannot notify the Guestcontroller."));
			CloseHandle(hPipe);
			return;
		}
		
		//TODO: can we assume cbwritten always matches sizeof(DWORD)?? Do we have to check that?
		printf("\nMessage sent to server, receiving reply as follows:\n");

		// Now we wait for an ACK by the GuestController
		DWORD guestControllerResponse = 0;
		DWORD cbRead = 0;
		BOOL fSuccess = FALSE;
		do
		{
			// Read from the pipe. 
			fSuccess = ReadFile(
				hPipe,    // pipe handle 
				&guestControllerResponse,    // buffer to receive reply 
				sizeof(DWORD),				// size of buffer 
				&cbRead,  // number of bytes read 
				NULL);    // not overlapped 

			if (!fSuccess && GetLastError() != ERROR_MORE_DATA)
			{
				// If we failed and the error is different from "there is more data", then we can't recover. Close everything and giveup.
				OutputDebugString(TEXT("DCOM_INJECTOR: Could not read ACK from the pipe. Giving up"));
				CloseHandle(hPipe);
				return;
			}
		} while (!fSuccess);  // repeat loop if ERROR_MORE_DATA 

		if (!fSuccess)
		{
			OutputDebugString(TEXT("DCOM_INJECTOR: Could not read ACK from the pipe. Giving up"));
			CloseHandle(hPipe);
			return;
		}

		// At this point we need to check if ACK has been received
		if (guestControllerResponse == DCOM_PROCESS_SPAWN_ACK) {
			// Ok everything has gone well. Printa debug string just for notification
			OutputDebugString(TEXT("DCOM_INJECTOR: ACK received from GuestController"));
		}
		else {
			// this should never happen, but never say never...
			OutputDebugString(TEXT("DCOM_INJECTOR: Invalid ACK received from guest controller!!"));
		}

		CloseHandle(hPipe);
		return;
		
	}
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
	
	OutputDebugString(TEXT("DCOM has spawned one process!"));
	BOOL processCreated = FALSE;

	// Save the previous value of the creation flags and make sure we add the create suspended BIT
	DWORD originalFlags = dwCreationFlags;
	dwCreationFlags = dwCreationFlags | CREATE_SUSPENDED;
	processCreated = realCreateProcessInternalW(hToken, lpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, lpStartupInfo, lpProcessInformation, hNewToken);
	if (processCreated) {
		// Allocate enough memory on the new process
		LPVOID baseAddress = (LPVOID)VirtualAllocEx(lpProcessInformation->hProcess, NULL, strlen(DLLPATH) + 1, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);

		// Copy the code to be injected
		WriteProcessMemory(lpProcessInformation->hProcess, baseAddress, DLLPATH, strlen(DLLPATH), NULL);

		OutputDebugStringA("-----> INJECTOR: DLL copied into host process memory space");

		// Notify the HostController that a new process has been created
		notifyNewPid(lpProcessInformation->dwProcessId);
		kern32dllmod = GetModuleHandle(TEXT("kernel32.dll"));
		HANDLE loadLibraryAddress = GetProcAddress(kern32dllmod, "LoadLibraryA");
		if (loadLibraryAddress == NULL)
		{
			OutputDebugStringW(TEXT("!!!!!LOADLIB IS NULL"));
			//error
			return 0;
		}
		else {
			OutputDebugStringW(TEXT("LOAD LIB OK"));
		}

		// Create a remote thread the remote thread
		HANDLE  threadHandle = CreateRemoteThread(lpProcessInformation->hProcess, NULL, 0, (LPTHREAD_START_ROUTINE)loadLibraryAddress, baseAddress, NULL, 0);
		if (threadHandle == NULL) {
			OutputDebugStringW(TEXT("!!!!REMTOE THREAD NOT OK"));
		}
		else {
			OutputDebugStringW(TEXT("!!!!REMTOE OK"));
		}
		OutputDebugStringA("-----> INJECTOR: Remote thread created");

		// Check if the process was meant to be stopped. If not, resume it now
		if ((originalFlags & CREATE_SUSPENDED) != CREATE_SUSPENDED) {
			// need to start it right away
			ResumeThread(lpProcessInformation->hThread);
			OutputDebugStringA("-----> INJECTOR: Thread resumed");
		}
	}
	return processCreated;
}