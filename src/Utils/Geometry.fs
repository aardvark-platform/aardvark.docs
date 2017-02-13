namespace Aardvark.Docs.Utils

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.Base.Rendering
open Aardvark.SceneGraph
open FShade
open System

module Geometry = 

    let private r = Random()
    
    let points n pointsize (bounds : Box2d) =
        let positions = Mod.constant [| for x in 1..n do yield bounds.Min.XYO + bounds.Size.XYO * V3d(r.NextDouble(), r.NextDouble(), 0.0) |]
        let colors = Mod.constant [| for x in 1..n do yield C4b(r.Next(256), r.Next(256), r.Next(256)) |]
        DrawCallInfo(FaceVertexCount = n, InstanceCount = 1)
            |> Sg.render IndexedGeometryMode.PointList 
            |> Sg.vertexAttribute DefaultSemantic.Positions positions
            |> Sg.vertexAttribute DefaultSemantic.Colors colors
            |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.pointSprite |> toEffect
               ]
            |> Sg.uniform "PointSize" (Mod.constant pointsize)


    let grid (bounds : Box2d) (color : C4b) =
        let lines =
            [
                [| for x in bounds.Min.X..0.5..bounds.Max.X do yield Line3d(V3d(x, bounds.Min.Y, 0.0), V3d(x, bounds.Max.Y, 0.0)) |]
                [| for y in bounds.Min.Y..0.5..bounds.Max.Y do yield Line3d(V3d(bounds.Min.X, y, 0.0), V3d(bounds.Max.X, y, 0.0)) |]
            ]
            |> Array.concat
        Sg.lines (Mod.constant color) (Mod.constant lines)
        |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.constantColor (C4f(color)) |> toEffect
                ThickLine.Effect
               ]
        |> Sg.uniform "LineWidth" (Mod.constant 0.5)