open Aardvark.Application
open Aardvark.Base
open Aardvark.Rendering
open Aardvark.SceneGraph
open Aardvark.SceneGraph.IO

[<EntryPoint>]
let main argv =
    // initialize runtime system
    Aardvark.Init()

    let model =
        Loader.Assimp.load (Path.combine [__SOURCE_DIRECTORY__; ".."; ".."; "data"; "aardvark"; "Aardvark.obj"])
        |> Sg.adapter
        |> Sg.transform (Trafo3d.Scale(1.0,1.0,-1.0))

    let scene =
        [
            for x in -5 .. 5 do
                for y in -5 .. 5 do
                    for z in -5 .. 5 do
                        yield
                            model |> Sg.translate (float x) (float y) (float z)
        ] |> Sg.ofSeq

    let sg =
        scene
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.diffuseTexture |> toEffect
                DefaultSurfaces.simpleLighting |> toEffect
            ]

    // start
    let initialView = CameraView.lookAt (V3d(9.3, 9.9, 8.6)) V3d.Zero V3d.OOI

    show {
        display Display.Mono
        samples 8
        backend Backend.GL
        debug false
        scene sg
        initialCamera initialView
    }

    0
