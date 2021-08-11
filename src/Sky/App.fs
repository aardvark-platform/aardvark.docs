namespace Sky

open System
open Aardvark.Base
open Aardvark.UI
open Aardvark.UI.Primitives
open Aardvark.Rendering
open Aardvark.Rendering.Text
open FSharp.Data.Adaptive
open Sky.Model
open Shaders
open Aardvark.Physics.Sky

type Message =
    | CameraMessage of FreeFlyController.Message
    | SetGpsLat   of float
    | SetGpsLong  of float
    | SetAltitude of float
    | SetTimeZone of float // int
    | SetTurbidity of float
    | SetLightPollution of float
    | SetPlanetScale of float
    | SetResolution of int
    | SetTime of int64
    | SetDate of int64
    | SetSunPosAlgo of Option<SunPosAlgorithm>
    | SetExposure of float
    | SetKey of float
    | SetExposureMode of Option<ExposureMode>
    | AdjustFoV of V2d
    | SetObjectNames of bool
    | ToggleObjectNames 
    | SetObjectNameThreshold of float
    | SetStarSigns of bool
    | ToggleStarSigns
    | SetNow
    | SetSkyModel of Option<SkyModel>
    | SetCIEType of Option<CIESkyType>
    | SetMagBoost of float
    | Nop
    
