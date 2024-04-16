module SimpleScaleApp

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
        | ChangeScale a     -> { model with scale = VectorControl.update model.scale a }

let view (model : MModel) =
    
    let frustum = Mod.constant (Frustum.perspective 60.0 0.1 100.0 1.0)
            
    require (Html.semui) (
        div [clazz "ui"; style "background-color: #1B1C1E"] [
            ArcBallController.controlledControl model.camera CameraMessage frustum
                (AttributeMap.ofList [
                    attribute "style" "width:65%; height: 100%; float: left;"
                ])
                (
                    let boxGeometry = Box3d(-V3d.III, V3d.III)
                    let box = Mod.constant (boxGeometry)   

                    let localScaleTrafo = 
                        adaptive {
                            let! x = model.scale.x.value
                            let! y = model.scale.y.value
                            let! z = model.scale.z.value
                            return Trafo3d.Scale(V3d(x,y,z))
                        }

                    let b = 
                        Sg.box (Mod.constant C4b.Blue) box

                    let s = 
                        Sg.sphere 5 (Mod.constant C4b.Red) (Mod.constant 2.0)
                            |> Sg.trafo localScaleTrafo

                    [b; s]  
                        |> Sg.ofList
                        |> Sg.shader {
                                do! DefaultSurfaces.trafo
                                do! DefaultSurfaces.vertexColor
                                do! DefaultSurfaces.simpleLighting
                            }
                        |> Sg.fillMode (Mod.constant FillMode.Fill)
                        |> Sg.cullMode (Mod.constant CullMode.None)
                        |> Sg.noEvents
                )

            div [style "width:35%; height: 100%; float:right; background-color: #1B1C1E"] [
                div [] [                      
                    yield VectorControl.view model.scale |> UI.map ChangeScale
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