namespace SimpleScaleModel

open Aardvark.Base.Incremental
open Aardvark.UI.Primitives
open Aardvark.UI
open Aardvark.Base
open VectorControlNs

[<DomainType>]
type Model =
    {
        camera          : CameraControllerState
        scale           : VectorModel
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Model =
    let initial =  { camera = { ArcBallController.initial with orbitCenter = Some V3d.Zero }; scale = VectorModel.initial }