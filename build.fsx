#load @"paket-files/build/aardvark-platform/aardvark.fake/DefaultSetup.fsx"

open Fake
open System
open System.IO
open System.Diagnostics
open Aardvark.Fake
open Fake.Testing

do MSBuildDefaults <- { MSBuildDefaults with Verbosity = Some Minimal }
do Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

DefaultSetup.install ["src/aardvark.docs.sln"]

Target "Run_HelloWorld" (fun() ->
    tracefn "exec: %d" (Shell.Exec "bin/Release/HelloWorld.exe")
)
"Default" ==> "Run_HelloWorld"

Target "Run_BackgroundColor" (fun() ->
    tracefn "exec: %d" (Shell.Exec "bin/Release/BackgroundColor.exe")
)
"Default" ==> "Run_BackgroundColor"

Target "Run_AdaptiveWorld" (fun() ->
    tracefn "exec: %d" (Shell.Exec "bin/Release/AdaptiveWorld.exe")
)
"Default" ==> "Run_AdaptiveWorld"

Target "Run_Gravity" (fun() ->
    tracefn "exec: %d" (Shell.Exec "bin/Release/Gravity.exe")
)
"Default" ==> "Run_Gravity"

Target "Run_OrthoCamera" (fun() ->
    tracefn "exec: %d" (Shell.Exec "bin/Release/OrthoCamera.exe")
)
"Default" ==> "Run_OrthoCamera"

Target "Run_SierpinskiTetrahedron" (fun() ->
    tracefn "exec: %d" (Shell.Exec "bin/Release/SierpinskiTetrahedron.exe")
)
"Default" ==> "Run_SierpinskiTetrahedron"

entry()
