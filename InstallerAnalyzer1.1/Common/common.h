#ifdef COMMON_EXPORTS
#    define EXPORT __declspec(dllexport)
#else
#    define EXPORT __declspec(dllimport)
#endif

#pragma once

// Constants used for IPC among our processes
#define COPYDATA_LOG 0

#define COPYDATA_PROC_SPAWNED 1
#define COPYDATA_PROC_DIED 2

#define COPYDATA_FILE_CREATED 3
#define COPYDATA_FILE_DELETED 4
#define COPYDATA_FILE_OPENED 5

#define COPYDATA_KEY_CREATED 10
#define COPYDATA_KEY_OPEN 11

// Guest Controller window's name
#ifdef UNICODE
#define GUESTCONTROLLER_WINDOW_NAME L"WKWatcher"
#else
#define GUESTCONTROLLER_WINDOW_NAME "WKWatcher"
#endif


