namespace SSAO

open Aardvark.Base
open FSharp.Data.Adaptive
open Aardvark.SceneGraph.IO
open Adaptify

type Scene =
    | Simple
    | Model of scene : list<Loader.Scene>

[<ModelType>]
type Model =
    {
        scene           : Scene
        radius          : float
        threshold       : float
        visualization   : SSAOVisualization
        scale           : float
        sigma           : float
        sharpness       : float
        gamma           : float
        samples         : int

        pendingLoad     : list<string>
        status          : Option<string>

        time            : MicroTime
    }