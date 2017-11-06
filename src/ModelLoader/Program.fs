open System
open Aardvark.Base
open Aardvark.Base.Rendering
open Aardvark.Base.Incremental
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
    win.Text <- "Model Loading (aardvark.docs)"

    // view, projection and default camera controllers
    let initialView = CameraView.lookAt (V3d(9.3, 9.9, 8.6)) V3d.Zero V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))
    
    let model = 
        Aardvark.SceneGraph.IO.Loader.Assimp.load @"..\..\data\aardvark\aardvark.obj" 
        |> Sg.adapter
        |> Sg.transform (Trafo3d.Scale(1.0,1.0,-1.0))

    let scene = model

    let scene = 
        [
            for x in -5 .. 5 do
                for y in -5 .. 5 do
                    for z in -5 .. 5 do
                        yield 
                            model |> Sg.translate (float x) (float y) (float z) 
        ] |> Sg.ofSeq

    let sg =
        scene
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.diffuseTexture |> toEffect
                DefaultSurfaces.simpleLighting |> toEffect
               ]
            |> Sg.viewTrafo (view |> Mod.map CameraView.viewTrafo)
            |> Sg.projTrafo (proj |> Mod.map Frustum.projTrafo)

    // specify render task
    let task =
        app.Runtime.CompileRender(win.FramebufferSignature, sg)

    // start
    win.RenderTask <- task
    win.Run()
    0
