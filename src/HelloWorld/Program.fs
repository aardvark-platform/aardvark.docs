open Aardvark.Application
open Aardvark.Base
open Aardvark.Rendering
open Aardvark.SceneGraph
open FSharp.Data.Adaptive

[<EntryPoint>]
let main argv =
    // initialize runtime system
    Aardvark.Init()

    // generate 11 x 11 x 11 colored boxes
    let norm x = (x + 5.0) * 0.1
    let boxes = seq {
        for x in -5.0..5.0 do
            for y in -5.0..5.0 do
                for z in -5.0..5.0 do
                    let bounds = Box3d.FromCenterAndSize(V3d(x, y, z), V3d(0.5, 0.5, 0.5))
                    let color = C4b(norm x, norm y, norm z)
                    yield Sg.box' color bounds
    }

    // define scene
    let sg =
        boxes
            |> Sg.ofSeq
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
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
