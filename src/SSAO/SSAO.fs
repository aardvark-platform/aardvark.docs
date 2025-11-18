namespace SSAO

open System
open System.IO
open Aardvark.Base
open FSharp.Data.Adaptive
open FSharp.Data.Adaptive.Operators
open Aardvark.Rendering
open System.Runtime.CompilerServices
open Aardvark.SceneGraph
open Aardvark.Rendering.Text

type SSAOVisualization =
    | Depth     = 0
    | Normal    = 1
    | Color     = 2
    | Ambient   = 3
    | Diffuse   = 4
    | AmbientAndDiffuse   = 5
    | Composed  = 6

//module SSAOVisualization =
//    let next (v : SSAOVisualization) =
//        match v with
//            | SSAOVisualization.Depth -> SSAOVisualization.Normal
//            | SSAOVisualization.Normal -> SSAOVisualization.Color
//            | SSAOVisualization.Color -> SSAOVisualization.Composed
//            | SSAOVisualization.Composed -> SSAOVisualization.Ambient
//            | _ -> SSAOVisualization.Depth
        
//    let prev (v : SSAOVisualization) =
//        match v with
//            | SSAOVisualization.Depth -> SSAOVisualization.Ambient
//            | SSAOVisualization.Composed -> SSAOVisualization.Color
//            | SSAOVisualization.Color -> SSAOVisualization.Normal
//            | SSAOVisualization.Normal -> SSAOVisualization.Depth
//            | _ -> SSAOVisualization.Ambient

