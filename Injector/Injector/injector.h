#include <Windows.h>
#include <sstream>
#include <string>
#include <detours.h>
#include <memory.h> 
#include "pugixml.hpp"
#include "../../InstallerAnalyzer1.1/Common/common.h"
#pragma comment(lib, "detours.lib")


PCHAR* CommandLineToArgvA(PCHAR CmdLine, int* _argc);

BOOL setDebugPrivileges();

BOOL HookAndInjectService(const char* dllPath, const char* serviceName);

HANDLE RtlCreateUserThread(
	HANDLE hProcess,
	LPVOID lpBaseAddress,
	LPVOID lpSpace
	);

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
	PDETOUR_CREATE_PROCESS_ROUTINEA pfCreateProcessA);

bool isMsiFile(char* filepath);
std::string getMsiexecPath();

BOOL StartSampleService(SC_HANDLE schSCManager, const char* serviceName, DWORD* processId);