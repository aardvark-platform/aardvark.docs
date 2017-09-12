namespace Step1Model

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open Step1Model

[<AutoOpen>]
module Mutable =

    
    
    type MModel(__initial : Step1Model.Model) =
        inherit obj()
        let mutable __current = __initial
        let _value = ResetMod.Create(__initial.value)
        
        member x.value = _value :> IMod<_>
        
        member x.Update(v : Step1Model.Model) =
            if not (System.Object.ReferenceEquals(__current, v)) then
                __current <- v
                
                ResetMod.Update(_value,v.value)
                
        
        static member Create(__initial : Step1Model.Model) : MModel = MModel(__initial)
        static member Update(m : MModel, v : Step1Model.Model) = m.Update(v)
        
        override x.ToString() = __current.ToString()
        member x.AsString = sprintf "%A" __current
        interface IUpdatable<Step1Model.Model> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Model =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let value =
                { new Lens<Step1Model.Model, Microsoft.FSharp.Core.int>() with
                    override x.Get(r) = r.value
                    override x.Set(r,v) = { r with value = v }
                    override x.Update(r,f) = { r with value = f r.value }
                }