type SSAOConfig =
    {
        radius          : aval<float>
        threshold       : aval<float>
        visualization   : aval<SSAOVisualization>
        scale           : aval<float>
        sigma           : aval<float>
        sharpness       : aval<float>
        gamma           : aval<float>
        samples         : aval<int>
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
        let DepthTexture = Symbol.Create "DepthTexture"

    [<ReflectedDefinition>]
    module Shader =
        open FShade

        let private reduceMin =  (1.0f/ 128.0f)
        let private reduceMul =  (1.0f / 8.0f)
        let private spanMax   =  8.0f

        [<AbstractClass; Sealed; Extension>]
        type Sampler2dExtensions private() =

            [<Extension>]
            static member SampleLevelFXAA(x : Sampler2d, fragCoord : V2f, level : float32) =
                let inverseVP = 1.0f / V2f (x.GetSize (int level))

                let rgbNW = x.SampleLevel(fragCoord + V2f(-1.0f, -1.0f) * inverseVP, level)
                let rgbNE = x.SampleLevel(fragCoord + V2f( 1.0f, -1.0f) * inverseVP, level)
                let rgbSW = x.SampleLevel(fragCoord + V2f(-1.0f,  1.0f) * inverseVP, level)
                let rgbSE = x.SampleLevel(fragCoord + V2f( 1.0f,  1.0f) * inverseVP, level)
                let rgbM = x.SampleLevel(fragCoord, 0.0f)
                let luma = V3f(0.299f, 0.587f, 0.114f)
                let lumaNW = Vec.dot rgbNW.XYZ luma
                let lumaNE = Vec.dot rgbNE.XYZ luma
                let lumaSW = Vec.dot rgbSW.XYZ luma
                let lumaSE = Vec.dot rgbSE.XYZ luma
                let lumaM  = Vec.dot rgbM.XYZ luma

                let lumaMin = min lumaM (min (min lumaNW lumaNE) (min lumaSW lumaSE))
                let lumaMax = max lumaM (max (max lumaNW lumaNE) (max lumaSW lumaSE))

                let dir =
                    V2f(
                        -((lumaNW + lumaNE) - (lumaSW + lumaSE)),
                        ((lumaNW + lumaSW) - (lumaNE + lumaSE))
                    )

                let dirReduce = max ((lumaNW + lumaNE + lumaSW + lumaSE) * (0.25f * reduceMul)) reduceMin
                let rcpDirMin = 1.0f / ((min (abs dir.X) (abs dir.Y)) + dirReduce) 

                let dir = min (V2f(spanMax, spanMax))
                              (max 
                                (V2f(-spanMax, -spanMax))
                                (dir * rcpDirMin)
                              ) * inverseVP           

                let rgbA = 
                    0.5f * (
                        x.SampleLevel(fragCoord + dir * (1.0f / 3.0f - 0.5f), level).XYZ + 
                        x.SampleLevel(fragCoord + dir * (2.0f / 3.0f - 0.5f), level).XYZ 
                    )

                let rgbB =
                    rgbA * 0.5f + 0.25f * (
                        x.SampleLevel(fragCoord - 0.5f * dir, level).XYZ + 
                        x.SampleLevel(fragCoord + 0.5f * dir, level).XYZ 
                    )  

                let lumaB = Vec.dot rgbB luma                                          
                if ((lumaB < lumaMin) || (lumaB > lumaMax)) then
                    V4f(rgbA, 1.0f)
                else
                    V4f(rgbB, 1.0f)        

        type UniformScope with
            member x.Visualization : SSAOVisualization = uniform?Visualization
            member x.Radius : float32 = uniform?Radius
            member x.Threshold : float32 = uniform?Threshold
            member x.Sigma : float32 = uniform?Sigma
            member x.Sharpness : float32 = uniform?Sharpness
            member x.Gamma : float32 = uniform?Gamma
            member x.Samples : int = uniform?Samples
            member x.Light : V3f = uniform?Light
            member x.SampleDirections : Arr<N<512>,V3f> = uniform?SampleDirections

        [<ReflectedDefinition>]
        let project (vp : V3f) =
            let mutable vp = vp
            vp.Z <- min -0.01f vp.Z
            let pp = uniform.ProjTrafo * V4f(vp, 1.0f)
            pp.XYZ / pp.W


        let random =
            sampler2d {
                texture uniform?Random
                addressU WrapMode.Wrap
                addressV WrapMode.Wrap
                filter Filter.MinMagPoint
            }



        let ambient =
            sampler2d {
                texture uniform?Ambient
                addressU WrapMode.Clamp
                addressV WrapMode.Clamp
                filter Filter.MinMagLinear
            }
         
        [<ReflectedDefinition>]
        let getAmbient (ndc : V2f) =
            let tc = 0.5f * (ndc + V2f.II)
            ambient.SampleLevel(tc, 0.0f)



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
                texture uniform?DepthTexture
                addressU WrapMode.Clamp
                addressV WrapMode.Clamp
                filter Filter.MinMagLinear
            }

        let depthCmp =
            sampler2dShadow {
                texture uniform?DepthTexture
                addressU WrapMode.Clamp
                addressV WrapMode.Clamp
                comparison ComparisonFunction.Greater
                filter Filter.MinMagMipLinear
            }
        let ambientOcclusion (v : Effects.Vertex) =
            fragment {
                let ndc = v.pos.XY / v.pos.W
                let wn = normal.Sample(v.tc).XYZ.Normalized
                let z = 2.0f * depth.Sample(v.tc).X - 1.0f
                let pp = V4f(ndc.X, ndc.Y, z, 1.0f)

                let vp = 
                    let temp = uniform.ProjTrafoInv * pp
                    temp.XYZ / temp.W

                let vn = 
                    uniform.ViewTrafo * V4f(wn, 0.0f) |> Vec.xyz |> Vec.normalize


                let x = random.Sample(pp.XY).XYZ |> Vec.normalize
                let z = vn
                let y = Vec.cross z x |> Vec.normalize
                let x = Vec.cross y z |> Vec.normalize
                    
                let mutable occlusion = 0.0f
                for si in 0 .. uniform.Samples - 1 do

                    let dir = uniform.SampleDirections.[si] * uniform.Radius
                    let p = vp + x * dir.X + y * dir.Y + z * dir.Z
              
                    let f = 1.0f - uniform.Threshold / -p.Z
                    let ppo = 0.5f * (project (p * f) + V3f.III)
                    let pp = 0.5f * (project p + V3f.III)
                    if depthCmp.Sample(pp.XY, ppo.Z) < 0.5f then
                        occlusion <- occlusion + depthCmp.Sample(pp.XY, pp.Z)
                    

                let occlusion = occlusion / float32 uniform.Samples
                let ambient = 1.0f - occlusion
                
                return V4f(ambient, ambient, ambient, 1.0f)
            }

            
        
        //[<ReflectedDefinition>]
        //let blurFunction (ndc : V2f) (r : float32) (centerC : V4f) (centerD : V4f) (w : float32) =
            

        [<ReflectedDefinition>]
        let getLinearDepth (ndc : V2f) =
            let tc = 0.5f * (ndc + V2f.II)
            let z = 2.0f * depth.SampleLevel(tc, 0.0f).X - 1.0f

            let pp = V4f(ndc.X, ndc.Y, z, 1.0f) 
            let temp = uniform.ProjTrafoInv * pp
            temp.Z / temp.W
            

        let blur (v : Effects.Vertex) =
            fragment {
                let s = 2.0f / V2f ambient.Size
                let ndc = v.pos.XY / v.pos.W
                

                let sigmaPos = uniform.Sigma
                if sigmaPos <= 0.0f then
                    return getAmbient ndc
                else
                    let sigmaPos2 = sigmaPos * sigmaPos
                    let sharpness = uniform.Sharpness
                    let sharpness2 = sharpness * sharpness
                    let r = 4
                    let d0 = getLinearDepth ndc
                    let mutable sum = V4f.Zero
                    let mutable wsum = 0.0f
                    for x in -r .. r do
                        for y in -r .. r do
                            let deltaPos = V2f(x,y) * s
                            let pos = ndc + deltaPos

                            let deltaDepth = getLinearDepth pos - d0
                            let value = getAmbient pos

                            let wp = exp (-V2f(x,y).LengthSquared / sigmaPos2)
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
                        let v = d ** 128.0f
                        return V4f(v, v, v, 1.0f)

                    | SSAOVisualization.Color ->
                        return color.Sample(v.tc)
                        
                    | SSAOVisualization.Normal -> 
                        let d = (normal.Sample(v.tc).XYZ.Normalized + V3f.III) * 0.5f
                        return V4f(d, 1.0f)
            
                    | SSAOVisualization.Ambient ->
                        let a = ambient.Sample(v.tc)
                        return a

                    | SSAOVisualization.Diffuse ->
                        let d = depth.Sample(v.tc).X * 2.0f - 1.0f
                        let pp = V4f(v.pos.X, v.pos.Y, d, 1.0f)
                        let a = uniform.ViewProjTrafoInv * pp
                        let wp = a.XYZ / a.W
                        let n = normal.Sample(v.tc).XYZ.Normalized
                        let lp = uniform.Light

                        let ld = Vec.normalize (lp - wp)
                        let diffuse = Vec.dot ld n |> clamp 0.0f 1.0f
                        
                        let c = color.Sample(v.tc).XYZ
                        return V4f(diffuse * c, 1.0f)
                        
                    | SSAOVisualization.AmbientAndDiffuse ->
                        let d = depth.Sample(v.tc).X * 2.0f - 1.0f
                        let pp = V4f(v.pos.X, v.pos.Y, d, 1.0f)
                        let vo = uniform.ViewProjTrafoInv * pp
                        let wp = vo.XYZ / vo.W

                        let a = ambient.Sample(v.tc).X ** uniform.Gamma
                        let n = normal.Sample(v.tc).XYZ.Normalized
                        let lp = uniform.Light

                        let ld = Vec.normalize (lp - wp)
                        let diffuse = Vec.dot ld n |> clamp 0.0f 1.0f

                        return V4f((a * diffuse) * V3f.III, 1.0f)
                    | _ ->
                        let d = depth.Sample(v.tc).X * 2.0f - 1.0f
                        let pp = V4f(v.pos.X, v.pos.Y, d, 1.0f)
                        let vo = uniform.ViewProjTrafoInv * pp
                        let wp = vo.XYZ / vo.W

                        let a = ambient.Sample(v.tc).X ** uniform.Gamma
                        let n = normal.Sample(v.tc).XYZ.Normalized
                        let lp = uniform.Light

                        let ld = Vec.normalize (lp - wp)
                        let diffuse = Vec.dot ld n |> clamp 0.0f 1.0f

                        let c = color.Sample(v.tc)
                        return V4f((a * diffuse) * c.XYZ, c.W)
                         

            }

        let fxaa (v : Effects.Vertex) =
            fragment {
                return color.SampleLevelFXAA(v.tc, 0.0f)
            }


    let compileWithSSAO (outputSignature : IFramebufferSignature) (config : SSAOConfig) (view : aval<Trafo3d>) (proj : aval<Trafo3d>) (size : aval<V2i>) (sg : ISg) =
        let size = size |> AVal.map (fun s -> V2i(max 1 s.X, max 1 s.Y))

        let runtime = outputSignature.Runtime :?> IRuntime
        let halfSize = 
            AVal.custom (fun t ->
                let s = size.GetValue t
                let d = config.scale.GetValue t
                V2i(
                    max 1 (int (float s.X * d)),
                    max 1 (int (float s.Y * d))
                )
            )

        let samples = 1

        let signature =
            runtime.CreateFramebufferSignature([
                DefaultSemantic.Colors, TextureFormat.Rgba8
                DefaultSemantic.DepthStencil, TextureFormat.Depth24Stencil8
                DefaultSemantic.Normals, TextureFormat.Rgba32f
            ], samples)
            
        let ambientSignature =
            runtime.CreateFramebufferSignature [
                DefaultSemantic.Colors, TextureFormat.Rgba8
            ]

        let randomTex = 
            let img = PixImage<float32>(Col.Format.RGB, V2i.II * 512)

            let rand = RandomSystem()
            img.GetMatrix<C3f>().SetByCoord (fun _ ->
                rand.UniformV3dDirection().ToC3d().ToC3f()
            ) |> ignore

            PixTexture2d(img, wantMipMaps = false)

        let clear = clear { color C4f.Zero; depth 1.0 }
        let task = runtime.CompileRender(signature, sg)

        let color, depth, normal =
            let output =
                Set.ofList [
                    DefaultSemantic.Colors
                    DefaultSemantic.DepthStencil
                    DefaultSemantic.Normals
                ]

            let map =
                task |> RenderTask.renderSemanticsWithClear output size clear

            map |> Map.find DefaultSemantic.Colors,
            map |> Map.find DefaultSemantic.DepthStencil,
            map |> Map.find DefaultSemantic.Normals

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
            |> AVal.constant

        let ambient = 
            Sg.fullScreenQuad
                |> Sg.shader {  
                    do! Shader.ambientOcclusion
                }
                |> Sg.texture Semantic.DepthTexture depth
                |> Sg.texture DefaultSemantic.Normals normal
                |> Sg.diffuseTexture color
                |> Sg.viewTrafo view
                |> Sg.projTrafo proj
                |> Sg.texture' "Random" randomTex
                |> Sg.uniform "Radius" config.radius
                |> Sg.uniform "Threshold" config.threshold
                |> Sg.uniform "Samples" config.samples
                |> Sg.uniform  "SampleDirections" sampleDirections
                |> Sg.compile runtime ambientSignature
                |> RenderTask.renderToColor halfSize

        let blurredAmbient =
            Sg.fullScreenQuad
                |> Sg.shader {
                    do! Shader.blur                    
                }
                |> Sg.texture Semantic.DepthTexture depth
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
                    font = DefaultFonts.Hack.Regular
                    color = C4b.White
                    align = TextAlignment.Left
                    flipViewDependent = false
                    renderStyle = RenderStyle.Billboard
                }

            let background = C4b(0uy, 0uy, 0uy, 128uy)

            let text =
                AVal.custom (fun t ->
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
                |> AVal.map (fun str ->
                    let shape = textConfig.Layout str
                    let bounds = shape.bounds.EnlargedBy(V2d(0.1, 0.0))
                    ShapeList.prepend (ConcreteShape.fillRoundedRectangle background 0.1 bounds) shape
                )

            let trafo =
                AVal.custom (fun token ->
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

        let tex =         
            Sg.fullScreenQuad
                |> Sg.texture Semantic.Ambient blurredAmbient
                |> Sg.texture Semantic.DepthTexture depth
                |> Sg.texture DefaultSemantic.Normals normal
                |> Sg.diffuseTexture color
                |> Sg.uniform "Visualization" config.visualization
                |> Sg.uniform "Gamma" config.gamma
                |> Sg.uniform "Light" (AVal.constant (10.0 * V3d.OOI))
                |> Sg.viewTrafo view
                |> Sg.projTrafo proj
                |> Sg.shader {     
                    do! Shader.compose
                }
                //|> Sg.andAlso current
                |> Sg.compile runtime ambientSignature
                |> RenderTask.renderToColor size

        Sg.fullScreenQuad
            |> Sg.diffuseTexture tex
            |> Sg.viewTrafo view
            |> Sg.projTrafo proj
            |> Sg.shader {     
                do! Shader.fxaa
            }
            |> Sg.compile runtime outputSignature

    let getScene (config : SSAOConfig) (sg : ISg) =
        Aardvark.Service.Scene.custom (fun values ->
            let sg =
                sg 
                |> Sg.viewTrafo values.viewTrafo
                |> Sg.projTrafo values.projTrafo

            compileWithSSAO values.signature config values.viewTrafo values.projTrafo values.size sg
        )