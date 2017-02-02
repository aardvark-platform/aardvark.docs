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
    win.Text <- "PointSprites (aardvark.docs)"
    
    // view, projection and default camera controllers
    let initialView = CameraView.lookAt (V3d(50.0, 50.0, 50.0)) V3d.Zero V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))

    // generate points
    let points =

        let n = 50

        let r = Random()
        let mutable ps = [| for i in 1..n do yield 25.0f * (V3f(r.NextDouble(), r.NextDouble(), r.NextDouble()) - V3f(0.5, 0.5, 0.5)) |]
        let vs = [| for i in 1..n do yield V3f(r.NextDouble() - 0.5, r.NextDouble() - 0.5, r.NextDouble() - 0.5) |]
        let cs = [| for i in 1..n do yield C4b(r.NextDouble(), r.NextDouble(), r.NextDouble()) |]
        let ms = [| for i in 1..n do yield float32(r.NextDouble()) |]

        let drawCall = 
            DrawCallInfo(
                FaceVertexCount = ps.Length,
                InstanceCount = 1
            )

        let positions = Mod.init ps

        async {
            let g = 0.2f
            let sw = Stopwatch()
            sw.Start()
            let mutable t = 0.0f

            while true do
                //do! Async.Sleep 10
                ps <- ps |> Array.mapi (fun i p ->
                    let mutable f = V3f.Zero
                    for j in 1..ps.Length-1 do
                        if i <> j then
                            let d = ps.[j] - ps.[i]
                            f <- f + g * d * ms.[i] * ms.[j] / d.LengthSquared
                    vs.[i] <- vs.[i] + f

                    let dt = float32 sw.Elapsed.TotalSeconds - t
                    t <- t + dt
                    ps.[i] + vs.[i] * dt
                )
                transact (fun () -> Mod.change positions ps)
        }
        |> Async.Start

        drawCall
            |> Sg.render IndexedGeometryMode.PointList 
            |> Sg.vertexAttribute DefaultSemantic.Positions positions
            |> Sg.vertexAttribute DefaultSemantic.Colors (Mod.constant cs)

    // define scene
    let sg =
        points
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.pointSprite |> toEffect
               ]
            |> Sg.uniform "PointSize" (Mod.constant 10.0)
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
