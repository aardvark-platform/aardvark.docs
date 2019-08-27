namespace BoxSelectionDemo

open System
open BoxSelectionModel
open Aardvark.UI.Primitives
open Aardvark.UI
open Aardvark.Base.Rendering
open Aardvark.Base.Incremental
open Aardvark.Base
open Aardvark.SceneGraph.``Sg Picking Extensions``

module Primitives =
    open Boxes
    
    let hoverColor = C4b.Blue
    let selectionColor = C4b.Red
    let colors = [new C4b(166,206,227); new C4b(178,223,138); new C4b(251,154,153); new C4b(253,191,111); new C4b(202,178,214)]
    let colorsBlue = [new C4b(241,238,246); new C4b(189,201,225); new C4b(116,169,207); new C4b(43,140,190); new C4b(4,90,141)]

    let mkNthBox i n = 
        let min = -V3d.One
        let max =  V3d.One

        let offset = 0.0 * (float n) * V3d.IOO

        new Box3d(min + V3d.IOO * 2.5 * (float i) - offset, max + V3d.IOO * 2.5 * (float i) - offset)

    let mkBoxes number =        
        [0..number-1] |> List.map (function x -> mkNthBox x number)

    let hoveredColor (model : MBoxSelectionDemoModel) (box : VisibleBox) =
        model.boxHovered 
        |> Mod.map (fun h -> 
            match h with
                | Some i -> if i = box.id then hoverColor else box.color
                | None -> box.color)

    let mkVisibleBox (color : C4b) (box : Box3d) : VisibleBox = 
        {
            id = Guid.NewGuid().ToString()
            geometry = box
            color = color           
        }