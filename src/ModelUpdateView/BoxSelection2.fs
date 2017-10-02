namespace ActionLifting

open System
open Aardvark.UI.Primitives
open Aardvark.UI
open Aardvark.Base.Rendering
open Aardvark.Base.Incremental
open Aardvark.Base
open Aardvark.SceneGraph.``Sg Picking Extensions``

open ActionLiftingModel

module ActionLifting =        
    open Boxes
    open BoxSelectionDemo

    let update (model : ActionLiftingModel) (act : Action) =
        match act with
            | CameraMessage m -> 
                { model with camera = CameraController.update model.camera m }
            | BoxesMessage m ->
                //{ model with boxes = CameraController.update model.camera m }
                model
            | Select id-> 
                let selection = 
                    if HSet.contains id model.selectedBoxes 
                    then HSet.remove id model.selectedBoxes 
                    else HSet.add id model.selectedBoxes

                { model with selectedBoxes = selection }
          
    let mkColor (model : MActionLiftingModel) (box : MVisibleBox) =  
        let id = box.id |> Mod.force
        model.selectedBoxes 
            |> ASet.contains id
            |> Mod.bind (function x -> if x then Mod.constant Primitives.selectionColor else box.color)       

    let viewBoxesUI (model : MBoxesModel) =
        [
            div [clazz "ui buttons"] [
               button [clazz "ui button"; onMouseClick (fun _ -> AddBox)] [text "Add Box"]
               button [clazz "ui button"; onMouseClick (fun _ -> RemoveBox)] [text "Remove Box"]                
            ]

            Incremental.div (AttributeMap.ofList [clazz "ui divided list"]) (            
                alist {                                
                    for b in model.boxes do
                        let! c = Mod.constant C4b.Red //mkColor model b

                        let bgc = sprintf "background: %s" (Html.ofC4b c)
                                    

                        //onClick(fun _ -> Select (b.id |> Mod.force))
                        yield div [clazz "item"; style bgc][
                            i [clazz "medium File Outline middle aligned icon"][]
                        ]                                                                    
                }
            )
        ]

    let mkISg (model : MActionLiftingModel) (box : MVisibleBox) =
                
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
                ]

    let view (model : MActionLiftingModel) =
                                   
        let frustum = Mod.constant (Frustum.perspective 60.0 0.1 100.0 1.0)                  

        require (Html.semui) (
            div [clazz "ui"; style "background: #1B1C1E"] [
                CameraController.controlledControl model.camera CameraMessage frustum
                    (AttributeMap.ofList [
                        attribute "style" "width:65%; height: 100%; float: left;"
                    ])
                    (
                         model.boxes.boxes 
                            |> AList.toASet 
                            |> ASet.map (function b -> mkISg model b)
                            |> Sg.set                          
                            |> Sg.noEvents
                    )
                div [style "width:35%; height: 100%; float:right"] (
                    viewBoxesUI model.boxes |> List.map (fun x -> UI.map BoxesMessage x)
                )                
            ]
        )

    let initial = {
        camera          = CameraController.initial
        boxHovered      = None
        boxes           = { boxes = Primitives.mkBoxes 3 |> List.mapi (fun i k -> Primitives.mkVisibleBox Primitives.colors.[i % 5] k) |> PList.ofList; boxesSet = HSet.empty }
        selectedBoxes   = HSet.empty                     
    }

    let app = {
        unpersist = Unpersist.instance
        threads = fun model -> CameraController.threads model.camera |> ThreadPool.map CameraMessage
        initial = initial
        update = update
        view = view
    }

    let start () = App.start app