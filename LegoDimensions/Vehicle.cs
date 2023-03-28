// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions
{
    /// <summary>
    /// Vehicle class for Lego Dimensions.
    /// </summary>
    public class Vehicle
    {
        /// <summary>
        /// The ID of the vehicle.
        /// </summary>
        public ushort Id { get; set; }

        /// <summary>
        /// The name of the vehicle.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The world the vehicle is from.
        /// </summary>
        public string World { get; set; }

        /// <summary>
        /// The vehicle's constructor.
        /// </summary>
        /// <param name="id">The ID of the vehicle.</param>
        /// <param name="name">The name of the vehicle.</param>
        /// <param name="world">The world the vehicle is from.</param>
        public Vehicle(ushort id, string name, string world)
        {
            Id = id;
            Name = name;
            World = world;
        }

        /// <summary>
        /// The list of all knowns vehicles.
        /// </summary>
        public static readonly List<Vehicle> Vehicles = new List<Vehicle>() {
            new Vehicle(000, "Police Car"             , "The Lego Movie"),
            new Vehicle(001, "Aerial Squad Car"       , "The Lego Movie"),
            new Vehicle(002, "Missile Striker"        , "The Lego Movie"),

            new Vehicle(003, "Gravity Sprinter"       , "The Simpsons"),
            new Vehicle(004, "Street Shredder"        , "The Simpsons"),
            new Vehicle(005, "Sky Clobberer"          , "The Simpsons"),

            new Vehicle(006, "Batmobile"              , "DC Comics"),
            new Vehicle(007, "Batblaster"             , "DC Comics"),
            new Vehicle(008, "Sonic Batray"           , "DC Comics"),

            new Vehicle(009, "Benny's Spaceship"      , "The Lego Movie"),
            new Vehicle(010, "Lasercraft"             , "The Lego Movie"),
            new Vehicle(011, "The Annihilator"        , "The Lego Movie"),

            new Vehicle(012, "DeLorean Time Machine"  , "Back to the Future"),
            new Vehicle(013, "Ultra Time Machine"     , "Back to the Future"),
            new Vehicle(014, "Electric Time Machine"  , "Back to the Future"),

            new Vehicle(015, "Hoverboard"             , "Back to the Future"),
            new Vehicle(016, "Cyclone Board"          , "Back to the Future"),
            new Vehicle(017, "Ultimate Hoverjet"      , "Back to the Future"),

            new Vehicle(018, "Eagle Interceptor"      , "Lego Legends of Chima"),
            new Vehicle(019, "Eagle Skyblazer"        , "Lego Legends of Chima"),
            new Vehicle(020, "Eagle Swoop Diver"      , "Lego Legends of Chima"),

            new Vehicle(021, "Swamp Skimmer"          , "Lego Legends of Chima"),
            new Vehicle(022, "Croc Command Sub"       , "Lego Legends of Chima"),
            new Vehicle(023, "Cragger's Fireship"     , "Lego Legends of Chima"),

            new Vehicle(024, "Cyber Guard"            , "DC Comics"),
            new Vehicle(025, "Cyber-Wrecker"          , "DC Comics"),
            new Vehicle(026, "Laser Robot Walker"     , "DC Comics"),

            new Vehicle(027, "K9"                     , "Doctor Who"),
            new Vehicle(028, "K9 Ruff Rover"          , "Doctor Who"),
            new Vehicle(029, "K9 Laser Cutter"        , "Doctor Who"),

            new Vehicle(030, "TARDIS"                 , "Doctor Who"),
            new Vehicle(031, "Laser-Pulse TARDIS"     , "Doctor Who"),
            new Vehicle(032, "Energy-Burst TARDIS"    , "Doctor Who"),

            new Vehicle(033, "Emmet's Excavator"      , "The Lego Movie"),
            new Vehicle(034, "The Destroydozer"       , "The Lego Movie"),
            new Vehicle(035, "Destruct-o-Mech"        , "The Lego Movie"),

            new Vehicle(036, "Winged Monkey"          , "Wizard of Oz"),
            new Vehicle(037, "Battle Monkey"          , "Wizard of Oz"),
            new Vehicle(038, "Commander Monkey"       , "Wizard of Oz"),

            new Vehicle(039, "Axe Chariot"            , "Lord of the Rings"),
            new Vehicle(040, "Axe Hurler"             , "Lord of the Rings"),
            new Vehicle(041, "Soaring Chariot"        , "Lord of the Rings"),

            new Vehicle(042, "Shelob the Great"       , "Lord of the Rings"),
            new Vehicle(043, "8-Legged Stalker"       , "Lord of the Rings"),
            new Vehicle(044, "Poison Slinger"         , "Lord of the Rings"),

            new Vehicle(045, "Homer's Car"            , "The Simpsons"),
            new Vehicle(046, "The SubmaHomer"         , "The Simpsons"),
            new Vehicle(047, "The Homecraft"          , "The Simpsons"),

            new Vehicle(048, "Taunt-o-Vision"         , "The Simpsons"),
            new Vehicle(049, "The MechaHomer"         , "The Simpsons"),
            new Vehicle(050, "Blast Cam"              , "The Simpsons"),

            new Vehicle(051, "Velociraptor"           , "Jurassic World"),
            new Vehicle(052, "Spike Attack Raptor"    , "Jurassic World"),
            new Vehicle(053, "Venom Raptor"           , "Jurassic World"),

            new Vehicle(054, "Gyrosphere"             , "Jurassic World"),
            new Vehicle(055, "Sonic Beam Gyrosphere"  , "Jurassic World"),
            new Vehicle(056, "Speed Boost Gyrosphere" , "Jurassic World"),

            new Vehicle(057, "Clown Bike"             , "The Simpsons"),
            new Vehicle(058, "Cannon Bike"            , "The Simpsons"),
            new Vehicle(059, "Anti-Gravity Rocket Bike","The Simpsons"),

            new Vehicle(060, "Mighty Lion Rider"      , "Lego Legends of Chima"),
            new Vehicle(061, "Lion Blazer"            , "Lego Legends of Chima"),
            new Vehicle(062, "Fire Lion"              , "Lego Legends of Chima"),

            new Vehicle(063, "Arrow Launcher"         , "Lord of the Rings"),
            new Vehicle(064, "Seeking Shooter"        , "Lord of the Rings"),
            new Vehicle(065, "Triple Ballista"        , "Lord of the Rings"),

            new Vehicle(066, "Mystery Machine"        , "Scooby-Doo"),
            new Vehicle(067, "Mystery Tow"            , "Scooby-Doo"),
            new Vehicle(068, "Mystery Monster"        , "Scooby-Doo"),

            new Vehicle(069, "Boulder Bomber"         , "Lego Ninjago"),
            new Vehicle(070, "Boulder Blaster"        , "Lego Ninjago"),
            new Vehicle(071, "Cyclone Jet"            , "Lego Ninjago"),

            new Vehicle(072, "Storm Fighter"          , "Lego Ninjago"),
            new Vehicle(073, "Lightning Jet"          , "Lego Ninjago"),
            new Vehicle(074, "Electro-Shooter"        , "Lego Ninjago"),

            new Vehicle(075, "Blade Bike"             , "Lego Ninjago"),
            new Vehicle(076, "Flying Fire Bike"       , "Lego Ninjago"),
            new Vehicle(077, "Blades of Fire"         , "Lego Ninjago"),

            new Vehicle(078, "Samurai Mech"           , "Lego Ninjago"),
            new Vehicle(079, "Samurai Shooter"        , "Lego Ninjago"),
            new Vehicle(080, "Soaring Samurai Mech"   , "Lego Ninjago"),

            new Vehicle(081, "Companion Cube"         , "Portal 2"),
            new Vehicle(082, "Laser Deflector"        , "Portal 2"),
            new Vehicle(083, "Gold Heart Emitter"     , "Portal 2"),

            new Vehicle(084, "Sentry Turret"          , "Portal 2"),
            new Vehicle(085, "Turret Striker"         , "Portal 2"),
            new Vehicle(086, "Flying Turret Carrier"  , "Portal 2"),

            new Vehicle(087, "Scooby Snack"           , "Scooby-Doo"),
            new Vehicle(088, "Scooby Fire Snack"      , "Scooby-Doo"),
            new Vehicle(089, "Scooby Ghost Snack"     , "Scooby-Doo"),

            new Vehicle(090, "Cloud Cukko Car"        , "The Lego Movie"),
            new Vehicle(091, "X-Stream Soaker"        , "The Lego Movie"),
            new Vehicle(092, "Rainbow Cannon"         , "The Lego Movie"),

            new Vehicle(093, "Invisible Jet"          , "DC Comics"),
            new Vehicle(094, "Stealth Laser Shooter"  , "DC Comics"),
            new Vehicle(095, "Torpedo Bomber"         , "DC Comics"),

            new Vehicle(096, "Ninja Copter"           , "Lego Ninjago"),
            new Vehicle(097, "Glaciator"              , "Lego Ninjago"),
            new Vehicle(098, "Freeze Fighter"         , "Lego Ninjago"),

            new Vehicle(099, "Traveling Time Train"   , "Back to the Future"),
            new Vehicle(100, "Flying Time Train "     , "Back to the Future"),
            new Vehicle(101, "Missile Blast Time Train","Back to the Future"),

            new Vehicle(102, "Aqua Watercraft"        , "DC Comics"),
            new Vehicle(103, "Seven Seas Speeder"     , "DC Comics"),
            new Vehicle(104, "Trident of Fire"        , "DC Comics"),

            new Vehicle(105, "Drill Driver"           , "DC Comics"),
            new Vehicle(106, "Bane Dig 'n' Drill"     , "DC Comics"),
            new Vehicle(107, "Bane Drill 'n' Blast"   , "DC Comics"),

            new Vehicle(108, "Quinn-mobile"           , "DC Comics"),
            new Vehicle(109, "Quinn Ultra Racer"      , "DC Comics"),
            new Vehicle(110, "Missile Launcher"       , "DC Comics"),

            new Vehicle(111, "The Jokers Chopper"     , "DC Comics"  ),
            new Vehicle(112, "Mischievous Missile Blaster","DC Comics"),
            new Vehicle(113, "Lock 'n' Laser Jet"     , "DC Comics"),

            new Vehicle(114, "Hover Pod"              , "DC Comics"),
            new Vehicle(115, "Krypton Striker"        , "DC Comics"),
            new Vehicle(116, "Hover Pod 2"            , "DC Comics"),

            new Vehicle(117, "Dalek"                  , "Doctor Who"),
            new Vehicle(118, "Fire 'n' Ride Dalek"    , "Doctor Who"),
            new Vehicle(119, "Silver Shooter Dalek"   , "Doctor Who"),

            new Vehicle(120, "Ecto-1"                 , "Ghostbusters"),
            new Vehicle(121, "Ecto-1 Blaster"         , "Ghostbusters"),
            new Vehicle(122, "Ecto-1 Water Diver"     , "Ghostbusters"),

            new Vehicle(123, "Ghost Trap"             , "Ghostbusters"),
            new Vehicle(124, "Ghost Stun'n'Trap"      , "Ghostbusters"),
            new Vehicle(125, "Proton Zapper"          , "Ghostbusters"),

            new Vehicle(126, "Unknown"                , "Unknown"),
            new Vehicle(127, "Unknown"                , "Unknown"),
            new Vehicle(128, "Unknown"                , "Unknown"),

            new Vehicle(129, "Unknown"                , "Unknown"),
            new Vehicle(130, "Unknown"                , "Unknown"),
            new Vehicle(131, "Unknown"                , "Unknown"),

            new Vehicle(132, "Llyod's Golden Dragon"  , "Lego Ninjago"),
            new Vehicle(133, "Llyod's Golden Dragon 1", "Lego Ninjago"),
            new Vehicle(134, "Llyod's Golden Dragon 2", "Lego Ninjago"),

            new Vehicle(135, "Unknown"                , "Unknown"),
            new Vehicle(136, "Unknown"                , "Unknown"),
            new Vehicle(137, "Unknown"                , "Unknown"),

            new Vehicle(138, "Unknown"                , "Unknown"),
            new Vehicle(139, "Unknown"                , "Unknown"),
            new Vehicle(140, "Unknown"                , "Unknown"),

            new Vehicle(141, "Unknown"                , "Unknown"),
            new Vehicle(142, "Unknown"                , "Unknown"),
            new Vehicle(143, "Unknown"                , "Unknown"),

            new Vehicle(144, "Mega Flight Dragon"     , "Lego Ninjago"),
            new Vehicle(145, "Mega Flight Dragon 1"   , "Lego Ninjago"),
            new Vehicle(146, "Mega Flight Dragon 2"   , "Lego Ninjago"),

            new Vehicle(147, "Unknown"                , "Unknown"),
            new Vehicle(148, "Unknown"                , "Unknown"),
            new Vehicle(149, "Unknown"                , "Unknown"),

            new Vehicle(150, "Unknown"                , "Unknown"),
            new Vehicle(151, "Unknown"                , "Unknown"),
            new Vehicle(152, "Unknown"                , "Unknown"),

            new Vehicle(153, "Unknown"                , "Unknown"),
            new Vehicle(154, "Unknown"                , "Unknown"),

            new Vehicle(155, "Flying White Dragon"    , "Lego Ninjago"),
            new Vehicle(156, "Golden Fire Dragon"     , "Lego Ninjago"),
            new Vehicle(157, "Ultra Destruction Dragon","Lego Ninjago"),

            new Vehicle(158, "Arcade Machine"         , "Midway Arcade"),
            new Vehicle(159, "8-bit Shooter"          , "Midway Arcade"),
            new Vehicle(160, "The Pixelator"          , "Midway Arcade"),

            new Vehicle(161, "G-61555 Spy Hunter"     , "Midway Arcade"),
            new Vehicle(162, "The Interdiver"         , "Midway Arcade"),
            new Vehicle(163, "Aerial Spyhunter"       , "Midway Arcade"),

            new Vehicle(164, "Slime Shooter"          , "Ghostbusters"),
            new Vehicle(165, "Slime Exploder"         , "Ghostbusters"),
            new Vehicle(166, "Slime Streamer"         , "Ghostbusters"),

            new Vehicle(167, "Terror Dog"             , "Ghostbusters"),
            new Vehicle(168, "Terror Dog Destroyer"   , "Ghostbusters"),
            new Vehicle(169, "Soaring Terror Dog"     , "Ghostbusters"),

            new Vehicle(170, "Tandem War Elefant"     , "Adventure Time"),
            new Vehicle(171, "Cosmic Squid"           , "Adventure Time"),
            new Vehicle(172, "Psychic Submarine"      , "Adventure Time"),

            new Vehicle(173, "BMO"                    , "Adventure Time"),
            new Vehicle(174, "DOGMO"                  , "Adventure Time"),
            new Vehicle(175, "SNAKEMO"                , "Adventure Time"),

            new Vehicle(176, "Jakemobile"             , "Adventure Time"),
            new Vehicle(177, "Snail Dude Jake"        , "Adventure Time"),
            new Vehicle(178, "Hover Jake"             , "Adventure Time"),

            new Vehicle(179, "Lumpy Car"              , "Adventure Time"),
            new Vehicle(180, "Lumpy Land Whale"       , "Adventure Time"),
            new Vehicle(181, "Lumpy Truck"            , "Adventure Time"),

            new Vehicle(182, "Lunatic Amp"            , "Adventure Time"),
            new Vehicle(183, "Shadow Scorpion"        , "Adventure Time"),
            new Vehicle(184, "Heavy Metal Monster"    , "Adventure Time"),

            new Vehicle(185, "B.A.'s Van"             , "The A-Team"),
            new Vehicle(186, "Fool Shmasher"          , "The A-Team"),
            new Vehicle(187, "Pain Plane"             , "The A-Team"),

            new Vehicle(188, "Phone Home"             , "E.T. the Extra-Terrestrial"),
            new Vehicle(189, "Mobile Uplink"          , "E.T. the Extra-Terrestrial"),
            new Vehicle(190, "Super-Charged Satellite", "E.T. the Extra-Terrestrial"),

            new Vehicle(191, "Niffler"                , "Fantastic Beasts and Where to Find Them"),
            new Vehicle(192, "Sinister Scorpion"      , "Fantastic Beasts and Where to Find Them"),
            new Vehicle(193, "Vicious Vulture"        , "Fantastic Beasts and Where to Find Them"),

            new Vehicle(194, "Swooping Evil"          , "Fantastic Beasts and Where to Find Them"),
            new Vehicle(195, "Brutal Bloom"           , "Fantastic Beasts and Where to Find Them"),
            new Vehicle(196, "Crawling Creeper"       , "Fantastic Beasts and Where to Find Them"),

            new Vehicle(197, "Ecto-1 (2016)"          , "Ghostbusters 2016"),
            new Vehicle(198, "Ectozer"                , "Ghostbusters 2016"),
            new Vehicle(199, "PerfEcto"               , "Ghostbusters 2016"),

            new Vehicle(200, "Flash 'n' Finish"       , "Gremlins"),
            new Vehicle(201, "Rampage Record Player"  , "Gremlins"),
            new Vehicle(202, "Stripe's Throne"        , "Gremlins"),

            new Vehicle(203, "R.C. Car"               , "Gremlins"),
            new Vehicle(204, "Gadget-o-matic"         , "Gremlins"),
            new Vehicle(205, "Scarlet Scorpion"       , "Gremlins"),

            new Vehicle(206, "Hogward Express"        , "Harry Potter"),
            new Vehicle(207, "Steam Warrior"          , "Harry Potter"),
            new Vehicle(208, "Soaring Steam Plane"    , "Harry Potter"),

            new Vehicle(209, "Enchanted Car"          , "Harry Potter"),
            new Vehicle(210, "Shark Sub"              , "Harry Potter"),
            new Vehicle(211, "Monstrous Mouth"        , "Harry Potter"),

            new Vehicle(212, "IMF Scrambler"          , "Mission: Impossible"),
            new Vehicle(213, "Shock Cycle"            , "Mission: Impossible"),
            new Vehicle(214, "IMF Covert Jet"         , "Mission: Impossible"),

            new Vehicle(215, "IMF Sports Car"         , "Mission: Impossible"),
            new Vehicle(216, "IMF Tank"               , "Mission: Impossible"),
            new Vehicle(217, "IMF Splorer"            , "Mission: Impossible"),

            new Vehicle(218, "Sonic Speedster"        , "Sonic the Hedgehog"),
            new Vehicle(219, "Blue Typhoon"           , "Sonic the Hedgehog"),
            new Vehicle(220, "Moto Bug"               , "Sonic the Hedgehog"),

            new Vehicle(221, "The Tornado"            , "Sonic the Hedgehog"),
            new Vehicle(222, "Crabmeat"               , "Sonic the Hedgehog"),
            new Vehicle(223, "Eggcatcher"             , "Sonic the Hedgehog"),

            new Vehicle(224, "K.I.T.T."               , "Knight Rider"),
            new Vehicle(225, "Goliath Armored Semi"   , "Knight Rider"),
            new Vehicle(226, "K.I.T.T. Jet"           , "Knight Rider"),

            new Vehicle(227, "Unknown"                , "Unknown"),
            new Vehicle(228, "Unknown"                , "Unknown"),
            new Vehicle(229, "Unknown"                , "Unknown"),

            new Vehicle(230, "Bionic Steed"           , "The LEGO Batman Movie"),
            new Vehicle(231, "Bat Raptor"             , "The LEGO Batman Movie"),
            new Vehicle(232, "Ultrabat"               , "The LEGO Batman Movie"),

            new Vehicle(233, "Batwing"                , "The LEGO Batman Movie"),
            new Vehicle(234, "The Black Thunder"      , "The LEGO Batman Movie"),
            new Vehicle(235, "Bat-Tank"               , "The LEGO Batman Movie"),

            new Vehicle(236, "Skeleton Orga"          , "The Goonies"),
            new Vehicle(237, "Skeleton Jukebox"       , "The Goonies"),
            new Vehicle(238, "Skele-Turkey"           , "The Goonies"),

            new Vehicle(239, "One-Eyed Willy's Pirate Ship", "The Goonies"),
            new Vehicle(240, "Fanged Fortune"         , "The Goonies"),
            new Vehicle(241, "Inferno Cannon"         , "The Goonies"),

            new Vehicle(242, "Buckbeak"               , "Harry Potter"),
            new Vehicle(243, "Giant Owl"              , "Harry Potter"),
            new Vehicle(244, "Fierce Falcon"          , "Harry Potter"),

            new Vehicle(245, "Saturn's Sandworm"      , "Beetlejuice"),
            new Vehicle(246, "Spooky Spider"          , "Beetlejuice"),
            new Vehicle(247, "Haunted Vacuum"         , "Beetlejuice"),

            new Vehicle(248, "PPG Smartphone"         , "The Powerpuff Girls"),
            new Vehicle(249, "PPG Hotline"            , "The Powerpuff Girls"),
            new Vehicle(250, "Powerpuff Mag-Net"      , "The Powerpuff Girls"),

            new Vehicle(251, "Ka-Pow Cannon"          , "The Powerpuff Girls"),
            new Vehicle(252, "Slammin' Guitar"        , "The Powerpuff Girls"),
            new Vehicle(253, "Megablast-Bot"          , "The Powerpuff Girls"),

            new Vehicle(254, "Octi"                   , "The Powerpuff Girls"),
            new Vehicle(255, "Super SKunk"            , "The Powerpuff Girls"),
            new Vehicle(256, "Sonic Squid"            , "The Powerpuff Girls"),

            new Vehicle(257, "T-Car"                  , "Teen Titans Go!"),
            new Vehicle(258, "T-Forklift"             , "Teen Titans Go!"),
            new Vehicle(259, "T-Plane"                , "Teen Titans Go!"),

            new Vehicle(260, "Spellbook of Azarath"   , "Teen Titans Go!"),
            new Vehicle(261, "Raven Wings"            , "Teen Titans Go!"),
            new Vehicle(262, "Giant Hand"             , "Teen Titans Go!"),

            new Vehicle(263, "Titan Robot"            , "Teen Titans Go!"),
            new Vehicle(264, "T-Rocket"               , "Teen Titans Go!"),
            new Vehicle(265, "Robot Retriever"        , "Teen Titans Go!"),
            };
    }
}
