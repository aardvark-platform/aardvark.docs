
[![Join the chat at https://gitter.im/aardvark-platform/Lobby](https://img.shields.io/badge/gitter-join%20chat-blue.svg)](https://gitter.im/aardvark-platform/Lobby)
[![license](https://img.shields.io/github/license/aardvark-platform/aardvark.docs.svg)](https://github.com/aardvark-platform/aardvark.docs/blob/master/LICENSE)

[The Aardvark Platform](https://aardvarkians.com/) |
[Platform Wiki](https://github.com/aardvarkplatform/aardvark.docs/wiki) | 
[Gallery](https://github.com/aardvarkplatform/aardvark.docs/wiki/Gallery) | 
[Quickstart](https://github.com/aardvarkplatform/aardvark.docs/wiki/Quickstart-Windows) | 
[Status](https://github.com/aardvarkplatform/aardvark.docs/wiki/Status)

Aardvark.Docs is part of the open-source [Aardvark platform](https://github.com/aardvark-platform/aardvark.docs/wiki) for visual computing, real-time graphics and visualization.

Each platform repository contains self-contained standalone examples (e.g. [rendering examples](https://github.com/aardvark-platform/aardvark.rendering/tree/master/src/Examples%20(netcore))). The examples presented here combine multiple packages.
A more technical platform walkthrough can be found [here](https://github.com/aardvark-platform/walkthrough).

Build
-----

Install [.NET Core SDK][dotnet-core-sdk] for your platform. 
run ``build.cmd or build.sh`` to install all dependencies.
Then run:

```console
$ dotnet build Ardvark.Docs.sln
```

Run
---

Requires [.NET Core Runtime][dotnet-core-runtime] version 3.1+, e.g. to run [Hello World][hello-world] example enter:

```console
$ dotnet run -c Release -p .\src\HelloWorld\HelloWorld.fsproj
```

[dotnet-core-runtime]: https://www.microsoft.com/net/download/core#/runtime
[dotnet-core-sdk]: https://www.microsoft.com/net/download/core
[hello-world]: https://github.com/aardvark-platform/aardvark.docs/wiki/Hello-World-Tutorial
