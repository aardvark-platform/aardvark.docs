#load @"paket-files/build/vrvis/Aardvark.Fake/DefaultSetup.fsx"

open Fake
open System
open System.IO
open System.Diagnostics
open Aardvark.Fake

do Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

DefaultSetup.install ["src/aardvark.docs.sln"]

Target "Run_HelloWorld" (fun() ->
    tracefn "exec: %d" (Shell.Exec "bin/Release/HelloWorld.exe")
)
"Default" ==> "Run_HelloWorld"

Target "Run_AdaptiveWorld" (fun() ->
    tracefn "exec: %d" (Shell.Exec "bin/Release/AdaptiveWorld.exe")
)
"Default" ==> "Run_AdaptiveWorld"

Target "Run_Gravity" (fun() ->
    tracefn "exec: %d" (Shell.Exec "bin/Release/Gravity.exe")
)
"Default" ==> "Run_Gravity"

entry()