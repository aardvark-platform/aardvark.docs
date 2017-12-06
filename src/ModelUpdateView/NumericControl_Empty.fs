module NumericControl_Empty

open Aardvark.Base             // math stuff such as V3d, Trafo3d
open Aardvark.UI            // the base infrastructure for elm style aardvark applications
open Aardvark.Base.Incremental

open NumericControlNs

type Action = Increment | Decrement    

let update (m : NumericModel) (a : Action) = m

let view (m : MNumericModel) =
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
        initial = { value = 0.0 }
        update = update
        view = view
    }

let start() = App.start app

