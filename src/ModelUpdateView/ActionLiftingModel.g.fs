namespace ActionLiftingModel

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open ActionLiftingModel

[<AutoOpen>]
module Mutable =

    
    
    type MBoxesModel(__initial : ActionLiftingModel.BoxesModel) =
        inherit obj()
        let mutable __current : Aardvark.Base.Incremental.IModRef<ActionLiftingModel.BoxesModel> = Aardvark.Base.Incremental.EqModRef<ActionLiftingModel.BoxesModel>(__initial) :> Aardvark.Base.Incremental.IModRef<ActionLiftingModel.BoxesModel>
        let _boxes = MList.Create(__initial.boxes, (fun v -> Boxes.Mutable.MVisibleBox.Create(v)), (fun (m,v) -> Boxes.Mutable.MVisibleBox.Update(m, v)), (fun v -> v))
        let _boxesSet = MSet.Create((fun (v : Boxes.VisibleBox) -> v.id :> obj), __initial.boxesSet, (fun v -> Boxes.Mutable.MVisibleBox.Create(v)), (fun (m,v) -> Boxes.Mutable.MVisibleBox.Update(m, v)), (fun v -> v))
        
        member x.boxes = _boxes :> alist<_>
        member x.boxesSet = _boxesSet :> aset<_>
        
        member x.Current = __current :> IMod<_>
        member x.Update(v : ActionLiftingModel.BoxesModel) =
            if not (System.Object.ReferenceEquals(__current.Value, v)) then
                __current.Value <- v
                
                MList.Update(_boxes, v.boxes)
                MSet.Update(_boxesSet, v.boxesSet)
                
        
        static member Create(__initial : ActionLiftingModel.BoxesModel) : MBoxesModel = MBoxesModel(__initial)
        static member Update(m : MBoxesModel, v : ActionLiftingModel.BoxesModel) = m.Update(v)
        
        override x.ToString() = __current.Value.ToString()
        member x.AsString = sprintf "%A" __current.Value
        interface IUpdatable<ActionLiftingModel.BoxesModel> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module BoxesModel =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let boxes =
                { new Lens<ActionLiftingModel.BoxesModel, Aardvark.Base.plist<Boxes.VisibleBox>>() with
                    override x.Get(r) = r.boxes
                    override x.Set(r,v) = { r with boxes = v }
                    override x.Update(r,f) = { r with boxes = f r.boxes }
                }
            let boxesSet =
                { new Lens<ActionLiftingModel.BoxesModel, Aardvark.Base.hset<Boxes.VisibleBox>>() with
                    override x.Get(r) = r.boxesSet
                    override x.Set(r,v) = { r with boxesSet = v }
                    override x.Update(r,f) = { r with boxesSet = f r.boxesSet }
                }
    
    
    type MActionLiftingModel(__initial : ActionLiftingModel.ActionLiftingModel) =
        inherit obj()
        let mutable __current : Aardvark.Base.Incremental.IModRef<ActionLiftingModel.ActionLiftingModel> = Aardvark.Base.Incremental.EqModRef<ActionLiftingModel.ActionLiftingModel>(__initial) :> Aardvark.Base.Incremental.IModRef<ActionLiftingModel.ActionLiftingModel>
        let _camera = Aardvark.UI.Primitives.Mutable.MCameraControllerState.Create(__initial.camera)
        let _boxes = MBoxesModel.Create(__initial.boxes)
        let _boxHovered = MOption.Create(__initial.boxHovered)
        let _selectedBoxes = MSet.Create(__initial.selectedBoxes)
        let _colors = ResetMod.Create(__initial.colors)
        
        member x.camera = _camera
        member x.boxes = _boxes
        member x.boxHovered = _boxHovered :> IMod<_>
        member x.selectedBoxes = _selectedBoxes :> aset<_>
        member x.colors = _colors :> IMod<_>
        
        member x.Current = __current :> IMod<_>
        member x.Update(v : ActionLiftingModel.ActionLiftingModel) =
            if not (System.Object.ReferenceEquals(__current.Value, v)) then
                __current.Value <- v
                
                Aardvark.UI.Primitives.Mutable.MCameraControllerState.Update(_camera, v.camera)
                MBoxesModel.Update(_boxes, v.boxes)
                MOption.Update(_boxHovered, v.boxHovered)
                MSet.Update(_selectedBoxes, v.selectedBoxes)
                ResetMod.Update(_colors,v.colors)
                
        
        static member Create(__initial : ActionLiftingModel.ActionLiftingModel) : MActionLiftingModel = MActionLiftingModel(__initial)
        static member Update(m : MActionLiftingModel, v : ActionLiftingModel.ActionLiftingModel) = m.Update(v)
        
        override x.ToString() = __current.Value.ToString()
        member x.AsString = sprintf "%A" __current.Value
        interface IUpdatable<ActionLiftingModel.ActionLiftingModel> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module ActionLiftingModel =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let camera =
                { new Lens<ActionLiftingModel.ActionLiftingModel, Aardvark.UI.Primitives.CameraControllerState>() with
                    override x.Get(r) = r.camera
                    override x.Set(r,v) = { r with camera = v }
                    override x.Update(r,f) = { r with camera = f r.camera }
                }
            let boxes =
                { new Lens<ActionLiftingModel.ActionLiftingModel, ActionLiftingModel.BoxesModel>() with
                    override x.Get(r) = r.boxes
                    override x.Set(r,v) = { r with boxes = v }
                    override x.Update(r,f) = { r with boxes = f r.boxes }
                }
            let boxHovered =
                { new Lens<ActionLiftingModel.ActionLiftingModel, Microsoft.FSharp.Core.Option<System.String>>() with
                    override x.Get(r) = r.boxHovered
                    override x.Set(r,v) = { r with boxHovered = v }
                    override x.Update(r,f) = { r with boxHovered = f r.boxHovered }
                }
            let selectedBoxes =
                { new Lens<ActionLiftingModel.ActionLiftingModel, Aardvark.Base.hset<System.String>>() with
                    override x.Get(r) = r.selectedBoxes
                    override x.Set(r,v) = { r with selectedBoxes = v }
                    override x.Update(r,f) = { r with selectedBoxes = f r.selectedBoxes }
                }
            let colors =
                { new Lens<ActionLiftingModel.ActionLiftingModel, Microsoft.FSharp.Collections.List<Aardvark.Base.C4b>>() with
                    override x.Get(r) = r.colors
                    override x.Set(r,v) = { r with colors = v }
                    override x.Update(r,f) = { r with colors = f r.colors }
                }
