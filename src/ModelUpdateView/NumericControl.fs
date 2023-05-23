module NumericControl

open Aardvark.Base             // math stuff such as V3d, Trafo3d
open Aardvark.UI            // the base infrastructure for elm style aardvark applications

open NumericControlNs
open Aardvark.Base.Incremental

type Action = Increment | Decrement    

let update (m : NumericModel) (a : Action) =
    match a with 
        | Increment -> { m with value = m.value + 0.1 }
        | Decrement -> { m with value = m.value - 0.1 }

let view (m : MNumericModel) =
    require Html.semui ( 
        body [] (        
            [
                div [] [
                        button [clazz "ui button"; onClick (fun _ -> Increment)] [text "+"]
                        button [clazz "ui button"; onClick (fun _ -> Decrement)] [text "-"]
                        br []
                        text "my value:"
                        br []
                        Incremental.text (m.value |> Mod.map(fun x -> x.ToString()))
                    ]
            ]
        )
    )

let view' (m : MNumericModel) =
    require Html.semui (         
        table [][ 
            tr[] [
                td[] [a [clazz "ui label circular Big"] [Incremental.text (m.value |> Mod.map(fun x -> sprintf "%.1f" x))]]
                td[] [div[clazz "ui buttons"][
                        button [clazz "ui icon button"; onClick (fun _ -> Increment)] [text "+"]
                        button [clazz "ui icon button"; onClick (fun _ -> Decrement)] [text "-"]                                        
                    ]
                ]
            ]
        ]        
    )

let app =
    {
        unpersist = Unpersist.instance        //ignore for now
        threads   = fun _ -> ThreadPool.empty //ignore for now
        initial   = { value = 0.0 }
        update    = update
        view      = view
    }

let start() = App.start app

