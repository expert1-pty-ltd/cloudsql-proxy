#include "extern.h"

// This function is simply used to execute the function pointer
// from the C# wrapper.
// Go - by itself - is unable to execute the function but it can call this
// C code which can
void invokeFunctionPointer(callbackFunc f, char* s, char* e)
{
		return f(s, e);
}