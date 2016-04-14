#define BUFSIZE 512
#define SHMEMSIZE 4096

#include <cstdio>
#include <string>
#include <tchar.h>
#define WIN32_NO_STATUS
#include <windows.h>
#undef WIN32_NO_STATUS
#include <winternl.h>
#include <ntstatus.h>
#include <psapi.h>
#include <strsafe.h>
#include "pugixml.hpp"
#include <sstream>
#include <detours.h>

// Windows UNICODE PAIN
#ifdef UNICODE
typedef std::wstring string;
typedef std::wstringstream stringstream;

template <typename T>string to_string(T a) {
	return std::to_wstring(a);
}
#endif

//#include <winsock2.h>
#pragma comment(lib, "detours.lib")	// Nedded for DTOURS
#pragma comment(lib, "ntdll.lib")	// Needed to hooking NtCreateFile

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

/* >>>>>>>>>>>>>> NtCreateFile <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtCreateFile)(PHANDLE FileHandle, ULONG DesiredAccess, PVOID ObjectAttributes, PVOID IoStatusBlock, PLARGE_INTEGER AllocationSize, ULONG FileAttributes, ULONG ShareAccess, ULONG CreateDisposition, ULONG CreateOptions, PVOID EaBuffer, ULONG EaLength);
NTSTATUS WINAPI MyNtCreateFile(PHANDLE FileHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PIO_STATUS_BLOCK IoStatusBlock, PLARGE_INTEGER AllocationSize, ULONG FileAttributes, ULONG ShareAccess, ULONG CreateDisposition, ULONG CreateOptions, PVOID EaBuffer, ULONG EaLength);
static pNtCreateFile realNtCreateFile;

/* >>>>>>>>>>>>>> NtOpenFile <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtOpenFile)(PHANDLE FileHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PIO_STATUS_BLOCK IoStatusBlock, ULONG ShareAccess, ULONG OpenOptions);
NTSTATUS WINAPI MyNtOpenFile(PHANDLE FileHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PIO_STATUS_BLOCK IoStatusBlock, ULONG ShareAccess, ULONG OpenOptions);
static pNtOpenFile realNtOpenFile;

/* >>>>>>>>>>>>>> NtOpenDirectoryObject <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtOpenDirectoryObject)(PHANDLE DirectoryHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes);
NTSTATUS WINAPI MyNtOpenDirectoryObject(PHANDLE FileHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes);
static pNtOpenDirectoryObject realNtOpenDirectoryObject;

/* >>>>>>>>>>>>>> NtDeleteFile <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtDeleteFile)(POBJECT_ATTRIBUTES ObjectAttributes);
NTSTATUS WINAPI MyNtDeleteFile(POBJECT_ATTRIBUTES ObjectAttributes);
static pNtDeleteFile realNtDeleteFile;

/* >>>>>>>>>>>>>> NtCreateKey <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtCreateKey)(PHANDLE KeyHandle,ACCESS_MASK DesiredAccess,POBJECT_ATTRIBUTES ObjectAttributes,ULONG TitleIndex,PUNICODE_STRING Class,ULONG CreateOptions,PULONG Disposition);
NTSTATUS WINAPI MyNtCreateKey(PHANDLE KeyHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, ULONG TitleIndex, PUNICODE_STRING Class, ULONG CreateOptions, PULONG Disposition);
static pNtCreateKey realNtCreateKey;

/* >>>>>>>>>>>>>> NtDeleteKey <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtDeleteKey)(HANDLE KeyHandle);
NTSTATUS WINAPI MyNtDeleteKey(HANDLE KeyHandle);
static pNtDeleteKey realNtDeleteKey;

/* >>>>>>>>>>>>>> NtOpenKey <<<<<<<<<<<<<<< */
typedef enum _KEY_INFORMATION_CLASS {
	KeyBasicInformation = 0,
	KeyNodeInformation = 1,
	KeyFullInformation = 2,
	KeyNameInformation = 3,
	KeyCachedInformation = 4,
	KeyFlagsInformation = 5,
	KeyVirtualizationInformation = 6,
	KeyHandleTagsInformation = 7,
	MaxKeyInfoClass = 8
} KEY_INFORMATION_CLASS;
typedef ULONG(WINAPI * pNtQueryKey)(HANDLE KeyHandle, KEY_INFORMATION_CLASS KeyInformationClass, PVOID KeyInformation, ULONG Length, PULONG ResultLength);
NTSTATUS WINAPI MyNtQueryKey(HANDLE KeyHandle, KEY_INFORMATION_CLASS KeyInformationClass, PVOID KeyInformation, ULONG Length, PULONG ResultLength);
static pNtQueryKey realNtQueryKey;

