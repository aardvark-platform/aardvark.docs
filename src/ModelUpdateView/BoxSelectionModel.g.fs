namespace BoxSelectionModel

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open BoxSelectionModel

[<AutoOpen>]
module Mutable =

    
    
    type MBoxSelectionDemoModel(__initial : BoxSelectionModel.BoxSelectionDemoModel) =
        inherit obj()
        let mutable __current : Aardvark.Base.Incremental.IModRef<BoxSelectionModel.BoxSelectionDemoModel> = Aardvark.Base.Incremental.EqModRef<BoxSelectionModel.BoxSelectionDemoModel>(__initial) :> Aardvark.Base.Incremental.IModRef<BoxSelectionModel.BoxSelectionDemoModel>
        let _camera = Aardvark.UI.Primitives.Mutable.MCameraControllerState.Create(__initial.camera)
        let _boxes = MList.Create(__initial.boxes, (fun v -> Boxes.Mutable.MVisibleBox.Create(v)), (fun (m,v) -> Boxes.Mutable.MVisibleBox.Update(m, v)), (fun v -> v))
        let _boxesSet = MSet.Create((fun (v : Boxes.VisibleBox) -> v.id :> obj), __initial.boxesSet, (fun v -> Boxes.Mutable.MVisibleBox.Create(v)), (fun (m,v) -> Boxes.Mutable.MVisibleBox.Update(m, v)), (fun v -> v))
        let _boxHovered = MOption.Create(__initial.boxHovered)
        let _selectedBoxes = MSet.Create(__initial.selectedBoxes)
        
        member x.camera = _camera
        member x.boxes = _boxes :> alist<_>
        member x.boxesSet = _boxesSet :> aset<_>
        member x.boxHovered = _boxHovered :> IMod<_>
        member x.selectedBoxes = _selectedBoxes :> aset<_>
        
        member x.Current = __current :> IMod<_>
        member x.Update(v : BoxSelectionModel.BoxSelectionDemoModel) =
            if not (System.Object.ReferenceEquals(__current.Value, v)) then
                __current.Value <- v
                
                Aardvark.UI.Primitives.Mutable.MCameraControllerState.Update(_camera, v.camera)
                MList.Update(_boxes, v.boxes)
                MSet.Update(_boxesSet, v.boxesSet)
                MOption.Update(_boxHovered, v.boxHovered)
                MSet.Update(_selectedBoxes, v.selectedBoxes)
                
        
        static member Create(__initial : BoxSelectionModel.BoxSelectionDemoModel) : MBoxSelectionDemoModel = MBoxSelectionDemoModel(__initial)
        static member Update(m : MBoxSelectionDemoModel, v : BoxSelectionModel.BoxSelectionDemoModel) = m.Update(v)
        
        override x.ToString() = __current.Value.ToString()
        member x.AsString = sprintf "%A" __current.Value
        interface IUpdatable<BoxSelectionModel.BoxSelectionDemoModel> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module BoxSelectionDemoModel =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let camera =
                { new Lens<BoxSelectionModel.BoxSelectionDemoModel, Aardvark.UI.Primitives.CameraControllerState>() with
                    override x.Get(r) = r.camera
                    override x.Set(r,v) = { r with camera = v }
                    override x.Update(r,f) = { r with camera = f r.camera }
                }
            let boxes =
                { new Lens<BoxSelectionModel.BoxSelectionDemoModel, Aardvark.Base.plist<Boxes.VisibleBox>>() with
                    override x.Get(r) = r.boxes
                    override x.Set(r,v) = { r with boxes = v }
                    override x.Update(r,f) = { r with boxes = f r.boxes }
                }
            let boxesSet =
                { new Lens<BoxSelectionModel.BoxSelectionDemoModel, Aardvark.Base.hset<Boxes.VisibleBox>>() with
                    override x.Get(r) = r.boxesSet
                    override x.Set(r,v) = { r with boxesSet = v }
                    override x.Update(r,f) = { r with boxesSet = f r.boxesSet }
                }
            let boxHovered =
                { new Lens<BoxSelectionModel.BoxSelectionDemoModel, Microsoft.FSharp.Core.Option<System.String>>() with
                    override x.Get(r) = r.boxHovered
                    override x.Set(r,v) = { r with boxHovered = v }
                    override x.Update(r,f) = { r with boxHovered = f r.boxHovered }
                }
            let selectedBoxes =
                { new Lens<BoxSelectionModel.BoxSelectionDemoModel, Aardvark.Base.hset<System.String>>() with
                    override x.Get(r) = r.selectedBoxes
                    override x.Set(r,v) = { r with selectedBoxes = v }
                    override x.Update(r,f) = { r with selectedBoxes = f r.selectedBoxes }
                }
