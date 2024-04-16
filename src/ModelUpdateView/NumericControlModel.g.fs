namespace NumericControlNs

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open NumericControlNs

[<AutoOpen>]
module Mutable =

    
    
    type MNumericModel(__initial : NumericControlNs.NumericModel) =
        inherit obj()
        let mutable __current : Aardvark.Base.Incremental.IModRef<NumericControlNs.NumericModel> = Aardvark.Base.Incremental.EqModRef<NumericControlNs.NumericModel>(__initial) :> Aardvark.Base.Incremental.IModRef<NumericControlNs.NumericModel>
        let _value = ResetMod.Create(__initial.value)
        
        member x.value = _value :> IMod<_>
        
        member x.Current = __current :> IMod<_>
        member x.Update(v : NumericControlNs.NumericModel) =
            if not (System.Object.ReferenceEquals(__current.Value, v)) then
                __current.Value <- v
                
                ResetMod.Update(_value,v.value)
                
        
        static member Create(__initial : NumericControlNs.NumericModel) : MNumericModel = MNumericModel(__initial)
        static member Update(m : MNumericModel, v : NumericControlNs.NumericModel) = m.Update(v)
        
        override x.ToString() = __current.Value.ToString()
        member x.AsString = sprintf "%A" __current.Value
        interface IUpdatable<NumericControlNs.NumericModel> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module NumericModel =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let value =
                { new Lens<NumericControlNs.NumericModel, System.Double>() with
                    override x.Get(r) = r.value
                    override x.Set(r,v) = { r with value = v }
                    override x.Update(r,f) = { r with value = f r.value }
                }
