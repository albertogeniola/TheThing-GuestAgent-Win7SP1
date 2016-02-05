// Injector.cpp : definisce il punto di ingresso dell'applicazione console.
#include "stdafx.h"
#include "injector.h"
#define GUESTCONTROLLER_WINDOW_NAME "WKWatcher"

/* Global variables */
HWND cwHandle;
static LPVOID lpvMem = NULL;      // pointer to shared memory
static HANDLE hMapObject = NULL;  // handle to file mapping

/*
This file is the injector. It will take care of configuring network hooking and start the program to analyze. 
You can invoke it by specifing exepath, dllpath and windowname. The windowname is the name of the window to which 
send messages about network traffic. 
*/
int WINAPI WinMain(HINSTANCE hInstance,	HINSTANCE hPrevInstance,LPSTR lpCmdLine,int nCmdShow)
{
	// Variables
	STARTUPINFO si;
	PROCESS_INFORMATION pi;
	bool processCreated= false;
	//pcap_t *adhandle;
	//pcap_if_t *alldevs;
	//pcap_if_t * d;
	//char errbuf[PCAP_ERRBUF_SIZE + 1];
	FILE* f;
	DWORD res;
	int argc = 0;
	PCHAR* argv;

	OutputDebugStringA("-----> INJECTOR: Starting.");

	argv = CommandLineToArgvA(GetCommandLine(), &argc);

	// Checking arguments
	// 1. ProcessPath
	// 2. dll to inject Path
	// 3. Name of the window to PostMessages to
	if (argc != 3)
	{
		OutputDebugStringA("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: Invalid injector usage.");
		fprintf(stderr, "Invalid Usage. Please specify 3 arguments: processpath, dllpath (space separated).");
		exit(-1);
	}
	// 1. ProcessPath: must exist
	if (fopen_s(&f, argv[1], "r")!=0)
	{
		OutputDebugStringA("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: ProcessPath invalid.");
		fprintf(stderr, "Cannot find file %s",argv[1]);
		return -1;
	}
	else
	{
		// File exists, close it now
		fclose(f);
	}

	// 2. Dll to inject
	if (fopen_s(&f, argv[2], "r")!=0)
	{
		OutputDebugStringA("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: DLL Path invalid.");
		fprintf(stderr, "Cannot find file %s", argv[2]);
		return -1;
	}
	else
	{
		// File exists, close it now
		fclose(f);
	}

	// 3. Name of the window to send message to
	cwHandle = FindWindow(NULL, GUESTCONTROLLER_WINDOW_NAME);
	// TODO: uncomment me
	if (cwHandle == NULL)
	{
		OutputDebugStringA("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: Cannot find the GuestController window to send message. Please check its name is ");
		OutputDebugStringA(GUESTCONTROLLER_WINDOW_NAME);
		fprintf(stderr, "Cannot find the window named %s", GUESTCONTROLLER_WINDOW_NAME);
		return -1;
	}

	OutputDebugStringA("-----> INJECTOR: Args are ok.");

	// Prepare arguments for create process
	ZeroMemory(&si, sizeof(STARTUPINFO));
	ZeroMemory(&pi, sizeof(PROCESS_INFORMATION));
	si.cb = sizeof(STARTUPINFO);

	/*
	//START EDIT
	// Configure the network listener
	// Network configuration
	// Look at the interfaces
	if (pcap_findalldevs_ex(PCAP_SRC_IF_STRING, NULL, &alldevs, errbuf) == -1)
	{
		OutputDebugStringA("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: Cannot register PCAP.");
		fprintf_s(stderr, "Error in pcap_findalldevs: %s\n", errbuf);
		return -2;
	}

	OutputDebugStringA("-----> INJECTOR: Pcap NICs looped.");

	// I will listen to all the available devices
	d = alldevs;
	while (d!=NULL)
	{
		if ((adhandle = pcap_open(d->name,  // name of the device
			65536,     // portion of the packet to capture. 
			// 65536 grants that the whole packet will be captured on all the MACs.
			0,         // promiscuous mode
			1000,      // read timeout
			NULL,      // remote authentication
			errbuf     // error buffer
			)) == NULL)
		{
			OutputDebugStringA("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: Unable to open PCAP driver.");
			fprintf(stderr, "\nUnable to open the adapter. %s is not supported by WinPcap\n");
			// Free the device list
			pcap_freealldevs(alldevs);
			exit(-2);
		}
		CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)&startThread, adhandle, 0, NULL);
		d = d->next;
	}
	pcap_freealldevs(alldevs);
	
	OutputDebugStringA("-----> INJECTOR: Pcap conf done.");

	processCreated = CreateProcess(
		NULL,
		argv[1],
		NULL,
		NULL,
		FALSE,
		CREATE_SUSPENDED,
		NULL,
		NULL,
		&si,
		&pi
		);
	
	if (!processCreated) {
		OutputDebugStringA("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: Unable to create process");
		return -3;
	}

	OutputDebugStringA("-----> INJECTOR: Process created suspended");
	
	// Load the loadLibraryA Address
	//LPVOID LoadLibraryAddr = (LPVOID)GetProcAddress(GetModuleHandle("kernel32.dll"),"LoadLibraryA");

	// Allocate enough memory on the new process
	LPVOID LLParam = (LPVOID)VirtualAllocEx(pi.hProcess, NULL, strlen(argv[2]),
		MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
	
	OutputDebugStringA("-----> INJECTOR: Memory allocated for DLL into host process");

	// Copy the code to be injected
	WriteProcessMemory(pi.hProcess, LLParam, argv[2], strlen(argv[2]), NULL);
	
	OutputDebugStringA("-----> INJECTOR: DLL copied into host process memory space");

	// Start the remote thread
	CreateRemoteThread(pi.hProcess, NULL, NULL, (LPTHREAD_START_ROUTINE)LoadLibraryA, LLParam, NULL, NULL);
	
	OutputDebugStringA("-----> INJECTOR: Remote thread created");

	//CloseHandle(hProcess);

	ResumeThread(pi.hThread);
	OutputDebugStringA("-----> INJECTOR: Thread resumed");
	//STOP EDIT
	*/
	
	if (!MyDetourCreateProcessWithDll(NULL, argv[1], NULL, NULL, TRUE, CREATE_DEFAULT_ERROR_MODE, NULL, NULL, &si, &pi, argv[2], NULL))
	{
		DWORD e = GetLastError();
		OutputDebugStringA("XXXXXXXXXXX INJECTOR ERROR XXXXXXXXXXXXXX: DetoursCreateProcessWithDll failed");
		printf("Error Creating process");
		exit(-1);
	}
	OutputDebugStringA("-----> INJECTOR: remote process created and DLL injected.");

	OutputDebugStringA("-----> INJECTOR: Waiting for child process to exit");
	// Wait until the process ends
	res = WaitForSingleObject(pi.hProcess, INFINITE);
	OutputDebugStringA("-----> INJECTOR: Child process ended.");
	CloseHandle(pi.hProcess);
	OutputDebugStringA("-----> INJECTOR: Child process handle closed.");
	return res;
}

