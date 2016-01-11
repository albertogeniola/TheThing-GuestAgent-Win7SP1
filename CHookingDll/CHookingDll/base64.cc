/*
c-example1.c - libb64 example code

This is part of the libb64 project, and has been placed in the public domain.
For details, see http://sourceforge.net/projects/libb64

This is a short example of how to use libb64's C interface to encode
and decode a string directly.
The main work is done between the START/STOP ENCODING and DECODING lines.

Note that this is extremely simple; you will almost never have all the data
in a string ready to encode/decode!
Because we all the data to encode/decode in a string, and know its length,
we can get away with a single en/decode_block call.
For a more realistic example, see c-example2.c
*/

#include "stdafx.h"
#include "base64.h"

char* encode(const wchar_t* input)
{
	return encode((char*)input, wcslen(input)*sizeof(wchar_t)+1);
}

wchar_t* decode(const char* input)
{
	return (wchar_t*)decode(input, strlen(input) + 1);
}

char* encode(const char* binput, const unsigned int bsize)
{
	/* set up a destination buffer large enough to hold the encoded data */
	int buffDim = 4 * (bsize / 3);
	char* output = (char*)malloc(buffDim);
	/* keep track of our encoded position */
	char* c = output;
	/* store the number of bytes encoded by a single call */
	int cnt = 0;
	/* we need an encoder state */
	base64_encodestate s;

	/*---------- START ENCODING ----------*/
	/* initialise the encoder state */
	base64_init_encodestate(&s);
	/* gather data from the input and send it to the output */
	cnt = base64_encode_block(binput, bsize, c, &s);
	c += cnt;
	/* since we have encoded the entire input string, we know that
	there is no more input data; finalise the encoding */
	cnt = base64_encode_blockend(c, &s);
	c += cnt;
	/*---------- STOP ENCODING  ----------*/

	/* we want to print the encoded data, so null-terminate it: */
	*c = 0;

	return output;
}

char* decode(const char* binput, const unsigned int bsize)
{
	/* set up a destination buffer large enough to hold the encoded data */
	char* output = (char*)malloc(bsize * 3 / 4);
	/* keep track of our decoded position */
	char* c = output;
	/* store the number of bytes decoded by a single call */
	int cnt = 0;
	/* we need a decoder state */
	base64_decodestate s;

	/*---------- START DECODING ----------*/
	/* initialise the decoder state */
	base64_init_decodestate(&s);
	/* decode the input data */
	cnt = base64_decode_block(binput, bsize, c, &s);
	c += cnt;
	/* note: there is no base64_decode_blockend! */
	/*---------- STOP DECODING  ----------*/

	/* we want to print the decoded data, so null-terminate it: */
	*c = 0;

	return output;
}