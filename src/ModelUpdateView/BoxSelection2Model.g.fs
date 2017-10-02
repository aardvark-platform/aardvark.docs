namespace BoxSelection2Model

open System
open Aardvark.Base
open Aardvark.Base.Incremental
open BoxSelection2Model

[<AutoOpen>]
module Mutable =

    
    
    type MBoxes(__initial : BoxSelection2Model.Boxes) =
        inherit obj()
        let mutable __current = __initial
        let _boxes = MList.Create(__initial.boxes, (fun v -> BoxSelectionModel.Mutable.MVisibleBox.Create(v)), (fun (m,v) -> BoxSelectionModel.Mutable.MVisibleBox.Update(m, v)), (fun v -> v))
        let _boxesSet = MSet.Create((fun (v : BoxSelectionModel.VisibleBox) -> v.id :> obj), __initial.boxesSet, (fun v -> BoxSelectionModel.Mutable.MVisibleBox.Create(v)), (fun (m,v) -> BoxSelectionModel.Mutable.MVisibleBox.Update(m, v)), (fun v -> v))
        
        member x.boxes = _boxes :> alist<_>
        member x.boxesSet = _boxesSet :> aset<_>
        
        member x.Update(v : BoxSelection2Model.Boxes) =
            if not (System.Object.ReferenceEquals(__current, v)) then
                __current <- v
                
                MList.Update(_boxes, v.boxes)
                MSet.Update(_boxesSet, v.boxesSet)
                
        
        static member Create(__initial : BoxSelection2Model.Boxes) : MBoxes = MBoxes(__initial)
        static member Update(m : MBoxes, v : BoxSelection2Model.Boxes) = m.Update(v)
        
        override x.ToString() = __current.ToString()
        member x.AsString = sprintf "%A" __current
        interface IUpdatable<BoxSelection2Model.Boxes> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module Boxes =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let boxes =
                { new Lens<BoxSelection2Model.Boxes, Aardvark.Base.plist<BoxSelectionModel.VisibleBox>>() with
                    override x.Get(r) = r.boxes
                    override x.Set(r,v) = { r with boxes = v }
                    override x.Update(r,f) = { r with boxes = f r.boxes }
                }
            let boxesSet =
                { new Lens<BoxSelection2Model.Boxes, Aardvark.Base.hset<BoxSelectionModel.VisibleBox>>() with
                    override x.Get(r) = r.boxesSet
                    override x.Set(r,v) = { r with boxesSet = v }
                    override x.Update(r,f) = { r with boxesSet = f r.boxesSet }
                }
    
    
    type MBoxSelection2Model(__initial : BoxSelection2Model.BoxSelection2Model) =
        inherit obj()
        let mutable __current = __initial
        let _camera = Aardvark.UI.Primitives.Mutable.MCameraControllerState.Create(__initial.camera)
        let _boxes = MBoxes.Create(__initial.boxes)
        let _boxHovered = MOption.Create(__initial.boxHovered)
        let _selectedBoxes = MSet.Create(__initial.selectedBoxes)
        
        member x.camera = _camera
        member x.boxes = _boxes
        member x.boxHovered = _boxHovered :> IMod<_>
        member x.selectedBoxes = _selectedBoxes :> aset<_>
        
        member x.Update(v : BoxSelection2Model.BoxSelection2Model) =
            if not (System.Object.ReferenceEquals(__current, v)) then
                __current <- v
                
                Aardvark.UI.Primitives.Mutable.MCameraControllerState.Update(_camera, v.camera)
                MBoxes.Update(_boxes, v.boxes)
                MOption.Update(_boxHovered, v.boxHovered)
                MSet.Update(_selectedBoxes, v.selectedBoxes)
                
        
        static member Create(__initial : BoxSelection2Model.BoxSelection2Model) : MBoxSelection2Model = MBoxSelection2Model(__initial)
        static member Update(m : MBoxSelection2Model, v : BoxSelection2Model.BoxSelection2Model) = m.Update(v)
        
        override x.ToString() = __current.ToString()
        member x.AsString = sprintf "%A" __current
        interface IUpdatable<BoxSelection2Model.BoxSelection2Model> with
            member x.Update v = x.Update v
    
    
    
    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module BoxSelection2Model =
        [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
        module Lens =
            let camera =
                { new Lens<BoxSelection2Model.BoxSelection2Model, Aardvark.UI.Primitives.CameraControllerState>() with
                    override x.Get(r) = r.camera
                    override x.Set(r,v) = { r with camera = v }
                    override x.Update(r,f) = { r with camera = f r.camera }
                }
            let boxes =
                { new Lens<BoxSelection2Model.BoxSelection2Model, BoxSelection2Model.Boxes>() with
                    override x.Get(r) = r.boxes
                    override x.Set(r,v) = { r with boxes = v }
                    override x.Update(r,f) = { r with boxes = f r.boxes }
                }
            let boxHovered =
                { new Lens<BoxSelection2Model.BoxSelection2Model, Microsoft.FSharp.Core.option<Microsoft.FSharp.Core.string>>() with
                    override x.Get(r) = r.boxHovered
                    override x.Set(r,v) = { r with boxHovered = v }
                    override x.Update(r,f) = { r with boxHovered = f r.boxHovered }
                }
            let selectedBoxes =
                { new Lens<BoxSelection2Model.BoxSelection2Model, Aardvark.Base.hset<Microsoft.FSharp.Core.string>>() with
                    override x.Get(r) = r.selectedBoxes
                    override x.Set(r,v) = { r with selectedBoxes = v }
                    override x.Update(r,f) = { r with selectedBoxes = f r.selectedBoxes }
                }
