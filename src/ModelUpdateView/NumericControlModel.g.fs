namespace NumericControlNs

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open NumericControlNs

[<AutoOpen>]
module Mutable =

    
    
    type MNumericModel(__initial : NumericControlNs.NumericModel) =
        inherit obj()
        let mutable __current = __initial
        let _value = ResetMod.Create(__initial.value)
        
        member x.value = _value :> IMod<_>
        
        member x.Update(v : NumericControlNs.NumericModel) =
            if not (System.Object.ReferenceEquals(__current, v)) then
                __current <- v
                
                ResetMod.Update(_value,v.value)
                
        
        static member Create(__initial : NumericControlNs.NumericModel) : MNumericModel = MNumericModel(__initial)
        static member Update(m : MNumericModel, v : NumericControlNs.NumericModel) = m.Update(v)
        
        override x.ToString() = __current.ToString()
        member x.AsString = sprintf "%A" __current
        interface IUpdatable<NumericControlNs.NumericModel> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module NumericModel =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let value =
                { new Lens<NumericControlNs.NumericModel, Microsoft.FSharp.Core.int>() with
                    override x.Get(r) = r.value
                    override x.Set(r,v) = { r with value = v }
                    override x.Update(r,f) = { r with value = f r.value }
                }
