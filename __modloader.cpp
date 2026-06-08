#include "cutil.hpp"
#include <unordered_map>
#include <vector>
#include <string>
extern "C"
{
    // callee have to free the params
    EXPORT __restrict depresult *solvedep(char16_t **__restrict _loaded, mod_kvp *__restrict _toload, int loadedlen, int toloadlen, char16_t ***__restrict reason)
    {
        std::unordered_map<std::u16string, std::unordered_set<std::u16string>> toload = std::unordered_map<std::u16string, std::unordered_set<std::u16string>>();
        std::unordered_map<std::u16string, std::unordered_set<std::u16string>> missing = std::unordered_map<std::u16string, std::unordered_set<std::u16string>>();
        std::unordered_set<std::u16string> enable = std::unordered_set<std::u16string>();
        std::unordered_set<std::u16string> disable = std::unordered_set<std::u16string>();
        std::unordered_set<std::u16string> loaded = std::unordered_set<std::u16string>();
        for (int a = 0; a < loadedlen; a++)
            loaded.insert(std::u16string(_loaded[a]));

        // serializes raw pointer of mods to load into alldep and toload
        std::unordered_set<std::u16string> dep = std::unordered_set<std::u16string>();
        for (int a = 0; a < toloadlen; a++)
        {
            std::u16string _mod = std::u16string(_toload[a].id);
            for (int b = 0; b < _toload[a].depcount; b++)
            {
                std::u16string _dep = std::u16string(_toload[a].dep[b]);
                if (loaded.count(_dep))
                    continue;
                dep.insert(_dep);
            }
            toload.insert(std::pair(std::u16string(_mod), dep));
            dep.clear();
        }

        // i dont have a single clue how to finish the algo with this, i will only check if it have missing dependencies
        for (const std::pair<std::u16string, std::unordered_set<std::u16string>> &pair : toload)
        {
            if (pair.second.empty())
            {
                enable.insert(pair.first);
                continue;
            }
            bool _missing = false;
            // disable mod if requiring non-existent dependency
            for (const std::u16string &mod : pair.second)
            {
                if (!loaded.count(mod) && !toload.count(mod))
                {
                    _missing = true;
                    missing[pair.first].insert(mod);
                }
            }
            if (_missing)
                disable.insert(pair.first);
        }
        int size = 0;
        int ptrsize = 0;
        std::vector<std::u16string> disable_ordered = std::vector<std::u16string>(disable.begin(), disable.end());
        for (const std::u16string &mod : enable)
        {
            size += mod.length() * 2 + 2;
            ptrsize += sizeof(int *);
        }
        for (const std::u16string &mod : disable_ordered)
        {
            size += mod.length() * 2 + 2;
            ptrsize += sizeof(int *);
        }

        // malloc a chunk of memory for result
        void *result_base = malloc(size + ptrsize + sizeof(depresult));
        if (!result_base)
            return nullptr;

        depresult *result = (depresult *)result_base;
        char16_t **result_ptrbase = (char16_t **)((char *)result_base + sizeof(depresult));
        char16_t *result_strbase = (char16_t *)((char *)result_ptrbase + ptrsize);

        result->dcount = disable.size();
        result->ecount = enable.size();
        int offset = 0;
        int offsetptr = 0;

        for (const std::u16string &mod : enable)
        {
            int len = mod.length() + 1;
            result_ptrbase[offsetptr++] = result_strbase + offset;
            memcpy(&result_strbase[offset], mod.c_str(), len * 2);
            offset += len;
        }

        result->enable = (char16_t **)result_ptrbase;
        result->disable = (char16_t **)&result_ptrbase[offsetptr];

        for (const std::u16string &mod : disable_ordered)
        {
            int len = mod.length() + 1;
            result_ptrbase[offsetptr++] = result_strbase + offset;
            memcpy(&result_strbase[offset], mod.c_str(), len * 2);
            offset += len;
        }

        // malloc a char16_t*[][] and then pass it as a restrict mem block
        // data struct: char16_t*[][] main, char16_t*[]ptr, char16_t[] str
        // ptr is nullable, will point to nullptr if missing of that mod is empty
        // str struct: string separated by comma, and string[] separated by \0
        size = 0;
        ptrsize = disable.size() * sizeof(int *);

        for (std::pair<std::u16string, std::unordered_set<std::u16string>> pair : missing)
            for (std::u16string mod : pair.second)
                size += mod.length() * 2 + 2;

        char *reason_base = (char *)malloc((ptrsize + size + sizeof(int *)));
        char16_t **reason_ptrbase = (char16_t **)(reason_base + sizeof(int *));
        char16_t *reason_strbase = (char16_t *)(reason_base + sizeof(int *) + ptrsize);

        offsetptr = 0;
        offset = 0;
        for (const std::u16string &disabledmod : disable_ordered)
        {
            if (!missing.count(disabledmod))
            {
                result_ptrbase[offsetptr++] = nullptr;
                continue;
            }
            int count = 0;
            result_ptrbase[offsetptr++] = result_strbase + offset;
            const std::unordered_set<std::u16string> &mods = missing.at(disabledmod);
            for (const std::u16string &mod : mods)
            {
                int len = mod.length();
                count++;
                memcpy(&result_strbase[offset], mod.c_str(), len * 2);
                offset += len;
                result_strbase[offset++] = (count < missing[disabledmod].size()) ? u',' : u'\0';
            }
        }

        // cleanup / return
        if (reason)
            *reason = (char16_t **)reason_base;
        free(_toload);
        free(_loaded);
        return result;
    }
}