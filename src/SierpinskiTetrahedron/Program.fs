open System
open Aardvark.Application
open Aardvark.Application.Slim
open Aardvark.Base
open Aardvark.Base.Rendering
open Aardvark.Base.Incremental
open Aardvark.Docs.SierpinksiTetrahedron
open Aardvark.SceneGraph

type Episode = { Start : float; Stop : float; GenerateSg : (IMod<float> -> ISg) }

[<EntryPoint>]
let main argv = 
    // initialize runtime system
    Ag.initialize(); Aardvark.Init()

    // simple OpenGL window
    use app = new OpenGlApplication()
    let win = app.CreateGameWindow(8)
    win.Title <- "SierpinskiTetrahedron (aardvark.docs)"
  
    // define scene
    let h = 0.5 * sqrt 3.0  // height of triangle
    let initialView = CameraView.lookAt (V3d(0.6, -1.0, 0.7)) (V3d(0.5, h / 3.0, h / 3.0)) V3d.ZAxis
    //let initialView = CameraView.lookAt (V3d(0.5, h / 3.0, 2.0)) (V3d(0.5, h / 3.0, 0.0)) V3d.OIO
    let view = 
        Mod.integrate initialView win.Time [
                DefaultCameraController.controlPan win.Mouse
                DefaultCameraController.controlOrbitAround win.Mouse (V3d(0.5, h / 2.0, h / 2.0) |> Mod.constant)
        ]
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))

    let t0 = Mod.init DateTime.Now
    let speed = Mod.init 0.5
    let t = Mod.map2 (fun (t0 : DateTime) (t : DateTime) -> (t - t0).TotalSeconds) t0 Mod.time
    let t = Mod.map2 ( (*) ) t speed

    let foo (a : float) b = 
        Mod.map(fun x ->
            match x with
            | x when x < a -> 0.0
            | x when x > b -> 1.0
            | _ -> (x - a) / (b - a)
        )

    let episodes n = 
        let phase0 t = (FoldingTriangle t).SceneGraph
        let episode0 = { Start = 1.0; Stop = 2.0; GenerateSg = phase0 }
        episode0 ::
        [
            for i in 0 .. n do
                let phase t = (FoldingTetrahedron t).SceneGraph i
                let episode = { Start = (float)i + 2.0; Stop = (float)i + 3.0; GenerateSg = phase }
                yield episode
        ]

    let series t (episodes : seq<Episode>) =
        let episodes = episodes |> Array.ofSeq
        let sgs = episodes |> Array.map (fun e -> e.GenerateSg (t |> foo e.Start e.Stop))
        t |> Mod.map (fun x ->
                [ 
                    for i in 0..episodes.Length-1 do
                        let e = episodes.[i]
                        if x >= e.Start && x <= e.Stop then
                            yield sgs.[i]
                ] 
                |> Sg.ofList
            )
            |> Sg.dynamic

    let test = series t (episodes 4)

    let sg =
        test
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
                app.Runtime.CompileClear(win.FramebufferSignature, Mod.constant C4f.Gray70)
                app.Runtime.CompileRender(win.FramebufferSignature, sg)
            ]

    // start
    win.RenderTask <- task
    win.Run()
    0
