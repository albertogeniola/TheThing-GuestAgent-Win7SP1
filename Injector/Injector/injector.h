#include <Windows.h>
#include <sstream>
#include <string>
#include <detours.h>
#include <memory.h> 
#include "pugixml.hpp"
#pragma comment(lib, "detours.lib")

void log(pugi::xml_node * element);

PCHAR* CommandLineToArgvA(PCHAR CmdLine, int* _argc);

void notifyNewPid(DWORD pid);

BOOL setDebugPrivileges();

BOOL HookAndInjectDCOMLauncher(const char* dllPath);

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