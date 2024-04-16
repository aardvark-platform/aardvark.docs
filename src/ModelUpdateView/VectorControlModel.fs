namespace VectorControlNs

open Aardvark.Base             // for math such as V3d
open Aardvark.Base.Incremental // for Mods etc and [<DomainType>]
open Aardvark.Base.Rendering   // for render attribs such as cullMode
open Aardvark.UI.Primitives    // for primitives such as camera controller state
open NumericControlNs

[<DomainType>] // records can be marked as domaintypes
type VectorModel = { 
    x : NumericModel
    y : NumericModel
    z : NumericModel
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module VectorModel = 
    let initial = { x = NumericModel.initial; y = NumericModel.initial; z = NumericModel.initial }


