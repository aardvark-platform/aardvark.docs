open System.Threading
open Aardvark.Base
open Aardium
open Aardvark.UI
open Aardvark.UI.Giraffe
open SSAO
open Aardvark.Application.Slim


[<EntryPoint>]
let main argv =
    // initialize aardvark's runtime systems (native dependency loader, etc.)
    Aardvark.Init()

    // ensure that aardium (our custom electron build) exists locally.
    // in case no proper aardium is found it's downloaded from nuget.org
    Aardium.Init()

    // create an OpenGlApplication for interacting with the GPU.
    // we could also use a VulkanApplication here, but GL has broader support across platforms atm.
    use app = new OpenGlApplication()

    // create and start our SSAO app
    use mapp = App.app app.Runtime |> App.start

    // start a minimal Giraffe server serving our app on port 4321
    Server.startLocalhost 4321 mapp.CancellationToken [
        WebPart.ofType<Primitives.EmbeddedResources>
        MutableApp.toWebPart app.Runtime mapp
    ] |> ignore

    // open aardium and show our served app.
    Aardium.run {
        width 1380
        height 768
        url "http://localhost:4321/"
#if DEBUG
        debug true
        log (fun msg -> Report.Line(2, $"[Aardium] {msg}"))
#else
        debug false
#endif
    }

    0