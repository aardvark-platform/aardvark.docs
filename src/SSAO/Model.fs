namespace SSAO

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.SceneGraph.IO

type Scene =
    | Simple
    | Model of scene : list<Loader.Scene>

[<DomainType>]
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