/* >>>>>>>>>>>>>> NtOpenKey <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtOpenKey)(PHANDLE KeyHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes);
NTSTATUS WINAPI MyNtOpenKey(PHANDLE KeyHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes);
static pNtOpenKey realNtOpenKey;

/* >>>>>>>>>>>>>> NtDeleteValueKey <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtDeleteValueKey)(HANDLE KeyHandle,PUNICODE_STRING ValueName);
NTSTATUS WINAPI MyNtDeleteValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName);
static pNtDeleteValueKey realNtDeleteValueKey;

/* >>>>>>>>>>>>>> NtEnumerateKey <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtEnumerateKey)(HANDLE KeyHandle, ULONG Index, KEY_INFORMATION_CLASS KeyInformationClass, PVOID KeyInformation, ULONG Length, PULONG ResultLength);
NTSTATUS WINAPI MyNtEnumerateKey(HANDLE KeyHandle, ULONG Index, KEY_INFORMATION_CLASS KeyInformationClass, PVOID KeyInformation, ULONG Length, PULONG ResultLength);
static pNtEnumerateKey realNtEnumerateKey;

/* >>>>>>>>>>>>>> NtEnumerateValueKey <<<<<<<<<<<<<<< */
typedef enum _KEY_VALUE_INFORMATION_CLASS {
	KeyValueBasicInformation = 0,
	KeyValueFullInformation = 1,
	KeyValuePartialInformation = 2,
	KeyValueFullInformationAlign64 = 3,
	KeyValuePartialInformationAlign64 = 4,
	MaxKeyValueInfoClass = 5
} KEY_VALUE_INFORMATION_CLASS;
typedef ULONG(WINAPI * pNtEnumerateValueKey)(HANDLE KeyHandle, ULONG Index, KEY_VALUE_INFORMATION_CLASS KeyValueInformationClass, PVOID KeyValueInformation, ULONG Length, PULONG ResultLength);
NTSTATUS WINAPI MyNtEnumerateValueKey(HANDLE KeyHandle, ULONG Index, KEY_VALUE_INFORMATION_CLASS KeyValueInformationClass, PVOID KeyValueInformation, ULONG Length, PULONG ResultLength);
static pNtEnumerateValueKey realNtEnumerateValueKey;

/* >>>>>>>>>>>>>> NtLockFile <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtLockFile)(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine, PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, PLARGE_INTEGER ByteOffset, PLARGE_INTEGER Length, ULONG Key, BOOLEAN FailImmediately, BOOLEAN ExclusiveLock);
NTSTATUS WINAPI MyNtLockFile(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine, PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, PLARGE_INTEGER ByteOffset, PLARGE_INTEGER Length, ULONG Key, BOOLEAN FailImmediately, BOOLEAN ExclusiveLock);
static pNtLockFile realNtLockFile;

/* >>>>>>>>>>>>>> NtOpenProcess <<<<<<<<<<<<<<< */
/*
typedef struct _CLIENT_ID
{
	HANDLE UniqueProcess;
	HANDLE UniqueThread;
} CLIENT_ID, *PCLIENT_ID;
typedef ULONG(WINAPI * pNtOpenProcess)(PHANDLE ProcessHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PCLIENT_ID ClientId);
NTSTATUS WINAPI MyNtOpenProcess(PHANDLE ProcessHandle, ACCESS_MASK DesiredAccess, POBJECT_ATTRIBUTES ObjectAttributes, PCLIENT_ID ClientId);
static pNtOpenProcess realNtOpenProcess;
*/

/* >>>>>>>>>>>>>> NtQueryDirectoryFile<<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtQueryDirectoryFile)(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine, PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, PVOID FileInformation, ULONG Length, FILE_INFORMATION_CLASS FileInformationClass, BOOLEAN ReturnSingleEntry, PUNICODE_STRING FileName, BOOLEAN RestartScan);
NTSTATUS WINAPI MyNtQueryDirectoryFile(HANDLE FileHandle, HANDLE Event, PIO_APC_ROUTINE ApcRoutine, PVOID ApcContext, PIO_STATUS_BLOCK IoStatusBlock, PVOID FileInformation, ULONG Length, FILE_INFORMATION_CLASS FileInformationClass, BOOLEAN ReturnSingleEntry, PUNICODE_STRING FileName, BOOLEAN RestartScan);
static pNtQueryDirectoryFile realNtQueryDirectoryFile;

/* >>>>>>>>>>>>>> NtQueryFullAttributesFile <<<<<<<<<<<<<<< */
typedef struct _FILE_NETWORK_OPEN_INFORMATION {
	LARGE_INTEGER CreationTime;
	LARGE_INTEGER LastAccessTime;
	LARGE_INTEGER LastWriteTime;
	LARGE_INTEGER ChangeTime;
	LARGE_INTEGER AllocationSize;
	LARGE_INTEGER EndOfFile;
	ULONG         FileAttributes;
} FILE_NETWORK_OPEN_INFORMATION, *PFILE_NETWORK_OPEN_INFORMATION;
typedef ULONG(WINAPI * pNtQueryFullAttributesFile)(POBJECT_ATTRIBUTES ObjectAttributes, PFILE_NETWORK_OPEN_INFORMATION FileInformation);
NTSTATUS WINAPI MyNtQueryFullAttributesFile(POBJECT_ATTRIBUTES ObjectAttributes, PFILE_NETWORK_OPEN_INFORMATION FileInformation);
static pNtQueryFullAttributesFile realNtQueryFullAttributesFile;

