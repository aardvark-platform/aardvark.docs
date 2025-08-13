open System
open Aardvark.Application
open Aardvark.Application.Slim
open Aardvark.Base
open Aardvark.Rendering
open Aardvark.SceneGraph
open Aardvark.SceneGraph.IO
open FSharp.Data.Adaptive

[<EntryPoint>]
let main argv =
    // initialize runtime system
    Aardvark.Init()

    // create simple render window
    use app = new OpenGlApplication()
    let win = app.CreateGameWindow(8)
    win.Title <- "Background Color (aardvark.docs)"

    // load model
    //let modelObj = Loader.Assimp.Load(@"W:\Datasets\Pro3D\confidential\2025-08-12_RockFace_Orig.obj\RockFace_Orig.obj", Assimp.PostProcessSteps.SplitLargeMeshes)
    //let modelObj = Loader.Assimp.load @"C:\Data\Development\aardvark.docs\data\aardvark\Aardvark.obj"
    let modelObj = Loader.Assimp.load @"W:\Datasets\Pro3D\confidential\2025-08-12_RockFace_Orig.obj\RockFace_Orig.obj"

    // view, projection and default camera controllers
    let initialView =
        let bb = modelObj.bounds - modelObj.bounds.Min
        CameraView.lookAt (V3d(-bb.Max.X, -bb.Max.Y, bb.Max.Z)) bb.Center V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> AVal.map (fun s -> Frustum.perspective 60.0 0.1 100.0 (float s.X / float s.Y))
    
    //// define scene

    let model =
        modelObj
        |> Sg.adapter
        |> Sg.transform (Trafo3d.Translation(-modelObj.bounds.Min))
        |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.diffuseTexture |> toEffect
                DefaultSurfaces.simpleLighting |> toEffect
               ]

    let box =
        Sg.box' C4b.Red Box3d.Unit
        |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.simpleLighting |> toEffect
               ]

    let sg =
        [model; box]
        |> Sg.ofSeq
        |> Sg.viewTrafo (view |> AVal.map CameraView.viewTrafo)
        |> Sg.projTrafo (proj |> AVal.map Frustum.projTrafo)

    // background color
    let bgColor = AVal.init C4f.Gray

    // specify render task(s)
    use task =
        [
            app.Runtime.CompileClear(win.FramebufferSignature, bgColor)
            app.Runtime.CompileRender(win.FramebufferSignature, sg)
        ]
        |> RenderTask.ofList

    // start
    win.RenderTask <- task
    win.Run()
    0
