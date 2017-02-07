open System
open System.Diagnostics

open Aardvark.Base
open Aardvark.Base.Rendering
open Aardvark.Base.Incremental
open Aardvark.Docs.SierpinksiTetrahedron
open Aardvark.Rendering.NanoVg
open Aardvark.SceneGraph
open Aardvark.Application
open Aardvark.Application.WinForms

[<EntryPoint>]
let main argv = 

    // initialize runtime system
    Ag.initialize(); Aardvark.Init()

    // simple OpenGL window
    use app = new OpenGlApplication()
    let win = app.CreateSimpleRenderWindow()
    win.Text <- "SierpinskiTetrahedron (aardvark.docs)"
  
    // define scene
    let h = 0.5 * sqrt 3.0  // height of triangle
    let initialView = CameraView.lookAt (V3d(0.6, -1.0, 0.7)) (V3d(0.5, h / 3.0, h / 3.0)) V3d.ZAxis
    //let initialView = CameraView.lookAt (V3d(0.5, h / 3.0, 2.0)) (V3d(0.5, h / 3.0, 0.0)) V3d.OIO
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))

    let ft = FoldingTetrahedron ()

    let sg =
        ft.GetSg()
            |> Sg.effect 
            [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.simpleLighting |> toEffect
            ]
            |> Sg.viewTrafo (view |> Mod.map CameraView.viewTrafo)
            |> Sg.projTrafo (proj |> Mod.map Frustum.projTrafo)

    // specify render task
    let task = 
        RenderTask.ofList 
            [
                app.Runtime.CompileClear(win.FramebufferSignature, Mod.constant C4f.White)
                app.Runtime.CompileRender(win.FramebufferSignature, sg)
            ]
        |> DefaultOverlays.withStatistics

    // start
    win.RenderTask <- task
    ft.Animation () |> Async.Start
    win.Run()
    0
