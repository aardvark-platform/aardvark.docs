module Step1

open Aardvark.Base             // math stuff such as V3d, Trafo3d
open Aardvark.UI            // the base infrastructure for elm style aardvark applications

open Step1Model

type Action = Increment | Decrement    

let update (m : Model) (a : Action) = m

let view (m : MModel) =
    require Html.semui ( 
        body [] (        
            [
                div [] [text "hello world"]
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

