open Sky

open Aardium
open Aardvark.UI
open Suave
open Aardvark.Rendering
open Aardvark.Rendering.Vulkan
open Aardvark.Base
open Aardvark.Application.Slim
open System

[<EntryPoint>]
let main args =
    Aardvark.Init()
    Aardium.init()

    let useVulkan = false

    let runtime, disposable =
        if useVulkan then
            let app = new HeadlessVulkanApplication(debug = true)
            app.Runtime :> IRuntime, app :> IDisposable
        else
            let app = new OpenGlApplication(debug = DebugLevel.Minimal)
            app.Runtime, app :> IDisposable

    use __ = disposable

    WebPart.startServer 4321 [
        MutableApp.toWebPart' runtime false (App.start App.app)
    ] |> ignore

    Aardium.run {
        title "Aardvark rocks \\o/"
        width 1024
        height 768
        url "http://localhost:4321/"
    }

    0
