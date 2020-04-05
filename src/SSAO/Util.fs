namespace SSAO

open Aardvark.Base
open FSharp.Data.Adaptive
open FSharp.Data.Adaptive.Operators
open Aardvark.UI
open Aardvark.UI.Primitives
open Aardvark.UI.Generic
open Aardvark.SceneGraph
open Aardvark.SceneGraph.IO
open Aardvark.Base.Rendering

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

    let semuirange =
        [
            { kind = ReferenceKind.Script; url = "https://cdn.jsdelivr.net/npm/semantic-ui-range@1.0.1/range.js"; name = "semui-range"}
            { kind = ReferenceKind.Stylesheet; url = "https://cdn.jsdelivr.net/npm/semantic-ui-range@1.0.1/range.css"; name = "semui-range"}
        ]

    let inline slider (att : list<string * AttributeValue<'msg>>) (min : float) (max : float) (step : float) (value : aval<float>) (onChange : float -> 'msg) =
        
        let channelName = sprintf "channel%d" (newId())
        
        let boot = 
            String.concat ";" [
                sprintf "$('#__ID__').range({ min: %f, max: %f, step: %f, start: %f, onChange: function(value, meta) { if(meta.triggeredByUser) aardvark.processEvent('__ID__', 'onchange', value); } });" min max step (AVal.force value)
                sprintf "%s.onmessage = function(value) {$('#__ID__').range('set value', value); };" channelName
            ]
            
        let changeAtt = 
            onEvent "onchange" [] (fun vs ->
                System.Double.Parse(List.head vs, System.Globalization.CultureInfo.InvariantCulture) |> onChange
            )
            
        require semuirange (
            onBoot' [channelName, AVal.channel value] boot (div (changeAtt :: att) [])
        )


