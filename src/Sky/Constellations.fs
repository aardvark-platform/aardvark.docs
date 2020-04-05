module Constellations

    let ursaMajorStars : (string*int)[] = [| // the wagon
        ("Megrez",           59774); // mag 3.312
        ("Merak",            53910);
        ("Alioth",           62956);
        ("Alkaid",           67301);
        ("Mizar",            65378);
        ("Phekta",           58001); // Phad
        ("Dubhe",            54061);

        ("Polaris",          11767); // not part of ursaMajor, just for orientation
        ("Vega",             91262); // not part of ursaMajor, just for orientation (mag 0)
    |]


    let ursaMajor = [|
        (67301, 65378) // Alkaid - Mizar
        (65378, 62956) // Mizar - Alioth
        (62956, 59774) // Alioth - Megrez
        (59774, 58001) // Megrez - Phekta
        (58001, 53910) // Phekta - Merak
        (53910, 54061) // Merak - Dubhe
        (54061, 59774) // Dubhe - Megrez
    |]

    let ursaMinor = [| // Wagon of Heaven
        (11767, 85822) // Alpha UMi / Polaris - Delta UMi / Yildun / Pherkard
        (85822, 82080) // Delta UMi / Yildun / Pherkard - Epsilon UMi
        (82080, 77055) // Epsilon UMi - zeta?
        (77055, 79822) // zeta - n / Alasco
        (79822, 75097) // n / Alasco - Gamma Umi / Pherkad 
        (75097, 72607) // Gamma Umi / Pherkad - Beta UMi / Kochab / Kocab
        (72607, 77055) // Beta UMi / Kochab / Kocab - zeta
    |]

    let orion = [| 
        (27366, 26727) // Saiph - Alnitak
        (26727, 27989) // Alnitak - Betelgeuse
        (27989, 26207) // Betelgeuse - Meissa
        (26207, 25336) // Meissa - Bellatrix
        (25336, 25930) // Bellatrix - Mintaka
        (25930, 26311) // Mintaka - Alnilam
        (26311, 26727) // Alnilam - Alnitak
        (25930, 24436) // Mintaka - Rigel
    |]

    let cassiopeia = [|
        (8886, 6686) // Epsilon Cas - Delta Cas
        (6686, 4427) // Delta Cas - Gamma Cas
        (4427, 3179) // Gamma Cas - Alpha Cas
        (3179, 746) // Alpha Cas - Beta Cas / Caph
    |]

    let virgo = [|
        (65474, 64238) // Porrima - X
        (64238, 61941) // X - Porrima
        (61941, 63090) // Porrima - Auva
        (63090, 63608) // Auva, Vindemiatrix
        (61941, 60129) // Porrima - Zaniah
        (60129, 57757) // Zaniah - Zavijava
        (64238, 66249) // X - Heze
    |]

    let andromeda = [|
        (9640, 5447) // Almach - Mirach
        (5447, 3092) // Mirach - spectroscopic binary
        (3092, 677)  // spectroscopic binary - Alpheratz
        (5447, 4436) // Mirach - multi star
        (4436, 3881) //	multi star - spectroscopic binary
    |]

    let all = [|
        ursaMajor
        ursaMinor
        orion
        cassiopeia
        virgo
        andromeda
    |]  