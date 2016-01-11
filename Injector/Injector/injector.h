#define HAVE_REMOTE
#define SHMEMSIZE 4096
#define SHARED_MEM_NAME "loggerwindowname"
#include <WinSock2.h>
#include <sstream>
#include <string>
#include <detours.h>
#include "pcap.h"
#include <memory.h> 
#include "pugixml.hpp"
#pragma comment(lib, "detours.lib")
#pragma comment(lib, "ws2_32.lib")
#pragma comment(lib, "wpcap.lib")


typedef struct ip_address{
	u_char byte1;
	u_char byte2;
	u_char byte3;
	u_char byte4;
}ip_address;

/* IPv4 header */
typedef struct ip_header{
	u_char  ver_ihl;        // Version (4 bits) + Internet header length (4 bits)
	u_char  tos;            // Type of service 
	u_short tlen;           // Total length 
	u_short identification; // Identification
	u_short flags_fo;       // Flags (3 bits) + Fragment offset (13 bits)
	u_char  ttl;            // Time to live
	u_char  proto;          // Protocol
	u_short crc;            // Header checksum
	ip_address  saddr;      // Source address
	ip_address  daddr;      // Destination address
	u_int   op_pad;         // Option + Padding
}ip_header;

typedef struct tcp_header{
	u_short sport;          // Source port
	u_short dport;          // Destination port
	u_int seqn;            // sequence number
	u_int ackn;            // ack number
}tcp_header;

typedef struct udp_header{
	u_short sport;          // Source port
	u_short dport;          // Destination port
	u_int len;            // length
	u_int check;            // checksum

}udp_header;

void packet_handler(u_char *dumpfile, const struct pcap_pkthdr *header, const u_char *pkt_data);
DWORD startThread(LPVOID lpdwThreadParam);
bool setupMemoryMapping(char* windowName);
void log(pugi::xml_node * element);
PCHAR* CommandLineToArgvA(PCHAR CmdLine, int* _argc);