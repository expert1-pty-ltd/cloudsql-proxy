// define a type for the function pointer
typedef void (*callbackFunc) (char*);

// define the interface for the invokeFunctionPointer
void invokeFunctionPointer(callbackFunc f, char* s);