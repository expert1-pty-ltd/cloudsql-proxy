#ifdef __WIN32
// define a type for the function pointer
typedef void (__stdcall *callbackFunc) (char*, char*, char*);
#else
// define a type for the function pointer
typedef void (*callbackFunc) (char*, char*, char*);
#endif

// define the interface for the invokeFunctionPointer
void invokeFunctionPointer(callbackFunc f, char* i, char* s, char* e);