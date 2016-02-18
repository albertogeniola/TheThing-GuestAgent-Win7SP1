#pragma once
#include <Windows.h>
#include <mutex>
#include <map>
#include <string>

class HandleMap
{
private:
	std::map<HANDLE, std::wstring> map;
	//std::recursive_mutex map_mutex; // protects the map from concurrent access

public:
	HandleMap();
	void Insert(HANDLE hFile, std::wstring fullPath);
	bool HandleMap::Lookup(HANDLE hFile, std::wstring& res);
	~HandleMap();
};

