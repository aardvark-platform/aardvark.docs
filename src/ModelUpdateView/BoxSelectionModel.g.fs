namespace BoxSelectionModel

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open BoxSelectionModel

[<AutoOpen>]
module Mutable =

    
    
    type MVisibleBox(__initial : BoxSelectionModel.VisibleBox) =
        inherit obj()
        let mutable __current = __initial
        let _geometry = ResetMod.Create(__initial.geometry)
        let _color = ResetMod.Create(__initial.color)
        let _id = ResetMod.Create(__initial.id)
        
        member x.geometry = _geometry :> IMod<_>
        member x.color = _color :> IMod<_>
        member x.id = _id :> IMod<_>
        
        member x.Update(v : BoxSelectionModel.VisibleBox) =
            if not (System.Object.ReferenceEquals(__current, v)) then
                __current <- v
                
                ResetMod.Update(_geometry,v.geometry)
                ResetMod.Update(_color,v.color)
                _id.Update(v.id)
                
        
        static member Create(__initial : BoxSelectionModel.VisibleBox) : MVisibleBox = MVisibleBox(__initial)
        static member Update(m : MVisibleBox, v : BoxSelectionModel.VisibleBox) = m.Update(v)
        
        override x.ToString() = __current.ToString()
        member x.AsString = sprintf "%A" __current
        interface IUpdatable<BoxSelectionModel.VisibleBox> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module VisibleBox =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let geometry =
                { new Lens<BoxSelectionModel.VisibleBox, Aardvark.Base.Box3d>() with
                    override x.Get(r) = r.geometry
                    override x.Set(r,v) = { r with geometry = v }
                    override x.Update(r,f) = { r with geometry = f r.geometry }
                }
            let color =
                { new Lens<BoxSelectionModel.VisibleBox, Aardvark.Base.C4b>() with
                    override x.Get(r) = r.color
                    override x.Set(r,v) = { r with color = v }
                    override x.Update(r,f) = { r with color = f r.color }
                }
            let id =
                { new Lens<BoxSelectionModel.VisibleBox, Microsoft.FSharp.Core.string>() with
                    override x.Get(r) = r.id
                    override x.Set(r,v) = { r with id = v }
                    override x.Update(r,f) = { r with id = f r.id }
                }
    
    
    type MBoxSelectionDemoModel(__initial : BoxSelectionModel.BoxSelectionDemoModel) =
        inherit obj()
        let mutable __current = __initial
        let _camera = Aardvark.UI.Primitives.Mutable.MCameraControllerState.Create(__initial.camera)
        let _boxes = MList.Create(__initial.boxes, (fun v -> MVisibleBox.Create(v)), (fun (m,v) -> MVisibleBox.Update(m, v)), (fun v -> v))
        let _boxesSet = MSet.Create((fun (v : BoxSelectionModel.VisibleBox) -> v.id :> obj), __initial.boxesSet, (fun v -> MVisibleBox.Create(v)), (fun (m,v) -> MVisibleBox.Update(m, v)), (fun v -> v))
        let _boxHovered = MOption.Create(__initial.boxHovered)
        let _selectedBoxes = MSet.Create(__initial.selectedBoxes)
        
        member x.camera = _camera
        member x.boxes = _boxes :> alist<_>
        member x.boxesSet = _boxesSet :> aset<_>
        member x.boxHovered = _boxHovered :> IMod<_>
        member x.selectedBoxes = _selectedBoxes :> aset<_>
        
        member x.Update(v : BoxSelectionModel.BoxSelectionDemoModel) =
            if not (System.Object.ReferenceEquals(__current, v)) then
                __current <- v
                
                Aardvark.UI.Primitives.Mutable.MCameraControllerState.Update(_camera, v.camera)
                MList.Update(_boxes, v.boxes)
                MSet.Update(_boxesSet, v.boxesSet)
                MOption.Update(_boxHovered, v.boxHovered)
                MSet.Update(_selectedBoxes, v.selectedBoxes)
                
        
        static member Create(__initial : BoxSelectionModel.BoxSelectionDemoModel) : MBoxSelectionDemoModel = MBoxSelectionDemoModel(__initial)
        static member Update(m : MBoxSelectionDemoModel, v : BoxSelectionModel.BoxSelectionDemoModel) = m.Update(v)
        
        override x.ToString() = __current.ToString()
        member x.AsString = sprintf "%A" __current
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
                { new Lens<BoxSelectionModel.BoxSelectionDemoModel, Aardvark.Base.plist<BoxSelectionModel.VisibleBox>>() with
                    override x.Get(r) = r.boxes
                    override x.Set(r,v) = { r with boxes = v }
                    override x.Update(r,f) = { r with boxes = f r.boxes }
                }
            let boxesSet =
                { new Lens<BoxSelectionModel.BoxSelectionDemoModel, Aardvark.Base.hset<BoxSelectionModel.VisibleBox>>() with
                    override x.Get(r) = r.boxesSet
                    override x.Set(r,v) = { r with boxesSet = v }
                    override x.Update(r,f) = { r with boxesSet = f r.boxesSet }
                }
            let boxHovered =
                { new Lens<BoxSelectionModel.BoxSelectionDemoModel, Microsoft.FSharp.Core.option<Microsoft.FSharp.Core.string>>() with
                    override x.Get(r) = r.boxHovered
                    override x.Set(r,v) = { r with boxHovered = v }
                    override x.Update(r,f) = { r with boxHovered = f r.boxHovered }
                }
            let selectedBoxes =
                { new Lens<BoxSelectionModel.BoxSelectionDemoModel, Aardvark.Base.hset<Microsoft.FSharp.Core.string>>() with
                    override x.Get(r) = r.selectedBoxes
                    override x.Set(r,v) = { r with selectedBoxes = v }
                    override x.Update(r,f) = { r with selectedBoxes = f r.selectedBoxes }
                }
