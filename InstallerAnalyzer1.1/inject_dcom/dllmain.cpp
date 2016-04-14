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

HWND cwHandle = NULL;
HMODULE kern32dllmod = NULL;




void NtStatusToString(NTSTATUS status, string* s)
{
	s->clear();
	*s = to_string(status);
}


void log(pugi::xml_node *element)
{
	DWORD res = 0;
	COPYDATASTRUCT ds;

	element->append_attribute(_T("ThreadId")) = to_string(GetCurrentThreadId()).c_str();
	element->append_attribute(_T("PId")) = to_string(GetCurrentProcessId()).c_str();
	
	// We use a wchart_t type for buffer

	std::wostringstream ss;
	element->print(ss, TEXT(""), pugi::format_no_declaration | pugi::format_raw, pugi::xml_encoding::encoding_utf16_le);

	std::wstring s = ss.str();
	const wchar_t* str = s.c_str();

	ds.dwData = COPYDATA_LOG;
	ds.cbData = s.length()*sizeof(wchar_t);
	ds.lpData = (PVOID)str;

	// Send message...
	SendMessage(cwHandle, WM_COPYDATA, 0, (LPARAM)&ds);

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

	_stprintf_s(buf, TEXT("Sending events to window name: %s"), GUESTCONTROLLER_WINDOW_NAME);

	OutputDebugString(buf);

	return true;
}


void notifyNewPid(HWND cwHandle, DWORD pid)
{
	DWORD res = 0;
	COPYDATASTRUCT ds;

	ds.dwData = COPYDATA_PROC_SPAWNED;
	ds.cbData = sizeof(DWORD);
	ds.lpData = (PVOID)&pid;

	// Send message...
	SendMessage(cwHandle, WM_COPYDATA, 0, (LPARAM)&ds);

}

void notifyRemovedPid(HWND cwHandle, DWORD pid)
{
	DWORD res = 0;
	COPYDATASTRUCT ds;

	ds.dwData = COPYDATA_PROC_DIED;
	ds.cbData = sizeof(DWORD);
	ds.lpData = (PVOID)&pid;

	// Send message...
	SendMessage(cwHandle, WM_COPYDATA, 0, (LPARAM)&ds);

}


BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
	)
{
	HMODULE ntdll = GetModuleHandleA("ntdll.dll");
	kern32dllmod = kern32dllmod = GetModuleHandle(TEXT("kernel32.dll"));

	if (!configureWindowName()) {
		OutputDebugStringA("Couldn't find HostController Window Name!");
		return FALSE;
	}

	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		OutputDebugStringA("XXXXX DCOM INJECTOR: injection started. Time to hook! XXXXX");
		realCreateProcessInternalW = (pCreateProcessInternalW)(GetProcAddress(kern32dllmod, "CreateProcessInternalW"));

		// CreateProcessInternalW
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realCreateProcessInternalW, MyCreateProcessInternalW);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(TEXT("CreateProcessInternalW not derouted correctly"));
		else
			OutputDebugString(TEXT("CreateProcessInternalW successful"));

		// ExitProcess
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)realExitProcess, MyExitProcess);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(TEXT("ExitProcess not derouted correctly"));
		else
			OutputDebugString(TEXT("ExitProcess successful"));

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


		// ExitProcess
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)realExitProcess, MyExitProcess);
		if (DetourTransactionCommit() != NO_ERROR)
			OutputDebugString(TEXT("ExitProcess not detached correctly"));
		else
			OutputDebugString(TEXT("ExitProcess detached successful"));

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
		notifyNewPid(cwHandle, lpProcessInformation->dwProcessId);
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

	// Use a node object to create the XML string: this will contain all information about the SysCall
	pugi::xml_document doc; 
	pugi::xml_node element = doc.append_child(TEXT("CreateProcessInternalW"));

	// >>>>>>>>>>>>>>> Result <<<<<< <<<<<<<<<
	string w = string();
	NtStatusToString(processCreated, &w);
	element.addAttribute(TEXT("Result"), w.c_str());

	log(&element);

	return processCreated;
}

VOID WINAPI MyExitProcess(UINT uExitCode)
{
	// We hook this to let the GuestController know about our intention to terminate
	notifyRemovedPid(cwHandle, GetCurrentProcessId());
	return realExitProcess(uExitCode);
}
