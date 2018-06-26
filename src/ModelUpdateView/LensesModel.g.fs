namespace LensesModel

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open LensesModel

[<AutoOpen>]
module Mutable =

    
    
    type MC(__initial : LensesModel.C) =
        inherit obj()
        let mutable __current : Aardvark.Base.Incremental.IModRef<LensesModel.C> = Aardvark.Base.Incremental.EqModRef<LensesModel.C>(__initial) :> Aardvark.Base.Incremental.IModRef<LensesModel.C>
        let _value = ResetMod.Create(__initial.value)
        
        member x.value = _value :> IMod<_>
        
        member x.Current = __current :> IMod<_>
        member x.Update(v : LensesModel.C) =
            if not (System.Object.ReferenceEquals(__current.Value, v)) then
                __current.Value <- v
                
                ResetMod.Update(_value,v.value)
                
        
        static member Create(__initial : LensesModel.C) : MC = MC(__initial)
        static member Update(m : MC, v : LensesModel.C) = m.Update(v)
        
        override x.ToString() = __current.Value.ToString()
        member x.AsString = sprintf "%A" __current.Value
        interface IUpdatable<LensesModel.C> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module C =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let value =
                { new Lens<LensesModel.C, System.String>() with
                    override x.Get(r) = r.value
                    override x.Set(r,v) = { r with value = v }
                    override x.Update(r,f) = { r with value = f r.value }
                }
    
    
    type MB(__initial : LensesModel.B) =
        inherit obj()
        let mutable __current : Aardvark.Base.Incremental.IModRef<LensesModel.B> = Aardvark.Base.Incremental.EqModRef<LensesModel.B>(__initial) :> Aardvark.Base.Incremental.IModRef<LensesModel.B>
        let _c = MC.Create(__initial.c)
        
        member x.c = _c
        
        member x.Current = __current :> IMod<_>
        member x.Update(v : LensesModel.B) =
            if not (System.Object.ReferenceEquals(__current.Value, v)) then
                __current.Value <- v
                
                MC.Update(_c, v.c)
                
        
        static member Create(__initial : LensesModel.B) : MB = MB(__initial)
        static member Update(m : MB, v : LensesModel.B) = m.Update(v)
        
        override x.ToString() = __current.Value.ToString()
        member x.AsString = sprintf "%A" __current.Value
        interface IUpdatable<LensesModel.B> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module B =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let c =
                { new Lens<LensesModel.B, LensesModel.C>() with
                    override x.Get(r) = r.c
                    override x.Set(r,v) = { r with c = v }
                    override x.Update(r,f) = { r with c = f r.c }
                }
    
    
    type MA(__initial : LensesModel.A) =
        inherit obj()
        let mutable __current : Aardvark.Base.Incremental.IModRef<LensesModel.A> = Aardvark.Base.Incremental.EqModRef<LensesModel.A>(__initial) :> Aardvark.Base.Incremental.IModRef<LensesModel.A>
        let _b = MB.Create(__initial.b)
        
        member x.b = _b
        
        member x.Current = __current :> IMod<_>
        member x.Update(v : LensesModel.A) =
            if not (System.Object.ReferenceEquals(__current.Value, v)) then
                __current.Value <- v
                
                MB.Update(_b, v.b)
                
        
        static member Create(__initial : LensesModel.A) : MA = MA(__initial)
        static member Update(m : MA, v : LensesModel.A) = m.Update(v)
        
        override x.ToString() = __current.Value.ToString()
        member x.AsString = sprintf "%A" __current.Value
        interface IUpdatable<LensesModel.A> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module A =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let b =
                { new Lens<LensesModel.A, LensesModel.B>() with
                    override x.Get(r) = r.b
                    override x.Set(r,v) = { r with b = v }
                    override x.Update(r,f) = { r with b = f r.b }
                }
