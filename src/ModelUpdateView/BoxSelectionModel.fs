namespace BoxSelectionModel

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.UI.Primitives
open Boxes


[<DomainType>]
type BoxSelectionDemoModel = {
    camera : CameraControllerState    
    //rendering : RenderingParameters

    boxes : plist<VisibleBox>
    boxesSet : hset<VisibleBox>    

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