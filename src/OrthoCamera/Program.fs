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

    // create simple render window
    use app = new OpenGlApplication()
    let win = app.CreateSimpleRenderWindow()
    win.Text <- "Ortho Camera (aardvark.docs)"

    let bounds = Box2d(V2d(-10.0, -10.0), V2d(+10.0, +10.0))

    let controlPan (renderControl : IRenderControl) (frustum : IMod<Frustum>)=
        let down = renderControl.Mouse.IsDown(MouseButtons.Middle)
        let location = renderControl.Mouse.Position |> Mod.map (fun pp -> pp.Position)

        adaptive {
            let! targetSizeInPixels = renderControl.Sizes
            let! d = down
            let! frustum = frustum

            if d then
                return location |> Mod.step (fun p delta (cam : CameraView) ->
                    let dx = ((frustum.right - frustum.left) / float targetSizeInPixels.X) * float delta.X
                    let dy = -((frustum.top - frustum.bottom) / float targetSizeInPixels.Y) * float delta.Y
                    let d = cam.Right * dx + cam.Up * dy
                    cam.WithLocation(cam.Location - d)
                )
            else
                return AdaptiveFunc.Identity
        }
    let controlZoom (renderControl : IRenderControl) (frustum : IMod<Frustum>)=
        let down = renderControl.Mouse.IsDown(MouseButtons.Right)
        let location = renderControl.Mouse.Position |> Mod.map (fun pp -> pp.Position)

        adaptive {
            let! targetSizeInPixels = renderControl.Sizes
            let! d = down
            let! frustum = frustum

            if d then
                return location |> Mod.step (fun p delta (cam : CameraView) ->
                    let dx = ((frustum.right - frustum.left) / float targetSizeInPixels.X) * float delta.X
                    let dy = -((frustum.top - frustum.bottom) / float targetSizeInPixels.Y) * float delta.Y
                    let d = cam.Right * dx + cam.Up * dy
                    cam.WithLocation(cam.Location - d)
                )
            else
                return AdaptiveFunc.Identity
        } 
    let control (renderControl : IRenderControl) (frustum : IMod<Frustum>) (cam : CameraView) : IMod<CameraView> =
        Mod.integrate cam renderControl.Time [
            controlPan renderControl frustum
        ]
        
    let ortho = win.Sizes |> Mod.map (fun s ->
        let a = float s.X / float s.Y;
        let frustum = Box3d(V3d(bounds.Min, 0.1), V3d(bounds.Max, 2.0))
        Frustum.ortho frustum
        )
    let initialView = CameraView.lookAt V3d.OOI V3d.OOO V3d.OIO   
    let view = initialView |> control win ortho
    
    
    

    // define scene
    let backgroundColor = Mod.init C4f.White

    let grid (bounds : Box2d) color =
        let lines =
            [
                [| for x in bounds.Min.X..0.5..bounds.Max.X do yield Line3d(V3d(x, bounds.Min.Y, 0.0), V3d(x, bounds.Max.Y, 0.0)) |]
                [| for y in bounds.Min.Y..0.5..bounds.Max.Y do yield Line3d(V3d(bounds.Min.X, y, 0.0), V3d(bounds.Max.X, y, 0.0)) |]
            ]
            |> Array.concat
        Sg.lines (Mod.constant C4b.Black) (Mod.constant lines)
        |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.constantColor C4f.Gray50 |> toEffect
                DefaultSurfaces.thickLine |> toEffect
               ]
        |> Sg.uniform "LineWidth" (Mod.constant 1.0)
        
    
    let r = Random()
    let points n pointsize (bounds : Box2d) =
        let positions = Mod.constant [| for x in 1..n do yield bounds.Min.XYO + bounds.Size.XYO * V3d(r.NextDouble(), r.NextDouble(), 0.0) |]
        let colors = Mod.constant [| for x in 1..n do yield C4b(r.Next(256), r.Next(256), r.Next(256)) |]
        DrawCallInfo(FaceVertexCount = n, InstanceCount = 1)
            |> Sg.render IndexedGeometryMode.PointList 
            |> Sg.vertexAttribute DefaultSemantic.Positions positions
            |> Sg.vertexAttribute DefaultSemantic.Colors colors
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.pointSprite |> toEffect
               ]
            |> Sg.uniform "PointSize" (Mod.constant pointsize)

    
    //let initialView = CameraView.lookAt (V3d(20.0, -10.0, 15.0)) V3d.Zero V3d.OOI
    //let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    //let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))

    let sg =
        [
            grid bounds C4b.Black
            points 1000 5 bounds
        ]
        |> Sg.ofSeq
        |> Sg.viewTrafo (view |> Mod.map CameraView.viewTrafo)
        |> Sg.projTrafo (ortho |> Mod.map Frustum.orthoTrafo)
        //|> Sg.projTrafo (proj |> Mod.map Frustum.projTrafo)

    // specify render task(s)
    let task =
        [
            app.Runtime.CompileClear(win.FramebufferSignature, backgroundColor)
            app.Runtime.CompileRender(win.FramebufferSignature, sg)
        ]
        |> RenderTask.ofList
        |> DefaultOverlays.withStatistics

    // start
    win.RenderTask <- task
    win.Run()
    0
