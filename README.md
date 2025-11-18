[![Discord](https://img.shields.io/discord/611129394764840960?label=Discord)](https://discord.gg/UyecnhM)
[![License](https://img.shields.io/github/license/aardvark-platform/aardvark.docs.svg?label=License)](https://github.com/aardvark-platform/aardvark.docs/blob/master/LICENSE)

[The Aardvark Platform](https://aardvarkians.com/) |
[Gallery](https://github.com/aardvark-platform/aardvark.docs/wiki/Gallery) | 
[Packages & Repositories](https://github.com/aardvark-platform/aardvark.docs/wiki/Packages-and-Repositories)

Aardvark.Docs is part of [The Aardvark Platform](https://github.com/aardvark-platform/aardvark.docs/wiki) for visual computing, real-time graphics and visualization. The examples in this repository combine multiple packages from different repositories. You can find the other Aardvark Platform repositories in the [Gallery](https://github.com/aardvark-platform/aardvark.docs/wiki/Gallery) and [Packages & Repositories](https://github.com/aardvark-platform/aardvark.docs/wiki/Packages-and-Repositories). For more information, please refer to the [aardvark.docs wiki](https://github.com/aardvark-platform/aardvark.docs/wiki).

# Build and Run

0. Tools
  * git
  * [.NET Core SDK](https://dotnet.microsoft.com/download)
  * (optional) [Visual Studio Code](https://code.visualstudio.com/Download)
    * extensions: Ionide-fsharp, Ionide-paket

1. Clone
  ```shell
  $ git clone https://github.com/aardvarkplatform/aardvark.docs.git
  ```
  
2. Build
  ```shell
  $ cd aardvark.docs
  $ ./build.sh
  ```
  
3. Run
  ```shell
  $ dotnet run -c Release -p ./src/HelloWorld/HelloWorld.fsproj
  ```

4. Look at the source code
  ```shell
  $ code . src/HelloWorld/Program.fs
  ```