/* >>>>>>>>>>>>>> NtQueryValueKey <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtQueryValueKey)(HANDLE KeyHandle,PUNICODE_STRING ValueName,KEY_VALUE_INFORMATION_CLASS KeyValueInformationClass,PVOID KeyValueInformation,ULONG Length,PULONG ResultLength);
NTSTATUS WINAPI MyNtQueryValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName, KEY_VALUE_INFORMATION_CLASS KeyValueInformationClass, PVOID KeyValueInformation, ULONG Length, PULONG ResultLength);
static pNtQueryValueKey realNtQueryValueKey;

/* >>>>>>>>>>>>>> NtSetInformationFile <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtSetInformationFile)(HANDLE FileHandle, PIO_STATUS_BLOCK IoStatusBlock, PVOID FileInformation, ULONG Length, FILE_INFORMATION_CLASS FileInformationClass);
NTSTATUS WINAPI MyNtSetInformationFile(HANDLE FileHandle, PIO_STATUS_BLOCK IoStatusBlock, PVOID FileInformation, ULONG Length, FILE_INFORMATION_CLASS FileInformationClass);
static pNtSetInformationFile realNtSetInformationFile;

/* >>>>>>>>>>>>>> NtSetValueKey <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtSetValueKey)(HANDLE KeyHandle,PUNICODE_STRING ValueName,ULONG TitleIndex,ULONG Type,PVOID Data,ULONG DataSize);
NTSTATUS WINAPI MyNtSetValueKey(HANDLE KeyHandle, PUNICODE_STRING ValueName, ULONG TitleIndex, ULONG Type, PVOID Data, ULONG DataSize);
static pNtSetValueKey realNtSetValueKey;

/* >>>>>>>>>>>>>> NtQueryInformationProcess: Not to be Hooked <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtQueryInformationProcess)(HANDLE ProcessHandle, PROCESSINFOCLASS ProcessInformationClass, PVOID ProcessInformation, ULONG ProcessInformationLength, PULONG ReturnLength);
static pNtQueryInformationProcess realNtQueryInformationProcess;

/* >>>>>>>>>>>>>> NtTerminateProcess <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtTerminateProcess)(HANDLE ProcessHandle, NTSTATUS ExitStatus);
NTSTATUS WINAPI MyNtTerminateProcess(HANDLE ProcessHandle, NTSTATUS ExitStatus);
static pNtTerminateProcess realNtTerminateProcess;

/* >>>>>>>>>>>>>> NtClose <<<<<<<<<<<<<<< */
typedef ULONG(WINAPI * pNtClose)(HANDLE Handle);
NTSTATUS WINAPI MyNtClose(HANDLE Handle);
static pNtClose realNtClose;

/* >>>>>>>>>>>>>> CreateProcessA <<<<<<<<<<<<<<< 
typedef BOOL(WINAPI * pCreateProcessA)(LPCTSTR lpApplicationName, LPTSTR lpCommandLine, LPSECURITY_ATTRIBUTES lpProcessAttributes,LPSECURITY_ATTRIBUTES lpThreadAttributes, BOOL bInheritHandles,  DWORD dwCreationFlags, LPVOID lpEnvironment, LPCTSTR lpCurrentDirectory, LPSTARTUPINFO lpStartupInfo, LPPROCESS_INFORMATION lpProcessInformation);
BOOL WINAPI MyCreateProcessA(LPCTSTR lpApplicationName, LPTSTR lpCommandLine, LPSECURITY_ATTRIBUTES lpProcessAttributes, LPSECURITY_ATTRIBUTES lpThreadAttributes, BOOL bInheritHandles, DWORD dwCreationFlags, LPVOID lpEnvironment, LPCTSTR lpCurrentDirectory, LPSTARTUPINFO lpStartupInfo, LPPROCESS_INFORMATION lpProcessInformation);
static pCreateProcessA realCreateProcessA;

 >>>>>>>>>>>>>> CreateProcessW <<<<<<<<<<<<<<< 
typedef BOOL(WINAPI * pCreateProcessW)(LPCWSTR lpApplicationName, LPWSTR lpCommandLine, LPSECURITY_ATTRIBUTES lpProcessAttributes, LPSECURITY_ATTRIBUTES lpThreadAttributes, BOOL bInheritHandles, DWORD dwCreationFlags, LPVOID lpEnvironment, LPCWSTR lpCurrentDirectory, LPSTARTUPINFO lpStartupInfo, LPPROCESS_INFORMATION lpProcessInformation);
BOOL WINAPI MyCreateProcessW(LPCWSTR lpApplicationName, LPWSTR lpCommandLine, LPSECURITY_ATTRIBUTES lpProcessAttributes, LPSECURITY_ATTRIBUTES lpThreadAttributes, BOOL bInheritHandles, DWORD dwCreationFlags, LPVOID lpEnvironment, LPCWSTR lpCurrentDirectory, LPSTARTUPINFO lpStartupInfo, LPPROCESS_INFORMATION lpProcessInformation);
static pCreateProcessW realCreateProcessW;
*/

