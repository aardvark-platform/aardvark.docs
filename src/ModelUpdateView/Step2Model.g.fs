namespace VectorControl

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open VectorControl

[<AutoOpen>]
module Mutable =

    
    
    type MVectorModel(__initial : VectorControl.VectorModel) =
        inherit obj()
        let mutable __current = __initial
        let _x = NumericControl.Mutable.MNumericModel.Create(__initial.x)
        let _y = NumericControl.Mutable.MNumericModel.Create(__initial.y)
        let _z = NumericControl.Mutable.MNumericModel.Create(__initial.z)
        
        member x.x = _x
        member x.y = _y
        member x.z = _z
        
        member x.Update(v : VectorControl.VectorModel) =
            if not (System.Object.ReferenceEquals(__current, v)) then
                __current <- v
                
                NumericControl.Mutable.MNumericModel.Update(_x, v.x)
                NumericControl.Mutable.MNumericModel.Update(_y, v.y)
                NumericControl.Mutable.MNumericModel.Update(_z, v.z)
                
        
        static member Create(__initial : VectorControl.VectorModel) : MVectorModel = MVectorModel(__initial)
        static member Update(m : MVectorModel, v : VectorControl.VectorModel) = m.Update(v)
        
        override x.ToString() = __current.ToString()
        member x.AsString = sprintf "%A" __current
        interface IUpdatable<VectorControl.VectorModel> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module VectorModel =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let x =
                { new Lens<VectorControl.VectorModel, NumericControl.NumericModel>() with
                    override x.Get(r) = r.x
                    override x.Set(r,v) = { r with x = v }
                    override x.Update(r,f) = { r with x = f r.x }
                }
            let y =
                { new Lens<VectorControl.VectorModel, NumericControl.NumericModel>() with
                    override x.Get(r) = r.y
                    override x.Set(r,v) = { r with y = v }
                    override x.Update(r,f) = { r with y = f r.y }
                }
            let z =
                { new Lens<VectorControl.VectorModel, NumericControl.NumericModel>() with
                    override x.Get(r) = r.z
                    override x.Set(r,v) = { r with z = v }
                    override x.Update(r,f) = { r with z = f r.z }
                }
