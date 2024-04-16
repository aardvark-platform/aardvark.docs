namespace Lenses



open Aardvark.Base             // for math such as V3d
open Aardvark.Base.Incremental // for Mods etc and [<DomainType>]
open Aardvark.Base.Rendering   // for render attribs such as cullMode
open Aardvark.UI.Primitives    // for primitives such as camera controller state

open LensesModel

module LensesStuff =        

    let update (a : A)(v:string) =
        { a with b = { a.b with c = { a.b.c with value = v }}}

    let updateValue(v : string) (c : C) =
        { c with value = v }

    let updateC (c : C) (b : B)=
        { b with c = c }

    let updateB (b : B) (a : A) =
        { a with b = b }


    let run =
        let init = A1.initial
        
        let a' = 
            { init with b = { init.b with c = { init.b.c with value = "hello world1" }}}

        
        let c' = init.b.c |> updateValue "hello world2"
        let b' = init.b |> updateC c' 
        let a' = init |> updateB b'

        let l = A.Lens.b |. B.Lens.c |. C.Lens.value
        let a' = l.Set(init, "hello world 3")
        
        a'
        //let m = update 


