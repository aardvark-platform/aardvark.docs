open System
open Aardvark.Base
open Aardvark.Base.Rendering
open Aardvark.Base.Incremental
open Aardvark.Rendering.NanoVg
open Aardvark.SceneGraph
open Aardvark.Application
open Aardvark.Application.WinForms

[<EntryPoint>]
let main argv = 
    Ag.initialize(); Aardvark.Init()

    use app = new OpenGlApplication()
    let win = app.CreateSimpleRenderWindow()
    win.Text <- "Aardvark Docs - 01_HelloWorld"

    let n = 5.0
    let norm x = (x + n) / (2.0 * n)
    let boxes = seq {
        for x in -n..n do
            for y in -n..n do
                for z in -n..n do
                    let bounds = Box3d.FromCenterAndSize(V3d(x, y, z), V3d(0.5, 0.5, 0.5))
                    let color = C4b(norm x, norm y, norm z)
                    yield Sg.box' color bounds
    }

    let initialView = CameraView.lookAt (V3d(n+2.0,-n-3.0,-n)) (V3d(n-1.0,-n,-n+1.5)) V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))

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

    let task =
        app.Runtime.CompileRender(win.FramebufferSignature, sg)
            |> DefaultOverlays.withStatistics

    win.RenderTask <- task
    win.Run()
    0
