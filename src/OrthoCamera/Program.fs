
open System
open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.Base.Rendering
open Aardvark.Application
open Aardvark.Application.WinForms
open Aardvark.Rendering.NanoVg
open Aardvark.SceneGraph

[<EntryPoint>]
let main argv =

    // initialize runtime system
    Ag.initialize();
    Aardvark.Init()

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
            controlZoom renderControl frustum
        ]
        
    let ortho = win.Sizes |> Mod.map (fun s ->
        let a = float s.X / float s.Y;
        let frustum = Box3d(V3d(bounds.Min.X * a, bounds.Min.Y, 0.1), V3d(bounds.Max.X * a, bounds.Max.Y, 2.0))
        Frustum.ortho frustum
        )
    let initialView = CameraView.lookAt V3d.OOI V3d.OOO V3d.OIO   
    let view = initialView |> control win ortho
    
    
    

    // define scene
    let backgroundColor = Mod.init C4f.White
    
    let sg =
        [
            Aardvark.Docs.Utils.Geometry.grid bounds C4b.Black
            Aardvark.Docs.Utils.Geometry.points 10000 8 bounds
        ]
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
