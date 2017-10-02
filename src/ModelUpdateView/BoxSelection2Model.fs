namespace BoxSelection2Model

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.UI.Primitives
open BoxSelectionModel

[<DomainType>]
type Boxes = {
    boxes : plist<VisibleBox>
    boxesSet : hset<VisibleBox>    
}

[<DomainType>]
type BoxSelection2Model = {
    camera : CameraControllerState        
    boxes : Boxes

    boxHovered : option<string>
    selectedBoxes : hset<string>
}

type BoxesAction =
    | AddBox
    | RemoveBox

type Action =
    | CameraMessage    of CameraControllerMessage
    | BoxesMessage      of BoxesAction
    | Select           of string                          
   

