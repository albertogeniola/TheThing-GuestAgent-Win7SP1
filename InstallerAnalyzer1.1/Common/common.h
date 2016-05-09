#ifdef COMMON_EXPORTS
#    define EXPORT __declspec(dllexport)
#else
#    define EXPORT __declspec(dllimport)
#endif

#pragma once

// According to the MSDN documentation, this is the maximum path length
#define PATH_MAX_LEN 260


// Pipe constants
#define EVENT_PIPE_TIMEOUT 20000 // Wait up to 20 seconds for event dispatching. Those are crucial and it is worth to wait if the GuestController is busy
#define LOG_PIPE_TIMEOUT 3000
#define EVENT_PIPE "\\\\.\\pipe\\wk_event_pipe"
#define LOG_PIPE "\\\\.\\pipe\\wk_log_pipe"

#define DLLPATH "C:\\GuestController\\CHookingDll.dll"
#define DCOM_LAUNCH_SERVICE_NAME "DcomLaunch"
#define WINDOWS_INSTALLER_SERVICE_NAME "msiserver"


// Process Constants
#define WK_PROCESS_EVENT TEXT("ProcessEvent")
#define WK_PROCESS_EVENT_TYPE TEXT("Type")
#define WK_PROCESS_EVENT_TYPE_SPAWNED TEXT("Spawned")
#define WK_PROCESS_EVENT_TYPE_DEAD TEXT("Dead")
#define WK_PROCESS_EVENT_PARENT_PID TEXT("ParentPid")
#define WK_PROCESS_EVENT_PID TEXT("Pid")

// File and registry constants
#define WK_FILE_EVENT TEXT("FileEvent")
#define WK_FILE_EVENT_MODE TEXT("Mode")
#define WK_FILE_EVENT_PATH TEXT("Path")
#define WK_FILE_EVENT_OLD_PATH TEXT("OldPath")
#define WK_FILE_EVENT_NEW_PATH TEXT("NewPath")
#define WK_REGISTRY_EVENT TEXT("RegistryEvent")
#define WK_REGISTRY_EVENT_MODE TEXT("Mode")
#define WK_REGISTRY_EVENT_PATH TEXT("Path")

#define WK_FILE_CREATED TEXT("Create")
#define WK_FILE_OPENED TEXT("Open")
#define WK_FILE_DELETED TEXT("Delete")
#define WK_FILE_RENAMED TEXT("Rename")
#define WK_KEY_OPENED TEXT("Open")
#define WK_KEY_CREATED TEXT("Create")

#define PIPE_ACK 1
#define PIPE_NACK 0
