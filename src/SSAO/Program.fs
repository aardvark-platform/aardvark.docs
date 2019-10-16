// Learn more about F# at http://fsharp.org

open System
open Aardvark.Base
open Suave
open Aardium
open Aardvark.UI
open Aardvark.Rendering.Vulkan
open SSAO
open Aardvark.Application.Slim

[<EntryPoint>]
let main argv =
    System.Threading.ThreadPool.SetMinThreads(24,24) |> (fun f -> if not f then failwith "" else ignore f)

    Ag.initialize()
    Aardvark.Init()
    Aardium.init()

    let app = new OpenGlApplication()


    WebPart.startServer 4321 [
        MutableApp.toWebPart' app.Runtime false (App.start (App.app app.Runtime))
    ] |> ignore
    

    Aardium.run {
        width 1380
        height 768
        url "http://localhost:4321/"
    }

    0 // return an integer exit code