namespace SSAO

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open SSAO

[<AutoOpen>]
module Mutable =

    
    
    type MModel(__initial : SSAO.Model) =
        inherit obj()
        let mutable __current : Aardvark.Base.Incremental.IModRef<SSAO.Model> = Aardvark.Base.Incremental.EqModRef<SSAO.Model>(__initial) :> Aardvark.Base.Incremental.IModRef<SSAO.Model>
        let _scene = ResetMod.Create(__initial.scene)
        let _radius = ResetMod.Create(__initial.radius)
        let _threshold = ResetMod.Create(__initial.threshold)
        let _visualization = ResetMod.Create(__initial.visualization)
        let _scale = ResetMod.Create(__initial.scale)
        let _sigma = ResetMod.Create(__initial.sigma)
        let _sharpness = ResetMod.Create(__initial.sharpness)
        let _gamma = ResetMod.Create(__initial.gamma)
        let _samples = ResetMod.Create(__initial.samples)
        let _pendingLoad = ResetMod.Create(__initial.pendingLoad)
        let _status = MOption.Create(__initial.status)
        let _time = ResetMod.Create(__initial.time)
        
        member x.scene = _scene :> IMod<_>
        member x.radius = _radius :> IMod<_>
        member x.threshold = _threshold :> IMod<_>
        member x.visualization = _visualization :> IMod<_>
        member x.scale = _scale :> IMod<_>
        member x.sigma = _sigma :> IMod<_>
        member x.sharpness = _sharpness :> IMod<_>
        member x.gamma = _gamma :> IMod<_>
        member x.samples = _samples :> IMod<_>
        member x.pendingLoad = _pendingLoad :> IMod<_>
        member x.status = _status :> IMod<_>
        member x.time = _time :> IMod<_>
        
        member x.Current = __current :> IMod<_>
        member x.Update(v : SSAO.Model) =
            if not (System.Object.ReferenceEquals(__current.Value, v)) then
                __current.Value <- v
                
                ResetMod.Update(_scene,v.scene)
                ResetMod.Update(_radius,v.radius)
                ResetMod.Update(_threshold,v.threshold)
                ResetMod.Update(_visualization,v.visualization)
                ResetMod.Update(_scale,v.scale)
                ResetMod.Update(_sigma,v.sigma)
                ResetMod.Update(_sharpness,v.sharpness)
                ResetMod.Update(_gamma,v.gamma)
                ResetMod.Update(_samples,v.samples)
                ResetMod.Update(_pendingLoad,v.pendingLoad)
                MOption.Update(_status, v.status)
                ResetMod.Update(_time,v.time)
                
        
        static member Create(__initial : SSAO.Model) : MModel = MModel(__initial)
        static member Update(m : MModel, v : SSAO.Model) = m.Update(v)
        
        override x.ToString() = __current.Value.ToString()
        member x.AsString = sprintf "%A" __current.Value
        interface IUpdatable<SSAO.Model> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Model =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let scene =
                { new Lens<SSAO.Model, SSAO.Scene>() with
                    override x.Get(r) = r.scene
                    override x.Set(r,v) = { r with scene = v }
                    override x.Update(r,f) = { r with scene = f r.scene }
                }
            let radius =
                { new Lens<SSAO.Model, System.Double>() with
                    override x.Get(r) = r.radius
                    override x.Set(r,v) = { r with radius = v }
                    override x.Update(r,f) = { r with radius = f r.radius }
                }
            let threshold =
                { new Lens<SSAO.Model, System.Double>() with
                    override x.Get(r) = r.threshold
                    override x.Set(r,v) = { r with threshold = v }
                    override x.Update(r,f) = { r with threshold = f r.threshold }
                }
            let visualization =
                { new Lens<SSAO.Model, SSAO.SSAOVisualization>() with
                    override x.Get(r) = r.visualization
                    override x.Set(r,v) = { r with visualization = v }
                    override x.Update(r,f) = { r with visualization = f r.visualization }
                }
            let scale =
                { new Lens<SSAO.Model, System.Double>() with
                    override x.Get(r) = r.scale
                    override x.Set(r,v) = { r with scale = v }
                    override x.Update(r,f) = { r with scale = f r.scale }
                }
            let sigma =
                { new Lens<SSAO.Model, System.Double>() with
                    override x.Get(r) = r.sigma
                    override x.Set(r,v) = { r with sigma = v }
                    override x.Update(r,f) = { r with sigma = f r.sigma }
                }
            let sharpness =
                { new Lens<SSAO.Model, System.Double>() with
                    override x.Get(r) = r.sharpness
                    override x.Set(r,v) = { r with sharpness = v }
                    override x.Update(r,f) = { r with sharpness = f r.sharpness }
                }
            let gamma =
                { new Lens<SSAO.Model, System.Double>() with
                    override x.Get(r) = r.gamma
                    override x.Set(r,v) = { r with gamma = v }
                    override x.Update(r,f) = { r with gamma = f r.gamma }
                }
            let samples =
                { new Lens<SSAO.Model, System.Int32>() with
                    override x.Get(r) = r.samples
                    override x.Set(r,v) = { r with samples = v }
                    override x.Update(r,f) = { r with samples = f r.samples }
                }
            let pendingLoad =
                { new Lens<SSAO.Model, Microsoft.FSharp.Collections.List<System.String>>() with
                    override x.Get(r) = r.pendingLoad
                    override x.Set(r,v) = { r with pendingLoad = v }
                    override x.Update(r,f) = { r with pendingLoad = f r.pendingLoad }
                }
            let status =
                { new Lens<SSAO.Model, Microsoft.FSharp.Core.Option<System.String>>() with
                    override x.Get(r) = r.status
                    override x.Set(r,v) = { r with status = v }
                    override x.Update(r,f) = { r with status = f r.status }
                }
            let time =
                { new Lens<SSAO.Model, Aardvark.Base.MicroTime>() with
                    override x.Get(r) = r.time
                    override x.Set(r,v) = { r with time = v }
                    override x.Update(r,f) = { r with time = f r.time }
                }
