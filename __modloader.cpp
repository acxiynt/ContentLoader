#include "__cutil.hpp"
#include <unordered_map>
#include <string>
extern "C"
{
    EXPORT __restrict depresult* solvedep( char16_t** __restrict const _loaded, mod_kvp* __restrict const _toload, int loadedlen, int toloadlen)
    {
        std::unordered_map<std::u16string, std::unordered_set<std::u16string>> toload = std::unordered_map<std::u16string, std::unordered_set<std::u16string>>();
        std::unordered_map<std::u16string, std::unordered_set<std::u16string>> alldep = std::unordered_map<std::u16string, std::unordered_set<std::u16string>>();
        std::unordered_set<std::u16string> dep = std::unordered_set<std::u16string>();
        std::unordered_set<std::u16string> enable = std::unordered_set<std::u16string>();
        std::unordered_set<std::u16string> disable = std::unordered_set<std::u16string>();
        std::unordered_set<std::u16string> loaded = std::unordered_set<std::u16string>();
        for(int a = 0; a < loadedlen; a++)
            loaded.insert(std::u16string(_loaded[a]));
        bool finished = false;
        depresult* result = (depresult*)malloc(sizeof(depresult));
        if(!result)
            return nullptr;
        //serializes raw pointer of mods to load into alldep and toload
        for(int a = 0; a < toloadlen; a++)
        {
            std::u16string _mod = std::u16string(_toload[a].id);
            for(int b = 0; b < toloadlen; b++)
            {
                std::u16string _dep = std::u16string(_toload[a].dep[b]);
                if(loaded.count(_dep))
                    continue;
                dep.insert(_dep);
                if(!alldep.count(_dep))
                    alldep.insert(std::pair(_dep, std::unordered_set<std::u16string>()));
                alldep[_dep].insert(_mod);
            }
            toload.insert(std::pair(std::u16string(_mod), dep));
            dep.clear();
        }

        //removes all dependency requiring loaded mod.
        for(std::u16string loadedmod : loaded)
        {
            if(alldep.count(loadedmod))
                alldep.erase(loadedmod);
        }

        //remove mods that have dependency on non-existent mod.
        for(std::pair<std::u16string, std::unordered_set<std::u16string>> pair : toload)
        {
            if(pair.second.empty())
            {
                enable.insert(pair.first);
                alldep.erase(pair.first);
            }
            for(std::u16string mod : pair.second)
            
        }
    }
}