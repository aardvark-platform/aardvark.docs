namespace Aardvark.Docs.SierpinksiTetrahedron

open System
open System.Diagnostics

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.SceneGraph

type FoldingTetrahedron (t : IMod<double>) =

    // define folding tetrahedron
    let h = 0.5 * sqrt 3.0  // height of triangle
    let hTetrahedron = (sqrt 6.0) / 3.0 // height of tetrahedron
    let v0 = V3d(0, 0, 0)
    let v1 = V3d(1, 0, 0)
    let v2 = V3d(0.5, h, 0.0)
    let v3 = V3d(0.25, 0.5 * h, 0.0)
    let v4 = V3d(0.5, 0.0, 0.0)
    let v5 = V3d(0.75, 0.5 * h, 0.0)
    let s = V3d(0.5, h / 3.0, 0.0)

    let axis34 = (v3-v4).Normalized
    let axis45 = (v4-v5).Normalized
    let axis53 = (v5-v3).Normalized
    let axis02 = (v0-v2).Normalized
    let axis12 = (v1-v2).Normalized
    let axis42 = (v4-v2).Normalized

    let oneThird = 1.0 / 3.0
    let angleMax = Math.PI - acos oneThird
    let rotPointAxis (p : V3d) (axis : V3d) (angle : float) = M44d.Translation(p) * M44d.Rotation(axis, angle) * M44d.Translation(-p)
    let rotPointAxis' (p : V3d) (axis : V3d) (angle : float) = Trafo3d.Translation(-p) * Trafo3d.Rotation(axis, angle) * Trafo3d.Translation(p)
    let fold0 = rotPointAxis v3 axis34
    let fold1 = rotPointAxis v4 axis45
    let fold2 = rotPointAxis v5 axis53

    let ps =  [| s;v3;v4; s;v4;v5; s;v5;v3; v0;v4;v3; v1;v5;v4; v2;v3;v5 |]
    let ns = Array.create<V3d> 18 V3d.OOI
    let cs = 
        [| 
            C4b.White; C4b.White; C4b.White; // inner triangle
            C4b.White; C4b.White; C4b.White;
            C4b.White; C4b.White; C4b.White;
            
            C4b.Red; C4b.Red; C4b.Red;
            C4b.Green; C4b.Green; C4b.Green;
            C4b.Blue; C4b.Blue; C4b.Blue; 
        |]

    member x.ComputePositions angle =
        let s0 = (fold0 angle).TransformPos s
        let s1 = (fold1 angle).TransformPos s
        let s2 = (fold2 angle).TransformPos s
        [| s0;v3;v4; s1;v4;v5; s2;v5;v3; v0;v4;v3; v1;v5;v4; v2;v3;v5 |]

     member x.ComputeNormals angle =   
        let n0 = (fold0 angle).TransformDir V3d.OOI
        let n1 = (fold1 angle).TransformDir V3d.OOI
        let n2 = (fold2 angle).TransformDir V3d.OOI
        [| n0;n0;n0; n1;n1;n1; n2;n2;n2; V3d.OOI;V3d.OOI;V3d.OOI; V3d.OOI;V3d.OOI;V3d.OOI; V3d.OOI;V3d.OOI;V3d.OOI;|]

    member private x.Positions = t |> Mod.map (fun t ->
        let angle = t * angleMax
        x.ComputePositions angle
        )

    member private x.Normals = t |> Mod.map (fun t ->
        let angle = t * angleMax
        x.ComputeNormals angle
        )

    member private x.GetSg () =
        DrawCallInfo(
            FaceVertexCount = 18,
            InstanceCount = 1
            )
            |> Sg.render IndexedGeometryMode.TriangleList 
            |> Sg.vertexAttribute DefaultSemantic.Positions x.Positions
            |> Sg.vertexAttribute DefaultSemantic.Colors (Mod.constant cs)
            |> Sg.vertexAttribute DefaultSemantic.Normals x.Normals
            
    member private x.GetSg2 () =
        let t = x.GetSg ()
        [
            t |> Sg.transform (Trafo3d.Rotation(V3d.XAxis, (acos oneThird)))
            t |> Sg.transform (rotPointAxis' v0 axis02 (acos oneThird))
            t |> Sg.transform (rotPointAxis' v1 axis12 -(acos oneThird))
            t |> Sg.transform (rotPointAxis' v4 axis42 Math.PI)
        ]
        |> Sg.ofSeq 

    member private x.GetSg3 n sg =
        match n with
        | 0 -> sg 
        | _ -> 
            let s =  x.GetSg3 (n-1) sg |>  Sg.transform (Trafo3d.Scale(0.5))
            [
                s 
                s |> Sg.transform (Trafo3d.Translation(0.5, 0.0, 0.0))
                s |> Sg.transform (Trafo3d.Translation(0.25, h / 2.0, 0.0))
                s |> Sg.transform (Trafo3d.Translation(0.25, h / 6.0, hTetrahedron / 2.0))
            ]
            |> Sg.ofSeq 
        
    member x.SceneGraph n = x.GetSg3 n (x.GetSg2()) 