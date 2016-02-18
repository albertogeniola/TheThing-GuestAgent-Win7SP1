#include "stdafx.h"
#include "HandleMap.h"


HandleMap::HandleMap()
{
}

void HandleMap::Insert(HANDLE hFile, std::wstring fullPath) {
	// Ensure exclusivity
	//std::lock_guard<std::recursive_mutex> lock(map_mutex);
	
	// The [] operator of the map returns a pointer to the mapped value or inserts a new value.
	map[hFile] = fullPath;

	// Now the destructor of the guard is invoked and lock will be released
}


bool HandleMap::Lookup(HANDLE hFile, std::wstring& res){
	// Ensure exclusivity
	//std::lock_guard<std::recursive_mutex> lock(map_mutex);

	// The AT() operator of a MAP will throw an exception if no matching value is found.
	try{
		res = map.at(hFile);
		return true;
	}
	catch (std::exception e) {
		return false;
	}

	// Lock will be released when going out of scope.
}

HandleMap::~HandleMap()
{
}
