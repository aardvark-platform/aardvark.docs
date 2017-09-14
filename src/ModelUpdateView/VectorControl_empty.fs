module VectorControl_Empty

open Aardvark.Base             // math stuff such as V3d, Trafo3d
open Aardvark.UI            // the base infrastructure for elm style aardvark applications

open VectorControlNs

// reuse numeric actions
type Action = 
    | SetX of NumericControl.Action
    | SetY of NumericControl.Action
    | SetZ of NumericControl.Action

// call update logic of indiviual numeric controls
let update (m : VectorModel) (a : Action) =
    match a with
        | _ -> m

// uses a table to show the individual numeric controls
let view (m : MVectorModel) =
    require Html.semui ( 
        body [] (        
            [
                table [] [
                    tr[][
                        td[][text "X:"]
                        td[][]
                    ]
                    tr[][
                        td[][text "Y:"]
                        td[][]
                    ]
                    tr[][
                        td[][text "Z:"]
                        td[][]
                    ]
                ]
            ]
        )
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





