#pragma once
#include <string>

#ifdef UNICODE
typedef std::wstring string;
typedef std::wstringstream stringstream;

template <typename T>string to_string(T a) {
	return std::to_wstring(a);
}
#endif


class FileInfo
{

private:
	string _path=nullptr;
	string _original_hash = nullptr;
	string _final_hash = nullptr;
	bool deleted=false;

public:
	FileInfo();
	~FileInfo();
};

