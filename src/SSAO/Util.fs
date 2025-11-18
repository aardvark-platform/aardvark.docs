namespace SSAO

open FSharp.Data.Adaptive
open Aardvark.UI
open Aardvark.UI.Primitives
open Aardvark.SceneGraph
open Aardvark.Rendering

[<AutoOpen>]
module Utilities =

    let ssaoRenderControl (att : list<string * AttributeValue<FreeFlyController.Message>>) (mapping : FreeFlyController.Message -> seq<'msg>) cfg (frustum : Frustum) (sg : ISg) =

        let view (s : AdaptiveCameraControllerState) =
            let scene = SSAO.getScene cfg sg
            let cam : aval<Camera> = AVal.map (fun v -> { cameraView = v; frustum = frustum }) s.view 
            DomNode.RenderControl(AttributeMap.ofList att, cam, scene, None)
                |> FreeFlyController.withControls s id (AVal.constant frustum)
            
        let app =
            {
                initial = FreeFlyController.initial
                update = FreeFlyController.update
                view = view 
                threads = FreeFlyController.threads
                unpersist = Unpersist.instance
            }

        subApp'
            (fun _ msg -> mapping msg)
            (fun _ _ -> Seq.empty)
            []
            app