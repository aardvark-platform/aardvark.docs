namespace SSAO

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.Base.Incremental.Operators
open Aardvark.UI
open Aardvark.UI.Generic
open Aardvark.SceneGraph
open Aardvark.SceneGraph.IO
open Aardvark.Base.Rendering
open Aardvark.Service
open Aardvark.UI.Primitives

module PointShader =
    open FShade
    type SizeVertex = 
        {
            [<PointSize>] s : float
        }

    let sizeShader (v : SizeVertex) =
        vertex {
            return { v with s = 50.0 }
        }

module App =
    do Loader.Assimp.initialize()
    
    type Message =
        | SetRadius of float
        | SetThreshold of float
        | SetSigma of float
        | SetSharpness of float
        | SetScale of float
        | SetGamma of float
        | SetSamples of int
        | SetVisualization of SSAOVisualization
        | LoadFiles of list<string>
        | SetScene of list<Loader.Scene>
        | Reset
        | SetStatus of string
        | Rendered

    let initial =
        {
           scene           = Simple
           radius          = 0.8
           threshold       = 0.65
           visualization   = SSAOVisualization.Composed
           scale           = 1.0
           sigma           = 8.0
           sharpness       = 2.0
           gamma           = 2.2
           samples         = 32
           pendingLoad     = []
           status          = None
           time            = MicroTime.Zero
        }

    let simpleScene (time : IMod<MicroTime>) =
        let time = time |> Mod.map ((*)0.5)
        //let time = Mod.constant MicroTime.Zero
        // Sg.box provides a scenegraph containing a box (which can be changed, as
        // indicated by the modifiable arguments).
        // Sg.box' is a static variant thereof
        let staticBox = Sg.box' C4b.Green (Box3d.FromMinAndSize(new V3d(-1, -2, 0), new V3d(15, 10, 1)))

        // now let us use the dynamic box (in order to change vertex attributes)
        let colors = [ C4b.Yellow; C4b.Green; C4b.Blue ]
        let mutable currentIndex = 0
        let boxColor = Mod.init colors.[currentIndex]
    
        let dynamicBox = Sg.box boxColor (Box3d.FromMinAndSize(new V3d(2, 1, 1), new V3d(3, 4, 2)) |> Mod.constant)

        // create a simple subdivision sphere (as with box the tick' version of the function 
        // can be used to generate a static sphere
        let sphere = Sg.sphere' 5 C4b.Red 2.0

        // quad can be used to create a simple quad, let us scale it using Sg.scale
        let groundPlane = 
            Sg.quad 
            |> Sg.vertexArray DefaultSemantic.Colors (Array.create 4 C4b.White)
            //|> Sg.translate 0.5 0.5 0.0
            |> Sg.scale 100.0

        let boxes = 
            Sg.ofSeq [
                staticBox   
                dynamicBox  
                Sg.box' C4b.White (Box3d.FromMinAndSize(new V3d(2, 1, 3), new V3d(1, 1, 3)))
            ]

        let cylinder = 
            IndexedGeometryPrimitives.solidCylinder (V3d(25,5,0)) V3d.ZAxis 6.0 1.5 1.5 12 C4b.Blue

        let cone =
            IndexedGeometryPrimitives.solidCone (V3d(30,0,0)) V3d.ZAxis 5.0 3.0 128 C4b.White

        let smallBox = Box3d.FromCenterAndSize(V3d.Zero, V3d.III * 1.5)
        let rand = RandomSystem()
        let points = 
            let ct = 2500
            let pos =
                [|
                    for i in 0 .. ct-1 do
                        let r = rand.UniformDouble() * 75.0 + 25.0
                        let phi = rand.UniformDouble() * Constant.PiTimesTwo

                        let rot = 
                            let r = rand.UniformV3dDirection() 
                            let angle = rand.UniformDouble() * Constant.PiTimesTwo
                            Trafo3d.Rotation(r, angle)

                        
                        let randomAxis = rand.UniformV3dDirection() 
                        let randomTurnrate = rand.UniformDouble() * 2.0
                        let randomMovespeed = (rand.UniformDouble() - 0.5) * 0.4 + 1.0

                        let p = V3d(r * cos phi, r * sin phi, 0.0)
                        yield randomAxis,randomTurnrate,randomMovespeed,rot * Trafo3d.Translation(p)
                |]

            let colors =
                Array.init ct (fun _ -> rand.UniformC3f().ToC4b())

            let rnd = 
                time |> Mod.map (fun mt -> 
                    pos |> Array.map (fun (randomAxis,randomTurnrate,randomMovespeed,trafo) -> 
                        let rot = Trafo3d.Rotation(randomAxis,randomTurnrate * mt.TotalSeconds * 1.5)

                        let trans = 
                            trafo * 
                            Trafo3d.RotationZ (randomMovespeed * 0.25 * mt.TotalSeconds)

                        let trafo = rot * trans

                        
                        let minZ = smallBox.ComputeCorners() |> Array.map (fun p -> trafo.Forward.TransformPos(p).Z) |> Array.min

                        trafo * Trafo3d.Translation(0.0, 0.0, -minZ) 
                    ) :> System.Array
                )

            let instancedAttributes =
                Map.ofList [
                    string DefaultSemantic.Colors, (typeof<C4b>, Mod.constant (colors :> System.Array))
                    "ModelTrafo", (typeof<Trafo3d>, rnd)
                ]

            Sg.box' C4b.White smallBox
            |> Sg.instanced' instancedAttributes //(Mod.constant pos)
            //let rand = RandomSystem()
            //IndexedGeometryPrimitives.points pos (Array.init pos.Length (fun _ -> rand.UniformC3f().ToC4b()))

        let scene =
            Sg.ofSeq [
                groundPlane
                boxes                                  |> Sg.translate -15.0 0.0 0.0
                sphere   |> Sg.translate 20.0 0.0 2.0  |> Sg.translate -15.0 0.0 0.0
                cylinder |> Sg.ofIndexedGeometry       |> Sg.translate -15.0 0.0 0.0
                cone     |> Sg.ofIndexedGeometry       |> Sg.translate -15.0 0.0 0.0
                points   
            ]
    
        scene
        |> Sg.shader {
            do! DefaultSurfaces.trafo
            do! DefaultSurfaces.vertexColor
            do! DefaultSurfaces.simpleLighting
        }

    let view (m : MModel) =
        let scene =
            m.scene |> Mod.map (fun s ->
                match s with
                | Simple -> 
                    simpleScene m.time

                | Model(scene) ->
                    //Sg.box' C4b.Red Box3d.Unit
                    //|> Sg.shader {
                    //    do! DefaultSurfaces.trafo
                    //    do! DefaultSurfaces.constantColor C4f.White
                    //    do! DefaultSurfaces.simpleLighting
                    //}
                    let bb = scene |> List.map (fun s -> s.bounds) |> Box3d
                    let scale = 300.0 / bb.Size.NormMax

                    scene 
                    |> List.map Sg.adapter
                    |> Sg.ofList
                    |> Sg.scale scale
                    |> Sg.transform (Trafo3d.FromBasis(V3d.IOO, V3d.OOI, -V3d.OIO, V3d.Zero))
                    |> Sg.shader {
                        do! DefaultSurfaces.trafo
                        do! DefaultSurfaces.constantColor C4f.White
                        do! DefaultSurfaces.diffuseTexture
                        do! DefaultSurfaces.simpleLighting
                    }
            )

        require Html.semui (
            div [] [
                ssaoRenderControl 
                    [ style "width: 100%; height: 100%"; attribute "showFPS" "true"] 
                    (function FreeFlyController.Message.Rendered -> Seq.singleton Rendered | _ -> Seq.empty)
                    {
                        radius           = m.radius
                        threshold        = m.threshold
                        visualization    = m.visualization
                        scale            = m.scale
                        sigma            = m.sigma
                        sharpness        = m.sharpness
                        gamma            = m.gamma
                        samples          = m.samples
                    } 
                    (Frustum.perspective 75.0 0.1 1000.0 1.0) 
                    (Sg.dynamic scene)

                div [ style "position: fixed; bottom: 5px; right: 5px"; clazz "ui mini red label" ] [
                    text (m.status |> Mod.map (function Some s -> s | None -> "idle"))
                ]

                div [ style "position: fixed; top: 20px; left: 20px"; clientEvent "onmouseenter" "$('#__ID__').animate({ opacity: 1.0 });";  clientEvent "onmouseleave" "$('#__ID__').animate({ opacity: 0.2 });" ] [
                    table [ clazz "ui inverted table" ] [
                        tr [] [
                            td [] "radius"
                            td [ clazz "right aligned" ] (m.radius |> Mod.map (sprintf " %.3f"))
                            td [] [ slider { min = 0.0; max = 3.0; step = 0.01  } [ clazz "ui inverted red slider"; style "width: 100px"] m.radius SetRadius ]
                        ]

                        tr [] [
                            td [] "threshold"
                            td [ clazz "right aligned" ] (m.threshold |> Mod.map (sprintf " %.3f"))
                            td [] [ slider { min = 0.0; max = 2.8; step = 0.01  }[ clazz "ui inverted red slider"; style "width: 100px"] m.threshold SetThreshold ]
                        ]
                
                        tr [] [
                            td [] "sigma"
                            td [ clazz "right aligned" ] (m.sigma |> Mod.map (sprintf " %.3f"))
                            td [] [ slider { min = 0.0; max = 16.0; step = 0.01  }[ clazz "ui inverted red slider"; style "width: 100px"]m.sigma SetSigma ]
                        ]
                        
                        tr [] [
                            td [] "sharpness"
                            td [ clazz "right aligned" ] (m.sharpness |> Mod.map (sprintf " %.3f"))
                            td [] [ slider { min = 0.0; max = 16.0; step = 0.01  } [ clazz "ui inverted red slider"; style "width: 100px"] m.sharpness SetSharpness ]
                        ]
                        
                        tr [] [
                            td [] "scale"
                            td [ clazz "right aligned" ] (m.scale |> Mod.map (sprintf " %.1f"))
                            td [] [ slider { min = 0.1; max = 1.0; step = 0.1  } [ clazz "ui inverted red slider"; style "width: 100px"] m.scale SetScale ]
                        ]
                        
                        tr [] [
                            td [] "gamma"
                            td [ clazz "right aligned" ] (m.gamma |> Mod.map (sprintf " %.3f"))
                            td [] [ slider { min = 0.0; max = 8.0; step = 0.05  } [ clazz "ui inverted red slider"; style "width: 100px"] m.gamma SetGamma ]
                        ]

                        tr [] [
                            td [] "samples"
                            td [ clazz "right aligned" ] (m.samples |> Mod.map (sprintf " %d"))
                            td [] [ slider { min = 8.0; max = 512.0; step = 8.0  } [ clazz "ui inverted red slider"; style "width: 100px"] (m.samples |> Mod.map float) (int>>clamp 8 512>>SetSamples) ]
                        ]

                        tr [] [
                            td [] "display"
                            td [ attribute "colspan" "2"] [
                                dropDown [] m.visualization SetVisualization (
                                    Map.ofList [
                                        SSAOVisualization.Composed, "composed"
                                        SSAOVisualization.Color, "color"
                                        SSAOVisualization.Normal, "normal"
                                        SSAOVisualization.Ambient, "ambient"
                                        SSAOVisualization.Depth, "depth"
                                    ]
                                )
                            ]
                        ]
                        tr [] [
                            td [ attribute "colspan" "3"; clazz "right aligned"] [
                                button [ clazz "ui yellow basic button"; onClick (fun () -> Reset) ] "Reset"
                                openDialogButton OpenDialogConfig.file [ clazz "ui green basic button"; onChooseFiles LoadFiles ] [ text "Load Scene" ]
                            ]
                        ]
                    ]
            
                ]

            ]
        )
      

    let threads (runtime : IRuntime) (m : Model) =
        match m.pendingLoad with
            | [] ->
                ThreadPool.empty
            | files ->
                let load =
                    proclist {
                        let results = System.Collections.Generic.List<_>()
                        for file in files do
                            yield SetStatus (sprintf "loading %s" (System.IO.Path.GetFileName file))
                            do! Proc.Sleep 0
                            Log.startTimed "loading %s" (System.IO.Path.GetFileName file)
                            do 
                                try
                                    let res = Loader.Assimp.Load( file, Assimp.PostProcessSteps.GenerateNormals ||| Assimp.PostProcessSteps.Triangulate )
                                    results.Add res
                                with e ->
                                    Log.error "could not load: %A" e
                            Log.stop()

                        if results.Count > 0 then
                            yield SetScene(Seq.toList results)
                            do! Proc.Sleep 0
                            yield SetStatus (sprintf "loaded %s" (files |> List.map System.IO.Path.GetFileName |> String.concat ", "))
                        else
                            yield SetStatus (sprintf "ERROR: load failed")
                    }
                ThreadPool.add "load" load ThreadPool.empty

    let sw = System.Diagnostics.Stopwatch.StartNew()
    let update (m : Model) (msg : Message) =
        match msg with
            | SetRadius r           -> { m with radius = r }
            | SetThreshold t        -> { m with threshold = t }
            | SetSigma s            -> { m with sigma = s }
            | SetSharpness s        -> { m with sharpness = s }
            | SetScale s            -> { m with scale = s }
            | SetVisualization v    -> { m with visualization = v }
            | SetGamma g            -> { m with gamma = g }
            | SetSamples s          -> { m with samples = s }
            | LoadFiles f           -> { m with pendingLoad = f }
            | SetScene(s)           -> { m with scene = Model(s); pendingLoad = [] }
            | Reset                 -> { initial with time = m.time }
            | SetStatus s           -> { m with status = Some s }
            | Rendered              -> { m with time = sw.MicroTime }

    let app (runtime : IRuntime) =
        {
            initial = initial
            update = update
            view = view
            threads = threads runtime
            unpersist = Unpersist.instance
        }