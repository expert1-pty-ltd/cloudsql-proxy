// define a type for the function pointer
typedef void (__stdcall *callbackFunc) (char*, char*, char*);

// define the interface for the invokeFunctionPointer
void invokeFunctionPointer(callbackFunc f, char* i, char* s, char* e);