/*
bool setupMemoryMapping(char* windowName)
{
	hMapObject = CreateFileMapping(
		INVALID_HANDLE_VALUE,   // use paging file
		NULL,                   // default security attributes
		PAGE_READWRITE,         // read/write access
		0,                      // size: high 32-bits
		SHMEMSIZE,              // size: low 32-bits
		TEXT(SHARED_MEM_NAME)); // name of map object
	if (hMapObject == NULL)
		return false;
	
	lpvMem = MapViewOfFile(
		hMapObject,     // object to map view of
		FILE_MAP_WRITE, // read/write access
		0,              // high offset:  map from
		0,              // low offset:   beginning
		0);             // default: map entire file
	if (lpvMem == NULL)
		return false;
	
	// Write zeroes
	memset(lpvMem, '\0', SHMEMSIZE);
	
	return true;
}*/

/*
void packet_handler(u_char *dumpfile, const struct pcap_pkthdr *header, const u_char *pkt_data)
{
	ip_header *ih;
	char protocol[10];
	ih = (ip_header *)(pkt_data + 14); //length of ethernet header
	int sourcePort = -1;
	int destinationPort = -1;
	int ip_len = (ih->ver_ihl & 0xf) * 4;
	tcp_header* th;
	udp_header* uh;
	std::wstring sourceAddr = std::wstring();
	std::wstring destinationAddr = std::wstring();

	pugi::xml_document doc; pugi::xml_node n = doc.append_child(L"Network");
	
	sourceAddr = std::to_wstring(ih->saddr.byte1);
	sourceAddr.append(L".");
	sourceAddr.append(std::to_wstring(ih->saddr.byte2));
	sourceAddr.append(L".");
	sourceAddr.append(std::to_wstring(ih->saddr.byte3));
	sourceAddr.append(L".");
	sourceAddr.append(std::to_wstring(ih->saddr.byte4));

	destinationAddr = std::to_wstring(ih->daddr.byte1);
	destinationAddr.append(L".");
	destinationAddr.append(std::to_wstring(ih->daddr.byte2));
	destinationAddr.append(L".");
	destinationAddr.append(std::to_wstring(ih->daddr.byte3));
	destinationAddr.append(L".");
	destinationAddr.append(std::to_wstring(ih->daddr.byte4));

	
	n.append_attribute(L"SourceAddr") = sourceAddr.c_str();
	n.append_attribute(L"DestinationAddr") = destinationAddr.c_str();

	switch (ih->proto)
	{
	case 6: // TCP
		sprintf_s(protocol, "TCP");
		th = (tcp_header *)((u_char*)ih + ip_len);
		sourcePort = ntohs(th->sport);
		destinationPort = ntohs(th->dport);
		n.append_attribute(L"Protocol") = L"TCP";
		n.append_attribute(L"DestinationPort") = std::to_wstring(destinationPort).c_str();
		n.append_attribute(L"SourcePort") = std::to_wstring(sourcePort).c_str();
		break;
	case 17:
		sprintf_s(protocol, "UDP");
		uh = (udp_header *)((u_char*)ih + ip_len);
		sourcePort = ntohs(uh->sport);
		destinationPort = ntohs(uh->dport);
		n.append_attribute(L"Protocol") = L"UDP";
		n.append_attribute(L"DestinationPort") = std::to_wstring(destinationPort).c_str();
		n.append_attribute(L"SourcePort") = std::to_wstring(sourcePort).c_str();
		break;
	default:
		sprintf_s(protocol, "Unkown");
		n.append_attribute(L"Protocol") = L"Other";
		n.append_attribute(L"ProtocolNumber") = std::to_wstring(ih->proto).c_str();
	}

	log(&n);
	//printf("\nIP Packet from %d.%d.%d.%d:%d to %d.%d.%d.%d:%d (%s) ", ih->saddr.byte1, ih->saddr.byte2, ih->saddr.byte3, ih->saddr.byte4, sourcePort, ih->daddr.byte1, ih->daddr.byte2, ih->daddr.byte3, ih->daddr.byte4, destinationPort, protocol);
}*/

