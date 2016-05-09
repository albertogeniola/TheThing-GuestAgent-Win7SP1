#include "stdafx.h"
#include "FileMap.h"


FileMap::FileMap()
{
}

void FileMap::UpdateFile(HANDLE h) {
	// Provide concurrency support by allowing only a thread per time
	std::lock_guard<std::mutex> lock(map_mutex);

	// If map contains 

}

FileMap::~FileMap()
{
}
