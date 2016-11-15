#include "stdafx.h"
#define WK_MITM_EVENT _T("wk_mitm_event")

int main() {

	// Open the event
	HANDLE evt = NULL;
	evt = OpenEvent(EVENT_MODIFY_STATE | SYNCHRONIZE, FALSE, WK_MITM_EVENT);
	
	if (evt == NULL) {
		printf("Error OpenEvent, code %i", GetLastError());
		system("pause");
		return -1;
	}

	if (SetEvent(evt) == 0) {
		printf("Error SetEvent, code %i", GetLastError());
		system("pause");
		return -2;
	}

	return 0;
}