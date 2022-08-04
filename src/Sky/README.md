# Sky Demo

Showcase of Aardvark.Physics.Sky functionality: 
* Atmospheric Scattering Models
* Sun/Moon/Planet/Star Position Algorithms
* Physically based rendering
* Tone Mapping

## How to build

This demo requires the .NET Core 3.1 SDK. It manages Nuget packages using `paket` (https://fsprojects.github.io/Paket/) that needs to be installed using the .NET Core CLI. The packages need to be restored before the build or development in Visual Studio.

`dotnet new tool-manifest`

`dotnet tool install Paket`

`dotnet paket restore`

`dotnet build Sky.fsproj -c Release`

The `Sky.exe` is built to `..\..\bin\Release\net6.0\` and can be run from this directory.
