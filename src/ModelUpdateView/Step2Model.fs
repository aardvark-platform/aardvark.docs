namespace VectorControl

open Aardvark.Base             // for math such as V3d
open Aardvark.Base.Incremental // for Mods etc and [<DomainType>]
open Aardvark.Base.Rendering   // for render attribs such as cullMode
open Aardvark.UI.Primitives    // for primitives such as camera controller state
open NumericControl

[<DomainType>] // records can be marked as domaintypes
type VectorModel = { 
    x : NumericModel
    y : NumericModel
    z : NumericModel
}

module VectorModel = 
    let initial = { x = NumericalModel.initial; y = NumericalModel.initial; z = NumericalModel.initial }


