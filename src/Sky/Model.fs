namespace Sky.Model

open System
open System.IO
open Adaptify
open Aardvark.Base
open Aardvark.UI.Primitives
open Aardvark.Physics.Sky

open Aardvark.Rendering

type SkyType = 
    | Preetham 
    | CIE 
    | HosekWilkie

type ExposureMode = 
    | Manual=0 
    | MiddleGray=1 
    | Auto=2
    
[<ModelType>]
type SkyInfo = 
    {
        skyType         : SkyType
        turbidity       : float // [1.9, 10]
        cieType         : CIESkyType
        lightPollution  : float
        res             : int
    }

[<ModelType>]
type StarInfo = 
    {
        starSigns       : bool
        magBoost        : float
        objectNames     : bool
        objectNameThreshold : float
    }

[<ModelType>]
type GeoInfo = 
    {
        gpsLat            : float // phi    [ °deg ] | from northpole (-90.0) over equator (0.0) to southpole (90.0)
        gpsLong           : float // lambda [ °deg ] | from east (-180.0) over greenwich (0.0 to west (180.0)
        timeZone          : int   // UTC + timeZone
        time              : DateTime // int64 // Ticks
    } with 
        member x.SunPosition : struct(SphericalCoordinate * float) =
            SunPosition.Compute(x.time, x.timeZone, x.gpsLong, x.gpsLat)
        member x.SunDirection : V3d = // direction to sun
            let struct(sunPos, distance) = x.SunPosition
            Sky.V3dFromPhiTheta(sunPos.Phi, sunPos.Theta)
        member x.MoonPosition : struct(SphericalCoordinate * float) = 
            MoonPosition.Compute(x.time, x.timeZone, x.gpsLong, x.gpsLat)
        member x.MoonDirection : V3d =
            let struct(moonPos, distance) = x.MoonPosition
            Sky.V3dFromPhiTheta(moonPos.Phi, moonPos.Theta)
        member x.JulianDayUTC : float = 
            x.time.ComputeJulianDayUTC(float x.timeZone)

module GeoInfo = 
    
    let vienna : GeoInfo = 
        {
            gpsLat = 48.20849
            gpsLong = 16.37208
            timeZone = 1 // UTC+1 winterTime | UTC+2 summerTime
            time = System.DateTime.Now
        }

[<ModelType>]
type Model =
    {
        //// location & time
        geoInfo       : GeoInfo

        skyInfo         : SkyInfo
        starInfo        : StarInfo
        planetScale     : float

        // tonemapping
        exposureMode    : ExposureMode
        exposure        : float
        key             : float

        cameraState     : CameraControllerState

        fov             : float
    }

    module Model = 

        let initial = {
            // time & location
            geoInfo = GeoInfo.vienna

            skyInfo = {
                skyType = Preetham
                turbidity = 1.9
                cieType = CIESkyType.ClearSky1
                lightPollution = 50.0
                res = 256
            }

            starInfo = {
                objectNames = false
                objectNameThreshold = 3.0
                starSigns = false
                magBoost = 0.0
            }

            planetScale = 1.0

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