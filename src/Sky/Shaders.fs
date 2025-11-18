namespace Sky

open Aardvark.Base
open Aardvark.Rendering
open FShade

module Shaders =
    
    type VertexSky = {
        [<Position>]            pos : V4f
        [<Semantic("SkyDir")>]  dir : V3f
    }
        
    let vsSky (v : VertexSky) =
        vertex {

            // transform fullscreen-quad position back to direction vector in object space of the environment box
            let viewDir = (uniform.ProjTrafoInv * v.pos).XY
            let cubeDir = (uniform.ModelViewTrafoInv * V4f(viewDir.X, viewDir.Y, -1.0f, 0.0f)).XYZ // NOTE: -z is forward in view-space

            let posFar = V4f(v.pos.X, v.pos.Y, 1.0f, 1.0f) // pos at far-plane
            
            return { v with pos = posFar; dir = cubeDir }
        }

    let cubeMapSampler =
        samplerCube {
            texture uniform?SkyImage
            filter Filter.MinMagMipLinear  // assume image to have lower res than screen
        }

    let psSky (v : VertexSky) =
        fragment {
            
            // cubes are aquired in z-up and opengl has y-up, in order not require image data to be transformed, 
            // part of this is swapped on upload and this lookup fixes the direction
            let dir = v.dir
            let dir = V3f(dir.X, -dir.Z, dir.Y)

            return V4f(cubeMapSampler.Sample(dir).XYZ, 1.0f)
        }

    type FSQVertex = {
        [<VertexId>]        vid     : uint32
        [<Position>]        pos     : V4f
        [<TexCoord>]        tc      : V2f
        }

    let screenQuad (v : FSQVertex) = 
        
        vertex {
           
            let x = float32 (v.vid >>> 1)   // /2: 0, 0, 1, 1 
            let y = float32 (v.vid &&& 1u)  // %2: 0, 1, 0, 1
            
            let coord = V2f(x, y)
            let pos = V4f(coord.X * 2.0f - 1.0f, coord.Y * 2.0f - 1.0f, 0.0f, 1.0f)

            return {
                vid = v.vid
                pos = pos
                tc = coord
            }
        }

    type UniformScope with
        member x.SunSize : float32 = x?SunSize
        member x.SunDirection : V3f = x?SunDirection
        member x.SunColor : V3f = x?SunColor
        member x.CameraFov : V2f = x?CameraFov // (Horizontal, Vertical) in Radians

        member x.MoonSize : float32 = x?MoonSize
        member x.MoonDirection : V3f = x?MoonDirection
        member x.MoonColor : V3f = x?MoonColor

        member x.RealSunDirection : V3f = x?RealSunDirection

        member x.PlanetSize : float32 = x?PlanetSize // planet size as viewportFactor / 1.0f = 100% viewport width
        member x.PlanetDir : V3f = x?PlanetDir
        member x.PlanetColor : V3f = x?PlanetColor

    let borderPixelSize = 64.0f
    let sunCoronaExponent = 1024.0f

    let sunSpriteGs (v : Point<VertexSky>) =
        
        triangle {

            let viewDir = (uniform.ViewTrafo * V4f(uniform.SunDirection, 0.0f)).XYZ

            if viewDir.Z < 0.0f then // direction in front of camera

                let proj = V3f(viewDir.X * uniform.ProjTrafo.M00, viewDir.Y * uniform.ProjTrafo.M11, viewDir.Z * uniform.ProjTrafo.M22) // only apply diagonal components, ignore eye-offset
                let projDir = proj.XY / proj.Z

                let borderSize = borderPixelSize / V2f(uniform.ViewportSize)
                let extendOffset = (2.0f * uniform.SunSize / uniform.CameraFov + borderSize) * 1.2f // scale with 1.2f as compensation for perspective distortion on screen side

                for i in 0..3 do
                    let x = float32 (i &&& 0x1) // 0, 1, 0, 1
                    let y = float32 (i >>> 1)   // 0, 0, 1, 1

                    let extend = V2f(x - 0.5f, y - 0.5f) * 2.0f * extendOffset
                    let pos = V4f(projDir.X + extend.X, projDir.Y + extend.Y, 1.0f, 1.0f) // pos at far-plane (Z=1.0f)
                
                    // this only works for fullscreen quads
                    let temp = (uniform.ProjTrafoInv * V4f(pos.X, pos.Y, 0.0f, 0.0f)).XY
                    let dir = (uniform.ViewTrafoInv * V4f(temp.X, temp.Y, -1.0f, 0.0f)).XYZ  // world direction

                    yield { pos = pos; dir = dir.Normalized }
        }

    let sunSpritePs (v : VertexSky) =
        fragment {
            
            let pos = v.pos
            let temp = (uniform.ProjTrafoInv * V4f(pos.X, pos.Y, 0.0f, 0.0f)).XY
            let dir = (uniform.ViewTrafoInv * V4f(temp.X, temp.Y, -1.0f, 0.0f)).XYZ  // world direction
            let vdir = dir.Normalized

//            let vdir = v.dir.Normalized

            let sunSizeAng = uniform.SunSize
            let sunDir = uniform.SunDirection
            let viewAng = acos (min (Vec.dot vdir sunDir) 1.0f) // dot of normalized vectors numerically can get > 1.0f
            let coronaAng = uniform.SunSize + 4.0f * Vec.Dot(V2f(0.5f, 0.5f), borderPixelSize * uniform.CameraFov / V2f(uniform.ViewportSize)) // average of horz and vert fov

            if viewAng > coronaAng then
                discard()

            let alpha = if viewAng <= sunSizeAng then 
                            1.0f 
                        else 
                            pow (abs (1.0f - (viewAng - sunSizeAng) / (coronaAng - sunSizeAng))) sunCoronaExponent
            
            // values can get up to 1.6fe9 
            // when rendering to half-precision: max value is 65,504 
            //  -> scale all luminance values by /1000
            //  -> as additive blending is used, clamp color to 30,000 for half-precision output support
            //let colMax = max uniform.SunColor.X (max uniform.SunColor.Y uniform.SunColor.Z)
            //let sunNorm = uniform.SunColor * 30000.0f / max 30000.0f colMax

            //if alpha = 1.0f then
            //    return V4f(30000.0f, 0.0f, 0.0f, 1.0f)
            //else
            return V4f(uniform.SunColor * alpha, 1.0f)
        }

    let moonTextureSampler = 
        sampler2d {
            texture uniform?MoonTexture
            filter Filter.Anisotropic
            addressU WrapMode.Wrap
            addressV WrapMode.Clamp
        }

    let moonSpritePs (v : VertexSky) =
        fragment {
            
            let pos = v.pos

            let temp = (uniform.ProjTrafoInv * V4f(pos.X, pos.Y, 0.0f, 0.0f)).XY
            let dir = (uniform.ViewTrafoInv * V4f(temp.X, temp.Y, -1.0f, 0.0f)).XYZ  // world direction
            let vdir = dir.Normalized

            //let vdir = v.dir.Normalized

            let moonSizeAng = uniform.MoonSize
            let moonDir = uniform.MoonDirection
            let viewAng = acos (min (Vec.dot vdir moonDir) 1.0f) // dot of normalized vectors numerically can get > 1.0f
            //let coronaAng = uniform.MoonSize + 4.0f * V2f.Dot(V2f(0.5f, 0.5f), borderPixelSize * uniform.CameraFov / V2f(uniform.ViewportSize)) // average of horz and vert fov

            if viewAng > moonSizeAng then
                discard()
            
            let (moonSurfaceNormal, texCoord) = 
                if viewAng > 1e-4f then
                    let x = viewAng / moonSizeAng
                    let moonSurfaceNormalZ =  sqrt (1.0f - x * x) // [0, 1] -> local z vector

                    // let h = (Vec.cross vdir moonDir).XY |> Vec.normalize
                    //let xx = atan2 h.X h.Y

                    //V3f(0.0f, 0.0f, moonSurfaceNormalZ)
                    
                    let up = V3f.OOI
                    let right = Vec.cross moonDir up |> Vec.normalize
                    let up = Vec.cross right moonDir |> Vec.normalize
                    
                    let x = (Vec.dot vdir right) / (sin moonSizeAng)
                    let y = (Vec.dot vdir up) / (sin moonSizeAng)
                        
                    let viewNormal = V3f(x, y, -moonSurfaceNormalZ)

                    let xx = Vec.dot viewNormal (V3f(right.X, up.X, moonDir.X))
                    let yy = Vec.dot viewNormal (V3f(right.Y, up.Y, moonDir.Y))
                    let zz = Vec.dot viewNormal (V3f(right.Z, up.Z, moonDir.Z))

                    let u = (atan2 x moonSurfaceNormalZ) * ConstantF.PiInv * 0.5f + 0.5f// [-PI, PI] -> [0, 1]
                    let v = 1.0f - (acos (clamp y -1.0f 1.0f) * ConstantF.PiInv) // [PI, 0] -> [0, 1]
                    
                    (V3f(xx, yy, zz), V2f(u, v))

                else 
                    (-moonDir, V2f(0.5f))
            
            let tex = moonTextureSampler.Sample(texCoord)
            let texNorm = 0.75f // average luminance of texture // TODO: actually calculate

            //let moonNormal = uniform.ViewTrafoInv * V4f(moonSurfaceNormal, 0.0f)
            //let shade = max 0.0f (Vec.dot moonNormal.XYZ uniform.RealSunDirection)

            let shade = max 0.0f (Vec.dot moonSurfaceNormal uniform.RealSunDirection)
            let shade = 0.0001f + shade * 0.9999f // 0.01f% reflection from earth https://en.wikipedia.org/wiki/Planetshine 
            
            let moonColor = uniform.MoonColor * shade / texNorm

            // when rendering to half-precision: max value is 65,504 
            //  -> scale all luminance values by /1000
            //  -> as additive blending is used, clamp color to 30,000 for half-precision output support
            //let colMax = max moonColor.X (max moonColor.Y moonColor.Z)
            //let moonNorm = moonColor * 30000.0f / max 30000.0f colMax

            //if alpha = 1.0f then
            //    return V4f(30000.0f, 0.0f, 0.0f, 1.0f)
            //else
            //return V4f(moonNorm * alpha, 1.0f)
            //return V4f(moonNorm, 1.0f)
            //return V4f(shade, shade, shade, 1.0f)
            return V4f(moonColor * tex.XYZ, 1.0f)
        }

    type Vertex = {
        [<Color>] c : V4f
    }
       
    [<GLSLIntrinsic("exp({0})")>]
    let Exp<'a when 'a :> IVector> (a : 'a) : 'a = onlyInShaderCode ""
    
    type UniformScope with
        member x.Exposure : float32 = x?Exposure
        member x.MagBoost : float32 = x?MagBoost
    
    [<GLSLIntrinsic("mix({0},{1},{2})")>]
    let LerpV<'a when 'a :> IVector> (a : 'a) (b : 'a) (s : 'a) : 'a = onlyInShaderCode ""

    [<GLSLIntrinsic("lessThanEqual({0},{1})")>]
    let LessThanEqual<'a when 'a :> IVector> (a : 'a) (b : 'a) : 'a = onlyInShaderCode ""

    [<ReflectedDefinition>]
    let private LinearToGammaSRGBVec(c : V3f) : V3f =
        let rTrue = c * 12.92f
        let rFalse = 1.055f * V3f(pow c (V3f(1.0f / 2.4f))) - 0.055f

        LerpV rFalse rTrue (LessThanEqual c (V3f 0.0031308f))

    let simpleTonemap (v : Vertex) =
        fragment {
            
            let col = v.c.XYZ

            // 1. apply exposure
            let col = col * (exp uniform.Exposure)


            // 2. apply tonemapping function
            
            // Linear:
            //let col = col
            
            // Reinhard:
            let col = col / (1.0f + col)

            // Exponential:
            //let col = 1.0f - Exp -col

            // apply gamma for output to 8-bit srgb 
            //let col = pow col (V3f (1.0f / 2.2333333f)) // 2.233333f reduces error to true sRGB that has a linear start and 2.4f gamma else
            let col = LinearToGammaSRGBVec col
   
            return V4f(col, 1.0f)

            }

    let magBoost (v : Vertex) =
        vertex {
            let boost = uniform.MagBoost
            let intScalePerMag = 2.511f // float3232 (Fun.Pow(100.0f, 1.0f/5.0f))                                
            let scale = pow intScalePerMag boost

            return { v with c = V4f(v.c.XYZ * scale, v.c.W) }
        }


    type VertexPos = {

        [<Position>] p : V4f
    }

    let equatorTrafo (v : VertexPos) =
        vertex {
            
            let p = v.p
            let p = uniform.ModelTrafo * V4f(p.XYZ, 0.0f)
            let p = uniform.ViewTrafo * V4f(p.XYZ, 0.0f)
            let p = uniform.ProjTrafo * V4f(p.XYZ, 1.0f)

            return { p = p }

            }

    type VertexStar = {

        [<Position>]  p : V4f
        [<Color>]     c : V4f
        [<PointSize>] s : float32
    }

    let starTrafo (v : VertexStar) =
        vertex {
            
            let dir = v.p.XYZ
            let direarth = (uniform.ModelTrafo * V4f(dir, 0.0f)).XYZ
            let vdir = (uniform.ViewTrafo * V4f(direarth, 0.0f)).XYZ
            let p = uniform.ProjTrafo * V4f(vdir, 1.0f)
            let pp = if p.Z <= 0.0f then V2f(-666.0f) else p.XY / p.W // discard if z < 0
            
            // calculate brightness 
            // NOTE: base calculation actually global uniform -> would need to refactor to withClientValues sg

            let fovRad = uniform.CameraFov
            let vpz = uniform.ViewportSize
            let sunDiameterRad = 0.533f * ConstantF.RadiansPerDegree // diameter in rad
            let sunRadPx = (sunDiameterRad / fovRad.X * float32 vpz.X) * 0.5f
            let sunPixels = sunRadPx * sunRadPx * ConstantF.Pi
            let c = V4f(v.c.XYZ * sunPixels, 1.0f)

            return { p = V4f(pp.X, pp.Y, 1.0f, 1.0f); c = c; s = 1.0f }

            }

    type VertexPlanet = {

        [<Position>] p : V4f
        [<Color>]    c : V4f
        [<TexCoord>] uv : V2f
    }

    let planetSpriteGs (v : Point<VertexPlanet>) = 
        triangle {

            let viewDir = (uniform.ViewTrafo * V4f(uniform.PlanetDir, 0.0f)).XYZ

            if viewDir.Z < 0.0f then // direction in front of camera

                let proj = V3f(viewDir.X * uniform.ProjTrafo.M00, viewDir.Y * uniform.ProjTrafo.M11, viewDir.Z * uniform.ProjTrafo.M22) // only apply diagonal components, ignore eye-offset
                let projDir = proj.XY / proj.Z

                let ar = float32 uniform.ViewportSize.X / float32 uniform.ViewportSize.Y
                let minSz = 1.0f / V2f(uniform.ViewportSize)
                let sizeX = max minSz.X uniform.PlanetSize
                let sizeY = max minSz.Y (uniform.PlanetSize * ar)

                // dimm color if actual planet is less than 1px 
                let actualSize = uniform.PlanetSize * uniform.PlanetSize * ConstantF.PiHalf // actual area
                let lum = uniform.PlanetColor.X // color is gray value
                let adj = min 1.0f (actualSize / (sizeX * sizeX))
                
                // in case sprite is 1px offset uv so that full square will be filled
                let uvClamp = (minSz - V2f(sizeX, sizeY)) * V2f(uniform.ViewportSize) + (1.0f - ConstantF.Sqrt2 * 0.5f)
                let uvClamp = V2f(max uvClamp.X 0.0f, max uvClamp.Y 0.0f)

                for i in 0..3 do
                    let x = float32 (i &&& 0x1) // 0, 1, 0, 1
                    let y = float32 (i >>> 1)   // 0, 0, 1, 1

                    let uv = V2f(x - 0.5f, y - 0.5f) * 2.0f
                    let extend = uv * V2f(sizeX, sizeY)
                    let pos = V4f(projDir.X + extend.X, projDir.Y + extend.Y, 1.0f, 1.0f) // pos at far-plane (Z=1.0f)
                
                    let uvBias = V2f(float32 (sign uv.X) * uvClamp.X, float32 (sign uv.Y) * uvClamp.Y)
                    yield { p = pos; c = V4f(uniform.PlanetColor * adj, 1.0f); uv = uv - uvBias }
        }

    let planetSpritePs (v : VertexPlanet) = 
        fragment {

            if v.uv.LengthSquared > 1.0f then
                discard()
        
            return v.c
        }

    let private blitSampler =
        sampler2d {
            texture uniform?BlitTexture
            filter Filter.MinMagLinear
            addressU WrapMode.Wrap
            addressV WrapMode.Wrap
        }
        
    type VertexFSQ = {
        [<TexCoord>] tc : V2f
    }

    let blit (v : VertexFSQ) =
        fragment {
            return blitSampler.Sample(v.tc)
        }

    let private sceneTexture =
        sampler2d {
            texture uniform?SceneTexture
            filter Filter.MinMagLinear
            addressU WrapMode.Wrap
            addressV WrapMode.Wrap
        }

    let private lumTexture =
        sampler2d {
            texture uniform?LumTexture
            filter Filter.MinMagPoint
            addressU WrapMode.Wrap
            addressV WrapMode.Wrap
        }

    let lumVector = V3f(0.2126f, 0.7152f, 0.0722f)

    let lumInit (v : VertexFSQ) =
        fragment {
            let scene = sceneTexture.Sample(v.tc)
            let lum = Vec.dot scene.XYZ lumVector
            let logLumClamped = clamp -10.0f 20.0f (log lum)
            return V4f(logLumClamped, 0.0f, 0.0f, 0.0f) 
        }

    [<ReflectedDefinition>]
    let private tmReinhard(lum : float32) =
        lum / (1.0f + lum)

    [<ReflectedDefinition>]
    let private tmReinhardVec(lum : V3f) =
        lum / (1.0f + lum)

    type UniformScope with
        member x.ExposureMode : Sky.Model.ExposureMode = uniform?ExposureMode
        //member x.Exposure : float32 = x?Exposure
        member x.MiddleGray : float32 = x?MiddleGray

    let tonemap (v : VertexFSQ) =
        fragment {
            let scene = sceneTexture.Sample(v.tc).XYZ
            
            let ev = 
                if uniform.ExposureMode = Sky.Model.ExposureMode.Manual then
                    exp uniform.Exposure
                else
                    let last = lumTexture.MipMapLevels - 1
                    let avgLum = exp (lumTexture.Read(V2i(0, 0), last).X)
                    let key = if uniform.ExposureMode = Sky.Model.ExposureMode.Auto then
                                1.001f - (2.0f / (2.0f + log(avgLum + 1.0f) / log(10.0f)))
                              else // ExposureMode.MiddleGray
                                uniform.MiddleGray
                    key / avgLum

            let color = scene * ev

            // reinhard tonemap
            let color = color / (1.0f + color)

            let color = LinearToGammaSRGBVec color

            return V4f(color, 1.0f)
        }

    let lumInitEffect = 
        toEffect  lumInit

    let tonemapEffect = 
        toEffect tonemap

    let planetEffect = 
        Effect.compose [
            toEffect planetSpriteGs
            toEffect planetSpritePs
            toEffect magBoost
        ]

    let starEffect = 
        Effect.compose [
            toEffect starTrafo
            toEffect magBoost   
        ]

    let starSignEffect = 
        Effect.compose [
            toEffect equatorTrafo
            toEffect DefaultSurfaces.sgColor
        ]
        
    let markerEffect = 
        Effect.compose [
            toEffect equatorTrafo
            toEffect DefaultSurfaces.thickLine
            toEffect DefaultSurfaces.sgColor
        ]

    let moonEffect = 
        Effect.compose [
            toEffect sunSpriteGs
            toEffect moonSpritePs
        ]

    let skyEffect = 
        Effect.compose [
            toEffect screenQuad
            toEffect vsSky
            toEffect psSky
        ]

    let sunEffect =
        Effect.compose [
            toEffect sunSpriteGs
            toEffect sunSpritePs
        ]