module App =
    open Aardvark.Physics.Sky
    open Microsoft.FSharp
    open Microsoft.FSharp.Reflection
    open Aardvark.UI.Primitives.SimplePrimitives
    open Aardvark.Base
    
    let initial = {

        // time & location
        altitude = 0.0
        gpsLong = 16.0
        gpsLat = 48.0 
        timezone = 2
        time = DateTime(2019, 8, 19, 23, 0, 0).Ticks //DateTime.Now.Ticks 

        // sky
        model = Preetham
        turbidity = 1.9
        cieType = CIESkyType.ClearSky1
        lightPollution = 50.0
        res = 256
        sunPosAlgo = Strous
        planetScale = 1.0
        objectNames = false
        objectNameThreshold = 3.0
        starSigns = false
        magBoost = 0.0
        // exposure
        exposureMode = ExposureMode.Auto
        exposure = -5.0
        key = 0.12

        cameraState = { FreeFlyController.initial with
                            // look north
                            view = CameraView.lookAt V3d.OOO V3d.OIO V3d.OOI
                      }
        fov = 70.0
    }

    [<AutoOpen>]
    module SgExtensions = 
    
        open Aardvark.Base.Ag
        open Aardvark.SceneGraph
        open Aardvark.SceneGraph.Semantics
        
        type ViewSpaceTrafoApplicator(child : IAdaptiveValue<ISg>) =
            inherit Sg.AbstractApplicator(child)

        [<Rule>]
        type ShapeSem() =
    
            member x.ModelTrafoStack(b : ViewSpaceTrafoApplicator, scope : Ag.Scope) =
                let trafo =
                    scope.ViewTrafo
                        |> AVal.map (fun view ->
                            let camPos = view.Backward.TransformPos V3d.Zero
                            Trafo3d.Translation(camPos)
                        )

                b.Child?ModelTrafoStack <- trafo::scope.ModelTrafoStack

    let update (m : Model) (msg : Message) =
        match msg with
            | CameraMessage msg ->
                { m with cameraState = FreeFlyController.update m.cameraState msg }
            | SetGpsLat v -> { m with gpsLat = v }
            | SetGpsLong v -> { m with gpsLong = v }
            | SetTimeZone v -> { m with timezone = int v }
            | SetAltitude v -> { m with altitude = v }
            | SetTurbidity v -> { m with turbidity = v }
            | SetLightPollution v -> { m with lightPollution = v }
            | SetPlanetScale v -> { m with planetScale = v }
            | SetResolution v -> { m with res = v }
            | SetTime v -> let time = new DateTime(v)
                           let cur = new DateTime(m.time)
                           { m with time = DateTime(cur.Year, cur.Month, cur.Day, time.Hour, time.Minute, 0).Ticks }
            | SetDate v -> let date = new DateTime(v)
                           let cur = new DateTime(m.time)
                           { m with time = DateTime(date.Year, date.Month, date.Day, cur.Hour, cur.Minute, 0).Ticks }
            | SetSunPosAlgo o -> match o with | Some v -> { m with sunPosAlgo = v } | None -> m
            | SetExposure v -> { m with exposure = v }
            | SetKey v -> { m with key = v }
            | SetExposureMode o -> match o with | Some v -> { m with exposureMode = v } | None -> m
            | SetMagBoost v -> { m with magBoost = v }
            | SetStarSigns v -> { m with starSigns = v }
            | ToggleStarSigns -> { m with starSigns = not m.starSigns }
            | SetObjectNames v -> { m with objectNames = v }
            | ToggleObjectNames -> { m with objectNames = not m.objectNames }
            | SetObjectNameThreshold v -> { m with objectNameThreshold = v }
            | SetSkyModel o ->  match o with | Some v -> { m with model = v } | None -> m
            | SetCIEType o -> match o with | Some v -> { m with cieType = v } | None -> m
            | SetNow -> let now = DateTime.Now
                        { m with time = DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).Ticks }
            | AdjustFoV v -> let newFov = clamp 0.1 170.0 (m.fov * (pow 1.05 -v.Y))
                             let sens = newFov / 70.0 / 100.0
                             { m with fov = newFov; cameraState = { m.cameraState with freeFlyConfig = { m.cameraState.freeFlyConfig with lookAtMouseSensitivity = sens } } }
            | Nop -> m

    type NumericConfig<'T> with 
        static member ctor(min : float, max : float, smallStep : float, largeStep : float) =
            { min = min; max = max; smallStep = smallStep; largeStep = largeStep }
            
    type SkyExtra = {
        moonPT : float*float;
        solarDiskRadius : float;
        lunarDiskRadius : float;
        lunarL : float
    }

    let resourcePath = "..\\..\\..\\data\\sky"

    let view (m : AdaptiveModel) =

        let frustum = m.fov |> AVal.map (fun fv -> 
                Frustum.perspective fv 0.1 100.0 1.0)
                
        let spArgs = adaptive {
            let! gpsLong = m.gpsLong
            let! gpsLat = m.gpsLat
            let! alt = m.altitude
            let! tz = m.timezone
            let! time = m.time

            let localTime = DateTime(time)

            return (localTime, tz, gpsLong, gpsLat, alt)
        }
        
        let lumVector = V3d(0.2126, 0.7152, 0.0722)

        let calcSphereRefl (viewDir : V3d) (lightDir : V3d) = 
            let rnd = HaltonRandomSeries(2, RandomSystem(42))
            let trafo = Trafo3d.FromNormalFrame(V3d.OOO, -viewDir) // the sphere normal is -viewDir
            // 64 sample solution
            let mutable sum = 0.0
            for i in 0..63 do 
                let uv = RandomSample.Disk(rnd, 0)
                let len = uv.Length
                let nz = sqrt (1.0 - len * len) // [0, 1] -> local z vector
                let n = V3d(uv.X, uv.Y, nz)
                let nv = trafo.Forward.TransformDir(n)
                let a = max 0.0 (Vec.dot nv lightDir)
                sum <- sum + a

            let refl = sum / 64.0
            refl

        // luminance of natural sky back during moonless night without light pollution is 22 magnitude per square arcsecond (mag/arcsec2) -> 1.7e-4 cd/m2
        // in cities 13 and 15 mag/arcsec2 -> 0.1 to 0.68 cd/m^2 = x1000
        // mag 20 = ~1.0 mcd/m2
        // mag 22 = 174 μcd/m2
        let skyBackLumiance = 1.7e-4
        let lightPol = m.lightPollution |> AVal.map (fun x -> skyBackLumiance + skyBackLumiance * x) // lightPollution is factor relative to natural sky back

        let skyData = spArgs |> AVal.map2 (fun (algo) (localTime, tz, gpsLong, gpsLat, alt) ->

            let sp  = match algo with
                      | SunPosAlgorithm.Strous -> SunPosition.Compute(localTime, tz, gpsLong, gpsLat)
                      //| SunPosAlgorithm.NREL -> let sampa = SunAndMoonPositionNREL(localTime, tz, gpsLong, gpsLat, alt)
                      //                          struct (SphericalCoordinate(sampa.Theta, sampa.Phi), sampa.SolarDiskRadius)
           
            let struct (sc, d) = sp
            let sunPT = (sc.Phi, sc.Theta)
            let special = match algo with
                          | SunPosAlgorithm.Strous -> let struct (sc, d) = MoonPosition.Compute(localTime, tz, gpsLong, gpsLat)
                                                      let sunRad = 0.5 * Astronomy.AngularDiameter(Astronomy.SunDiameter, Astronomy.AU) * Constant.DegreesPerRadian
                                                      let moonRad = 0.5 * Astronomy.AngularDiameter(Astronomy.MoonDiameter, d) * Constant.DegreesPerRadian
                                                      Some { moonPT = (sc.Phi, sc.Theta); solarDiskRadius = sunRad; lunarDiskRadius = moonRad; lunarL = 1.0 }
                          //| SunPosAlgorithm.NREL ->
                          //      let sampa = SunAndMoonPositionNREL(localTime, tz, gpsLong, gpsLat, alt)
                          //      Some { moonPT = (sampa.MoonPhi, sampa.MoonTheta); solarDiskRadius = sampa.SolarDiskRadius; lunarDiskRadius = sampa.LunarDiskRadius; lunarL = 1.0 }

            (sunPT, special)) m.sunPosAlgo
        
        
        let nightTimeFadeout (theta : float) =
        
            // https://en.wikipedia.org/wiki/Twilight
            let solar_elevation = Constant.PiHalf - theta - 0.05 // sun elevation -> 0 at horizon // subtract 0.05 rad to start fadeout at 3°
            //var fadeout = solar_elevation >= 0.0 ? 1.0 : Fun.Max(0.0, 1.0 - solar_elevation.Square() * 20.0); // total fadeout at ~ -0.22rad ~= 13°
            let fadeout = if solar_elevation >= 0.0 then 1.0 else max 0.0 (1.0 / (solar_elevation.Abs() * 50.0).Exp() - 0.00001)
            fadeout

        
        let sunDiameter = skyData |> AVal.map (fun s -> (match snd s with | Some sd -> sd.solarDiskRadius * 2.0 | _ -> 0.533) * Constant.RadiansPerDegree) // 31'27'' - 32'32''
        let sunDir = skyData |> AVal.map (fun ((p,t),_) -> Sky.V3dFromPhiTheta(p, t))
        let sunColor = AVal.map2 (fun ((p,t),_) (tu) -> 
                                        let sunColorXYZ = SunLightScattering(p, t, tu).GetRadiance().ToC3f()
                                        let sunColorRgb = sunColorXYZ.XYZinC3fToLinearSRGB().Clamped(0.0f, float32 1e30)
                                        //let sunLuminance = sunColorRgb * Constant.Pi // radiance in k lumen/sr (cd) ???
                                        let sunLuminance = sunColorRgb
                                        // Wiki: 1.6 Gcd / m^2  Solar disk at noon
                                        sunLuminance
                            ) skyData m.turbidity

        let moonDiameter = skyData |> AVal.map (fun s -> (match snd s with | Some sd -> sd.lunarDiskRadius * 2.0 | _ -> 0.529) * Constant.RadiansPerDegree) // 29'20'' - 34'6''
        let moonDir = skyData |> AVal.map (fun s -> match snd s with | Some sd -> Sky.V3dFromPhiTheta(fst sd.moonPT, snd sd.moonPT) | _ -> V3d.OOO)
        let moonColor = AVal.map2 (fun (sd) (tu) -> 
                                        match snd sd with 
                                        | Some x -> 
                                            let (p,t) = x.moonPT
                                            let moonColorXYZ = SunLightScattering(p, t, tu).GetRadiance().ToC3f() // assume spectrum as sun for now // NOTE: will also attenuate the moon below the horizon
                                            let moonColorRgb = moonColorXYZ.XYZinC3fToLinearSRGB().Clamped(0.0f, float32 1e30).ToC3d()
                                            let moonLuminance = moonColorRgb * 2.5e3 / 1.6e9
                                            // Wiki: 2.5k cd/m^2 Moon surface vs 1.6e9 cd/m^2 of sun // NOTE: other paper states 4.9-5.4k average of moon disk near perigee (super moon)
                                            // Geometric albedo of moon 0.12 -> could also calculate illumination from solar disk with this albedo
                                            let srSun = Constant.PiTimesTwo * (1.0 - cos (Constant.RadiansPerDegree * 0.533 * 0.5))
                                            let i = srSun * 1.6e9
                                            let lum = i * 0.12 / Constant.PiTimesTwo
                                            moonLuminance
                                        | None -> C3d.Black
                            ) skyData m.turbidity

        let moonRefl = AVal.map2 (fun md sd -> calcSphereRefl md sd) moonDir sunDir

        let spaceVisible = AVal.map2 (fun (model : SkyModel) (cie : CIESkyType) ->
                                            match model with
                                            | CIE -> CIESkyExt.IsSunVisible(cie)
                                            | _ -> true
                                            ) m.model m.cieType

        let skyImage = adaptive {

            let! (sunPT, special) = skyData
            let (phi, theta) = sunPT
            let! turb = m.turbidity
            let! res = m.res
            let! moonRefl = moonRefl
            let! pol = lightPol
            let! cie = m.cieType
            let! model = m.model

            Log.line "sun theta: %d" (90 - int (theta.DegreesFromRadians()))
            
            let createSky p t =
                match model with
                | Preetham -> new PreethamSky(p, t, clamp 1.7 10.0 turb) :> IPhysicalSky
                | CIE -> new CIESky(p, t, cie, -1.0, -1.0) :> IPhysicalSky
                | HosekWilkie -> new HosekSky(p, t, clamp 1.0 10.0 turb, C3f.Gray50, Col.Format.CieXYZ) :> IPhysicalSky

            let skySun = createSky phi theta
            //let preethamSun = new CIESky(phi, theta, 12, 10000.0, 10000.0)
            
            let lightPolFun (v : V3d) = 
                (1.0 - (abs v.Z)) * 0.33 + 0.67
            
            //let polCol = C3b(255uy, 209uy, 163uy).ToC3f() // 4000k sRGB
            //let polCol = C3b(255uy, 228uy, 206uy).ToC3f() // 5000k sRGB
            let polCol = C3b(64uy, 64uy, 96uy).ToC3f()
            let polColLum = Vec.dot (polCol.ToV3d()) lumVector
            let pol = pol * polCol.SRGBToXYZinC3f().ToC3d() / polColLum

            let sunScale = nightTimeFadeout theta
            let moonScale = match special with | Some x -> nightTimeFadeout (snd x.moonPT) | None -> 0.0

            let skyMoon = match special with 
                          | Some x -> Some (createSky (fst x.moonPT) (snd x.moonPT))
                          | None _ -> None
                               
            let cubeFaces = Array.init 6 (fun i -> PixImage.CreateCubeMapSide<float32, C4f>(i, res, 4, 
                                                            fun v ->
                                                                let mutable xyz = C3f.Black
                                                                xyz <- xyz + skySun.GetRadiance(v).ToC3d() * sunScale
                                                                if skyMoon.IsSome then
                                                                    xyz <- xyz + skyMoon.Value.GetRadiance(v).ToC3d() * 2.5e3 / 1.6e9 * moonRefl // TODO: actual amount of reflected light
                                                                xyz <- xyz + (lightPolFun v) * pol
                                                                let rgb = xyz.XYZinC3fToLinearSRGB().Clamped(0.0f, Single.MaxValue)
                                                                if rgb.ToV3f().AnyNaN then 
                                                                    C4f.Black
                                                                else
                                                                    //let lumTest = rgb.ToV3d().Dot(V3d(0.2, 0.7, 0.1))
                                                                    rgb.ToC4f()
                                                            ) :> PixImage)

            // NOTE: magic face swap
            let cubeImg = PixImageCube.Create ([
                                CubeSide.PositiveX, cubeFaces.[2]
                                CubeSide.NegativeX, cubeFaces.[0]
                                CubeSide.PositiveY, cubeFaces.[5]
                                CubeSide.NegativeY, cubeFaces.[4]
                                CubeSide.PositiveZ, cubeFaces.[1]
                                CubeSide.NegativeZ, cubeFaces.[3]
                            ] |> Map.ofList)
                            
            let tex = cubeImg
                        |> PixImageCube.toTexture true
            return tex
        }

        let pass1 = RenderPass.after "pass1" RenderPassOrder.Arbitrary RenderPass.main 
        let pass2 = RenderPass.after "pass2" RenderPassOrder.Arbitrary pass1
        
        let sgBkg = DrawCallInfo(4) |> Sg.render IndexedGeometryMode.TriangleStrip
                    |> Sg.shader {
                        do! Shaders.screenQuad
                        do! Shaders.vsSky
                        do! Shaders.psSky
                        //do! Shaders.simpleTonemap
                    }
                    |> Sg.cullMode (AVal.constant CullMode.None)
                    |> Sg.writeBuffers' (Set.ofList [DefaultSemantic.Colors])
                    |> Sg.texture (Symbol.Create "SkyImage") skyImage
                    |> Sg.pass pass1

        let cameraFov = m.fov |> AVal.map (fun fv -> V2d(fv * Constant.RadiansPerDegree, fv * Constant.RadiansPerDegree)) // NOTE: not actual fov of render control

        let blendAdd =
            { BlendMode.Add with SourceAlphaFactor = BlendFactor.Zero }

        let sgSun = DrawCallInfo(1) |> Sg.render IndexedGeometryMode.PointList
                    |> Sg.shader {
                        do! Shaders.sunSpriteGs
                        do! Shaders.sunSpritePs
                        //do! Shaders.simpleTonemap
                    }
                    |> Sg.cullMode (AVal.constant CullMode.None)
                    |> Sg.uniform "SunColor" sunColor
                    |> Sg.uniform "SunDirection" sunDir
                    |> Sg.uniform "SunSize" sunDiameter
                    |> Sg.uniform "CameraFov" cameraFov
                    |> Sg.writeBuffers' (Set.ofList [DefaultSemantic.Colors])
                    |> Sg.blendMode' blendAdd
                    |> Sg.pass pass2
                    |> Sg.onOff spaceVisible

        let sgMoon =DrawCallInfo(1) |> Sg.render IndexedGeometryMode.PointList
                    |> Sg.shader {
                        do! Shaders.sunSpriteGs
                        do! Shaders.moonSpritePs
                        //do! Shaders.simpleTonemap
                    }
                    |> Sg.cullMode (AVal.constant CullMode.None)
                    |> Sg.uniform "MoonColor" moonColor
                    |> Sg.uniform "MoonDirection" moonDir
                    |> Sg.uniform "MoonSize" moonDiameter
                    |> Sg.uniform "SunDirection" moonDir // this is the fake sun direction for the sunSpirteGS
                    |> Sg.uniform "SunSize" moonDiameter // this is the fake sun size for the sunSpriteGS
                    |> Sg.uniform "CameraFov" cameraFov
                    |> Sg.uniform "RealSunDirection" sunDir
                    |> Sg.texture (Symbol.Create "MoonTexture") (AVal.constant (FileTexture(Path.combine [ resourcePath; "8k_moon.jpg"], { wantSrgb = true; wantCompressed = false; wantMipMaps = true }) :> ITexture))
                    |> Sg.writeBuffers' (Set.ofList [DefaultSemantic.Colors])
                    |> Sg.blendMode' blendAdd
                    |> Sg.pass pass2
                    |> Sg.onOff spaceVisible

        let sgMoon = skyData |> AVal.map (fun sd -> match snd sd with | Some s -> sgMoon | None -> Sg.empty) |> Sg.dynamic

        // visible planets Mercury, Venus, Mars, Jupiter and Saturn
        // [Planet, Color & Albedo, Mean Radius in km]
        // color is an average color of pictures in sRGB with geometric albedo in A
        let planets = [| (Planet.Mercury, C4d(0.58, 0.57, 0.57, 0.142), 2439.7); 
                         (Planet.Venus, C4d(0.59, 0.39, 0.1, 0.689), 6051.8); 
                         (Planet.Mars, C4d(0.50, 0.43, 0.32, 0.17), 3389.5); 
                         (Planet.Jupiter, C4d(0.54, 0.50, 0.39, 0.538), 69911.0); 
                         (Planet.Saturn, C4d(0.43, 0.43, 0.38, 0.499), 58232.0) |]

        
        let planetSgs = planets |> Array.map (fun (p, c, r) -> 
                let dirAndDistance = spArgs |> AVal.map (fun (time, tz, long, lat, _) -> 
                                                    Astronomy.PlanetDirectionAndDistance(p, time, tz, long, lat))
                let dir = dirAndDistance |> AVal.map (fun (struct (phi, theta, _)) -> 
                                                    Sky.V3dFromPhiTheta(phi, theta))

                let size = dirAndDistance |> AVal.map2 (fun ps (struct (_, _, distance)) -> 
                                                                // distance is in AU
                                                                // 1 AU = 149,597,870,700m
                                                                let distKm = 149597870.7 * distance
                                                                let radius = atan (r / distKm)
                                                                radius * (pow ps 2.5) // diameter in radians * user factor
                                                            ) m.planetScale

                let size = AVal.map2 (fun (fovRad : V2d) (rRad : float) -> rRad / fovRad.X) cameraFov size

                let lum = spArgs |> AVal.map (fun (time, tz, long, lat, _) -> 
                    
                        let jd = time.ComputeJulianDayUTC(float tz) // Julian day UTC
                        let rc = Astronomy.RectangularHeliocentricEclipticCoordinates(p, jd)
                        let distAu = rc.Length
                        let distKm = 149597870.7 * distAu // km
                        let sunRadiusKm = 695700.0 // km
                        let sunLuminance = 1.6e9
                        let a = atan (sunRadiusKm / distKm)
                        let sr = Constant.PiTimesTwo * (1.0 - cos a)
                        let i = sunLuminance * sr / Constant.PiTimesTwo

                        let colorLum = c.ToV3d().Dot(lumVector)
                        let colorNorm = c / colorLum
                        let albedo = colorNorm.RGB * c.A
                        let lum = i * albedo // lum if planet is 1px -> the actual cover depending on viewport resolution and fov
                        lum
                    )

                DrawCallInfo(1) |> Sg.render IndexedGeometryMode.PointList
                    |> Sg.shader {
                        do! Shaders.planetSpriteGs
                        do! Shaders.planetSpritePs
                        do! Shaders.magBoost
                        //do! Shaders.simpleTonemap
                    }
                    |> Sg.cullMode (AVal.constant CullMode.None)
                    |> Sg.uniform "MagBoost" m.magBoost
                    |> Sg.uniform "PlanetColor" lum// (AVal.constant(c * 1000.0)) // TODO calculate brightness depending on sun position and color and albedo
                    |> Sg.uniform "PlanetDir" dir
                    |> Sg.uniform "PlanetSize" size
                    |> Sg.uniform "SunDirection" dir // TODO calculate direction of sun relative to this planet
                    |> Sg.uniform "CameraFov" cameraFov
                    |> Sg.uniform "RealSunDirection" sunDir 
                    |> Sg.writeBuffers' (Set.ofList [DefaultSemantic.Colors])
                    |> Sg.blendMode (AVal.constant blendAdd)
                    |> Sg.pass pass2
                    |> Sg.onOff spaceVisible
                )

        Log.startTimed "reading star database"
        let stars = Hip.readHip311Database (Path.combine [ resourcePath; "hip2.dat" ])
        Log.stop()

        let starDict = stars |> Seq.map (fun x -> (x.HIP, x)) |> Dictionary.ofSeq
        
        //let namedStars = Hip.NamedStars |> Array.map (fun (_, i) -> starDict.[i])
        //let ursaMajorStars = ursaMajorStars |> Array.map (fun (_, i) -> starDict.[i])
        
        // stars to show
        //let stars = ursaMajorStars

        let dirs = stars |> Array.map (fun s -> V3f(Conversion.CartesianFromSpherical(s.RArad, s.DErad)))

        let colors = stars |> Array.map (fun s -> 
                                            let mag = s.Hpmag
                                            let intScalePerMag = float32 (Fun.Pow(100.0, 1.0/5.0)) // ~2.511
                                            let i0 = 0.0325f // luminance of star with mag 0.0 / ~luminance of vega
                                            let l = i0 / (pow intScalePerMag mag) // mag -26.7 -> 1.6e9 (per solar disc size), mag -12.7 average full moon -> 2.5e3
                                            // luminance the star would have if sun was 1px -> scale with viewportSize and fov (currently in shader)
                                            C4f(l, l, l, 1.0f)
                                        )
        
        let starTrafo = spArgs |> AVal.map (fun (time, tz, long, lat, _) ->
                                        let jd = time.ComputeJulianDayUTC(float tz)
                                        let t1 = Astronomy.ICRFtoCEP(jd)
                                        let t2 = Astronomy.CEPtoITRF(jd, 0.0, 0.0)
                                        let t3 = Astronomy.ITRFtoLocal(long, lat)
                                        let m33d = t3 * t2 * t1
                                        let m44d = M44d.FromRows(m33d.R0.XYZO, m33d.R1.XYZO, m33d.R2.XYZO, V4d.OOOI)
                                        Trafo3d(m44d, m44d.Inverse)
                                        )

        //Astronomy.StarDirectionJ2000(s.RArad, s.Drad, time, tz, long, lat)) stars)
        let starSg = DrawCallInfo(stars.Length) |> Sg.render IndexedGeometryMode.PointList
                        |> Sg.vertexAttribute DefaultSemantic.Positions (AVal.constant dirs)
                        |> Sg.vertexAttribute DefaultSemantic.Colors (AVal.constant colors)
                        |> Sg.shader {
                            do! Shaders.starTrafo
                            //do! Shaders.simpleTonemap
                            do! Shaders.magBoost
                        }
                        |> Sg.uniform "CameraFov" cameraFov
                        |> Sg.uniform "MagBoost" m.magBoost
                        |> Sg.depthTest' DepthTest.None
                        |> Sg.blendMode' blendAdd
                        |> Sg.pass pass2
                        |> Sg.trafo starTrafo
                        |> Sg.onOff spaceVisible

        let starSignLines = Constellations.all |> Array.collect id |> Array.collect (fun (i0, i1) -> 
                                    let s0 = starDict.[i0]
                                    let s1 = starDict.[i1]
        
                                    let v0 = V3f(Conversion.CartesianFromSpherical(s0.RArad, s0.DErad))
                                    let v1 = V3f(Conversion.CartesianFromSpherical(s1.RArad, s1.DErad))

                                    [| v0; v1 |]
                                )

        let starSignSg = DrawCallInfo(starSignLines.Length) |> Sg.render IndexedGeometryMode.LineList
                            |> Sg.vertexAttribute DefaultSemantic.Positions (AVal.constant starSignLines)
                            |> Sg.shader {
                                do! Shaders.equatorTrafo
                                //do! DefaultSurfaces.thickLine
                                do! DefaultSurfaces.sgColor
                            }
                            //|> Sg.uniform "LineWidth" (AVal.constant(0.5))
                            |> Sg.uniform "Color" (AVal.constant(C4b(115, 194, 251, 96).ToC4f()))
                            |> Sg.blendMode (AVal.constant BlendMode.Blend)
                            |> Sg.trafo starTrafo
                            |> Sg.onOff m.starSigns

        let objNames = m.objectNameThreshold |> ASet.bind (fun t -> (Hip.NamedStars |> Array.choose (fun (str, hip) -> 
                                let s = starDict.[hip]
                                if s.Hpmag < float32 t then 
                                    // distance = size
                                    // TODO/ISSUE: angle offset woul get rotated by star trafo
                                    let t = Trafo3d.Scale(0.15) * Trafo3d.Translation(10.0 * Conversion.CartesianFromSpherical(s.RArad, s.DErad))
                                    Some (AVal.constant(t), AVal.constant(str))
                                else 
                                    None) |> ASet.ofArray))

        let cfg = { font = Font("Arial"); color = C4b(11, 102, 35, 128); align = TextAlignment.Center; flipViewDependent = false; renderStyle = RenderStyle.Billboard }
        let objNameSg =  ViewSpaceTrafoApplicator(AVal.constant (Sg.textsWithConfig cfg objNames |> Aardvark.SceneGraph.SgFSharp.Sg.trafo starTrafo))
                            |> Sg.noEvents
                            |> Sg.blendMode (AVal.constant BlendMode.Blend)
                            |> Sg.onOff m.objectNames
                            
        // closing circle / line strip
        let circle = Array.init 13 (fun i -> 
                        let a = (float)i / 12.0 * Constant.PiTimesTwo
                        V3f(cos a, sin a, 0.0))
        // north, east, south, west markings / line list
        let marks = Array.init 8 (fun i ->
                            let a = (float)(i/2) / 4.0 * Constant.PiTimesTwo
                            let u = ((float)(i%2) - 0.5) * 2.0 * Constant.RadiansPerDegree
                            V3f(cos a, sin a, u)
                            )

        let equatorSg = DrawCallInfo(13) |> Sg.render IndexedGeometryMode.LineStrip
                        |> Sg.vertexAttribute DefaultSemantic.Positions (AVal.constant circle)
                        |> Sg.shader {
                        do! Shaders.equatorTrafo
                        do! DefaultSurfaces.thickLine
                        do! DefaultSurfaces.sgColor
                        }
                        |> Sg.uniform "LineWidth" (AVal.constant(2.0))
                        |> Sg.uniform "Color" (AVal.constant(C4f.Red))

        let marksSg = DrawCallInfo(8) |> Sg.render IndexedGeometryMode.LineList
                        |> Sg.vertexAttribute DefaultSemantic.Positions (AVal.constant marks)
                        |> Sg.shader {
                        do! Shaders.equatorTrafo
                        do! DefaultSurfaces.thickLine
                        do! DefaultSurfaces.sgColor
                        }
                        |> Sg.uniform "LineWidth" (AVal.constant(2.0))
                        |> Sg.uniform "Color" (AVal.constant(C4f.Red))

        let cfg = { font = Font("Arial"); color = C4b.Red; align = TextAlignment.Center; flipViewDependent = false; renderStyle = RenderStyle.Billboard }

        let markLabelStr = [| "N"; "O"; "S"; "W" |]
        let markLabels = markLabelStr |> Array.mapi (fun i str -> 
                        
                        let label = Aardvark.Rendering.Text.Sg.textWithConfig cfg (AVal.init str)
                        let ang = float i / 4.0 * Constant.PiTimesTwo
                        let dir = V3d(sin ang, cos ang, -0.06) // clockwise, starting with [0, 1, 0] / North
                        ViewSpaceTrafoApplicator(AVal.constant label)
                            |> Sg.noEvents
                            |> Sg.trafo (AVal.constant (Trafo3d.Translation(dir * 20.0)))
                    )
                            
        let markLabelSg = Sg.ofArray(markLabels)

        let sgSky = Sg.ofSeq [ sgBkg; sgSun; sgMoon; Sg.ofSeq(planetSgs); starSg ]
        let sgOverlay = Sg.ofSeq [ equatorSg; marksSg; markLabelSg; starSignSg; objNameSg ]

        //let sg = Sg.ofSeq [ sgBkg; sgSun; sgMoon; Sg.ofSeq(planetSgs); starSg; equatorSg; marksSg; markLabelSg; starSignSg; objNameSg ]
        //            |> Sg.uniform "Exposure" m.exposure
        
        let att =
            [
                style "position: fixed; left: 0; top: 0; width: 100%; height: 100%"
                onWheel (fun w -> AdjustFoV w)
                attribute "data-samples" "8"
            ]
        
        let spCases = FSharpType.GetUnionCases typeof<SunPosAlgorithm>
        let spAlgoValues = AMap.ofSeq( spCases |> Seq.map (fun c -> (FSharpValue.MakeUnion(c, [||]) :?> SunPosAlgorithm, text (c.Name))) )
        let spOptionMod : IAdaptiveValue<Option<SunPosAlgorithm>> = m.sunPosAlgo |> AVal.map (fun x -> Some x)
        
        let quad =
            let drawCall = 
                DrawCallInfo(
                    FaceVertexCount = 4,
                    InstanceCount = 1
                )

            let positions =     [| V3f(-1,-1,0); V3f(1,-1,0); V3f(-1,1,0); V3f(1,1,0) |]
            let texcoords =     [| V2f(0,0); V2f(1,0); V2f(0,1); V2f(1,1) |]

            drawCall
                |> Sg.render IndexedGeometryMode.TriangleStrip 
                |> Sg.vertexAttribute DefaultSemantic.Positions (AVal.constant positions)
                |> Sg.vertexAttribute DefaultSemantic.DiffuseColorCoordinates (AVal.constant texcoords)

        //let rc = FreeFlyController.controlledControl 
        let rc = FreeFlyController.controlledControlWithClientValues m.cameraState CameraMessage frustum (AttributeMap.ofList att) RenderControlConfig.standard 
                    (fun clientValues ->
                        let runtime = clientValues.runtime
                        
                        let hdrColorSig = clientValues.runtime.CreateFramebufferSignature(1, [
                                DefaultSemantic.Colors, RenderbufferFormat.Rgba32f; 
                                DefaultSemantic.Depth, RenderbufferFormat.Depth24Stencil8
                               ]
                            )       
                            
                        let lumSig = clientValues.runtime.CreateFramebufferSignature(1, [
                                DefaultSemantic.Colors, RenderbufferFormat.R32f; 
                               ]
                            )    

                        let sceneTex = sgSky 
                                            |> Sg.viewTrafo clientValues.viewTrafo
                                            |> Sg.projTrafo clientValues.projTrafo
                                            |> Aardvark.SceneGraph.RuntimeSgExtensions.Sg.compile clientValues.runtime hdrColorSig
                                            |> RenderTask.renderToColor clientValues.size
                                    
                        let lumInitTask = quad 
                                        |> Sg.shader { do! Shaders.lumInit }
                                        |> Sg.uniform "SceneTexture" sceneTex
                                        |> Aardvark.SceneGraph.RuntimeSgExtensions.Sg.compile clientValues.runtime lumSig

                        let lumAtt =
                            let size = clientValues.size
                            let levels = size |> AVal.map (Vec.NormMax >> Fun.Log2Int)
                            runtime.CreateTextureAttachment(
                                runtime.CreateTexture2D(TextureFormat.R32f, levels, 1, size), 0, 0
                            )

                        let lumFbo =
                            runtime.CreateFramebuffer(lumSig, [DefaultSemantic.Colors, lumAtt])

                        let temp = RenderTask.renderTo lumFbo lumInitTask
                        let lumTex =
                            temp |> AdaptiveResource.map (fun fbo ->
                                let out = fbo.Attachments.[DefaultSemantic.Colors] :?> ITextureLevel
                                runtime.GenerateMipMaps(out.Texture)
                                out.Texture
                            )

                        let sgFinal =
                            quad
                                |> Sg.shader {
                                        do! Shaders.tonemap
                                    }
                                |> Sg.uniform "SceneTexture" sceneTex
                                |> Sg.uniform "LumTexture" lumTex
                                |> Sg.uniform "ExposureMode" m.exposureMode
                                |> Sg.uniform "MiddleGray" m.key
                                |> Sg.uniform "Exposure" m.exposure
                                |> Sg.writeBuffers' (Set.ofList [DefaultSemantic.Colors])
                                |> Sg.depthTest' DepthTest.None

                        let sgOverlay =
                            sgOverlay
                                |> Sg.viewTrafo clientValues.viewTrafo
                                |> Sg.projTrafo clientValues.projTrafo


                        Sg.ofSeq [ sgFinal |> Sg.pass pass1; sgOverlay |> Sg.pass pass2] )


        let timeAttr = 
            AttributeMap.ofListCond [
                always <| attribute "type" "time"
                ("value", (m.time |> AVal.map (fun t -> Some (AttributeValue.String (DateTime(t).ToString("HH:mm"))))))
                always <| onChange (fun str -> match DateTime.TryParse str with | (true, dt) -> SetTime(dt.Ticks) | _ -> Nop)
            ]

        let dateAttr = 
            AttributeMap.ofListCond [
                always <| attribute "type" "date"
                ("value", (m.time |> AVal.map (fun t -> Some (AttributeValue.String (DateTime(t).ToString("yyyy-MM-dd"))))))
                always <| onChange (fun str -> match DateTime.TryParse str with | (true, dt) -> SetDate(dt.Ticks) | _ -> Nop)
            ]

        let skyModelCases = FSharpType.GetUnionCases typeof<SkyModel>
        let skyModelValues =  AMap.ofSeq( skyModelCases |> Seq.map (fun c -> (FSharpValue.MakeUnion(c, [||]) :?> SkyModel, text (c.Name))) )
        let skyOptionMod : IAdaptiveValue<Option<SkyModel>> = m.model |> AVal.map (fun x -> Some x)

        //let cieModelCases = Enum.GetValues typeof<CIESkyType>
        let cieValues =  AMap.ofArray(Array.init 15 (fun i -> (EnumHelpers.GetValue(i), text (Enum.GetName(typeof<CIESkyType>, i))))) // TODO: ToDescription
        let cieOptionMod : IAdaptiveValue<Option<CIESkyType>> = m.cieType |> AVal.map (fun x -> Some x)

        let exposureModeCases = Enum.GetValues typeof<ExposureMode> :?> (ExposureMode [])
        let exposureModeValues =  AMap.ofArray( exposureModeCases |> Array.map (fun c -> (c, text (Enum.GetName(typeof<ExposureMode>, c)) )))
        let exposureModeOptionMod : IAdaptiveValue<Option<ExposureMode>> = m.exposureMode |> AVal.map (fun x -> Some x)

        require Html.semui (
            body [] [

                rc

                div [style "position: fixed; width:260pt; margin:0px; border-radius:10px; padding:12px; background:DarkSlateGray; color: white"] [ // sidebar 
                   
                    h4 [ ] [text "Time & Location"]
                    Html.table [
                        Html.row "GPS Lat" [ simplenumeric {
                                    attributes [clazz "ui inverted input"]
                                    value m.gpsLat
                                    update SetGpsLat
                                    step 0.1
                                    largeStep 1.0
                                    min -90.0
                                    max 90.0
                                }]
                        Html.row "GPS Long" [ simplenumeric {
                                    attributes [clazz "ui inverted input"]
                                    value m.gpsLong
                                    update SetGpsLong
                                    step 0.1
                                    largeStep 1.0
                                    min -18.0
                                    max 180.0
                                }]
                        Html.row "Altitude" [ simplenumeric {
                                    attributes [clazz "ui inverted input"]
                                    value m.altitude
                                    update SetAltitude
                                    step 1.0
                                    largeStep 10.0
                                    min 0.0
                                    max 10000.0
                                }]
                        Html.row "Time Zone" [ simplenumeric { // TODO integer input
                                    attributes [clazz "ui inverted input"]
                                    value (m.timezone |> AVal.map (fun x -> float x))
                                    update SetTimeZone
                                    step 1.0
                                    largeStep 1.0
                                    min -12.0
                                    max 12.0
                                }]
                        Html.row "Time of Day" [ Incremental.input timeAttr ]
                        Html.row "Date" [ Incremental.input dateAttr ]

                        Html.row "" [ button [clazz "ui button"; onClick (fun _ -> SetNow)] [text "Now"] ]

                        //Html.row "Time Zone" [ simplenumeric {
                        //            attributes [clazz "ui inverted input"]
                        //            value m.timezone
                        //            update SetTimeZone
                        //            step 1.0
                        //            largeStep 1.0
                        //            min -12.0
                        //            max 12.0
                        //        }]
                            //Simple.labeledFloatInput' "GPS Lat" -90.0 90.0 1.0 SetGpsLat m.gpsLat (AttributeMap.ofList [ clazz "ui small labeled input"; style "width: 140pt"]) (AttributeMap.ofList [ clazz "ui label"; style "width: 70pt"]) 
                            //br []
                            //Simple.labeledFloatInput' "GPS Long" -180.0 180.0 1.0 SetGpsLong m.gpsLong (AttributeMap.ofList [ clazz "ui small labeled input"; style "width: 140pt"]) (AttributeMap.ofList [ clazz "ui label"; style "width: 70pt" ]) 
                            //br []
                            //Simple.labeledFloatInput' "Altitude" -10000000.0 10000000.0 10.0 SetAltitude m.altitude (AttributeMap.ofList [ clazz "ui small labeled input"; style "width: 140pt"]) (AttributeMap.ofList [ clazz "ui label"; style "width: 70pt" ]) 
                            //br []
                            //Simple.labeledIntegerInput "Time Zone" -12 12 SetTimeZone m.timezone
                            //br []
                            //p [] [ text "Time: TODO" ]
                        ]
                        
                    h4 [style "color:white"] [text "Sky"]
                    Html.table [
                                            
                        Html.row "Model" [ dropdown { allowEmpty = false; placeholder = "" } [ clazz "ui inverted selection dropdown" ] skyModelValues skyOptionMod SetSkyModel ]

                        //Html.row "Altitude" [ Incremental.numeric { min = 0.1; max = 1.0; smallStep = 1.9; largeStep = 10.0 } (AttributeMap.ofList [ clazz "ui inverted input"]) m.turbidity SetTurbidity ]
                        //Html.row "Altitude" [ Incremental.numeric (NumericConfig.ctor(0.1, 1.0, 1.9, 10.0)) (AttributeMap.ofList [ clazz "ui inverted input"]) m.turbidity SetTurbidity ]
                        
                        Html.row "Turbidity" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.turbidity; update SetTurbidity; step 0.1; largeStep 1.0; min 1.9; max 10.0; }]

                        Html.row "Type" [ dropdown { allowEmpty = false; placeholder = "" } [ clazz "ui inverted selection dropdown" ] cieValues cieOptionMod SetCIEType ] 
                        
                        Html.row "Light Pollution" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.lightPollution; update SetLightPollution; step 5.0; largeStep 50.0; min 0.0; max 10000.0; }]
                                //[ simplenumeric {
                                //    attributes [clazz "ui inverted input"]
                                //    value m.turbidity
                                //    update SetTurbidity
                                //    step 0.1
                                //    largeStep 1.0
                                //    min 1.9
                                //    max 10.0
                                //}]
                        // Html.row "Sun Position" [ dropdown { allowEmpty = false; placeholder = "" } [ clazz "ui inverted selection dropdown" ] spAlgoValues spOptionMod SetSunPosAlgo ]
                        // Html.row "Resolution" [ text "TODO" ]
                        Html.row "mag Boost" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.magBoost; update SetMagBoost; step 0.1; largeStep 1.0; min 0.0; max 10.0; }]
                        Html.row "Planet Scale" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.planetScale; update SetPlanetScale; step 0.1; largeStep 1.0; min 1.0; max 10.0; }]
                        Html.row "Star Signs" [ checkbox [clazz "ui inverted toggle checkbox"] m.starSigns ToggleStarSigns "" ]
                        Html.row "Object Names" [ checkbox [clazz "ui inverted toggle checkbox"] m.objectNames ToggleObjectNames "" ]
                        Html.row "mag Threshold" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.objectNameThreshold; update SetObjectNameThreshold; step 0.1; largeStep 1.0; min -20.0; max 20.0; }]
                            //Simple.labeledFloatInput' "Turbidity" 1.9 10.0 0.1 SetTurbidity m.turbidity (AttributeMap.ofList [ clazz "ui small labeled input"; style "width: 140pt; color : black"]) (AttributeMap.ofList [ clazz "ui label"; style "width: 70pt"]) 
                            //br []
                            //p [] [ text "Model: TODO" ]
                            //Simple.labeledIntegerInput "Resolution" 16 4096 SetResolution m.res
                        ]

                    h4 [style "color:white"] [text "Tonemapping"]
                    Html.table [
                        Html.row "Mode" [ dropdown { allowEmpty = false; placeholder = "" } [ clazz "ui inverted selection dropdown" ] exposureModeValues exposureModeOptionMod SetExposureMode ]
                        Html.row "Exposure" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.exposure; update SetExposure; step 0.1; largeStep 1.0; min -20.0; max 10.0; }]
                        Html.row "Middle Gray" [ simplenumeric { attributes [clazz "ui inverted input"]; value m.key; update SetKey; step 0.001; largeStep 0.01; min 0.001; max 1.0; }]
                        ]
                ]

                //div [clazz "ui inverted segment"] [
                //    onBoot "$('#__ID__').accordion();" (
                //        div [style "width:200px;"] [
                //            div [clazz "ui inverted fluid accordion"] [
                //                div [clazz "title"] [
                //                    i [clazz "dropdown icon"] []
                //                    text "Time & Location"
                //                ]
                //                div [clazz "content"] [
                //                    Simple.labeledFloatInput "GPS Lat" -180.0 180.0 1.0 SetGpsLat m.gpsLat // (AttributeMap.ofList [ clazz "ui input"; style "width: 60pt; color : black"]) (AttributeMap.ofList [ clazz "ui label"; style "width: 80pt"]) 
                //                    br []
                //                    Simple.labeledFloatInput "GPS Long" -180.0 180.0 1.0 SetGpsLong m.gpsLong //(AttributeMap.ofList [ clazz "ui input"; style "width: 60pt; color : black"]) (AttributeMap.ofList [ clazz "ui label"; style "width: 80pt" ]) 
                //                ]
                //            ]
                //        ]
                //    )
                //]
            ]) 

    let threads (model : Model) = 
        FreeFlyController.threads model.cameraState |> ThreadPool.map CameraMessage

    let app =
        {
            initial = initial
            update = update
            view = view
            threads = threads
            unpersist = Unpersist.instance
        }