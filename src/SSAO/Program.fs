open System.Threading
open Aardvark.Base
open Suave
open Aardium
open Aardvark.UI
open SSAO
open Aardvark.Application.Slim


[<EntryPoint>]
let main argv =
    // setting the min threads to 24 helps avoiding stutters due to IO completion ports.
    ThreadPool.SetMinThreads(24,24) |> ignore

    // initialize aardvark's runtime systems (native dependency loader, etc.)
    Aardvark.Init()

    // ensure that aardium (our custom electron build) exists locally.
    // in case no proper aardium is found it's downloaded from nuget.org
    Aardium.init()

    // create an OpenGlApplication for interacting with the GPU.
    // we could also use a VulkanApplication here, but GL has broader support across platforms atm.
    let app = new OpenGlApplication()

    // create and start our SSAO app
    let ssao = App.app app.Runtime
    let running = App.start ssao

    // start a minimal suave server serving our app on port 4321
    use stopServer = 
        WebPart.startServerLocalhost 4321 [
            MutableApp.toWebPart app.Runtime running
        ]

    // open aardium and show our served app.
    Aardium.run {
        width 1380
        height 768
        url "http://localhost:4321/"
    }

    0