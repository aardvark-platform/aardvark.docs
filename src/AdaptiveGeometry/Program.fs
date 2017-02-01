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

    // initialize runtime system
    Ag.initialize(); Aardvark.Init()

    // simple OpenGL window
    use app = new OpenGlApplication()
    let win = app.CreateSimpleRenderWindow()
    win.Text <- "AdaptiveGeometry (aardvark.docs)"
    
    // view, projection and default camera controllers
    let initialView = CameraView.lookAt (V3d(16.0, 11.0, 6.0)) V3d.Zero V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))

    // generate adaptive mesh
    let mesh =
        let drawCall = 
            DrawCallInfo(
                FaceVertexCount = 2 * 8,
                InstanceCount = 1
            )

        let positions = Mod.init [| V3f(0,0,0); V3f(-1,-1,0); V3f(1,-1,0); V3f(1,1,0); V3f(-1,1,0) |]
        let normals =   Mod.init [| V3f.OOI; V3f.OOI; V3f.OOI; V3f.OOI; V3f.OOI |]
        let colors =    Mod.init [| C4b.White; C4b.Red; C4b.Green; C4b.Blue; C4b.Yellow |]
        //let indices =   Mod.constant [| 0;1;2; 0;2;3; 0;3;4; 0;4;1 |]
        let indices =   Mod.constant [| 1;2; 2;3; 3;4; 4;1; 0;1; 0;2; 0;3; 0;4; |]

        async {
            let mutable z = 0.0
            while true do
                do! Async.Sleep 10
                z <- z + 0.001
                transact (fun () ->
                    Mod.change positions [| V3f(0.0,0.0,z); V3f(-1,-1,0); V3f(1,-1,0); V3f(1,1,0); V3f(-1,1,0) |]
                )
        }
        |> Async.Start

        drawCall
            |> Sg.render IndexedGeometryMode.LineList 
            |> Sg.vertexAttribute DefaultSemantic.Positions positions
            |> Sg.vertexAttribute DefaultSemantic.Normals normals
            |> Sg.vertexAttribute DefaultSemantic.Colors colors
            |> Sg.index indices

    // define scene
    let sg =
        mesh
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
            |> DefaultOverlays.withStatistics

    // start
    win.RenderTask <- task
    win.Run()
    0
