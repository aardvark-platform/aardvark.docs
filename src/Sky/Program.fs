module Program

open Aardium
open Aardvark.UI
open Aardvark.UI.Giraffe
open Aardvark.Rendering
open Aardvark.Rendering.Vulkan
open Aardvark.Base
open Aardvark.Application.Slim
open System

[<EntryPoint>]
let main _args =
    Aardvark.Init()
    Aardium.Init()

    let useVulkan = false

    let runtime, disposable =
        if useVulkan then
            let app = new HeadlessVulkanApplication(debug = true)
            app.Runtime :> IRuntime, app :> IDisposable
        else
            let app = new OpenGlApplication(debug = DebugLevel.Minimal)
            app.Runtime, app :> IDisposable

    use _ = disposable

    use mapp = App.start Sky.App.app

    Server.startLocalhost 4321 mapp.CancellationToken [
        WebPart.ofType<Primitives.EmbeddedResources>
        MutableApp.toWebPart' runtime false mapp
    ] |> ignore

    Aardium.run {
        title "Aardvark rocks \\o/"
        width 1024
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
