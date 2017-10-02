namespace ActionLiftingModel

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.UI.Primitives
open Boxes

[<DomainType>]
type BoxesModel = {
    boxes : plist<VisibleBox>
    boxesSet : hset<VisibleBox>    
}

type BoxesAction =
    | AddBox
    | RemoveBox

[<DomainType>]
type ActionLiftingModel = {
    camera : CameraControllerState        
    boxes : BoxesModel

    boxHovered : option<string>
    selectedBoxes : hset<string>

    colors : list<C4b>
}

type Action =
    | CameraMessage    of CameraControllerMessage
    | BoxesMessage     of BoxesAction
    | Select           of string                          
   

