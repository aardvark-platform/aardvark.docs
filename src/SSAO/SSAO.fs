namespace SSAO

open System
open System.IO
open Aardvark.Base
open Aardvark.Base.Incremental
open Aardvark.Base.Incremental.Operators
open Aardvark.Base.Rendering
open System.Runtime.CompilerServices
open Aardvark.SceneGraph
open Aardvark.Rendering.Text

type SSAOVisualization =
    | Depth     = 0
    | Normal    = 1
    | Color     = 2
    | Ambient   = 3
    | Composed  = 4

module SSAOVisualization =
    let next (v : SSAOVisualization) =
        match v with
            | SSAOVisualization.Depth -> SSAOVisualization.Normal
            | SSAOVisualization.Normal -> SSAOVisualization.Color
            | SSAOVisualization.Color -> SSAOVisualization.Composed
            | SSAOVisualization.Composed -> SSAOVisualization.Ambient
            | _ -> SSAOVisualization.Depth
        
    let prev (v : SSAOVisualization) =
        match v with
            | SSAOVisualization.Depth -> SSAOVisualization.Ambient
            | SSAOVisualization.Composed -> SSAOVisualization.Color
            | SSAOVisualization.Color -> SSAOVisualization.Normal
            | SSAOVisualization.Normal -> SSAOVisualization.Depth
            | _ -> SSAOVisualization.Ambient

type SSAOConfig =
    {
        radius          : IMod<float>
        threshold       : IMod<float>
        visualization   : IMod<SSAOVisualization>
        scale           : IMod<float>
        sigma           : IMod<float>
        sharpness       : IMod<float>
        gamma           : IMod<float>
        samples           : IMod<int>
    }

    static member Default =
        {
            radius = ~~0.05
            threshold = ~~0.1
            visualization = ~~SSAOVisualization.Composed
            scale = ~~0.5
            sigma = ~~3.0
            sharpness = ~~1.0
            gamma = ~~2.2
            samples = ~~32
        }


