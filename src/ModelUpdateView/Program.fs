module MediaUI

open System
open System.Windows.Forms

open Aardvark.Base
open Aardvark.Application
open Aardvark.Application.WinForms
open Aardvark.UI

open Suave
open Suave.WebPart

let startMedia argv =
    Xilium.CefGlue.ChromiumUtilities.unpackCef()
    Chromium.init argv
    Ag.initialize()
    Aardvark.Init()
    use app = new OpenGlApplication()
    let runtime = app.Runtime
    use form = new Form(Width = 1024, Height = 768)

    let app = Step1Done.app

    let instance = 
        app |> App.start

    WebPart.startServer 4321 [ 
        MutableApp.toWebPart runtime instance
        Suave.Files.browseHome
    ]  

    use ctrl = new AardvarkCefBrowser()
    ctrl.Dock <- DockStyle.Fill
    form.Controls.Add ctrl
    ctrl.StartUrl <- "http://localhost:4321/"

    Application.Run form
    System.Environment.Exit 0

[<EntryPoint;STAThread>]
let main argv = startMedia argv; 0