/*
DWORD startThread(LPVOID lpdwThreadParam)
{
	//printf("Starting Thread for monitoring...");
	pcap_loop((pcap_t*)lpdwThreadParam, 0, packet_handler, NULL);
	return 0;
}
*/
/*
void log(pugi::xml_node * element)
{
	DWORD res = 0;
	COPYDATASTRUCT ds;
	
	std::wstringstream ss;

	element->print(ss, L"", pugi::format_no_declaration | pugi::format_raw);

	// Create a std::string and copy your document data in to the string    
	std::wstring str = ss.str();

	ds.dwData = 0;
	ds.cbData = str.size()*sizeof(wchar_t);
	ds.lpData = (wchar_t*)str.c_str();

	// Send message...
	SendMessage(cwHandle, WM_COPYDATA, 0, (LPARAM)&ds);
}
*/


PCHAR*
CommandLineToArgvA(
PCHAR CmdLine,
int* _argc
)
{
	PCHAR* argv;
	PCHAR  _argv;
	ULONG   len;
	ULONG   argc;
	CHAR   a;
	ULONG   i, j;

	BOOLEAN  in_QM;
	BOOLEAN  in_TEXT;
	BOOLEAN  in_SPACE;

	len = strlen(CmdLine);
	i = ((len + 2) / 2)*sizeof(PVOID)+sizeof(PVOID);

	argv = (PCHAR*)GlobalAlloc(GMEM_FIXED,
		i + (len + 2)*sizeof(CHAR));

	_argv = (PCHAR)(((PUCHAR)argv) + i);

	argc = 0;
	argv[argc] = _argv;
	in_QM = FALSE;
	in_TEXT = FALSE;
	in_SPACE = TRUE;
	i = 0;
	j = 0;

	while (a = CmdLine[i]) {
		if (in_QM) {
			if (a == '\"') {
				in_QM = FALSE;
			}
			else {
				_argv[j] = a;
				j++;
			}
		}
		else {
			switch (a) {
			case '\"':
				in_QM = TRUE;
				in_TEXT = TRUE;
				if (in_SPACE) {
					argv[argc] = _argv + j;
					argc++;
				}
				in_SPACE = FALSE;
				break;
			case ' ':
			case '\t':
			case '\n':
			case '\r':
				if (in_TEXT) {
					_argv[j] = '\0';
					j++;
				}
				in_TEXT = FALSE;
				in_SPACE = TRUE;
				break;
			default:
				in_TEXT = TRUE;
				if (in_SPACE) {
					argv[argc] = _argv + j;
					argc++;
				}
				_argv[j] = a;
				j++;
				in_SPACE = FALSE;
				break;
			}
		}
		i++;
	}
	_argv[j] = '\0';
	argv[argc] = NULL;

	(*_argc) = argc;
	return argv;
}


