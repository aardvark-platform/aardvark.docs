open System
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

    // simple OpenGL window
    use app = new OpenGlApplication()
    let win = app.CreateGameWindow(8)
    win.Title <- "AdaptiveWorld (aardvark.docs)"

    // view, projection and default camera controllers
    let initialView = CameraView.lookAt (V3d(16.0, 11.0, 6.0)) V3d.Zero V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))

    // generate adaptive boxes
    let norm x = (x + 10.0) * 0.1
    let color x y = C4b(norm x, norm y, 0.2)
    let bounds x y = Box3d.FromCenterAndSize(V3d(x, y, 0.25), V3d(0.7, 0.7, 0.5))
    
    let poi = view |> Mod.map (fun v -> v.Location + v.Forward * 5.0)
    let transform x y = poi |> Mod.map (fun p ->
            let d = (V2d(float x, y) - p.XY).Length
            Trafo3d.Translation(-x, -y, 0.0) * Trafo3d.Scale(1.0, 1.0, d * 0.1) * Trafo3d.RotationZ(d * 0.25) * Trafo3d.Translation(x, y, 0.0)
            )

    let boxes = seq {
        for x in -10.0..10.0 do
            for y in -10.0..10.0 do
                yield Sg.box' (color x y) (bounds x y) |> Sg.trafo (transform x y)
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
