open System
open System.IO
open Aardvark.Application
open Aardvark.Application.Slim
open Aardvark.Base
open Aardvark.Base.Rendering
open Aardvark.Base.Incremental
open Aardvark.SceneGraph

[<EntryPoint>]
let main argv =
    // initialize runtime system
    Ag.initialize(); Aardvark.Init()

    // create simple render window
    use app = new OpenGlApplication()
    let win = app.CreateGameWindow(8)
    win.Title <- "Hello World (aardvark.docs)"

    // view, projection and default camera controllers
    let initialView = CameraView.lookAt (V3d(9.3, 9.9, 8.6)) V3d.Zero V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))
    
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
            |> Sg.group
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.simpleLighting |> toEffect
               ]
            |> Sg.viewTrafo (view |> Mod.map CameraView.viewTrafo)
            |> Sg.projTrafo (proj |> Mod.map Frustum.projTrafo)

    // specify render task
    let task =
        app.Runtime.CompileRender(win.FramebufferSignature, sg)

    // start
    win.RenderTask <- task
    win.Run()
    0
