module SimpleScaleApp_empty

open SimpleScaleModel
open Aardvark.UI
open Aardvark.UI.Primitives
open Aardvark.Base.Incremental
open Aardvark.Base
open Aardvark.Base.Rendering
open VectorControlNs
open FShade.Imperative.CVecComponent

type Action =
        | CameraMessage     of ArcBallController.Message              
        | ChangeScale       of VectorControl.Action

let update (model : Model) (act : Action) : Model =
        match act with
        | CameraMessage a   -> { model with camera = ArcBallController.update model.camera a}
        | ChangeScale a     -> model //update scale via vector control

let view (model : MModel) =
    
    let frustum = Mod.constant (Frustum.perspective 60.0 0.1 100.0 1.0)
            
    require (Html.semui) (
        div [clazz "ui"; style "background-color: #1B1C1E"] [
            ArcBallController.controlledControl model.camera CameraMessage frustum
                (AttributeMap.ofList [
                    attribute "style" "width:65%; height: 100%; float: left;"
                ])
                (
                    //make sg list

                    []  |> Sg.ofList
                        |> Sg.shader {
                                do! DefaultSurfaces.trafo
                                do! DefaultSurfaces.vertexColor
                                do! DefaultSurfaces.simpleLighting
                            }                        
                )

            div [style "width:35%; height: 100%; float:right; background-color: #1B1C1E"] [
                div [] [                      
                    text "add vector control to scale sgs"
                ]
            ]
        ]
    )

let app : App<Model, MModel, Action> =
    {
        unpersist   = Unpersist.instance
        threads     = fun model -> ArcBallController.threads model.camera |> ThreadPool.map CameraMessage
        initial     = Model.initial
        update      = update
        view        = view
    }

let start () = App.start app