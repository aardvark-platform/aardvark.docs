namespace BoxSelectionDemo

open System
open BoxSelectionModel
open Aardvark.UI.Primitives
open Aardvark.UI
open Aardvark.Base.Rendering
open Aardvark.Base.Incremental
open Aardvark.Base
open Aardvark.SceneGraph.``Sg Picking Extensions``

module BoxSelectionDemo_Empty =
    open Boxes

    let mkVisibleBox (color : C4b) (box : Box3d) : VisibleBox = 
        {
            id = Guid.NewGuid().ToString()
            geometry = box
            color = color           
        }

    let update (model : BoxSelectionDemoModel) (act : Action) =
        match act with
            | CameraMessage m -> 
                     { model with camera = CameraController.update model.camera m }
            | _ -> failwith "cant handle message yet"

    let mkColor (model : MBoxSelectionDemoModel) (box : MVisibleBox) = 
        Mod.constant C4b.Gray

    ///Specifies how to draw a single box
    let mkISg (model : MBoxSelectionDemoModel) (box : MVisibleBox) =
                
            let color = mkColor model box

            Sg.box color box.geometry
                    |> Sg.shader {
                        do! DefaultSurfaces.trafo
                        do! DefaultSurfaces.vertexColor
                        do! DefaultSurfaces.simpleLighting
                        }                
                    |> Sg.requirePicking
                    |> Sg.noEvents
                    |> Sg.withEvents [
                       //onclick select
                       //onenter enter
                       //onleave exit
                    ]

    let view (model : MBoxSelectionDemoModel) =
                                   
        let frustum = Mod.constant (Frustum.perspective 60.0 0.1 100.0 1.0)
      
        //require (Html.semui) (
        div [clazz "ui"; style "background: #1B1C1E"] [
            CameraController.controlledControl model.camera CameraMessage frustum
                (AttributeMap.ofList [
                    attribute "style" "width:65%; height: 100%; float: left;"
                ])
                (
                    Sg.box (Mod.constant C4b.Gray) (Mod.constant Box3d.Unit)
                        |> Sg.shader {
                            do! DefaultSurfaces.trafo
                            do! DefaultSurfaces.vertexColor
                            do! DefaultSurfaces.simpleLighting
                            }                
                        |> Sg.requirePicking
                        |> Sg.noEvents
                        //|> Sg.withEvents [
                        //    //onclick select
                        //    //onenter enter
                        //    //onleave exit
                        //]
                )
        ]
        //)

    let initial =
        {
            camera          = CameraController.initial
            boxHovered      = None
            boxes           = plist.Empty // Primitives.mkBoxes 3 |> List.mapi (fun i k -> Primitives.mkVisibleBox Primitives.colors.[i % 5] k) |> PList.ofList
            selectedBoxes   = HSet.empty         
            boxesSet        = HSet.empty
        }

    let app : App<BoxSelectionDemoModel,MBoxSelectionDemoModel,Action> =
            {
                unpersist = Unpersist.instance
                threads = fun model -> CameraController.threads model.camera |> ThreadPool.map CameraMessage
                initial = initial
                update = update
                view = view
            }

    let start () = App.start app