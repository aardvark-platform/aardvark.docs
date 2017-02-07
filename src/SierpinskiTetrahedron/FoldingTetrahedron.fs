namespace Aardvark.Docs.SierpinksiTetrahedron

open System
open System.Diagnostics

open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.SceneGraph

type FoldingTetrahedron () =

    // define folding tetrahedron
    let h = 0.5 * sqrt 3.0  // height of triangle
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

    let oneThird = 1.0 / 3.0
    let angleMax = Math.PI - acos oneThird
    let rotPointAxis (p : V3d) (axis : V3d) (angle : float) = M44d.Translation(p) * M44d.Rotation(axis, angle) * M44d.Translation(-p)
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

    let positions = Mod.init ps
    let normals = Mod.init ns

    member x.ComputePositionsAndNormals angle =
        let s0 = (fold0 angle).TransformPos s
        let s1 = (fold1 angle).TransformPos s
        let s2 = (fold2 angle).TransformPos s
        let ps = [| s0;v3;v4; s1;v4;v5; s2;v5;v3; v0;v4;v3; v1;v5;v4; v2;v3;v5 |]

        let n0 = V3d.OON
        let n1 = (fold0 angle).TransformDir V3d.OOI
        let n2 = (fold1 angle).TransformDir V3d.OOI
        let n3 = (fold2 angle).TransformDir V3d.OOI
        let ns = [| n0;n0;n0; n1;n1;n1; n2;n2;n2; V3d.OOI;V3d.OOI;V3d.OOI; V3d.OOI;V3d.OOI;V3d.OOI; V3d.OOI;V3d.OOI;V3d.OOI;|]
        (ps, ns)

    member x.UpdatePositionsAndNormals angle = 
        let (ps, ns) = x.ComputePositionsAndNormals angle
        transact (fun () -> 
            Mod.change positions ps
            Mod.change normals ns
        )

    member x.Animation () = 
        async {
            do! Async.Sleep 5000
            let sw = Stopwatch()
            sw.Start()
            while sw.Elapsed.TotalSeconds <= 10.0 do
                let angle = sw.Elapsed.TotalSeconds / 10.0 * angleMax
                x.UpdatePositionsAndNormals angle
            x.UpdatePositionsAndNormals angleMax
        }

    member x.GetSg () =
        DrawCallInfo(
            FaceVertexCount = 18,
            InstanceCount = 1
            )
            |> Sg.render IndexedGeometryMode.TriangleList 
            |> Sg.vertexAttribute DefaultSemantic.Positions positions
            |> Sg.vertexAttribute DefaultSemantic.Colors (Mod.constant cs)
            |> Sg.vertexAttribute DefaultSemantic.Normals normals
            
