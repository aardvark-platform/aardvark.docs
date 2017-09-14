namespace NumericControlNs

open Aardvark.Base             // for math such as V3d
open Aardvark.Base.Incremental // for Mods etc and [<DomainType>]
open Aardvark.Base.Rendering   // for render attribs such as cullMode
open Aardvark.UI.Primitives    // for primitives such as camera controller state

[<DomainType>] // records can be marked as domaintypes
type NumericModel = { 
    value : float
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module NumericModel =
    let initial = { value = 1.0 }

