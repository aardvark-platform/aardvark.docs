open System
open System.Diagnostics
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
    win.Text <- "Gravity (aardvark.docs)"
    
    // view, projection and default camera controllers
    let initialView = CameraView.lookAt (V3d(50.0, 50.0, 50.0)) V3d.Zero V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))

    // simulation
    let n = 50          // number of particles
    let r = Random()

    
    let cs = [| for i in 1..n do yield C4b(r.NextDouble(), r.NextDouble(), r.NextDouble()) |]
    

    let positions = Mod.init (Array.create<V3f> n V3f.Zero)

    let simulation = async {
        let sw = Stopwatch()
        sw.Start()

        let g = 0.01f           // "gravitational constant"
        let mutable t = 0.0     // time
        let mutable ps = [| for i in 1..n do yield 25.0f * (V3f(r.NextDouble(), r.NextDouble(), r.NextDouble()) - V3f(0.5, 0.5, 0.5)) |]
        let vs = Array.create<V3f> n V3f.Zero                           // velocities
        let ms = [| for i in 1..n do yield float32(r.NextDouble()) |]   // masses

        while true do
            for i in 0..ps.Length-1 do
                let p = ps.[i]
                for j in i+1..ps.Length-1 do
                    let v = ps.[j] - p
                    let f = g / v.LengthSquared
                    vs.[i] <- vs.[i] + v * ms.[j] * f
                    vs.[j] <- vs.[j] - v * ms.[i] * f
                
            let dt = sw.Elapsed.TotalSeconds - t
            t <- sw.Elapsed.TotalSeconds

            ps <- ps |> Array.mapi (fun i p -> p + vs.[i] * float32(dt))
            transact (fun () -> Mod.change positions ps)
    }
    
    // define scene
    let points =
        let drawCall = 
            DrawCallInfo(
                FaceVertexCount = n,
                InstanceCount = 1
            )
        drawCall
            |> Sg.render IndexedGeometryMode.PointList 
            |> Sg.vertexAttribute DefaultSemantic.Positions positions
            |> Sg.vertexAttribute DefaultSemantic.Colors (Mod.constant cs)

    let sg =
        points
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.pointSprite |> toEffect
               ]
            |> Sg.uniform "PointSize" (Mod.constant 8.0)
            |> Sg.viewTrafo (view |> Mod.map CameraView.viewTrafo)
            |> Sg.projTrafo (proj |> Mod.map Frustum.projTrafo)

    // specify render task
    let task =
        app.Runtime.CompileRender(win.FramebufferSignature, sg)
            |> DefaultOverlays.withStatistics

    // start
    win.RenderTask <- task
    simulation |> Async.Start
    win.Run()
    0
