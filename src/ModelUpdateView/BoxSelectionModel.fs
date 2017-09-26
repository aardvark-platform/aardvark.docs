namespace BoxSelectionModel

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.UI.Primitives

[<DomainType>]
type VisibleBox = {
    geometry : Box3d
    color    : C4b    

    [<TreatAsValue; PrimaryKey>]
    id       : string
}

[<DomainType>]
type BoxSelectionDemoModel = {
    camera : CameraControllerState    
    //rendering : RenderingParameters

    boxes : plist<VisibleBox>
    boxesSet : hset<VisibleBox>
    boxesMap : hmap<string,VisibleBox>

    boxHovered : option<string>
    selectedBoxes : hset<string>
}

type Action =
    | CameraMessage    of CameraControllerMessage
    | Select           of string     
    | ClearSelection
    | HoverIn          of string 
    | HoverOut                     
    | AddBox
    | RemoveBox    