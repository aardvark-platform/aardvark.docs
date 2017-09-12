module Step1Done

open Aardvark.Base             // math stuff such as V3d, Trafo3d
open Aardvark.UI            // the base infrastructure for elm style aardvark applications

open Step1Model
open Aardvark.Base.Incremental

type Action = Increment | Decrement    

let update (m : Model) (a : Action) =
    match a with 
        | Increment -> { m with value = m.value + 1 }
        | Decrement -> { m with value = m.value - 1 }

let view (m : MModel) =
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

let app =
    {
        unpersist = Unpersist.instance
        threads = fun _ -> ThreadPool.empty
        initial = { value = 0 }
        update = update
        view = view
    }

let start() = App.start app

