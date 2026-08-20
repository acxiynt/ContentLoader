# Genesis.ContentLoader
    json loader made for Genesis.Core modloader

## How to use:
    how to install:
    1: Open release page.
    2: Choose what version to install by your current game version.
    3: Click the wanted file and wait.
    4: put the downloaded file(Genesis.ContentLoader.dll)into the plugin/lib folder(default: game root folder\Lib).

## How to compile：
    Install/download:
        Nuget package manager
        Doloc Town base game
        Genesis.Core(https://github.com/acxiynt/GenesisLoader)
    1: Create a folder called "include" in the project's root directory.
    2: Put Assembly-csharp, firstpass(found in Data/Managed of doloc town), and Genesis.Core into include.
    3: Run compile command(Dotnet build -c [type]).
    available types:
    debug: debug only, very messy for normal playthrough.
    release: recommended for normal playthrough, removed most debug infos.

## Prerequisite:
    Please install:
        Genesis.Core(https://github.com/acxiynt/GenesisLoader)

## Documentation:
    Soon™

## Extra features:
    Dev mode

## Credits:
    Harmony(https://github.com/pardeike/Harmony) - Hook API for early V4 and versions before that。
    Monomod(https://github.com/MonoMod/MonoMod) - Dependency for harmony, now its the api source for hooks。
    Mono.Cecil(https://github.com/jbevain/cecil) - Prerequisite for Monomod。