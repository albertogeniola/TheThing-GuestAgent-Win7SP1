#pragma once
#include <Windows.h>
#include <map>
#include <mutex>
#include "FileInfo.h"


class FileMap
{
private:
	std::map<HANDLE, FileInfo> m;
	std::mutex map_mutex;  // protects the map

public:
	FileMap();
	void UpdateFile(HANDLE h);
	~FileMap();
};

