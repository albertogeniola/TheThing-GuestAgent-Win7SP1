#include "common.h"

#ifdef UNICODE
EXPORT const wchar_t * WINDOW_NAME = GUESTCONTROLLER_WINDOW_NAME;
#else
EXPORT const char * WINDOW_NAME = GUESTCONTROLLER_WINDOW_NAME;
#endif


