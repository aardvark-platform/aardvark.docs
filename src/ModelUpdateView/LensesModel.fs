namespace LensesModel

open Aardvark.Base             // for math such as V3d
open Aardvark.Base.Incremental // for Mods etc and [<DomainType>]
open Aardvark.Base.Rendering   // for render attribs such as cullMode
open Aardvark.UI.Primitives    // for primitives such as camera controller state

[<DomainType>]
type C = {
  value : string  
}

[<DomainType>]
type B = {
  c : C
}

[<DomainType>]
type A = {
  b : B
}

module A1 =
    let initial = { 
        b = { 
            c = { 
                value = "initial" }}}




