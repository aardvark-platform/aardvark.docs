#r "paket: groupref Build //"
#load ".fake/build.fsx/intellisense.fsx"
#load @"paket-files/build/aardvark-platform/aardvark.fake/DefaultSetup.fsx"

open System
open System.IO
open Fake.Core
open Fake.DotNet
open Aardvark.Fake

do Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

DefaultSetup.install ["src/aardvark.docs.sln"]

#if DEBUG
do System.Diagnostics.Debugger.Launch() |> ignore
#endif

entry()