bool configureWindowName()
{
	char buf[SHMEMSIZE];
	buf[0] = '\0';

	cwHandle = FindWindow(NULL, GUESTCONTROLLER_WINDOW_NAME);

	if (cwHandle == NULL)
	{
		return false;
	}

	//sprintf_s(buf, "Sending events to window name: %s", GUESTCONTROLLER_WINDOW_NAME);

	OutputDebugString(buf);

	return true;
}

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
	PDETOUR_CREATE_PROCESS_ROUTINEA pfCreateProcessA)
{
	DWORD dwMyCreationFlags = (dwCreationFlags | CREATE_SUSPENDED);
	PROCESS_INFORMATION pi;

	if (pfCreateProcessA == NULL) {
		pfCreateProcessA = CreateProcessA;
	}

	if (!pfCreateProcessA(lpApplicationName,
		lpCommandLine,
		lpProcessAttributes,
		lpThreadAttributes,
		bInheritHandles,
		dwMyCreationFlags,
		lpEnvironment,
		lpCurrentDirectory,
		lpStartupInfo,
		&pi)) {
		return FALSE;
	}

	LPCSTR rlpDlls[2];
	DWORD nDlls = 0;
	if (lpDllName != NULL) {
		rlpDlls[nDlls++] = lpDllName;
	}

	if (!DetourUpdateProcessWithDll(pi.hProcess, rlpDlls, nDlls)) {
		TerminateProcess(pi.hProcess, ~0u);
		return FALSE;
	}

	if (lpProcessInformation) {
		CopyMemory(lpProcessInformation, &pi, sizeof(pi));
	}

	// Notify the GuestController we have spawned the process
	notifyNewPid(pi.dwProcessId);

	if (!(dwCreationFlags & CREATE_SUSPENDED)) {
		ResumeThread(pi.hThread);
	}
	return TRUE;
}

void notifyNewPid(DWORD pid)
{
	DWORD res = 0;
	COPYDATASTRUCT ds;

	ds.dwData = COPYDATA_PROC_SPAWNED;
	ds.cbData = sizeof(DWORD);
	ds.lpData = (PVOID)&pid;

	// Send message...
	SendMessage(cwHandle, WM_COPYDATA, 0, (LPARAM)&ds);

}