module VectorControl

open Aardvark.Base             // math stuff such as V3d, Trafo3d
open Aardvark.Base.Incremental
open Aardvark.UI            // the base infrastructure for elm style aardvark applications

open VectorControlNs
open NumericControlNs

type Action = 
    | UpdateX of NumericControl.Action
    | UpdateY of NumericControl.Action
    | UpdateZ of NumericControl.Action
    | Normalize
    | Reset

let toVectorModel (v : V3d) = 
    let x : NumericModel = { value = v.X }
    let y : NumericModel = { value = v.Y }
    let z : NumericModel = { value = v.Z }

    { x = x; y = y; z = z }
// call update logic of indiviual numeric controls
let update (m : VectorModel) (a : Action) =
    match a with
        | UpdateX a -> { m with x = NumericControl.update m.x a }
        | UpdateY a -> { m with y = NumericControl.update m.y a }
        | UpdateZ a -> { m with z = NumericControl.update m.z a }
        | Normalize -> 
                let v = V3d(m.x.value,m.y.value,m.z.value)
                v.Normalized |> toVectorModel                                
        | Reset -> VectorModel.initial

let view (m : MVectorModel) =
    require Html.semui (             
        div[][
            table [] [
                tr[][
                    td[][a [clazz "ui label circular Big"][text "X:"]]
                    td[][NumericControl.view' m.x |> UI.map UpdateX]
                ]
                tr[][
                    td[][a [clazz "ui label circular Big"][text "Y:"]]
                    td[][NumericControl.view' m.y |> UI.map UpdateY]
                ]
                tr[][
                    td[][a [clazz "ui label circular Big"][text "Z:"]]
                    td[][NumericControl.view' m.z |> UI.map UpdateZ]
                ]              
                tr[][
                    td[attribute "colspan" "2"][
                        div[clazz "ui buttons small"][
                            button [clazz "ui button"; onClick (fun _ -> Normalize)] [text "Norm"]
                            button [clazz "ui button"; onClick (fun _ -> Reset)] [text "Reset"]
                        ]
                    ]
                ]
            ]               
        ]
    )

let app =
    {
        unpersist = Unpersist.instance
        threads = fun _ -> ThreadPool.empty
        initial = VectorModel.initial
        update = update
        view = view
    }

let start() = App.start app





