open System
open Aardvark.Application
open Aardvark.Base
open Aardvark.Rendering
open Aardvark.SceneGraph
open FSharp.Data.Adaptive

open Aardvark.Docs.SierpinksiTetrahedron

type Episode = { Start : float; Stop : float; GenerateSg : (aval<float> -> ISg) }

[<EntryPoint>]
let main argv =
    // initialize runtime system
    Aardvark.Init()

    use win =
        window {
            display Display.Mono
            samples 8
            backend Backend.GL
            debug false
        }

    // define scene
    let speed = 0.5
    let t =
        let t0 = DateTime.Now
        win.Time |> AVal.map (fun t -> (t - t0).TotalSeconds * speed)

    let foo (a : float) b =
        AVal.map(fun x ->
            match x with
            | x when x < a -> 0.0
            | x when x > b -> 1.0
            | _ -> (x - a) / (b - a)
        )

    let episodes n =
        let phase0 t = (FoldingTriangle t).SceneGraph
        let episode0 = { Start = 1.0; Stop = 2.0; GenerateSg = phase0 }
        episode0 ::
        [
            for i in 0 .. n do
                let phase t = (FoldingTetrahedron t).SceneGraph i
                let episode = { Start = (float)i + 2.0; Stop = (float)i + 3.0; GenerateSg = phase }
                yield episode
        ]

    let series t (episodes : seq<Episode>) =
        let episodes = episodes |> Array.ofSeq
        let sgs = episodes |> Array.map (fun e -> e.GenerateSg (t |> foo e.Start e.Stop))
        t |> AVal.map (fun x ->
                [
                    for i in 0..episodes.Length-1 do
                        let e = episodes.[i]
                        if x >= e.Start && x <= e.Stop then
                            yield sgs.[i]
                ]
                |> Sg.ofList
            )
            |> Sg.dynamic

    let test = series t (episodes 4)

    let sg =
        test
            |> Sg.effect
            [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.simpleLighting |> toEffect
            ]

    // start
    win.Scene <- sg
    win.Run()

    0
