#include "cencode.h"
#include "cdecode.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <assert.h>

char* encode(const char* binput, const unsigned int bsize);
char* decode(const char* binput, const unsigned int bsize);

char* encode(const wchar_t* input);

wchar_t* decode(const char* input);