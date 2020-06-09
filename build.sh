#!/bin/bash

if [ ! -f .paket/paket ]; then
    dotnet tool install Paket --tool-path .paket
fi

./.paket/paket restore 

dotnet build Aardvark.Docs.sln