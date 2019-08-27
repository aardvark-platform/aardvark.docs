open Aardium
open Aardvark.UI
open Suave
open Aardvark.Application.Slim
open Aardvark.Base

[<EntryPoint>]
let main args =
    Ag.initialize()
    Aardvark.Init()
    Aardium.init()

    let app = new OpenGlApplication()

    WebPart.startServer 4321 [
        MutableApp.toWebPart' app.Runtime false (App.start VectorControl_Empty.app)
    ] |> ignore
    
    Aardium.run {
        title "Aardvark rocks \\o/"
        width 1024
        height 768
        url "http://localhost:4321/"
    }

    0