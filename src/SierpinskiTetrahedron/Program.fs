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
    win.Text <- "SierpinskiTetrahedron (aardvark.docs)"
    
    // view, projection and default camera controllers
    let initialView = CameraView.lookAt (V3d(3.0, 2.0, 1.0)) (V3d(0.5, 0.5, 0.0)) V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))

    // define scene
    let rotPointAxis (p : V3d) (axis : V3d) (angle : float) = M44d.Translation(p) * M44d.Rotation(axis, angle) * M44d.Translation(-p)
    
    let h = 0.5 * sqrt 3.0
    let v0 = V3d.Zero
    let v1 = V3d(1, 0, 0)
    let v2 = V3d(0.5, h, 0.0)
    let v3 = V3d(0.25, 0.5 * h, 0.0)
    let v4 = V3d(0.5, 0.0, 0.0)
    let v5 = V3d(0.75, 0.5 * h, 0.0)

    let fold0 = 
        printfn "asdfasdfkj: %A %A"  ((v3+v4)*0.5) ((v3-v4).Normalized)
        rotPointAxis ((v3+v4)*0.5) ((v3-v4).Normalized)
    let fold1 = rotPointAxis v4 ((v4-v5).Normalized)
    let fold2 = rotPointAxis v5 ((v5-v3).Normalized)

    let ps =  [| v3;v5;v4; v0;v3;v4; v1;v4;v5; v2;v5;v3 |]
    let ns = Array.create<V3f> 12 V3f.OON
    let cs = 
        [| 
            C4b.White;C4b.White;C4b.White; 
            C4b.Red;C4b.Red;C4b.Red; 
            C4b.Green;C4b.Green;C4b.Green; 
            C4b.Blue;C4b.Blue;C4b.Blue 
        |]

    let positions = Mod.init ps

    let update angle =
        let v0' = (fold0 angle).TransformPos v0
        let v1' = (fold1 angle).TransformPos v1
        let v2' = (fold2 angle).TransformPos v2
        let qs = [| v3;v5;v4; v0';v3;v4; v1';v4;v5; v2';v5;v3 |]

        transact (fun () -> Mod.change positions qs)

    let animation = async {
        let sw = Stopwatch()
        sw.Start()
        while sw.Elapsed.TotalSeconds <= 10.0 do
            let angle = sw.Elapsed.TotalSeconds / 10.0 * 120.0
            //printfn "angle: %f" angle

            update (Conversion.RadiansFromDegrees(angle))
    }

    let foldableTetrahedron =

        DrawCallInfo(
            FaceVertexCount = 12,
            InstanceCount = 1
            )
            |> Sg.render IndexedGeometryMode.TriangleList 
            |> Sg.vertexAttribute DefaultSemantic.Positions positions
            |> Sg.vertexAttribute DefaultSemantic.Colors (Mod.constant cs)
            |> Sg.vertexAttribute DefaultSemantic.Normals (Mod.constant ns)
            
        
    let sg =
        foldableTetrahedron
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                //DefaultSurfaces.simpleLighting |> toEffect
               ]
            |> Sg.viewTrafo (view |> Mod.map CameraView.viewTrafo)
            |> Sg.projTrafo (proj |> Mod.map Frustum.projTrafo)

    // specify render task
    let task =
        app.Runtime.CompileRender(win.FramebufferSignature, sg)
            |> DefaultOverlays.withStatistics

    // start
    let abc = fold0 (Conversion.RadiansFromDegrees(120.0))
    printfn "AAAAAAHHH: %A" (abc.TransformPos v0)
    
    win.RenderTask <- task
    animation |> Async.Start
    win.Run()
    0
