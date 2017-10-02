namespace BoxSelectionDemo

open System
open BoxSelectionModel
open Aardvark.UI.Primitives
open Aardvark.UI
open Aardvark.Base.Rendering
open Aardvark.Base.Incremental
open Aardvark.Base
open Aardvark.SceneGraph.``Sg Picking Extensions``

module BoxSelectionDemo_2 =
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
            | Select id-> 
                let selection = 
                    if HSet.contains id model.selectedBoxes 
                    then HSet.remove id model.selectedBoxes 
                    else HSet.add id model.selectedBoxes

                { model with selectedBoxes = selection } 
            | HoverIn id -> { model with boxHovered = Some id }            
            | HoverOut   -> { model with boxHovered = None }        
            | _ -> model

    let mkColor (model : MBoxSelectionDemoModel) (box : MVisibleBox) = 
        let id = box.id |> Mod.force

        let color =  
            model.selectedBoxes 
                |> ASet.contains id 
                |> Mod.bind (function x -> if x then Mod.constant C4b.Red else box.color)

        color

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
                        Sg.onClick (fun _  -> Select (box.id |> Mod.force))
                        Sg.onEnter (fun _  -> HoverIn (box.id |> Mod.force))
                        Sg.onLeave (fun () -> HoverOut)
                    ]

    let view (model : MBoxSelectionDemoModel) =
                                   
        let frustum = Mod.constant (Frustum.perspective 60.0 0.1 100.0 1.0)
             
        let color = 
            model.boxHovered |> Mod.map (
                function x -> match x with
                                | Some k -> if k = "box" then C4b.Blue else C4b.Gray
                                | None -> C4b.Gray)

        require (Html.semui) (
            div [clazz "ui"; style "background: #1B1C1E"] [
                CameraController.controlledControl model.camera CameraMessage frustum
                    (AttributeMap.ofList [
                        attribute "style" "width:65%; height: 100%; float: left;"
                    ])
                    (
                        Sg.box color (Mod.constant Box3d.Unit)
                            |> Sg.shader {
                                do! DefaultSurfaces.trafo
                                do! DefaultSurfaces.vertexColor
                                do! DefaultSurfaces.simpleLighting
                                }                
                            |> Sg.requirePicking
                            |> Sg.noEvents
                            |> Sg.withEvents [
                                Sg.onClick (fun _  -> Select ("box"))
                                Sg.onEnter (fun _  -> HoverIn ("box"))
                                Sg.onLeave (fun () -> HoverOut)
                            ]
                    )
                div [style "width:35%; height: 100%; float:right"] [
                    div [clazz "ui buttons"] [
                        button [clazz "ui button"; onMouseClick (fun _ -> AddBox)] [text "Add Box"]
                        button [clazz "ui button"; onMouseClick (fun _ -> RemoveBox)] [text "Remove Box"]
                        button [clazz "ui button"; onMouseClick (fun _ -> ClearSelection)] [text "Clear Selection"]
                    ]
                ]
            ]
        )

    let initial =
        {
            camera          = CameraController.initial
            boxHovered      = None
            boxes           = Primitives.mkBoxes 3 |> List.mapi (fun i k -> Primitives.mkVisibleBox Primitives.colors.[i % 5] k) |> PList.ofList
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