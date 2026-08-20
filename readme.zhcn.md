# Genesis.ContentLoader
    用Genesis.Core编写而成的json加载器
    
## 用法:
    如何安装:
    第一步: 打开Release页面。
    第二步: 根据游戏版本选择要下载的版本。
    第三步: 点击要下载的文件并等待。
    第四步: 把下载好的文件(Genesis.ContentLoader.dll)拖入到插件/依赖库文件夹(默认是游戏根目录\Lib)。

## 前置:
    需安装:
        Genesis.Core(https://github.com/acxiynt/GenesisLoader)

## 编译：
    需下载:
        Nuget包管理器
        Doloc Town游戏本体
        Genesis.Core(https://github.com/acxiynt/GenesisLoader)
    1: 在项目根目录里创建一个叫"include"的文件夹。
    2: 把Assembly-csharp和firstpass(游戏本体里找)以及下载到的Genesis.Core文件放在include文件夹里。
    3: 在终端运行编译命令(Dotnet build -c [编译类型])。
    可选择的编译类型:
    debug: 调试专用，不推荐日常使用(生成的日志太大)。
    release: 日常使用，移除了大部分调试信息。


## 文档:
    自己看代码吧，凡是此插件提供的API都有功能注释。

## 额外功能:
    开发者模式

## 鸣谢:
    Harmony(https://github.com/pardeike/Harmony) - V4早期及以前的易用钩子。
    Monomod(https://github.com/MonoMod/MonoMod) - V4早期及以前是Harmony的前置，现为钩子的API源。
    Mono.Cecil(https://github.com/jbevain/cecil) - Monomod的前置。