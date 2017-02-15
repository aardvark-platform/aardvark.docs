namespace Aardvark.Docs.SierpinksiTetrahedron

open System
open System.Diagnostics

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.SceneGraph

type FoldingTriangle (t : IMod<double>) = 
    // define folding tetrahedron
    let h = 0.5 * sqrt 3.0  // height of triangle
    let v0 = V3d(0, 0, 0)
    let v1 = V3d(1, 0, 0)
    let v2 = V3d(0.5, h, 0.0)
    let v3 = V3d(0.25, 0.5 * h, 0.0)
    let v4 = V3d(0.5, 0.0, 0.0)
    let v5 = V3d(0.75, 0.5 * h, 0.0)

    let axis34 = (v3-v4).Normalized
    let axis45 = (v4-v5).Normalized
    let axis53 = (v5-v3).Normalized

    let oneThird = 1.0 / 3.0
    let angleMax = Math.PI - acos oneThird
    let rotPointAxis (p : V3d) (axis : V3d) (angle : float) = M44d.Translation(p) * M44d.Rotation(axis, angle) * M44d.Translation(-p)
    let fold0 = rotPointAxis v3 axis34
    let fold1 = rotPointAxis v4 axis45
    let fold2 = rotPointAxis v5 axis53

    let ps =  [| v3;v5;v4; v0;v3;v4; v1;v4;v5; v2;v5;v3 |]
    let ns = Array.create<V3d> 12 V3d.OON
    let cs = [| C4b.White;C4b.White;C4b.White; C4b.Red;C4b.Red;C4b.Red; C4b.Green;C4b.Green;C4b.Green; C4b.Blue;C4b.Blue;C4b.Blue |]

    member x.ComputePositions angle =
        let v0' = (fold0 angle).TransformPos v0
        let v1' = (fold1 angle).TransformPos v1
        let v2' = (fold2 angle).TransformPos v2
        [| v3;v5;v4; v0';v3;v4; v1';v4;v5; v2';v5;v3 |]
        
    member x.ComputeNormals angle =
        let n0 = V3d.OON
        let n1 = (fold0 angle).TransformDir V3d.OON
        let n2 = (fold1 angle).TransformDir V3d.OON
        let n3 = (fold2 angle).TransformDir V3d.OON
        [| n0;n0;n0; n1;n1;n1; n2;n2;n2; n3;n3;n3 |]

    member private x.Positions = t |> Mod.map (fun t ->
        let angle = t * angleMax
        x.ComputePositions angle
        )

    member private x.Normals = t |> Mod.map (fun t ->
        let angle = t * angleMax
        x.ComputeNormals angle
        )

    member x.SceneGraph =
        DrawCallInfo(
            FaceVertexCount = 12,
            InstanceCount = 1
            )
            |> Sg.render IndexedGeometryMode.TriangleList 
            |> Sg.vertexAttribute DefaultSemantic.Positions x.Positions
            |> Sg.vertexAttribute DefaultSemantic.Colors (Mod.constant cs)
            |> Sg.vertexAttribute DefaultSemantic.Normals x.Normals
            |> Sg.scale 2.0