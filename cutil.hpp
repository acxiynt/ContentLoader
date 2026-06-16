#ifndef CUTIL_H
#define CUTIL_H
#ifndef EXPORT
#if _WIN32
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif
#endif
#include <stdlib.h>
#include <unordered_set>

typedef struct
{
    char16_t *id;
    char16_t **dep;
    int depcount;
} mod_kvp;

typedef struct
{
    char16_t **enable;
    char16_t **disable;
    int ecount;
    int dcount;
} depresult;

#endif