module SSAO =
    
    module Semantic =
        let Ambient = Symbol.Create "Ambient"

    module Shader =
        open FShade

        let normal =
            sampler2d {
                texture uniform?Normals
                addressU WrapMode.Clamp
                addressV WrapMode.Clamp
                filter Filter.MinMagLinear
            }
            
        let color =
            sampler2d {
                texture uniform?DiffuseColorTexture
                addressU WrapMode.Clamp
                addressV WrapMode.Clamp
                filter Filter.MinMagLinear
            }

        let depth =
            sampler2d {
                texture uniform?Depth
                addressU WrapMode.Clamp
                addressV WrapMode.Clamp
                filter Filter.MinMagLinear
            }

        let depthCmp =
            sampler2dShadow {
                texture uniform?Depth
                addressU WrapMode.Clamp
                addressV WrapMode.Clamp
                comparison ComparisonFunction.Greater
                filter Filter.MinMagMipLinear
            }

        let ambient =
            sampler2d {
                texture uniform?Ambient
                addressU WrapMode.Clamp
                addressV WrapMode.Clamp
                filter Filter.MinMagLinear
            }
            
        let random =
            sampler2d {
                texture uniform?Random
                addressU WrapMode.Wrap
                addressV WrapMode.Wrap
                filter Filter.MinMagPoint
            }

        type UniformScope with
            member x.Visualization : SSAOVisualization = uniform?Visualization
            member x.Radius : float = uniform?Radius
            member x.Threshold : float = uniform?Threshold
            member x.Sigma : float = uniform?Sigma
            member x.Sharpness : float = uniform?Sharpness
            member x.Gamma : float = uniform?Gamma
            member x.Samples : int = uniform?Samples

        let sampleDirections =
            let rand = RandomSystem()
            let arr = 
                Array.init 512 (fun _ ->
                    let phi = rand.UniformDouble() * Constant.PiTimesTwo
                    let theta = rand.UniformDouble() * (Constant.PiHalf - 10.0 * Constant.RadiansPerDegree)
                    V3d(
                        cos phi * sin theta,
                        sin phi * sin theta,
                        cos theta
                    )
                )
            arr |> Array.map (fun v -> v * (0.5 + 0.5 * rand.UniformDouble())) //(0.02 + rand.UniformDouble() * 0.03))

        [<ReflectedDefinition>]
        let project (vp : V3d) =
            let mutable vp = vp
            vp.Z <- min -0.01 vp.Z
            let pp = uniform.ProjTrafo * V4d(vp, 1.0)
            pp.XYZ / pp.W

        let ambientOcclusion (v : Effects.Vertex) =
            fragment {
                let ndc = v.pos.XY / v.pos.W
                let wn = normal.Sample(v.tc).XYZ.Normalized
                let z = 2.0 * depth.Sample(v.tc).X - 1.0
                let pp = V4d(ndc.X, ndc.Y, z, 1.0)

                let vp = 
                    let temp = uniform.ProjTrafoInv * pp
                    temp.XYZ / temp.W

                let vn = 
                    uniform.ViewTrafo * V4d(wn, 0.0) |> Vec.xyz |> Vec.normalize


                let x = random.Sample(pp.XY).XYZ |> Vec.normalize
                let z = vn
                let y = Vec.cross z x |> Vec.normalize
                let x = Vec.cross y z |> Vec.normalize
                    
                let mutable occlusion = 0.0
                for si in 0 .. uniform.Samples - 1 do

                    let dir = sampleDirections.[si] * uniform.Radius
                    let p = vp + x * dir.X + y * dir.Y + z * dir.Z
              
                    let f = 1.0 - uniform.Threshold / -p.Z
                    let ppo = 0.5 * (project (p * f) + V3d.III)
                    let pp = 0.5 * (project p + V3d.III)
                    if depthCmp.Sample(pp.XY, ppo.Z) < 0.5 then
                        occlusion <- occlusion + depthCmp.Sample(pp.XY, pp.Z)
                    

                let occlusion = occlusion / float uniform.Samples
                let ambient = 1.0 - occlusion
                
                return V4d(ambient, ambient, ambient, 1.0)
            }
            
        
        //[<ReflectedDefinition>]
        //let blurFunction (ndc : V2d) (r : float) (centerC : V4d) (centerD : V4d) (w : float) =
            

        [<ReflectedDefinition>]
        let getLinearDepth (ndc : V2d) =
            let tc = 0.5 * (ndc + V2d.II)
            let z = 2.0 * depth.SampleLevel(tc, 0.0).X - 1.0

            let pp = V4d(ndc.X, ndc.Y, z, 1.0) 
            let temp = uniform.ProjTrafoInv * pp
            temp.Z / temp.W
            
        [<ReflectedDefinition>]
        let getAmbient (ndc : V2d) =
            let tc = 0.5 * (ndc + V2d.II)
            ambient.SampleLevel(tc, 0.0)

        let blur (v : Effects.Vertex) =
            fragment {
                let s = 2.0 / V2d ambient.Size
                let ndc = v.pos.XY / v.pos.W
                

                let sigmaPos = uniform.Sigma
                if sigmaPos <= 0.0 then
                    return getAmbient ndc
                else
                    let sigmaPos2 = sigmaPos * sigmaPos
                    let sharpness = uniform.Sharpness
                    let sharpness2 = sharpness * sharpness
                    let r = 4
                    let d0 = getLinearDepth ndc
                    let mutable sum = V4d.Zero
                    let mutable wsum = 0.0
                    for x in -r .. r do
                        for y in -r .. r do
                            let deltaPos = V2d(x,y) * s
                            let pos = ndc + deltaPos

                            let deltaDepth = getLinearDepth pos - d0
                            let value = getAmbient pos

                            let wp = exp (-V2d(x,y).LengthSquared / sigmaPos2)
                            let wd = exp (-deltaDepth*deltaDepth * sharpness2)

                            let w = wp * wd

                            sum <- sum + w * value
                            wsum <- wsum + w



                    return sum / wsum
            }

        let compose (v : Effects.Vertex) =
            fragment {
                match uniform.Visualization with
                    | SSAOVisualization.Depth -> 
                        let d = depth.Sample(v.tc).X
                        //let u_zNear = 0.01
                        //let u_zFar = 100.0
                        //let linear = (2.0 * u_zNear) / (u_zFar + u_zNear - d * (u_zFar - u_zNear))
                        let linear = d ** (128.0)
                        return V4d(linear, linear, linear, 1.0)

                    | SSAOVisualization.Color ->
                        return color.Sample(v.tc)
                        
                    | SSAOVisualization.Normal -> 
                        let d = (normal.Sample(v.tc).XYZ.Normalized + V3d.III) * 0.5
                        return V4d(d, 1.0)
            
                    | SSAOVisualization.Ambient ->
                        let a = ambient.Sample(v.tc)
                        return a

                    | _ ->
                        let a = ambient.Sample(v.tc).X ** uniform.Gamma
                        let c = color.Sample(v.tc)
                        return V4d(a * c.XYZ, c.W)
                         

            }

    

    let compileWithSSAO (outputSignature : IFramebufferSignature) (config : SSAOConfig) (view : IMod<Trafo3d>) (proj : IMod<Trafo3d>) (size : IMod<V2i>) (sg : ISg) =
        let size = size |> Mod.map (fun s -> V2i(max 1 s.X, max 1 s.Y))

        let runtime = outputSignature.Runtime
        let halfSize = 
            Mod.custom (fun t ->
                let s = size.GetValue t
                let d = config.scale.GetValue t
                V2i(
                    max 1 (int (float s.X * d)),
                    max 1 (int (float s.Y * d))
                )
            )

        let signature =
            runtime.CreateFramebufferSignature [
                DefaultSemantic.Colors, RenderbufferFormat.Rgba8
                DefaultSemantic.Depth, RenderbufferFormat.Depth24Stencil8
                DefaultSemantic.Normals, RenderbufferFormat.Rgba32f
            ]
            
        let ambientSignature =
            runtime.CreateFramebufferSignature [
                DefaultSemantic.Colors, RenderbufferFormat.Rgba8
            ]

        let randomTex = 
            let img = PixImage<float32>(Col.Format.RGB, V2i.II * 512)

            let rand = RandomSystem()
            img.GetMatrix<C3f>().SetByCoord (fun _ ->
                rand.UniformV3dDirection().ToC3d().ToC3f()
            ) |> ignore

            runtime.PrepareTexture(PixTexture2d(PixImageMipMap [| img :> PixImage |], TextureParams.empty))


        let clear = runtime.CompileClear(signature, ~~[DefaultSemantic.Colors, C4f(0,0,0,0); DefaultSemantic.Normals, C4f(0, 0, 0, 0)], ~~1.0)
        let task = runtime.CompileRender(signature, sg)

        let mutable oldColor = None
        let mutable oldNormal = None
        let mutable oldDepth = None
        let mutable oldFbo = None

        let framebufferAndTextures =
            size |> Mod.map (fun s ->
                do
                    let oc = oldColor
                    let on = oldNormal
                    let od = oldDepth
                    let off = oldFbo

                    async {
                        do! Async.Sleep 50
                        use __ = runtime.ContextLock
                        oc |> Option.iter runtime.DeleteTexture
                        on |> Option.iter runtime.DeleteTexture
                        od |> Option.iter runtime.DeleteTexture
                        off |> Option.iter runtime.DeleteFramebuffer
                
                    } |> Async.Start


                let color = runtime.CreateTexture(s, TextureFormat.Rgba8, 1, 1)
                let normal = runtime.CreateTexture(s, TextureFormat.Rgba32f, 1, 1)
                let depth = runtime.CreateTexture(s, TextureFormat.Depth24Stencil8, 1, 1)
                let fbo = 
                    runtime.CreateFramebuffer(
                        signature, 
                        [
                            DefaultSemantic.Colors,     { texture = color; level = 0; slice = 0 } :> IFramebufferOutput
                            DefaultSemantic.Depth,      { texture = depth; level = 0; slice = 0 } :> IFramebufferOutput
                            DefaultSemantic.Normals,    { texture = normal; level = 0; slice = 0 } :> IFramebufferOutput
                        ]
                    )
                oldColor <- Some color
                oldNormal <- Some normal
                oldDepth <- Some depth
                oldFbo <- Some fbo

                (fbo, color :> ITexture, normal :> ITexture, depth :> ITexture)
            )

        let result =
            Mod.custom (fun token ->
                use __ = runtime.ContextLock

                let (fbo,c,n,d) = framebufferAndTextures.GetValue token
                let output = OutputDescription.ofFramebuffer fbo
                clear.Run(token, RenderToken.Empty, output)
                task.Run(token, RenderToken.Empty, output)

                //let dst = Matrix<float32>(fbo.Size)
                //runtime.DownloadDepth(unbox d, 0, 0, dst)

                //let img = PixImage<byte>(Col.Format.RGBA, fbo.Size)
                //img.GetMatrix<C4b>().SetMap(dst, fun d -> HSVf(d / 3.0f, 1.0f, 1.0f).ToC3f().ToC4b()) |> ignore
                //img.SaveAsImage @"C:\Users\Schorsch\Desktop\depth.png"


                //runtime.Download(unbox<IBackendTexture> c).SaveAsImage @"C:\Users\Schorsch\Desktop\render.png"


                (c,n,d)
            )

        let color   = result |> Mod.map (fun (c,_,_) -> c)
        let normal  = result |> Mod.map (fun (_,n,_) -> n)
        let depth   = result |> Mod.map (fun (_,_,d) -> d)

        let ambient = 
            Sg.fullScreenQuad
                |> Sg.shader {
                    do! Shader.ambientOcclusion
                }
                |> Sg.texture DefaultSemantic.Depth depth
                |> Sg.texture DefaultSemantic.Normals normal
                |> Sg.diffuseTexture color
                |> Sg.viewTrafo view
                |> Sg.projTrafo proj
                |> Sg.uniform "Random" (Mod.constant (randomTex :> ITexture))
                |> Sg.uniform "Radius" config.radius
                |> Sg.uniform "Threshold" config.threshold
                |> Sg.uniform "Samples" config.samples
                |> Sg.compile runtime ambientSignature
                |> RenderTask.renderToColor halfSize

        let blurredAmbient =
            Sg.fullScreenQuad
                |> Sg.shader {
                    do! Shader.blur
                }
                |> Sg.texture DefaultSemantic.Depth depth
                |> Sg.texture Semantic.Ambient ambient
                |> Sg.viewTrafo view
                |> Sg.projTrafo proj
                |> Sg.uniform "Radius" config.radius
                |> Sg.uniform "Threshold" config.threshold
                |> Sg.uniform "Sigma" config.sigma
                |> Sg.uniform "Sharpness" config.sharpness
                |> Sg.compile runtime ambientSignature
                |> RenderTask.renderToColor halfSize
            


        let current =
            let textConfig =
                {
                    font = Font "Consolas"
                    color = C4b.White
                    align = TextAlignment.Left
                    flipViewDependent = false
                    renderStyle = RenderStyle.Billboard
                }

            let background = C4b(0uy, 0uy, 0uy, 128uy)

            let text =
                Mod.custom (fun t ->
                    let vis = config.visualization.GetValue t
                    let r = config.radius.GetValue t
                    let s = config.scale.GetValue t
                    let sigma = config.sigma.GetValue t
                    let sharpness = config.sharpness.GetValue t
                    let g = config.gamma.GetValue t
                    let t = config.threshold.GetValue t
                    match vis with
                        | SSAOVisualization.Composed | SSAOVisualization.Ambient ->
                            sprintf "%A\r\nradius: %.3f\r\nthreshold: %.3f\r\nscale: %.2f\r\nsigma: %.3f\r\nsharpness: %.3f\r\ngamma: %.2f" vis r t s sigma sharpness g
                        | _ ->
                            sprintf "%A" vis
                )


            let shape =
                text
                |> Mod.map (fun str ->
                    let shape = textConfig.Layout str
                    let bounds = shape.bounds.EnlargedBy(V2d(0.1, 0.0))
                    ShapeList.prepend (ConcreteShape.fillRoundedRectangle background 0.1 bounds) shape
                )

            let trafo =
                Mod.custom (fun token ->
                    let shape = shape.GetValue token
                    let s = size.GetValue token
                    let bounds = shape.bounds
                    let pixelSize = 30.0
                    let border = 10.0
                    
                    Trafo3d.Translation(V3d(0.0, 0.0, 0.0) - V3d(bounds.Min.X, bounds.Max.Y, 0.0)) *
                    Trafo3d.Scale(2.0 * pixelSize / float s.X, 2.0 * pixelSize / float s.Y, 1.0) *
                    Trafo3d.Translation(-1.0 + 2.0 * border / float s.X, 1.0 - 2.0 * border / float s.X, 0.0)
                        
                )

            Sg.shape shape
                |> Sg.trafo trafo

        Sg.fullScreenQuad
            |> Sg.texture Semantic.Ambient blurredAmbient
            |> Sg.texture DefaultSemantic.Depth depth
            |> Sg.texture DefaultSemantic.Normals normal
            |> Sg.diffuseTexture color
            |> Sg.uniform "Visualization" config.visualization
            |> Sg.uniform "Gamma" config.gamma
            |> Sg.viewTrafo view
            |> Sg.projTrafo proj
            |> Sg.shader {
                do! Shader.compose
            }
            //|> Sg.andAlso current
            |> Sg.compile runtime outputSignature


    let getScene (config : SSAOConfig) (sg : ISg) =
        Aardvark.Service.Scene.custom (fun values ->
            let sg =
                sg 
                |> Sg.viewTrafo values.viewTrafo
                |> Sg.projTrafo values.projTrafo
                

            compileWithSSAO values.signature config values.viewTrafo values.projTrafo values.size sg
        )







