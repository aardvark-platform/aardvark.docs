
open System
open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.Base.Rendering
open Aardvark.Application
open Aardvark.Application.WinForms
open Aardvark.Rendering.NanoVg
open Aardvark.SceneGraph
open Aardvark.SceneGraph.SgFSharp.Sg

[<EntryPoint>]
let main argv =

    // initialize runtime system
    Ag.initialize();
    Aardvark.Init()

    // create simple render window
    use app = new OpenGlApplication()
    let win = app.CreateSimpleRenderWindow()
    win.Text <- "Ortho Camera (aardvark.docs)"

    
    let initialView = CameraView.lookAt V3d.OOI V3d.OOO V3d.OIO
    let initialBounds =
        let a = float win.Size.X / float win.Size.Y
        Box3d(V3d(-10.0 * a, -10.0, 0.1), V3d(10.0 * a, 10.0, 2.0))

    let aspectRatio = win.Sizes |> Mod.map (fun size -> float size.X / float size.Y)
    let bounds = aspectRatio |> Mod.map (fun a -> Box3d(V3d(-10.0 * a, -10.0, 0.1), V3d(10.0 * a, 10.0, 2.0)))

    let controlPan (renderControl : IRenderControl) (frustum : IMod<Frustum>) : IMod<AdaptiveFunc<CameraView>> =
        let down = renderControl.Mouse.IsDown(MouseButtons.Middle)
        let location = renderControl.Mouse.Position |> Mod.map (fun pp -> pp.Position)

        adaptive {
            let! targetSizeInPixels = renderControl.Sizes
            let! d = down
            let! frustum = frustum

            if d then
                return location |> Mod.step (fun _ delta (cam : CameraView) ->
                    let dx = ((frustum.right - frustum.left) / float targetSizeInPixels.X) * float delta.X
                    let dy = -((frustum.top - frustum.bottom) / float targetSizeInPixels.Y) * float delta.Y
                    let d = cam.Right * dx + cam.Up * dy
                    cam.WithLocation(cam.Location - d)
                )
            else
                return AdaptiveFunc.Identity
        }

    let controlZoom (renderControl : IRenderControl) (bounds : Box3d) : IMod<AdaptiveFunc<Box3d>> =
        let down = renderControl.Mouse.IsDown(MouseButtons.Right)
        let location = renderControl.Mouse.Position |> Mod.map (fun pp -> pp.Position)

        adaptive {
            let! d = down
            if d then
                let! winSizeInPixels = renderControl.Sizes
                let winSize = V2d(winSizeInPixels)
                return location |> Mod.step (fun p d (bb : Box3d) ->
                    let bb = bounds.XY
                    let p = bb.Min + V2d(p) / winSize
                    let d = bb.Size / winSize * V2d(d)
                    let scale = 1.0 + d.Y
                    printfn "p    : %A" p
                    printfn "scale: %f" scale
                    let min = (bb.Min - p) * scale + p
                    let max = (bb.Max - p) * scale + p
                    let result = Box3d(V3d(min, bounds.Min.Z), V3d(max, bounds.Max.Z))
                    printfn "bounds <= %A" bounds
                    printfn "bounds => %A" result
                    result
                )
            else
                return AdaptiveFunc.Identity
        }

    let control (renderControl : IRenderControl) (frustum : IMod<Frustum>) (cam : CameraView) : IMod<CameraView> =
        Mod.integrate cam renderControl.Time [
            controlPan renderControl frustum
        ]

    let control2 (renderControl : IRenderControl) (bounds : Box3d) : IMod<Box3d> =
        Mod.integrate bounds renderControl.Time [
            controlZoom renderControl bounds
        ]
        
    let ortho = initialBounds |> control2 win |> Mod.map Frustum.ortho   
    let view = initialView |> control win ortho
    
    
    

    // define scene
    let backgroundColor = Mod.init C4f.White

    let bounds2d = bounds |> Mod.map (fun b -> b.XY)
    let grid = bounds2d |> Mod.map (fun bb -> Aardvark.Docs.Utils.Geometry.grid bb C4b.Black) |> Sg.dynamic
    let points = bounds2d |> Mod.map (fun bb -> Aardvark.Docs.Utils.Geometry.points 10000 8 bb) |> Sg.dynamic
    
    let sg =
        [ grid; points ]
        |> Sg.ofSeq
        |> Sg.viewTrafo (view |> Mod.map CameraView.viewTrafo)
        |> Sg.projTrafo (ortho |> Mod.map Frustum.orthoTrafo)

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
