open Aardvark.Application
open Aardvark.Application.Slim
open Aardvark.Base
open Aardvark.Rendering
open Aardvark.SceneGraph
open Aardvark.SceneGraph.Assimp
open FSharp.Data.Adaptive
open Aardvark.Data.Wavefront

[<EntryPoint>]
let main argv =
    // initialize runtime system
    Aardvark.Init()

    // create simple render window
    use app = new OpenGlApplication()
    let win = app.CreateGameWindow(8)
    win.Title <- "Background Color (aardvark.docs)"

    // load model
    //let modelObj = Loader.Assimp.load (Path.combine [__SOURCE_DIRECTORY__; ".."; ".."; "data"; "aardvark"; "aardvark.obj"])
    //Loader.Assimp.load @"C:\Users\aszabo\Downloads\OneDrive_1_13.8.2025\RockFace_Orig.obj"
    let path = @"C:\Users\aszabo\Downloads\OneDrive_1_13.8.2025\RockFace_Orig.obj"
    let wobj = ObjParser.Load(path, useDoublePrecision = true)
    let textureFilename =
        match wobj.Materials |> Seq.tryFind (fun v -> v.Name = "RockFace_Orig") with
        | Some v -> v.[WavefrontMaterial.Property.DiffuseColorMap] :?> string
        | None -> failwith "bad"
    
    let verts = wobj.Vertices.ToArrayOfT<V4d>() |> Array.map _.XYZ
    let bounds = Box3d verts
    let centerTrafo = Trafo3d.Translation(-bounds.Center) * Trafo3d.Scale(2.0/bounds.Size.NormMax)
    
    let positions = verts |> Array.map (fun v -> centerTrafo.Forward.TransformPos v) |> Array.map V3f
    let texCoords = wobj.TextureCoordinates |> CSharpList.toArray |> Array.map (fun v -> V2f(v.X,1.0f-v.Y))
    let normals = wobj.Normals |> CSharpList.toArray
    
    let ps,ns,tcs =
        let ps = ResizeArray()
        let ns = ResizeArray()
        let tcs = ResizeArray()
        for set in wobj.FaceSets do
            let iPos = set.VertexIndices
            let iNormals =
                if isNull set.NormalIndices then iPos
                else set.NormalIndices
            let iTcs = 
                if isNull set.TexCoordIndices then iPos
                else set.TexCoordIndices
            for ti in 0 .. set.ElementCount - 1 do
                let fi = set.FirstIndices.[ti]
                let cnt = set.FirstIndices.[ti+1] - fi

                if cnt = 3 then
                    tcs.Add texCoords.[iTcs.[fi + 0]]
                    tcs.Add texCoords.[iTcs.[fi + 1]]
                    tcs.Add texCoords.[iTcs.[fi + 2]]

                    ps.Add positions.[iPos.[fi + 0]]
                    ps.Add positions.[iPos.[fi + 1]]
                    ps.Add positions.[iPos.[fi + 2]]

                    // ns.Add normals.[iNormals.[fi + 0]]
                    // ns.Add normals.[iNormals.[fi + 1]]
                    // ns.Add normals.[iNormals.[fi + 2]]
        ps.ToArray(), ns.ToArray(), tcs.ToArray()
    

    // view, projection and default camera controllers
    let initialView =
        CameraView.lookAt bounds.Min bounds.Center V3d.OOI
    let view = initialView |> DefaultCameraController.control win.Mouse win.Keyboard win.Time
    let proj = win.Sizes |> AVal.map (fun s -> Frustum.perspective 60.0 0.1 100.0 (float s.X / float s.Y))
    
    //// define scene

    let model =
        Sg.draw IndexedGeometryMode.TriangleList
        |> Sg.vertexAttribute' DefaultSemantic.Positions ps
        |> Sg.vertexAttribute' DefaultSemantic.DiffuseColorCoordinates tcs
        //|> Sg.vertexAttribute' DefaultSemantic.Normals ns
        |> Sg.transform centerTrafo.Inverse
        |> Sg.fileTexture DefaultSemantic.DiffuseColorTexture textureFilename true
        |> Sg.effect [
                DefaultSurfaces.stableTrafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.diffuseTexture |> toEffect
                //DefaultSurfaces.simpleLighting |> toEffect
               ]

    let box =
        Sg.box' C4b.Red (Box3d(0.0, 0.0, 0.0, 1.0, 1.0, 0.01))
        |> Sg.effect [
                DefaultSurfaces.trafo |> toEffect
                DefaultSurfaces.vertexColor |> toEffect
                DefaultSurfaces.simpleLighting |> toEffect
               ]

    let sg =
        [model; box]
        |> Sg.ofSeq
        |> Sg.viewTrafo (view |> AVal.map CameraView.viewTrafo)
        |> Sg.projTrafo (proj |> AVal.map Frustum.projTrafo)

    // background color
    let bgColor = AVal.init C4f.Gray

    // specify render task(s)
    use task =
        [
            app.Runtime.CompileClear(win.FramebufferSignature, bgColor)
            app.Runtime.CompileRender(win.FramebufferSignature, sg)
        ]
        |> RenderTask.ofList

    // start
    win.RenderTask <- task
    win.Run()
    0
