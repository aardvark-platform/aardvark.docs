namespace ActionLifting

open Aardvark.Base
open Aardvark.Base.Incremental    
open Aardvark.UI

open ActionLiftingModel
open Boxes

module BoxesApp =
    open BoxSelectionDemo
    
    
    let update (m:BoxesModel) (a:BoxesAction) =
        match a with
          | AddBox _ ->
            let i = m.boxes.Count                
            let box = Primitives.mkNthBox i (i+1) |> Primitives.mkVisibleBox Primitives.colors.[i % 5]

            { m with boxes = PList.append box m.boxes }
          | RemoveBox _ ->
            let i = m.boxes.Count - 1
            let boxes = PList.removeAt i m.boxes

            { m with boxes = boxes }

    let view (m:MBoxesModel)(mkColor : MVisibleBox -> IMod<C4b>) =
        div[][
            div [clazz "ui buttons"] [
                button [clazz "ui button"; onMouseClick (fun _ -> AddBox)] [text "Add Box"]
                button [clazz "ui button"; onMouseClick (fun _ -> RemoveBox)] [text "Remove Box"]                
            ]

            Incremental.div (AttributeMap.ofList [clazz "ui divided list"]) (
                alist {                                
                    for b in m.boxes do
                        let! c = mkColor b

                        let bgc = sprintf "background: %s" (Html.ofC4b c)
                                    
                        //onClick(fun _ -> Select (b.id |> Mod.force))
                        yield div [clazz "item"; style bgc][
                            i [clazz "medium File Outline middle aligned icon"][]
                        ]                                                                    
                }
            )
        ]

    //let viewAnnotationsInGroup (path:list<Index>) (model:MDrawingModel)(select : MAnnotation -> 'outer)(lift : AnnotationGroups.Action -> 'outer) (annotations: alist<MAnnotation>) : alist<DomNode<'outer>> =

    let view' (m:MBoxesModel)(mkColor : MVisibleBox -> IMod<C4b>) (select : MVisibleBox -> 'outer) (lift : BoxesAction -> 'outer) : DomNode<'outer>=

        div[][
            div [clazz "ui buttons"] [
                button [clazz "ui button"; onMouseClick (fun _ -> lift AddBox)] [text "Add Box"]
                button [clazz "ui button"; onMouseClick (fun _ -> lift RemoveBox)] [text "Remove Box"]                
            ]
            
            Incremental.div (AttributeMap.ofList [clazz "ui divided list"]) (
                alist {                                
                    for b in m.boxes do
                        let! c = mkColor b

                        let bgc = sprintf "background: %s" (Html.ofC4b c)
                                    
                        let select = fun _ -> select b
                        yield div [clazz "item"; style bgc; onClick select][                            
                            i [clazz "medium File Outline middle aligned icon"][]
                        ]                                                                    
                }
            )
        ]
