open System
open System.Diagnostics
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
    win.Title <- "Gravity (aardvark.docs)"
    
    // view, projection and default camera controllers
    let initialView = CameraView.lookAt (V3d(50.0, 50.0, 50.0)) V3d.Zero V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let viewPos = view |> Mod.map (fun x -> x.Location)
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))

    // simulation
    let n = 64          // number of particles
    let r = Random()

    let positions = Mod.init (Array.create<V3f> n V3f.Zero)
    let positions' = Mod.init (Array.create<V3f> (n*2) V3f.Zero)
    let colors = Mod.init (Array.create<C4b> n C4b.Black)
    let colors' = Mod.init (Array.create<C4b> (n*2) C4b.Black)

    let simulation = async {
        let sw = Stopwatch()
        sw.Start()

        let g = 0.01f           // "gravitational constant"
        let mutable t = 0.0     // time
        let mutable ps = [| for i in 1..n do yield 35.0f * (V3f(r.NextDouble(), r.NextDouble(), r.NextDouble()) - V3f(0.5, 0.5, 0.5)) |]
        let mutable ps' = Array.create<V3f> (n*2) V3f.Zero
        let mutable cs = [| for i in 1..n do yield C4b(r.NextDouble(), r.NextDouble(), r.NextDouble()) |]
        let mutable cs' = cs |> Array.collect (fun c -> [| c; C4b.Black |])
    
        let vs = Array.create<V3f> n V3f.Zero                           // velocities
        let ms = [| for i in 1..n do yield float32(r.NextDouble()) |]   // masses

        do! Async.Sleep 5000
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
            ps' <- ps |> Array.mapi (fun i p -> [| p; p - 0.5f * vs.[i] |]) |> Array.concat

            transact (fun () ->
                Mod.change positions ps
                Mod.change positions' ps'
                Mod.change colors cs
                Mod.change colors' cs'
                )
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
            |> Sg.vertexAttribute DefaultSemantic.Colors colors
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.pointSprite |> toEffect
               ]
            |> Sg.uniform "PointSize" (Mod.constant 8.0);

    let lines =
        let drawCall = 
            DrawCallInfo(
                FaceVertexCount = n * 2,
                InstanceCount = 1
            )
        drawCall
            |> Sg.render IndexedGeometryMode.LineList 
            |> Sg.vertexAttribute DefaultSemantic.Positions positions'
            |> Sg.vertexAttribute DefaultSemantic.Colors colors'
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.thickLine |> toEffect
               ]
            |> Sg.uniform "LineWidth" (Mod.constant 3.0)

    let gridSize = 256.0
    let grid =
        let lines = [| for i in -gridSize..gridSize do
                        yield Line3d(V3d(i, -gridSize, 0.01), V3d(i, +gridSize, 0.01))
                        yield Line3d(V3d(-gridSize, i, 0.01), V3d(+gridSize, i, 0.01))
                    |]
        Sg.lines (Mod.constant C4b.Black) (Mod.constant lines)
        |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.constantColor C4f.Gray10 |> toEffect
                DefaultSurfaces.thickLine |> toEffect
               ]
        |> Sg.uniform "LineWidth" (Mod.constant 1.0)

    let aardvark = Aardvark.Docs.Utils.Geometry.aardvark C4b.White 3.0
        
    let transparentPlaneRenderPass = RenderPass.after "transparent" RenderPassOrder.Arbitrary RenderPass.main
    let transparentPlane =
        let drawCall = 
            DrawCallInfo(
                FaceVertexCount = 4,
                InstanceCount = 1
            )
        drawCall
            |> Sg.render IndexedGeometryMode.QuadList 
            |> Sg.vertexAttribute DefaultSemantic.Positions (Mod.constant [| V3f(-gridSize, -gridSize, 0.0); V3f(gridSize, -gridSize, 0.0); V3f(gridSize, gridSize, 0.0); V3f(-gridSize, gridSize, 0.0) |])
            |> Sg.vertexAttribute DefaultSemantic.Colors (Mod.constant (Array.create 4 (C4b(0, 0, 0, 192))))
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
               ]
            |> Sg.blendMode (Mod.constant BlendMode.Blend)
            |> Sg.pass (transparentPlaneRenderPass)

    

    
    let sg =
        [ 
          points
          lines
          aardvark
          grid
          transparentPlane
        ]
        |> Sg.group
        |> Sg.viewTrafo (view |> Mod.map CameraView.viewTrafo)
        |> Sg.projTrafo (proj |> Mod.map Frustum.projTrafo)

    // specify render task
    let task = 
        RenderTask.ofList [
            //app.Runtime.CompileClear(win.FramebufferSignature, Mod.constant C4f.White)
            app.Runtime.CompileRender(win.FramebufferSignature, sg)
        ]
        //|> DefaultOverlays.withStatistics

    // start
    win.RenderTask <- task
    simulation |> Async.Start
    win.Run()
    0
