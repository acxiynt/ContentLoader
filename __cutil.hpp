#ifndef __cutil_h
#define __cutil_h
#ifndef EXPORT
#define EXPORT __declspec(dllexport)
#endif
#include <stdlib.h>
#include <unordered_set>

typedef struct
{
    char16_t* id;
    char16_t** dep;
    int depcount;
}mod_kvp;

typedef struct
{
    char16_t global_buf;
    char16_t** enable;
    char16_t** disable;
    int ecount;
    int dcount;
}depresult;
#endif
