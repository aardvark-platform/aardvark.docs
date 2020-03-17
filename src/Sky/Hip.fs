namespace Sky

open System.IO
open Aardvark.Base

module Hip =

    type Hip311Entry = {
        HIP     : int       // Hipparcos identifier
        Sn      : byte      // [0,159] Solution type new reduction
        So      : byte      // [0,5] Solution type old reduction
        Nc      : byte      // Number of components
        RArad   : float     // Right Ascension in ICRS, Ep=1991.25 (rad)
        DErad   : float     // Declination in ICRS, Ep=1991.25 (rad)
        Plx     : float32   // Parallax (mas)
        pmRA    : float32   // Proper motion in Right Ascension (mas/yr)
        pmDA    : float32   // Proper motion in Declination (mas/yr)
        e_RArad : float32   // Formal error on RAra
        e_Derad : float32   // Formal error on DEra
        e_Plx   : float32   // Formal error on Plx
        e_pmRA  : float32   // Formal error on pmRA
        e_pmDe  : float32   // Formal error on pmDE
        Ntr     : uint16    // Number of field transits used (3 digits)
        F2      : float32   // Goodness of fit
        F1      : byte      // Percentage rejected data (%)
        var     : float32   // Cosmic dispersion added (stochastic solution)
        ic      : uint16    // Entry in one of the suppl.catalogues
        Hpmag   : float32   // Hipparcos magnitude
        e_Hpmag : float32   // Error on mean Hpmag
        sHp     : float32   // Scatter of Hpmag
        VA      : byte      // [0,2] Reference to variability annex
        BV      : float32   // Colour index
        e_BV    : float32   // Formal error on colour index
        VI      : float32   // V-I colour index
        UW      : float32[] // Upper-triangular weight matrix (G1) (5x5 -> 15 values, see ReadMe)
    }

    let parseEntry str =
        {
            HIP     = Text(0, 6, str).ParseInt()
            Sn      = byte (Text(7, 10, str).ParseInt())
            So      = (byte str.[11]) - (byte '0')
            Nc      = (byte str.[13]) - (byte '0')
            RArad   = Text(15, 28, str).ParseDouble()
            DErad   = Text(29, 42, str).ParseDouble()
            Plx     = Text(43, 50, str).ParseFloat()
            pmRA    = Text(51, 59, str).ParseFloat()
            pmDA    = Text(60, 68, str).ParseFloat()
            e_RArad = Text(69, 75, str).ParseFloat()
            e_Derad = Text(76, 82, str).ParseFloat()
            e_Plx   = Text(83, 89, str).ParseFloat()
            e_pmRA  = Text(90, 96, str).ParseFloat()
            e_pmDe  = Text(97, 103, str).ParseFloat()
            Ntr     = uint16 (Text(104, 107, str).ParseInt())
            F2      = Text(108, 113, str).ParseFloat()
            F1      = byte (Text(114, 116, str).ParseFloat())
            var     = Text(117, 123, str).ParseFloat()
            ic      = uint16 (Text(124, 128, str).ParseInt())
            Hpmag   = Text(129, 136, str).ParseFloat()
            e_Hpmag = Text(137, 143, str).ParseFloat()
            sHp     = Text(144, 148, str).ParseFloat()
            VA      = (byte str.[150]) - (byte '0')
            BV      = Text(152, 158, str).ParseFloat()
            e_BV    = Text(159, 164, str).ParseFloat()
            VI      = Text(165, 171, str).ParseFloat()
            UW      = Array.init 15 (fun i -> Text(171 + i*7, 177 + i*7, str).ParseFloat())
        }

    let readHip311Database(filename) : Hip311Entry[] =
        
        use f = File.OpenRead(filename)
        use tr = new StreamReader(f)

        let records = f.Length / 277L
        
        Array.init (int records) (fun i -> 
                let line = tr.ReadLine()
                parseEntry line
            )

    let NamedStars : (string*int)[] = [| 
            ("Acamar",           13847);
            ("Groombridge 1830", 57939);
            ("Achernar",          7588);
            ("Hadar",            68702);
            ("Acrux",            60718);
            ("Hamal",             9884);
            ("Adhara",           33579);
            ("Izar",             72105);
            ("Agena",            68702);
            ("Kapteyn's star",   24186);
            ("Albireo",          95947);
            ("Kaus Australis",   90185);
            ("Alcor",            65477);
            ("Kocab",            72607);
            ("Alcyone",          17702);
            ("Kruger 60",       110893);
            ("Aldebaran",        21421);
            ("Luyten's star",    36208);
            ("Alderamin",       105199);
            ("Markab",          113963);
            ("Algenib",           1067);
            ("Megrez",           59774);
            ("Algieba",          50583);
            ("Menkar",           14135);
            ("Algol",            14576);
            ("Merak",            53910);
            ("Alhena",           31681);
            ("Mintaka",          25930);
            ("Alioth",           62956);
            ("Mira",             10826);
            ("Alkaid",           67301);
            ("Mirach",            5447);
            ("Almaak",            9640);
            ("Mirphak",          15863);
            ("Alnair",          109268);
            ("Mizar",            65378);
            ("Alnath",           25428);
            ("Nihalv",           25606);
            ("Alnilam",          26311);
            ("Nunki",            92855);
            ("Alnitak",          26727);
            ("Phad",             58001);
            ("Alphard",          46390);
            ("Pleione",          17851);
            ("Alphekka",         76267);
            ("Polaris",          11767);
            ("Alpheratz",          677);
            ("Pollux",           37826);
            ("Alshain",          98036);
            ("Procyon",          37279);
            ("Altair",           97649);
            ("Proxima",          70890);
            ("Ankaa",             2081);
            ("Rasalgethi",       84345);
            ("Antares",          80763);
            ("Rasalhaguev",      86032);
            ("Arcturus",         69673);
            ("Red Rectangle",    30089);
            ("Arneb",            25985);
            ("Regulus",          49669);
            ("Babcock's star",  112247);
            ("Rigel",            24436);
            ("Barnard's star",   87937);
            ("Rigil Kent",       71683);
            ("Bellatrix",        25336);
            ("Sadalmelik",      109074);
            ("Betelgeuse",       27989);
            ("Saiph",            27366);
            ("Campbell's star",  96295);
            ("Scheat",          113881);
            ("Canopus",          30438);
            ("Shaula",           85927);
            ("Capella",          24608);
            ("Shedir",            3179);
            ("Caph",               746);
            ("Sheliak",          92420);
            ("Castor",           36850);
            ("Sirius",           32349);
            ("Cor Caroli",       63125);
            ("Spica",            65474);
            ("Cyg X-1",          98298);
            ("Tarazed",          97278);
            ("Deneb",           102098);
            ("Thuban",           68756);
            ("Denebola",         57632);
            ("Unukalhai",        77070);
            ("Diphda",            3419);
            ("Van Maanen 2",      3829);
            ("Dubhe",            54061);
            ("Vega",             91262);
            ("Enif",            107315);
            ("Vindemiatrix",     63608);
            ("Etamin",           87833);
            ("Zaurak",           18543);
            ("Fomalhaut",       113368);
            ("3C 273",           60936);
        |]































































































 

