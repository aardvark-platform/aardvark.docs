namespace VectorControlNs

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open VectorControlNs

[<AutoOpen>]
module Mutable =

    
    
    type MVectorModel(__initial : VectorControlNs.VectorModel) =
        inherit obj()
        let mutable __current : Aardvark.Base.Incremental.IModRef<VectorControlNs.VectorModel> = Aardvark.Base.Incremental.EqModRef<VectorControlNs.VectorModel>(__initial) :> Aardvark.Base.Incremental.IModRef<VectorControlNs.VectorModel>
        let _x = NumericControlNs.Mutable.MNumericModel.Create(__initial.x)
        let _y = NumericControlNs.Mutable.MNumericModel.Create(__initial.y)
        let _z = NumericControlNs.Mutable.MNumericModel.Create(__initial.z)
        
        member x.x = _x
        member x.y = _y
        member x.z = _z
        
        member x.Current = __current :> IMod<_>
        member x.Update(v : VectorControlNs.VectorModel) =
            if not (System.Object.ReferenceEquals(__current.Value, v)) then
                __current.Value <- v
                
                NumericControlNs.Mutable.MNumericModel.Update(_x, v.x)
                NumericControlNs.Mutable.MNumericModel.Update(_y, v.y)
                NumericControlNs.Mutable.MNumericModel.Update(_z, v.z)
                
        
        static member Create(__initial : VectorControlNs.VectorModel) : MVectorModel = MVectorModel(__initial)
        static member Update(m : MVectorModel, v : VectorControlNs.VectorModel) = m.Update(v)
        
        override x.ToString() = __current.Value.ToString()
        member x.AsString = sprintf "%A" __current.Value
        interface IUpdatable<VectorControlNs.VectorModel> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module VectorModel =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let x =
                { new Lens<VectorControlNs.VectorModel, NumericControlNs.NumericModel>() with
                    override x.Get(r) = r.x
                    override x.Set(r,v) = { r with x = v }
                    override x.Update(r,f) = { r with x = f r.x }
                }
            let y =
                { new Lens<VectorControlNs.VectorModel, NumericControlNs.NumericModel>() with
                    override x.Get(r) = r.y
                    override x.Set(r,v) = { r with y = v }
                    override x.Update(r,f) = { r with y = f r.y }
                }
            let z =
                { new Lens<VectorControlNs.VectorModel, NumericControlNs.NumericModel>() with
                    override x.Get(r) = r.z
                    override x.Set(r,v) = { r with z = v }
                    override x.Update(r,f) = { r with z = f r.z }
                }
