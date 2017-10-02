namespace Lenses



open Aardvark.Base             // for math such as V3d
open Aardvark.Base.Incremental // for Mods etc and [<DomainType>]
open Aardvark.Base.Rendering   // for render attribs such as cullMode
open Aardvark.UI.Primitives    // for primitives such as camera controller state

open LensesModel

module LensesStuff =

    let updateC (m : A) =
        { m with b = { m.b with c = { m.b.c with value = "helo world" }}}





