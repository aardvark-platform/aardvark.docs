#load @"paket-files/build/vrvis/Aardvark.Fake/DefaultSetup.fsx"

open Fake
open System
open System.IO
open System.Diagnostics
open Aardvark.Fake

do Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

DefaultSetup.install ["src/aardvark.docs.sln"]

Target "Run_01_HelloWorld" (fun() ->
    tracefn "exec: %d" (Shell.Exec "bin/Release/01_HelloWorld.exe")
)

"Default" ==> "Run_01_HelloWorld"

entry()