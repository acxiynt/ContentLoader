#ifndef CUTIL_H
#define CUTIL_H
#ifndef EXPORT
#define EXPORT __attribute__((visibility("default")))
#if _WIN32
#define EXPORT __declspec(dllexport)
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
    void *global_buf;
    char16_t **enable;
    char16_t **disable;
    int ecount;
    int dcount;
} depresult;

#endif
