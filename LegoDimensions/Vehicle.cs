// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions
{
    /// <summary>
    /// Vehicle class for Lego Dimensions.
    /// </summary>
    public class Vehicle : ILegoTag
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
        /// Gets or sets the list of abilities of the vehicle.
        /// </summary>
        public List<string> Abilities { get; set; }

        /// <summary>
        /// Gets the vehicle rebuild.
        /// </summary>
        public VehicleRebuild Rebuild
        {
            get
            {
                if (Id < 155)
                {
                    return (VehicleRebuild)(Id % 3);
                }

                return (VehicleRebuild)((Id + 1) % 3);
            }
        }

        /// <summary>
        /// The vehicle's constructor.
        /// </summary>
        /// <param name="id">The ID of the vehicle.</param>
        /// <param name="name">The name of the vehicle.</param>
        /// <param name="world">The world the vehicle is from.</param>
        public Vehicle(ushort id, string name, string world, List<string> abilities)
        {
            Id = id;
            Name = name;
            World = world;
            Abilities = abilities;
        }

        /// <summary>
        /// The list of all knowns vehicles.
        /// </summary>
        public static readonly List<Vehicle> Vehicles = new List<Vehicle>() {
            new Vehicle(000, "Police Car"             , "The Lego Movie", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(001, "Aerial Squad Car"       , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Accelerator Switches","Tow Bar" }),
            new Vehicle(002, "Missile Striker"        , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Accelerator Switches","Tow Bar" }),

            new Vehicle(003, "Gravity Sprinter"       , "The Simpsons", new List<string>() { "Accelerator Switches" }),
            new Vehicle(004, "Street Shredder"        , "The Simpsons", new List<string>() { "Accelerator Switches","Speed Boost","Tow Bar" }),
            new Vehicle(005, "Sky Clobberer"          , "The Simpsons", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack","Accelerator Switches","Speed Boost","Tow Bar" }),

            new Vehicle(006, "Batmobile"              , "DC Comics", new List<string>() { "Accelerator Switches" }),
            new Vehicle(007, "Batblaster"             , "DC Comics", new List<string>() { "Accelerator Switches","Sonar Smash","Tow Bar" }),
            new Vehicle(008, "Sonic Batray"           , "DC Comics", new List<string>() { "Accelerator Switches","Special Attack","Sonar Smash","Tow Bar" }),

            new Vehicle(009, "Benny's Spaceship"      , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(010, "Lasercraft"             , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup" }),
            new Vehicle(011, "The Annihilator"        , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Gold LEGO Blowup" }),

            new Vehicle(012, "DeLorean Time Machine"  , "Back to the Future", new List<string>() { "Accelerator Switches","Time Travel" }),
            new Vehicle(013, "Ultra Time Machine"     , "Back to the Future", new List<string>() { "Special Attack","Electricity","Tow Bar","Accelerator Switches","Time Travel" }),
            new Vehicle(014, "Electric Time Machine"  , "Back to the Future", new List<string>() { "Silver LEGO Blowup","Flying","Flight Docks and Flight Cargo Hooks","Time Travel","Special Attack","Electricity","Tow Bar" }),

            new Vehicle(015, "Hoverboard"             , "Back to the Future", new List<string>() { "Hover" }),
            new Vehicle(016, "Cyclone Board"          , "Back to the Future", new List<string>() { "Special Attack","Hover" }),
            new Vehicle(017, "Ultimate Hoverjet"      , "Back to the Future", new List<string>() { "Flying","Special Attack","Silver LEGO Blowup","Hover" }),

            new Vehicle(018, "Eagle Interceptor"      , "Lego Legends of Chima", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(019, "Eagle Skyblazer"        , "Lego Legends of Chima", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Speed Boost","Silver LEGO Blowup" }),
            new Vehicle(020, "Eagle Swoop Diver"      , "Lego Legends of Chima", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack","Gold LEGO Blowup","Speed Boost","Silver LEGO Blowup" }),

            new Vehicle(021, "Swamp Skimmer"          , "Lego Legends of Chima", new List<string>() { "Sail" }),
            new Vehicle(022, "Croc Command Sub"       , "Lego Legends of Chima", new List<string>() { "Sail","Speed Boost","Special Attack" }),
            new Vehicle(023, "Cragger's Fireship"     , "Lego Legends of Chima", new List<string>() { "Dive","Silver LEGO Blowup","Sail","Speed Boost","Special Attack" }),

            new Vehicle(024, "Cyber Guard"            , "DC Comics", new List<string>() { "Mech Walker" }),
            new Vehicle(025, "Cyber-Wrecker"          , "DC Comics", new List<string>() { "Mech Walker","Super Strength","Dig" }),
            new Vehicle(026, "Laser Robot Walker"     , "DC Comics", new List<string>() { "Gold LEGO Blowup","Mech Walker","Super Strength","Dig" }),

            new Vehicle(027, "K9"                     , "Doctor Who", new List<string>() { "Silver LEGO Blowup" }),
            new Vehicle(028, "K9 Ruff Rover"          , "Doctor Who", new List<string>() { "Sonar Smash","Silver LEGO Blowup" }),
            new Vehicle(029, "K9 Laser Cutter"        , "Doctor Who", new List<string>() { "Gold LEGO Blowup","Sonar Smash","Silver LEGO Blowup" }),

            new Vehicle(030, "TARDIS"                 , "Doctor Who", new List<string>() { "TARDIS Travel","Stealth","Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(031, "Laser-Pulse TARDIS"     , "Doctor Who", new List<string>() { "TARDIS Travel","Stealth","Flying","Gold LEGO Blowup" }),
            new Vehicle(032, "Energy-Burst TARDIS"    , "Doctor Who", new List<string>() { "TARDIS Travel","Stealth","Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup" }),

            new Vehicle(033, "Emmet's Excavator"      , "The Lego Movie", new List<string>() { "Accelerator Switches","Dig" }),
            new Vehicle(034, "The Destroydozer"       , "The Lego Movie", new List<string>() { "Accelerator Switches","Dig","Tow Bar" }),
            new Vehicle(035, "Destruct-o-Mech"        , "The Lego Movie", new List<string>() { "Mech Walker","Super Strength","Accelerator Switches","Dig","Tow Bar" }),

            new Vehicle(036, "Winged Monkey"          , "Wizard of Oz", new List<string>() { "" }),
            new Vehicle(037, "Battle Monkey"          , "Wizard of Oz", new List<string>() { "Special Attack","Silver LEGO Blowup","Flying" }),
            new Vehicle(038, "Commander Monkey"       , "Wizard of Oz", new List<string>() { "Flying","Special Weapon,Sonar Smash","Special Attack","Silver LEGO Blowup" }),

            new Vehicle(039, "Axe Chariot"            , "Lord of the Rings", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(040, "Axe Hurler"             , "Lord of the Rings", new List<string>() { "Accelerator Switches","Special Attack","Tow Bar" }),
            new Vehicle(041, "Soaring Chariot"        , "Lord of the Rings", new List<string>() { "Accelerator Switches","Flying","Flight Docks and Flight Cargo Hooks","Tow Bar","Special Attack" }),

            new Vehicle(042, "Shelob the Great"       , "Lord of the Rings", new List<string>() { "Dig" }),
            new Vehicle(043, "8-Legged Stalker"       , "Lord of the Rings", new List<string>() { "Super Strength","Special Attack","Dig" }),
            new Vehicle(044, "Poison Slinger"         , "Lord of the Rings", new List<string>() { "Special Attack","Dig","Super Strength" }),

            new Vehicle(045, "Homer's Car"            , "The Simpsons", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(046, "The SubmaHomer"         , "The Simpsons", new List<string>() { "Dive","Silver LEGO Blowup","Accelerator Switches","Tow Bar" }),
            new Vehicle(047, "The Homecraft"          , "The Simpsons", new List<string>() { "Sail","Dive","Silver LEGO Blowup","Accelerator Switches","Tow Bar" }),

            new Vehicle(048, "Taunt-o-Vision"         , "The Simpsons", new List<string>() { "Taunt","Silver LEGO Blowup" }),
            new Vehicle(049, "The MechaHomer"         , "The Simpsons", new List<string>() { "Gold LEGO Blowup","Taunt","Silver LEGO Blowup" }),
            new Vehicle(050, "Blast Cam"              , "The Simpsons", new List<string>() { "Special Weapon","Gold LEGO Blowup","Taunt","Silver LEGO Blowup" }),

            new Vehicle(051, "Velociraptor"           , "Jurassic World", new List<string>() { "Guardian Ability","Vine Cut" }),
            new Vehicle(052, "Spike Attack Raptor"    , "Jurassic World", new List<string>() { "Special Attack","Vine Cut","Guardian Ability","Super Strength" }),
            new Vehicle(053, "Venom Raptor"           , "Jurassic World", new List<string>() { "Super Strength","Dig","Vine Cut","Guardian Ability","Special Attack" }),

            new Vehicle(054, "Gyrosphere"             , "Jurassic World", new List<string>() { "Gyrosphere Switch" }),
            new Vehicle(055, "Sonic Beam Gyrosphere"  , "Jurassic World", new List<string>() { "Gyrosphere Switch","Sonar Smash" }),
            new Vehicle(056, "Speed Boost Gyrosphere" , "Jurassic World", new List<string>() { "Gyrosphere Switch","Speed Boost","Sonar Smash" }),

            new Vehicle(057, "Clown Bike"             , "The Simpsons", new List<string>() { "Accelerator Switches" }),
            new Vehicle(058, "Cannon Bike"            , "The Simpsons", new List<string>() { "Accelerator Switches","Special Attack","Tow Bar" }),
            new Vehicle(059, "Anti-Gravity Rocket Bike","The Simpsons", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Accelerator Switches","Special Attack","Tow Bar" }),

            new Vehicle(060, "Mighty Lion Rider"      , "Lego Legends of Chima", new List<string>() { "Accelerator Switches" }),
            new Vehicle(061, "Lion Blazer"            , "Lego Legends of Chima", new List<string>() { "Special Attack","Tow Bar","Accelerator Switches" }),
            new Vehicle(062, "Fire Lion"              , "Lego Legends of Chima", new List<string>() { "Special Weapon","Special Attack","Tow Bar","Accelerator Switches" }),

            new Vehicle(063, "Arrow Launcher"         , "Lord of the Rings", new List<string>() { "Accelerator Switches" }),
            new Vehicle(064, "Seeking Shooter"        , "Lord of the Rings", new List<string>() { "Accelerator Switches","Special Attack","Tow Bar" }),
            new Vehicle(065, "Triple Ballista"        , "Lord of the Rings", new List<string>() { "Accelerator Switches","Special Weapon","Special Attack","Tow Bar" }),

            new Vehicle(066, "Mystery Machine"        , "Scooby-Doo", new List<string>() { "Accelerator Switches" }),
            new Vehicle(067, "Mystery Tow"            , "Scooby-Doo", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(068, "Mystery Monster"        , "Scooby-Doo", new List<string>() { "Accelerator Switches","Water Spray","Tow Bar" }),

            new Vehicle(069, "Boulder Bomber"         , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(070, "Boulder Blaster"        , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup" }),
            new Vehicle(071, "Cyclone Jet"            , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack,Silver LEGO Blowup" }),

            new Vehicle(072, "Storm Fighter"          , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(073, "Lightning Jet"          , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Electricity" }),
            new Vehicle(074, "Electro-Shooter"        , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Electricity","Special Attack" }),

            new Vehicle(075, "Blade Bike"             , "Lego Ninjago", new List<string>() { "Accelerator Switches" }),
            new Vehicle(076, "Flying Fire Bike"       , "Lego Ninjago", new List<string>() { "Accelerator Switches","Special Attack,Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(077, "Blades of Fire"         , "Lego Ninjago", new List<string>() { "Accelerator Switches","Special Attack,Flying","Flight Docks and Flight Cargo Hooks" }),

            new Vehicle(078, "Samurai Mech"           , "Lego Ninjago", new List<string>() { "Mech Walker","Super Strength" }),
            new Vehicle(079, "Samurai Shooter"        , "Lego Ninjago", new List<string>() { "Silver LEGO Blowup","Mech Walker","Super Strength" }),
            new Vehicle(080, "Soaring Samurai Mech"   , "Lego Ninjago", new List<string>() {  "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Mech Walker","Super Strength" }),

            new Vehicle(081, "Companion Cube"         , "Portal 2", new List<string>() { "Weight Switch" }),
            new Vehicle(082, "Laser Deflector"        , "Portal 2", new List<string>() { "Laser Deflector","Weight Switch" }),
            new Vehicle(083, "Gold Heart Emitter"     , "Portal 2", new List<string>() { "Hearts Regenerate","Weight Switch","Laser Deflector" }),

            new Vehicle(084, "Sentry Turret"          , "Portal 2", new List<string>() { "Turret Switches" }),
            new Vehicle(085, "Turret Striker"         , "Portal 2", new List<string>() { "Gold LEGO Blowup","Turret Switches" }),
            new Vehicle(086, "Flying Turret Carrier"  , "Portal 2", new List<string>() { "Silver LEGO Blowup","Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Turret Switches" }),

            new Vehicle(087, "Scooby Snack"           , "Scooby-Doo", new List<string>() { "Super Strength" }),
            new Vehicle(088, "Scooby Fire Snack"      , "Scooby-Doo", new List<string>() { "Gold LEGO Blowup","Super Strength" }),
            new Vehicle(089, "Scooby Ghost Snack"     , "Scooby-Doo", new List<string>() { "Stealth","Gold LEGO Blowup","Super Strength" }),

            new Vehicle(090, "Cloud Cukko Car"        , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(091, "X-Stream Soaker"        , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Water Spray" }),
            new Vehicle(092, "Rainbow Cannon"         , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack","Water Spray" }),

            new Vehicle(093, "Invisible Jet"          , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Stealth" }),
            new Vehicle(094, "Stealth Laser Shooter"  , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Stealth","Gold LEGO Blowup" }),
            new Vehicle(095, "Torpedo Bomber"         , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Stealth","Gold LEGO Blowup","Silver LEGO Blowup" }),

            new Vehicle(096, "Ninja Copter"           , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(097, "Glaciator"              , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack" }),
            new Vehicle(098, "Freeze Fighter"         , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Special Attack" }),

            new Vehicle(099, "Traveling Time Train"   , "Back to the Future", new List<string>() { "Accelerator Switches","Time Travel" }),
            new Vehicle(100, "Flying Time Train "     , "Back to the Future", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Time Travel","Accelerator Switches","Tow Bar" }),
            new Vehicle(101, "Missile Blast Time Train","Back to the Future", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Time Travel","Accelerator Switches","Tow Bar" }),

            new Vehicle(102, "Aqua Watercraft"        , "DC Comics", new List<string>() { "Sail","Dive" }),
            new Vehicle(103, "Seven Seas Speeder"     , "DC Comics", new List<string>() { "Speed Boost","Special Weapon","Sail","Dive" }),
            new Vehicle(104, "Trident of Fire"        , "DC Comics", new List<string>() { "Silver LEGO Blowup","Special Attack","Speed Boost","Special Weapon","Sail","Dive" }),

            new Vehicle(105, "Drill Driver"           , "DC Comics", new List<string>() { "Accelerator Switches","Drill","Dig" }),
            new Vehicle(106, "Bane Dig 'n' Drill"     , "DC Comics", new List<string>() { "Tow Bar","Dig","Drill","Accelerator Switches" }),
            new Vehicle(107, "Bane Drill 'n' Blast"   , "DC Comics", new List<string>() { "Special Attack","Silver LEGO Blowup","Drill","Dig","Accelerator Switches","Tow Bar" }),

            new Vehicle(108, "Quinn-mobile"           , "DC Comics", new List<string>() { "Accelerator Switches" }),
            new Vehicle(109, "Quinn Ultra Racer"      , "DC Comics", new List<string>() { "Speed Boost","Tow Bar","Accelerator Switches" }),
            new Vehicle(110, "Missile Launcher"       , "DC Comics", new List<string>() { "Silver LEGO Blowup","Speed Boost","Tow Bar","Accelerator Switches" }),

            new Vehicle(111, "The Jokers Chopper"     , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack" }),
            new Vehicle(112, "Mischievous Missile Blaster","DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack" }),
            new Vehicle(113, "Lock 'n' Laser Jet"     , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Special Attack" }),

            new Vehicle(114, "Hover Pod"              , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(115, "Krypton Striker"        , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks,Special Weapon" }),
            new Vehicle(116, "Hover Pod 2"            , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Special Weapon" }),

            new Vehicle(117, "Dalek"                  , "Doctor Who", new List<string>() { "" }),
            new Vehicle(118, "Fire 'n' Ride Dalek"    , "Doctor Who", new List<string>() { "Gold LEGO Blowup" }),
            new Vehicle(119, "Silver Shooter Dalek"   , "Doctor Who", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Gold LEGO Blowup" }),

            new Vehicle(120, "Ecto-1"                 , "Ghostbusters", new List<string>() { "Accelerator Switches" }),
            new Vehicle(121, "Ecto-1 Blaster"         , "Ghostbusters", new List<string>() { "Accelerator Switches","Water Spray","Tow Bar" }),
            new Vehicle(122, "Ecto-1 Water Diver"     , "Ghostbusters", new List<string>() { "Dive","Silver LEGO Blowup","Accelerator Switches","Water Spray","Tow Bar" }),

            new Vehicle(123, "Ghost Trap"             , "Ghostbusters", new List<string>() { "Ghost Trap" }),
            new Vehicle(124, "Ghost Stun'n'Trap"      , "Ghostbusters", new List<string>() { "Ghost Trap","Special Attack" }),
            new Vehicle(125, "Proton Zapper"          , "Ghostbusters", new List<string>() { "Gold LEGO Blowup","Special Weapon,Ghost Trap","Special Attack" }),

            new Vehicle(126, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(127, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(128, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(129, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(130, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(131, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(132, "Llyod's Golden Dragon"  , "Lego Ninjago", new List<string>() { "Flying" }),
            new Vehicle(133, "Sword Projector Dragon", "Lego Ninjago", new List<string>() { "Flying","Special Attack" }),
            new Vehicle(134, "Llyod's Golden Dragon 2", "Lego Ninjago", new List<string>() { "Unknown" }),

            new Vehicle(135, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(136, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(137, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(138, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(139, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(140, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(141, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(142, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(143, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(144, "Mega Flight Dragon"     , "Lego Ninjago", new List<string>() { "Flying","Special Weapon","Special Attack" }),
            new Vehicle(145, "Mega Flight Dragon 1"   , "Lego Ninjago", new List<string>() { "Unknown" }),
            new Vehicle(146, "Mega Flight Dragon 2"   , "Lego Ninjago", new List<string>() { "Unknown" }),

            new Vehicle(147, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(148, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(149, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(150, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(151, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(152, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(153, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(154, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(155, "Flying White Dragon"    , "Lego Ninjago", new List<string>() { "Flying" }),
            new Vehicle(156, "Golden Fire Dragon"     , "Lego Ninjago", new List<string>() { "Flying","Special Attack" }),
            new Vehicle(157, "Ultra Destruction Dragon","Lego Ninjago", new List<string>() { "Flying","Special Weapon","Special Attack" }),

            new Vehicle(158, "Arcade Machine"         , "Midway Arcade", new List<string>() { "Arcade Docks" }),
            new Vehicle(159, "8-bit Shooter"          , "Midway Arcade", new List<string>() { "Flying","Arcade Docks" }),
            new Vehicle(160, "The Pixelator"          , "Midway Arcade", new List<string>() { "Special Attack","Arcade Docks","Flying" }),

            new Vehicle(161, "G-61555 Spy Hunter"     , "Midway Arcade", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(162, "The Interdiver"         , "Midway Arcade", new List<string>() { "Sail","Silver LEGO Blowup","Accelerator Switches","Tow Bar" }),
            new Vehicle(163, "Aerial Spyhunter"       , "Midway Arcade", new List<string>() {"Flying","Flight Docks and Flight Cargo Hooks,Gold LEGO Blowup","Sail","Silver LEGO Blowup","Accelerator Switches","Tow Bar"  }),

            new Vehicle(164, "Slime Shooter"          , "Ghostbusters", new List<string>() { "Slime Bolts","Special Attack" }),
            new Vehicle(165, "Slime Exploder"         , "Ghostbusters", new List<string>() { "Slime Beam","Slime Bolts","Special Attack" }),
            new Vehicle(166, "Slime Streamer"         , "Ghostbusters", new List<string>() { "Slime Bomb","Silver LEGO Blowup","Slime Beam","Slime Bolts","Special Attack" }),

            new Vehicle(167, "Terror Dog"             , "Ghostbusters", new List<string>() { "Guardian Ability" }),
            new Vehicle(168, "Terror Dog Destroyer"   , "Ghostbusters", new List<string>() { "Silver LEGO Blowup,Dig","Guardian Ability" }),
            new Vehicle(169, "Soaring Terror Dog"     , "Ghostbusters", new List<string>() { "Flying","Special Weapon","Silver LEGO Blowup,Dig","Guardian Ability" }),

            new Vehicle(170, "Tandem War Elefant"     , "Adventure Time", new List<string>() { "Hover","Gold LEGO Blowup","Guardian Ability" }),
            new Vehicle(171, "Cosmic Squid"           , "Adventure Time", new List<string>() { "Flight Docks and Flight Cargo Hooks","Tow Bar","Water Spray","Hover","Gold LEGO Blowup","Guardian Ability" }),
            new Vehicle(172, "Psychic Submarine"      , "Adventure Time", new List<string>() { "Gold LEGO Blowup","Underwater Interactions","Underwater Drone","Flight Docks and Flight Cargo Hooks","Tow Bar,Water Spray","Hover","Guardian Ability" }),

            new Vehicle(173, "BMO"                    , "Adventure Time", new List<string>() { "BMO Docks","Illumination","Guardian Ability" }),
            new Vehicle(174, "DOGMO"                  , "Adventure Time", new List<string>() { "Dig","Illumination","Guardian Ability","BMO Docks" }),
            new Vehicle(175, "SNAKEMO"                , "Adventure Time", new List<string>() { "Electricity","Dig","Illumination","Guardian Ability","BMO Docks" }),

            new Vehicle(176, "Jakemobile"             , "Adventure Time", new List<string>() { "Tow Bar","Guardian Ability","Accelerator Switches" }),
            new Vehicle(177, "Snail Dude Jake"        , "Adventure Time", new List<string>() { "Sonar Smash","Super Jump","Super Strength","Guardian Ability","Tow Bar","Accelerator Switches" }),
            new Vehicle(178, "Hover Jake"             , "Adventure Time", new List<string>() { "Sail","Tow Bar","Water Spray","Guardian Ability","Sonar Smash","Super Jump","Super Strength","Tow Bar","Accelerator Switches" }),

            new Vehicle(179, "Lumpy Car"              , "Adventure Time", new List<string>() { "Accelerator Switches","Tow Bar","Jump" }),
            new Vehicle(180, "Lumpy Land Whale"       , "Adventure Time", new List<string>() { "Underwater Drone","Sonar Smash","Underwater Interactions","Accelerator Switches","Tow Bar","Jump" }),
            new Vehicle(181, "Lumpy Truck"            , "Adventure Time", new List<string>() { "Rainbow LEGO Objects","Gold LEGO Blowup","Flight Docks and Flight Cargo Hooks","Underwater Drone","Sonar Smash,Underwater Interactions","Accelerator Switches","Tow Bar","Jump" }),

            new Vehicle(182, "Lunatic Amp"            , "Adventure Time", new List<string>() { "Sonar Smash","Super Jump","Dig","Tow Bar" }),
            new Vehicle(183, "Shadow Scorpion"        , "Adventure Time", new List<string>() { "Flight Docks and Flight Cargo Hooks","Special Attack","Tow Bar","Sonar Smash","Super Jump","Dig" }),
            new Vehicle(184, "Heavy Metal Monster"    , "Adventure Time", new List<string>() { "Guardian Ability","Super Strength","Cursed Red LEGO Objects","Flight Docks and Flight Cargo Hooks","Special Attack","Tow Bar","Sonar Smash","Super Jump","Dig" }),

            new Vehicle(185, "B.A.'s Van"             , "The A-Team", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(186, "Fool Shmasher"          , "The A-Team", new List<string>() { "Accelerator Switches","Silver LEGO Blowup","Special Attack","Super Strength","Tow Bar" }),
            new Vehicle(187, "Pain Plane"             , "The A-Team", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Sail","Accelerator Switches","Silver LEGO Blowup","Special Attack","Super Strength","Tow Bar" }),

            new Vehicle(188, "Phone Home"             , "E.T. the Extra-Terrestrial", new List<string>() { "Phone Home","Sonar Smash" }),
            new Vehicle(189, "Mobile Uplink"          , "E.T. the Extra-Terrestrial", new List<string>() { "Phone Home","Special Attack","Sonar Smash" }),
            new Vehicle(190, "Super-Charged Satellite", "E.T. the Extra-Terrestrial", new List<string>() { "Silver LEGO Blowup","Gold LEGO Blowup","Flight Docks and Flight Cargo Hooks","Tow Bar","Phone Home","Special Attack","Sonar Smash" }),

            new Vehicle(191, "Niffler"                , "Fantastic Beasts and Where to Find Them", new List<string>() { "Playable Character","Enhanced Stud Attraction","Dig","Guardian Ability" }),
            new Vehicle(192, "Sinister Scorpion"      , "Fantastic Beasts and Where to Find Them", new List<string>() { "Vine Cut","Special Weapon","Playable Character","Enhanced Stud Attraction","Dig","Guardian Ability" }),
            new Vehicle(193, "Vicious Vulture"        , "Fantastic Beasts and Where to Find Them", new List<string>() { "Gold LEGO Blowup","Guardian Ability","Vine Cut","Special Weapon","Playable Character","Enhanced Stud Attraction","Dig" }),

            new Vehicle(194, "Swooping Evil"          , "Fantastic Beasts and Where to Find Them", new List<string>() { "Playable Character","Guardian Ability" }),
            new Vehicle(195, "Brutal Bloom"           , "Fantastic Beasts and Where to Find Them", new List<string>() { "Special Weapon","Guardian Ability","Tow Bar","Vine Cut","Playable Character" }),
            new Vehicle(196, "Crawling Creeper"       , "Fantastic Beasts and Where to Find Them", new List<string>() { "Super Jump","Electricity","Dig","Special Weapon","Guardian Ability","Tow Bar","Vine Cut","Playable Character" }),

            new Vehicle(197, "Ecto-1 (2016)"          , "Ghostbusters 2016", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(198, "Ectozer"                , "Ghostbusters 2016", new List<string>() { "Accelerator Switches","Tow Bar","Special Attack","Super Strength" }),
            new Vehicle(199, "PerfEcto"               , "Ghostbusters 2016", new List<string>() { "Electricity","Water Spray","Flying","Sail","Accelerator Switches","Tow Bar","Special Attack","Super Strength" }),

            new Vehicle(200, "Flash 'n' Finish"       , "Gremlins", new List<string>() { "Illumination","Sonar Smash","Gold LEGO Blowup" }),
            new Vehicle(201, "Rampage Record Player"  , "Gremlins", new List<string>() { "Special Attack","Special Weapon","Illumination","Sonar Smash","Gold LEGO Blowup" }),
            new Vehicle(202, "Stripe's Throne"        , "Gremlins", new List<string>() { "Super Jump","Cursed Red LEGO Objects","Special Attack","Special Weapon","Illumination","Sonar Smash","Gold LEGO Blowup" }),

            new Vehicle(203, "R.C. Car"               , "Gremlins", new List<string>() { "Accelerator Switches","Special Attack","Electricity" }),
            new Vehicle(204, "Gadget-o-matic"         , "Gremlins", new List<string>() { "Accelerator Switches","Special Attack,Tow Bar","Electricity" }),
            new Vehicle(205, "Scarlet Scorpion"       , "Gremlins", new List<string>() { "Super Jump","Vine Cut","Accelerator Switches","Special Attack","Tow Bar","Electricity" }),

            new Vehicle(206, "Hogward Express"        , "Harry Potter", new List<string>() { "Accelerator Switches","Tow Bar","Sonar Smash" }),
            new Vehicle(207, "Steam Warrior"          , "Harry Potter", new List<string>() { "Super Strength","Super Jump","Special Attack","Guardian Ability","Accelerator Switches","Tow Bar","Sonar Smash" }),
            new Vehicle(208, "Soaring Steam Plane"    , "Harry Potter", new List<string>() { "Gold LEGO Blowup","Flight Docks and Flight Cargo Hooks","Super Strength","Super Jump","Special Attack","Guardian Ability","Accelerator Switches","Tow Bar","Sonar Smash" }),

            new Vehicle(209, "Enchanted Car"          , "Harry Potter", new List<string>() { "Accelerator Switches","Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(210, "Shark Sub"              , "Harry Potter", new List<string>() { "Underwater Drone","Underwater Interactions","Sonar Smash","Accelerator Switches","Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(211, "Monstrous Mouth"        , "Harry Potter", new List<string>() { "Vine Cut","Gold LEGO Blowup","Underwater Drone","Underwater Interactions","Sonar Smash","Accelerator Switches","Tow Bar","Flight Docks and Flight Cargo Hooks" }),

            new Vehicle(212, "IMF Scrambler"          , "Mission: Impossible", new List<string>() { "Accelerator Switches","Jump","Special Attack" }),
            new Vehicle(213, "Shock Cycle"            , "Mission: Impossible", new List<string>() { "Accelerator Switches","Jump","Special Weapon","Silver LEGO Blowup","Special Attack" }),
            new Vehicle(214, "IMF Covert Jet"         , "Mission: Impossible", new List<string>() { "Flight Docks and Flight Cargo Hooks","Underwater Interactions","Special Weapon","Tow Bar","Accelerator Switches","Jump","Silver LEGO Blowup","Special Attack" }),

            new Vehicle(215, "IMF Sports Car"         , "Mission: Impossible", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(216, "IMF Tank"               , "Mission: Impossible", new List<string>() { "Super Strength","Gold LEGO Blowup","Tow Bar","Accelerator Switches" }),
            new Vehicle(217, "IMF Splorer"            , "Mission: Impossible", new List<string>() { "Underwater Drone","Underwater Interactions","Super Strength","Gold LEGO Blowup","Tow Bar","Accelerator Switches" }),

            new Vehicle(218, "Sonic Speedster"        , "Sonic the Hedgehog", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(219, "Blue Typhoon"           , "Sonic the Hedgehog", new List<string>() { "Flight Docks and Flight Cargo Hooks","Sail","Sonar Smash","Tow Bar","Accelerator Switches" }),
            new Vehicle(220, "Moto Bug"               , "Sonic the Hedgehog", new List<string>() { "Accelerator Switches","Dig","Vine Cut","Super Jump","Flight Docks and Flight Cargo Hooks","Sail","Sonar Smash","Tow Bar" }),

            new Vehicle(221, "The Tornado"            , "Sonic the Hedgehog", new List<string>() { "Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(222, "Crabmeat"               , "Sonic the Hedgehog", new List<string>() { "Jump","Gold LEGO Blowup","Super Jump","Guardian Ability","Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(223, "Eggcatcher"             , "Sonic the Hedgehog", new List<string>() { "Tow Bar","Flight Docks and Flight Cargo Hooks","Electricity","Jump","Gold LEGO Blowup","Super Jump","Guardian Ability" }),

            new Vehicle(224, "K.I.T.T."               , "Knight Rider", new List<string>() { "Accelerator Switches","Gold LEGO Blowup" }),
            new Vehicle(225, "Goliath Armored Semi"   , "Knight Rider", new List<string>() { "Tow Bar","Super Strength","Electricity","Accelerator Switches","Gold LEGO Blowup" }),
            new Vehicle(226, "K.I.T.T. Jet"           , "Knight Rider", new List<string>() { "Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Silver LEGO Blowup","Tow Bar","Super Strength","Electricity","Accelerator Switches" }),

            new Vehicle(227, "Unknown"                , "Unknown", new List<string>() { "Flight Docks and Flight Cargo Hooks","Special Attack","Tow Bar" }),
            new Vehicle(228, "Unknown"                , "Unknown", new List<string>() { "Gold LEGO Blowup","Accelerator Switches","Super Jump","Special Attack","Flight Docks and Flight Cargo Hooks","Tow Bar" }),
            new Vehicle(229, "Unknown"                , "Unknown", new List<string>() { "Flight Docks and Flight Cargo Hooks","Tow Bar","Drone Mazes","Gold LEGO Blowup","Accelerator Switches","Super Jump","Special Attack" }),

            new Vehicle(230, "Bionic Steed"           , "The LEGO Batman Movie", new List<string>() { "Super Jump","Special Weapon" }),
            new Vehicle(231, "Bat Raptor"             , "The LEGO Batman Movie", new List<string>() { "Super Jump","Super Strength","Special Weapon" }),
            new Vehicle(232, "Ultrabat"               , "The LEGO Batman Movie", new List<string>() { "Sonar Smash","Super Strength","Super Jump","Super Strength","Special Weapon" }),

            new Vehicle(233, "Batwing"                , "The LEGO Batman Movie", new List<string>() { "Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(234, "The Black Thunder"      , "The LEGO Batman Movie", new List<string>() { "Accelerator Switches","Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(235, "Bat-Tank"               , "The LEGO Batman Movie", new List<string>() { "Accelerator Switches","Tow Bar","Silver LEGO Blowup","Flight Docks and Flight Cargo Hooks" }),

            new Vehicle(236, "Skeleton Orga"          , "The Goonies", new List<string>() { "Organ Docks","Special Weapon","Sonar Smash" }),
            new Vehicle(237, "Skeleton Jukebox"       , "The Goonies", new List<string>() { "Jump","Special Weapon","Electricity","Organ Docks","Sonar Smash" }),
            new Vehicle(238, "Skele-Turkey"           , "The Goonies", new List<string>() { "Gold LEGO Blowup","Flight Docks and Flight Cargo Hooks","Tow Bar","Jump","Special Weapon","Electricity","Organ Docks","Sonar Smash" }),

            new Vehicle(239, "One-Eyed Willy's Pirate Ship", "The Goonies", new List<string>() { "Sail","Silver LEGO Blowup","Special Attack" }),
            new Vehicle(240, "Fanged Fortune"         , "The Goonies", new List<string>() { "Water Spray","Special Attack","Vine Cut","Sail","Silver LEGO Blowup" }),
            new Vehicle(241, "Inferno Cannon"         , "The Goonies", new List<string>() { "Special Attack","Special Weapon","Silver LEGO Blowup","Water Spray","Vine Cut","Sail" }),

            new Vehicle(242, "Buckbeak"               , "Harry Potter", new List<string>() { "Super Strength","Flying","Guardian Ability","Silver LEGO Blowup" }),
            new Vehicle(243, "Giant Owl"              , "Harry Potter", new List<string>() { "Flying","Electricity","Super Strength","Guardian Ability","Silver LEGO Blowup" }),
            new Vehicle(244, "Fierce Falcon"          , "Harry Potter", new List<string>() { "Flying","Special Weapon","Sonar Smash","Electricity","Super Strength","Guardian Ability","Silver LEGO Blowup" }),

            new Vehicle(245, "Saturn's Sandworm"      , "Beetlejuice", new List<string>() { "Sonar Smash" }),
            new Vehicle(246, "Spooky Spider"          , "Beetlejuice", new List<string>() { "Special Weapon","Super Jump","Cursed Red LEGO Objects","Enhanced Stud Attraction","Sonar Smash" }),
            new Vehicle(247, "Haunted Vacuum"         , "Beetlejuice", new List<string>() { "Gold LEGO Blowup","Tow Bar","Silver LEGO Blowup","Special Weapon","Super Jump","Cursed Red LEGO Objects","Enhanced Stud Attraction","Sonar Smash" }),

            new Vehicle(248, "PPG Smartphone"         , "The Powerpuff Girls", new List<string>() { "Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup" }),
            new Vehicle(249, "PPG Hotline"            , "The Powerpuff Girls", new List<string>() { "Special Weapon","Rainbow LEGO Objects","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup" }),
            new Vehicle(250, "Powerpuff Mag-Net"      , "The Powerpuff Girls", new List<string>() { "Special Weapon","Enhanced Stud Attraction","Silver LEGO Blowup","Rainbow LEGO Objects","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup" }),

            new Vehicle(251, "Ka-Pow Cannon"          , "The Powerpuff Girls", new List<string>() { "Special Weapon","Silver LEGO Blowup" }),
            new Vehicle(252, "Slammin' Guitar"        , "The Powerpuff Girls", new List<string>() { "Special Weapon","Super Strength","Accelerator Switches","Tow Bar","Silver LEGO Blowup" }),
            new Vehicle(253, "Megablast-Bot"          , "The Powerpuff Girls", new List<string>() { "Special Attack","Special Weapon","Sonar Smash","Super Strength","Accelerator Switches","Tow Bar","Silver LEGO Blowup" }),

            new Vehicle(254, "Octi"                   , "The Powerpuff Girls", new List<string>() { "Gold LEGO Blowup" }),
            new Vehicle(255, "Super SKunk"            , "The Powerpuff Girls", new List<string>() { "Vine Cut","Super Jump","Dig","Gold LEGO Blowup" }),
            new Vehicle(256, "Sonic Squid"            , "The Powerpuff Girls", new List<string>() { "Water Spray","Vine Cut","Super Jump","Dig","Gold LEGO Blowup" }),

            new Vehicle(257, "T-Car"                  , "Teen Titans Go!", new List<string>() { "Super Jump","Accelerator Switches","Tow Bar" }),
            new Vehicle(258, "T-Forklift"             , "Teen Titans Go!", new List<string>() { "Super Jump","Accelerator Switches","Drone Mazes","Tow Bar" }),
            new Vehicle(259, "T-Plane"                , "Teen Titans Go!", new List<string>() { "Accelerator Switches","Flight Docks and Flight Cargo Hooks","Tow Bar","Sail","Super Jump","Drone Mazes" }),

            new Vehicle(260, "Spellbook of Azarath"   , "Teen Titans Go!", new List<string>() { "Summon Ability","Rainbow LEGO Objects","Cursed Red LEGO Objects","Sonar Smash" }),
            new Vehicle(261, "Raven Wings"            , "Teen Titans Go!", new List<string>() { "Cursed Red LEGO Objects","Flying","Gold LEGO Blowup","Tow Bar","Sonar Smash","Summon Ability","Rainbow LEGO Objects" }),
            new Vehicle(262, "Giant Hand"             , "Teen Titans Go!", new List<string>() { "Super Jump","Dig","Super Strength","Cursed Red LEGO Objects","Flying","Gold LEGO Blowup","Tow Bar","Sonar Smash","Summon Ability","Rainbow LEGO Objects" }),

            new Vehicle(263, "Titan Robot"            , "Teen Titans Go!", new List<string>() { "Special Weapon","Super Strength","Tow Bar","Silver LEGO Blowup" }),
            new Vehicle(264, "T-Rocket"               , "Teen Titans Go!", new List<string>() { "Gold LEGO Blowup","Flying","Special Weapon","Super Strength","Tow Bar","Silver LEGO Blowup" }),
            new Vehicle(265, "Robot Retriever"        , "Teen Titans Go!", new List<string>() { "Super Jump","Dig","Electricity","Gold LEGO Blowup","Flying","Special Weapon","Super Strength","Tow Bar","Silver LEGO Blowup" }),
            };
    }
}
