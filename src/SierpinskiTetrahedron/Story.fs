namespace Aardvark.Docs.SierpinksiTetrahedron

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.SceneGraph

module Story =

    type Block = { Duration : float; Generate : (IMod<float> -> ISg) }

    let none duration = { Duration = duration; Generate = (fun t -> Sg.group' []) }

    let some duration sg = { Duration = duration; Generate = (fun t -> sg) }

    let mapRangeToUnit (min, max) = 
        Mod.map(fun x ->
            match x with
            | x when x < min -> 0.0
            | x when x > max -> 1.0
            | _ -> (x - min) / (max - min)
        )

    let mapRangeToConst (min, max) c = 
        Mod.map(fun x ->
            match x with
            | x when x < min -> 0.0
            | x when x > max -> 0.0
            | _ -> c
        )

    let scale (min, max) c = 
        Mod.map(fun x ->
            match x with
            | x when x < min -> 0.0
            | x when x > max -> 0.0
            | _ -> c
        )