/* >>>>>>>>>>>>>> CreateProcessInternalA <<<<<<<<<<<<<<< 
typedef BOOL(WINAPI * pCreateProcessInternalA)(HANDLE hToken,
	LPCTSTR lpApplicationName,
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
BOOL WINAPI MyCreateProcessInternalA(HANDLE hToken,
	LPCTSTR lpApplicationName,
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
static pCreateProcessInternalA realCreateProcessInternalA;
*/

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


/* >>>>>>>>>>>>>> ExitProcess <<<<<<<<<<<<<<< */
typedef VOID(WINAPI * pExitProcess)(UINT uExitCode);
VOID WINAPI MyExitProcess(UINT uExitCode);
static pExitProcess realExitProcess;

// >>>>>>>>>>>>>> Utilities <<<<<<<<<<<<<<<<<<< 
void GetHandleFileName(HANDLE hHandle, std::wstring* fname);
string StandardAccessMaskToString(ACCESS_MASK DesiredAccess);
bool IsRequestingWriteAccess(ACCESS_MASK DesiredAccess);
bool IsRequestingRegistryWriteAccess(ACCESS_MASK DesiredAccess);
std::wstring GetFullPathByObjectAttributes(POBJECT_ATTRIBUTES ObjectAttributes);

void FileCreateOptionsToString(ULONG OpenCreateOption, std::wstring* s);
void ShareAccessToString(ULONG ShareAccess, std::wstring* s);
void IoStatusToString(IO_STATUS_BLOCK* IoStatusBlock, std::wstring* s);
void FileAttributesToString(ULONG FileAttributes, std::wstring* s);
void KeyCreateOptionsToString(ULONG CreateOption, std::wstring* s);
void NtStatusToString(NTSTATUS status, std::wstring* s);
const wchar_t* CreateDispositionToString(ULONG CreateDisposition);
const wchar_t* KeyInformationClassToString(KEY_INFORMATION_CLASS keyinfo);
void GetKeyPathFromKKEY(HANDLE keym, std::wstring* s);
const wchar_t* KeyValueInformationClassToString(KEY_VALUE_INFORMATION_CLASS value_info_class);
BOOL GetFileNameFromHandle(HANDLE hFile, std::wstring* w);
void add_value_name(pugi::xml_node * element, PUNICODE_STRING ValueName);
void from_unicode_to_wstring(PUNICODE_STRING u, std::wstring* w);

/* Messages functions */
void log(pugi::xml_node *element);
bool configureWindowName();
void notifyNewPid(HWND cwHandle, DWORD pid);
void notifyRemovedPid(HWND cwHandle, DWORD pid);
void NotifyFileAccess(std::wstring fullPath, const int AccessMode);
void NotifyRegistryAccess(std::wstring fullPath, const int AccessMode);
void NotifyFileRename(std::wstring oldPath, std::wstring newPath);

const DWORD WRITE_FLAGS[] = { 
	FILE_WRITE_DATA,
	FILE_APPEND_DATA,
	FILE_ADD_FILE,
	FILE_ADD_SUBDIRECTORY,
	FILE_WRITE_EA,
	FILE_DELETE_CHILD,
	FILE_WRITE_ATTRIBUTES,
	FILE_ALL_ACCESS,
	FILE_GENERIC_WRITE
};

const DWORD REGISTRY_WRITE_FLAGS[] = {
	KEY_SET_VALUE,
	KEY_CREATE_SUB_KEY,
	KEY_CREATE_LINK,
	KEY_WRITE,
	KEY_ALL_ACCESS
};

typedef struct FILE_RENAME_INFORMATION {
	BOOLEAN ReplaceIfExists;
	HANDLE  RootDirectory;
	ULONG   FileNameLength;
	WCHAR   FileName[1];
} FILE_RENAME_INFORMATION, *PFILE_RENAME_INFORMATION;