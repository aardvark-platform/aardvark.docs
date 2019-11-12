open System
open System.IO
open Aardvark.Application
open Aardvark.Application.Slim
open Aardvark.Base
open Aardvark.Base.Rendering
open Aardvark.Base.Incremental
open Aardvark.SceneGraph

module Shaders = 
    open FShade 


[<EntryPoint>]
let main argv =

    Shadows.run()
    System.Environment.Exit 0

    // initialize runtime system
    Ag.initialize(); Aardvark.Init()

    // create simple render window
    use app = new OpenGlApplication()
    let win = app.CreateGameWindow(8)
    win.Title <- "Hello World (aardvark.docs)"

    // view, projection and default camera controllers
    let initialView = CameraView.lookAt (V3d(9.3, 9.9, 8.6)) V3d.Zero V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> Mod.map (fun s -> Frustum.perspective 60.0 0.1 1000.0 (float s.X / float s.Y))
    
    // define scene
    let sg =
        Sg.box' C4b.DarkCyan Box3d.Unit
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                //Shaders.inFrustum |> toEffect
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
