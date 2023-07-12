// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions.Tag
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
                var id = Id - 1000;
                if (id < 155)
                {
                    return (VehicleRebuild)(id % 3);
                }

                return (VehicleRebuild)((id + 1) % 3);
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
            new Vehicle(0000, "Empty Vehicle Tag"             , "Unknown", new List<string>() { "" }),

            new Vehicle(1000, "Police Car"             , "The Lego Movie", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(1001, "Aerial Squad Car"       , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Accelerator Switches","Tow Bar" }),
            new Vehicle(1002, "Missile Striker"        , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Accelerator Switches","Tow Bar" }),

            new Vehicle(1003, "Gravity Sprinter"       , "The Simpsons", new List<string>() { "Accelerator Switches" }),
            new Vehicle(1004, "Street Shredder"        , "The Simpsons", new List<string>() { "Accelerator Switches","Speed Boost","Tow Bar" }),
            new Vehicle(1005, "Sky Clobberer"          , "The Simpsons", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack","Accelerator Switches","Speed Boost","Tow Bar" }),

            new Vehicle(1006, "Batmobile"              , "DC Comics", new List<string>() { "Accelerator Switches" }),
            new Vehicle(1007, "Batblaster"             , "DC Comics", new List<string>() { "Accelerator Switches","Sonar Smash","Tow Bar" }),
            new Vehicle(1008, "Sonic Batray"           , "DC Comics", new List<string>() { "Accelerator Switches","Special Attack","Sonar Smash","Tow Bar" }),

            new Vehicle(1009, "Benny's Spaceship"      , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1010, "Lasercraft"             , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup" }),
            new Vehicle(1011, "The Annihilator"        , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Gold LEGO Blowup" }),

            new Vehicle(1012, "DeLorean Time Machine"  , "Back to the Future", new List<string>() { "Accelerator Switches","Time Travel" }),
            new Vehicle(1013, "Ultra Time Machine"     , "Back to the Future", new List<string>() { "Special Attack","Electricity","Tow Bar","Accelerator Switches","Time Travel" }),
            new Vehicle(1014, "Electric Time Machine"  , "Back to the Future", new List<string>() { "Silver LEGO Blowup","Flying","Flight Docks and Flight Cargo Hooks","Time Travel","Special Attack","Electricity","Tow Bar" }),

            new Vehicle(1015, "Hoverboard"             , "Back to the Future", new List<string>() { "Hover" }),
            new Vehicle(1016, "Cyclone Board"          , "Back to the Future", new List<string>() { "Special Attack","Hover" }),
            new Vehicle(1017, "Ultimate Hoverjet"      , "Back to the Future", new List<string>() { "Flying","Special Attack","Silver LEGO Blowup","Hover" }),

            new Vehicle(1018, "Eagle Interceptor"      , "Lego Legends of Chima", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1019, "Eagle Skyblazer"        , "Lego Legends of Chima", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Speed Boost","Silver LEGO Blowup" }),
            new Vehicle(1020, "Eagle Swoop Diver"      , "Lego Legends of Chima", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack","Gold LEGO Blowup","Speed Boost","Silver LEGO Blowup" }),

            new Vehicle(1021, "Swamp Skimmer"          , "Lego Legends of Chima", new List<string>() { "Sail" }),
            new Vehicle(1022, "Croc Command Sub"       , "Lego Legends of Chima", new List<string>() { "Sail","Speed Boost","Special Attack" }),
            new Vehicle(1023, "Cragger's Fireship"     , "Lego Legends of Chima", new List<string>() { "Dive","Silver LEGO Blowup","Sail","Speed Boost","Special Attack" }),

            new Vehicle(1024, "Cyber Guard"            , "DC Comics", new List<string>() { "Mech Walker" }),
            new Vehicle(1025, "Cyber-Wrecker"          , "DC Comics", new List<string>() { "Mech Walker","Super Strength","Dig" }),
            new Vehicle(1026, "Laser Robot Walker"     , "DC Comics", new List<string>() { "Gold LEGO Blowup","Mech Walker","Super Strength","Dig" }),

            new Vehicle(1027, "K9"                     , "Doctor Who", new List<string>() { "Silver LEGO Blowup" }),
            new Vehicle(1028, "K9 Ruff Rover"          , "Doctor Who", new List<string>() { "Sonar Smash","Silver LEGO Blowup" }),
            new Vehicle(1029, "K9 Laser Cutter"        , "Doctor Who", new List<string>() { "Gold LEGO Blowup","Sonar Smash","Silver LEGO Blowup" }),

            new Vehicle(1030, "TARDIS"                 , "Doctor Who", new List<string>() { "TARDIS Travel","Stealth","Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1031, "Laser-Pulse TARDIS"     , "Doctor Who", new List<string>() { "TARDIS Travel","Stealth","Flying","Gold LEGO Blowup" }),
            new Vehicle(1032, "Energy-Burst TARDIS"    , "Doctor Who", new List<string>() { "TARDIS Travel","Stealth","Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup" }),

            new Vehicle(1033, "Emmet's Excavator"      , "The Lego Movie", new List<string>() { "Accelerator Switches","Dig" }),
            new Vehicle(1034, "The Destroydozer"       , "The Lego Movie", new List<string>() { "Accelerator Switches","Dig","Tow Bar" }),
            new Vehicle(1035, "Destruct-o-Mech"        , "The Lego Movie", new List<string>() { "Mech Walker","Super Strength","Accelerator Switches","Dig","Tow Bar" }),

            new Vehicle(1036, "Winged Monkey"          , "Wizard of Oz", new List<string>() { "" }),
            new Vehicle(1037, "Battle Monkey"          , "Wizard of Oz", new List<string>() { "Special Attack","Silver LEGO Blowup","Flying" }),
            new Vehicle(1038, "Commander Monkey"       , "Wizard of Oz", new List<string>() { "Flying","Special Weapon","Sonar Smash","Special Attack","Silver LEGO Blowup" }),

            new Vehicle(1039, "Axe Chariot"            , "Lord of the Rings", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(1040, "Axe Hurler"             , "Lord of the Rings", new List<string>() { "Accelerator Switches","Special Attack","Tow Bar" }),
            new Vehicle(1041, "Soaring Chariot"        , "Lord of the Rings", new List<string>() { "Accelerator Switches","Flying","Flight Docks and Flight Cargo Hooks","Tow Bar","Special Attack" }),

            new Vehicle(1042, "Shelob the Great"       , "Lord of the Rings", new List<string>() { "Dig" }),
            new Vehicle(1043, "8-Legged Stalker"       , "Lord of the Rings", new List<string>() { "Super Strength","Special Attack","Dig" }),
            new Vehicle(1044, "Poison Slinger"         , "Lord of the Rings", new List<string>() { "Special Attack","Dig","Super Strength" }),

            new Vehicle(1045, "Homer's Car"            , "The Simpsons", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(1046, "The SubmaHomer"         , "The Simpsons", new List<string>() { "Dive","Silver LEGO Blowup","Accelerator Switches","Tow Bar" }),
            new Vehicle(1047, "The Homecraft"          , "The Simpsons", new List<string>() { "Sail","Dive","Silver LEGO Blowup","Accelerator Switches","Tow Bar" }),

            new Vehicle(1048, "Taunt-o-Vision"         , "The Simpsons", new List<string>() { "Taunt","Silver LEGO Blowup" }),
            new Vehicle(1049, "The MechaHomer"         , "The Simpsons", new List<string>() { "Gold LEGO Blowup","Taunt","Silver LEGO Blowup" }),
            new Vehicle(1050, "Blast Cam"              , "The Simpsons", new List<string>() { "Special Weapon","Gold LEGO Blowup","Taunt","Silver LEGO Blowup" }),

            new Vehicle(1051, "Velociraptor"           , "Jurassic World", new List<string>() { "Guardian Ability","Vine Cut" }),
            new Vehicle(1052, "Spike Attack Raptor"    , "Jurassic World", new List<string>() { "Special Attack","Vine Cut","Guardian Ability","Super Strength" }),
            new Vehicle(1053, "Venom Raptor"           , "Jurassic World", new List<string>() { "Super Strength","Dig","Vine Cut","Guardian Ability","Special Attack" }),

            new Vehicle(1054, "Gyrosphere"             , "Jurassic World", new List<string>() { "Gyrosphere Switch" }),
            new Vehicle(1055, "Sonic Beam Gyrosphere"  , "Jurassic World", new List<string>() { "Gyrosphere Switch","Sonar Smash" }),
            new Vehicle(1056, "Speed Boost Gyrosphere" , "Jurassic World", new List<string>() { "Gyrosphere Switch","Speed Boost","Sonar Smash" }),

            new Vehicle(1057, "Clown Bike"             , "The Simpsons", new List<string>() { "Accelerator Switches" }),
            new Vehicle(1058, "Cannon Bike"            , "The Simpsons", new List<string>() { "Accelerator Switches","Special Attack","Tow Bar" }),
            new Vehicle(1059, "Anti-Gravity Rocket Bike","The Simpsons", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Accelerator Switches","Special Attack","Tow Bar" }),

            new Vehicle(1060, "Mighty Lion Rider"      , "Lego Legends of Chima", new List<string>() { "Accelerator Switches" }),
            new Vehicle(1061, "Lion Blazer"            , "Lego Legends of Chima", new List<string>() { "Special Attack","Tow Bar","Accelerator Switches" }),
            new Vehicle(1062, "Fire Lion"              , "Lego Legends of Chima", new List<string>() { "Special Weapon","Special Attack","Tow Bar","Accelerator Switches" }),

            new Vehicle(1063, "Arrow Launcher"         , "Lord of the Rings", new List<string>() { "Accelerator Switches" }),
            new Vehicle(1064, "Seeking Shooter"        , "Lord of the Rings", new List<string>() { "Accelerator Switches","Special Attack","Tow Bar" }),
            new Vehicle(1065, "Triple Ballista"        , "Lord of the Rings", new List<string>() { "Accelerator Switches","Special Weapon","Special Attack","Tow Bar" }),

            new Vehicle(1066, "Mystery Machine"        , "Scooby-Doo", new List<string>() { "Accelerator Switches" }),
            new Vehicle(1067, "Mystery Tow"            , "Scooby-Doo", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(1068, "Mystery Monster"        , "Scooby-Doo", new List<string>() { "Accelerator Switches","Water Spray","Tow Bar" }),

            new Vehicle(1069, "Boulder Bomber"         , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1070, "Boulder Blaster"        , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup" }),
            new Vehicle(1071, "Cyclone Jet"            , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack","Silver LEGO Blowup" }),

            new Vehicle(1072, "Storm Fighter"          , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1073, "Lightning Jet"          , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Electricity" }),
            new Vehicle(1074, "Electro-Shooter"        , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Electricity","Special Attack" }),

            new Vehicle(1075, "Blade Bike"             , "Lego Ninjago", new List<string>() { "Accelerator Switches" }),
            new Vehicle(1076, "Flying Fire Bike"       , "Lego Ninjago", new List<string>() { "Accelerator Switches","Special Attack","Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1077, "Blades of Fire"         , "Lego Ninjago", new List<string>() { "Accelerator Switches","Special Attack","Flying","Flight Docks and Flight Cargo Hooks" }),

            new Vehicle(1078, "Samurai Mech"           , "Lego Ninjago", new List<string>() { "Mech Walker","Super Strength" }),
            new Vehicle(1079, "Samurai Shooter"        , "Lego Ninjago", new List<string>() { "Silver LEGO Blowup","Mech Walker","Super Strength" }),
            new Vehicle(1080, "Soaring Samurai Mech"   , "Lego Ninjago", new List<string>() {  "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Mech Walker","Super Strength" }),

            new Vehicle(1081, "Companion Cube"         , "Portal 2", new List<string>() { "Weight Switch" }),
            new Vehicle(1082, "Laser Deflector"        , "Portal 2", new List<string>() { "Laser Deflector","Weight Switch" }),
            new Vehicle(1083, "Gold Heart Emitter"     , "Portal 2", new List<string>() { "Hearts Regenerate","Weight Switch","Laser Deflector" }),

            new Vehicle(1084, "Sentry Turret"          , "Portal 2", new List<string>() { "Turret Switches" }),
            new Vehicle(1085, "Turret Striker"         , "Portal 2", new List<string>() { "Gold LEGO Blowup","Turret Switches" }),
            new Vehicle(1086, "Flying Turret Carrier"  , "Portal 2", new List<string>() { "Silver LEGO Blowup","Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Turret Switches" }),

            new Vehicle(1087, "Scooby Snack"           , "Scooby-Doo", new List<string>() { "Super Strength" }),
            new Vehicle(1088, "Scooby Fire Snack"      , "Scooby-Doo", new List<string>() { "Gold LEGO Blowup","Super Strength" }),
            new Vehicle(1089, "Scooby Ghost Snack"     , "Scooby-Doo", new List<string>() { "Stealth","Gold LEGO Blowup","Super Strength" }),

            new Vehicle(1090, "Cloud Cukko Car"        , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1091, "X-Stream Soaker"        , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Water Spray" }),
            new Vehicle(1092, "Rainbow Cannon"         , "The Lego Movie", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack","Water Spray" }),

            new Vehicle(1093, "Invisible Jet"          , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Stealth" }),
            new Vehicle(1094, "Stealth Laser Shooter"  , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Stealth","Gold LEGO Blowup" }),
            new Vehicle(1095, "Torpedo Bomber"         , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Stealth","Gold LEGO Blowup","Silver LEGO Blowup" }),

            new Vehicle(1096, "Ninja Copter"           , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1097, "Glaciator"              , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack" }),
            new Vehicle(1098, "Freeze Fighter"         , "Lego Ninjago", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Special Attack" }),

            new Vehicle(1099, "Traveling Time Train"   , "Back to the Future", new List<string>() { "Accelerator Switches","Time Travel" }),
            new Vehicle(1100, "Flying Time Train "     , "Back to the Future", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Time Travel","Accelerator Switches","Tow Bar" }),
            new Vehicle(1101, "Missile Blast Time Train","Back to the Future", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Time Travel","Accelerator Switches","Tow Bar" }),

            new Vehicle(1102, "Aqua Watercraft"        , "DC Comics", new List<string>() { "Sail","Dive" }),
            new Vehicle(1103, "Seven Seas Speeder"     , "DC Comics", new List<string>() { "Speed Boost","Special Weapon","Sail","Dive" }),
            new Vehicle(1104, "Trident of Fire"        , "DC Comics", new List<string>() { "Silver LEGO Blowup","Special Attack","Speed Boost","Special Weapon","Sail","Dive" }),

            new Vehicle(1105, "Drill Driver"           , "DC Comics", new List<string>() { "Accelerator Switches","Drill","Dig" }),
            new Vehicle(1106, "Bane Dig 'n' Drill"     , "DC Comics", new List<string>() { "Tow Bar","Dig","Drill","Accelerator Switches" }),
            new Vehicle(1107, "Bane Drill 'n' Blast"   , "DC Comics", new List<string>() { "Special Attack","Silver LEGO Blowup","Drill","Dig","Accelerator Switches","Tow Bar" }),

            new Vehicle(1108, "Quinn-mobile"           , "DC Comics", new List<string>() { "Accelerator Switches" }),
            new Vehicle(1109, "Quinn Ultra Racer"      , "DC Comics", new List<string>() { "Speed Boost","Tow Bar","Accelerator Switches" }),
            new Vehicle(1110, "Missile Launcher"       , "DC Comics", new List<string>() { "Silver LEGO Blowup","Speed Boost","Tow Bar","Accelerator Switches" }),

            new Vehicle(1111, "The Jokers Chopper"     , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack" }),
            new Vehicle(1112, "Mischievous Missile Blaster","DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Attack" }),
            new Vehicle(1113, "Lock 'n' Laser Jet"     , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Special Attack" }),

            new Vehicle(1114, "Hover Pod"              , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1115, "Krypton Striker"        , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Special Weapon" }),
            new Vehicle(1116, "Hover Pod 2"            , "DC Comics", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Special Weapon" }),

            new Vehicle(1117, "Dalek"                  , "Doctor Who", new List<string>() { "" }),
            new Vehicle(1118, "Fire 'n' Ride Dalek"    , "Doctor Who", new List<string>() { "Gold LEGO Blowup" }),
            new Vehicle(1119, "Silver Shooter Dalek"   , "Doctor Who", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Silver LEGO Blowup","Gold LEGO Blowup" }),

            new Vehicle(1120, "Ecto-1"                 , "Ghostbusters", new List<string>() { "Accelerator Switches" }),
            new Vehicle(1121, "Ecto-1 Blaster"         , "Ghostbusters", new List<string>() { "Accelerator Switches","Water Spray","Tow Bar" }),
            new Vehicle(1122, "Ecto-1 Water Diver"     , "Ghostbusters", new List<string>() { "Dive","Silver LEGO Blowup","Accelerator Switches","Water Spray","Tow Bar" }),

            new Vehicle(1123, "Ghost Trap"             , "Ghostbusters", new List<string>() { "Ghost Trap" }),
            new Vehicle(1124, "Ghost Stun'n'Trap"      , "Ghostbusters", new List<string>() { "Ghost Trap","Special Attack" }),
            new Vehicle(1125, "Proton Zapper"          , "Ghostbusters", new List<string>() { "Gold LEGO Blowup","Special Weapon","Ghost Trap","Special Attack" }),

            new Vehicle(1126, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1127, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1128, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(1129, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1130, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1131, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(1132, "Llyod's Golden Dragon"  , "Lego Ninjago", new List<string>() { "Flying" }),
            new Vehicle(1133, "Sword Projector Dragon", "Lego Ninjago", new List<string>() { "Flying","Special Attack" }),
            new Vehicle(1134, "Llyod's Golden Dragon 2", "Lego Ninjago", new List<string>() { "Unknown" }),

            new Vehicle(1135, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1136, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1137, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(1138, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1139, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1140, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(1141, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1142, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1143, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(1144, "Mega Flight Dragon"     , "Lego Ninjago", new List<string>() { "Flying","Special Weapon","Special Attack" }),
            new Vehicle(1145, "Mega Flight Dragon 1"   , "Lego Ninjago", new List<string>() { "Unknown" }),
            new Vehicle(1146, "Mega Flight Dragon 2"   , "Lego Ninjago", new List<string>() { "Unknown" }),

            new Vehicle(1147, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1148, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1149, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(1150, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1151, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1152, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(1153, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),
            new Vehicle(1154, "Unknown"                , "Unknown", new List<string>() { "Unknown" }),

            new Vehicle(1155, "Flying White Dragon"    , "Lego Ninjago", new List<string>() { "Flying" }),
            new Vehicle(1156, "Golden Fire Dragon"     , "Lego Ninjago", new List<string>() { "Flying","Special Attack" }),
            new Vehicle(1157, "Ultra Destruction Dragon","Lego Ninjago", new List<string>() { "Flying","Special Weapon","Special Attack" }),

            new Vehicle(1158, "Arcade Machine"         , "Midway Arcade", new List<string>() { "Arcade Docks" }),
            new Vehicle(1159, "8-bit Shooter"          , "Midway Arcade", new List<string>() { "Flying","Arcade Docks" }),
            new Vehicle(1160, "The Pixelator"          , "Midway Arcade", new List<string>() { "Special Attack","Arcade Docks","Flying" }),

            new Vehicle(1161, "G-61555 Spy Hunter"     , "Midway Arcade", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(1162, "The Interdiver"         , "Midway Arcade", new List<string>() { "Sail","Silver LEGO Blowup","Accelerator Switches","Tow Bar" }),
            new Vehicle(1163, "Aerial Spyhunter"       , "Midway Arcade", new List<string>() {"Flying","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Sail","Silver LEGO Blowup","Accelerator Switches","Tow Bar"  }),

            new Vehicle(1164, "Slime Shooter"          , "Ghostbusters", new List<string>() { "Slime Bolts","Special Attack" }),
            new Vehicle(1165, "Slime Exploder"         , "Ghostbusters", new List<string>() { "Slime Beam","Slime Bolts","Special Attack" }),
            new Vehicle(1166, "Slime Streamer"         , "Ghostbusters", new List<string>() { "Slime Bomb","Silver LEGO Blowup","Slime Beam","Slime Bolts","Special Attack" }),

            new Vehicle(1167, "Terror Dog"             , "Ghostbusters", new List<string>() { "Guardian Ability" }),
            new Vehicle(1168, "Terror Dog Destroyer"   , "Ghostbusters", new List<string>() { "Silver LEGO Blowup","Dig","Guardian Ability" }),
            new Vehicle(1169, "Soaring Terror Dog"     , "Ghostbusters", new List<string>() { "Flying","Special Weapon","Silver LEGO Blowup","Dig","Guardian Ability" }),

            new Vehicle(1170, "Tandem War Elefant"     , "Adventure Time", new List<string>() { "Hover","Gold LEGO Blowup","Guardian Ability" }),
            new Vehicle(1171, "Cosmic Squid"           , "Adventure Time", new List<string>() { "Flight Docks and Flight Cargo Hooks","Tow Bar","Water Spray","Hover","Gold LEGO Blowup","Guardian Ability" }),
            new Vehicle(1172, "Psychic Submarine"      , "Adventure Time", new List<string>() { "Gold LEGO Blowup","Underwater Interactions","Underwater Drone","Flight Docks and Flight Cargo Hooks","Tow Bar","Water Spray","Hover","Guardian Ability" }),

            new Vehicle(1173, "BMO"                    , "Adventure Time", new List<string>() { "BMO Docks","Illumination","Guardian Ability" }),
            new Vehicle(1174, "DOGMO"                  , "Adventure Time", new List<string>() { "Dig","Illumination","Guardian Ability","BMO Docks" }),
            new Vehicle(1175, "SNAKEMO"                , "Adventure Time", new List<string>() { "Electricity","Dig","Illumination","Guardian Ability","BMO Docks" }),

            new Vehicle(1176, "Jakemobile"             , "Adventure Time", new List<string>() { "Tow Bar","Guardian Ability","Accelerator Switches" }),
            new Vehicle(1177, "Snail Dude Jake"        , "Adventure Time", new List<string>() { "Sonar Smash","Super Jump","Super Strength","Guardian Ability","Tow Bar","Accelerator Switches" }),
            new Vehicle(1178, "Hover Jake"             , "Adventure Time", new List<string>() { "Sail","Tow Bar","Water Spray","Guardian Ability","Sonar Smash","Super Jump","Super Strength","Tow Bar","Accelerator Switches" }),

            new Vehicle(1179, "Lumpy Car"              , "Adventure Time", new List<string>() { "Accelerator Switches","Tow Bar","Jump" }),
            new Vehicle(1180, "Lumpy Land Whale"       , "Adventure Time", new List<string>() { "Underwater Drone","Sonar Smash","Underwater Interactions","Accelerator Switches","Tow Bar","Jump" }),
            new Vehicle(1181, "Lumpy Truck"            , "Adventure Time", new List<string>() { "Rainbow LEGO Objects","Gold LEGO Blowup","Flight Docks and Flight Cargo Hooks","Underwater Drone","Sonar Smash","Underwater Interactions","Accelerator Switches","Tow Bar","Jump" }),

            new Vehicle(1182, "Lunatic Amp"            , "Adventure Time", new List<string>() { "Sonar Smash","Super Jump","Dig","Tow Bar" }),
            new Vehicle(1183, "Shadow Scorpion"        , "Adventure Time", new List<string>() { "Flight Docks and Flight Cargo Hooks","Special Attack","Tow Bar","Sonar Smash","Super Jump","Dig" }),
            new Vehicle(1184, "Heavy Metal Monster"    , "Adventure Time", new List<string>() { "Guardian Ability","Super Strength","Cursed Red LEGO Objects","Flight Docks and Flight Cargo Hooks","Special Attack","Tow Bar","Sonar Smash","Super Jump","Dig" }),

            new Vehicle(1185, "B.A.'s Van"             , "The A-Team", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(1186, "Fool Shmasher"          , "The A-Team", new List<string>() { "Accelerator Switches","Silver LEGO Blowup","Special Attack","Super Strength","Tow Bar" }),
            new Vehicle(1187, "Pain Plane"             , "The A-Team", new List<string>() { "Flying","Flight Docks and Flight Cargo Hooks","Sail","Accelerator Switches","Silver LEGO Blowup","Special Attack","Super Strength","Tow Bar" }),

            new Vehicle(1188, "Phone Home"             , "E.T. the Extra-Terrestrial", new List<string>() { "Phone Home","Sonar Smash" }),
            new Vehicle(1189, "Mobile Uplink"          , "E.T. the Extra-Terrestrial", new List<string>() { "Phone Home","Special Attack","Sonar Smash" }),
            new Vehicle(1190, "Super-Charged Satellite", "E.T. the Extra-Terrestrial", new List<string>() { "Silver LEGO Blowup","Gold LEGO Blowup","Flight Docks and Flight Cargo Hooks","Tow Bar","Phone Home","Special Attack","Sonar Smash" }),

            new Vehicle(1191, "Niffler"                , "Fantastic Beasts and Where to Find Them", new List<string>() { "Playable Character","Enhanced Stud Attraction","Dig","Guardian Ability" }),
            new Vehicle(1192, "Sinister Scorpion"      , "Fantastic Beasts and Where to Find Them", new List<string>() { "Vine Cut","Special Weapon","Playable Character","Enhanced Stud Attraction","Dig","Guardian Ability" }),
            new Vehicle(1193, "Vicious Vulture"        , "Fantastic Beasts and Where to Find Them", new List<string>() { "Gold LEGO Blowup","Guardian Ability","Vine Cut","Special Weapon","Playable Character","Enhanced Stud Attraction","Dig" }),

            new Vehicle(1194, "Swooping Evil"          , "Fantastic Beasts and Where to Find Them", new List<string>() { "Playable Character","Guardian Ability" }),
            new Vehicle(1195, "Brutal Bloom"           , "Fantastic Beasts and Where to Find Them", new List<string>() { "Special Weapon","Guardian Ability","Tow Bar","Vine Cut","Playable Character" }),
            new Vehicle(1196, "Crawling Creeper"       , "Fantastic Beasts and Where to Find Them", new List<string>() { "Super Jump","Electricity","Dig","Special Weapon","Guardian Ability","Tow Bar","Vine Cut","Playable Character" }),

            new Vehicle(1197, "Ecto-1 (2016)"          , "Ghostbusters 2016", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(1198, "Ectozer"                , "Ghostbusters 2016", new List<string>() { "Accelerator Switches","Tow Bar","Special Attack","Super Strength" }),
            new Vehicle(1199, "PerfEcto"               , "Ghostbusters 2016", new List<string>() { "Electricity","Water Spray","Flying","Sail","Accelerator Switches","Tow Bar","Special Attack","Super Strength" }),

            new Vehicle(1200, "Flash 'n' Finish"       , "Gremlins", new List<string>() { "Illumination","Sonar Smash","Gold LEGO Blowup" }),
            new Vehicle(1201, "Rampage Record Player"  , "Gremlins", new List<string>() { "Special Attack","Special Weapon","Illumination","Sonar Smash","Gold LEGO Blowup" }),
            new Vehicle(1202, "Stripe's Throne"        , "Gremlins", new List<string>() { "Super Jump","Cursed Red LEGO Objects","Special Attack","Special Weapon","Illumination","Sonar Smash","Gold LEGO Blowup" }),

            new Vehicle(1203, "R.C. Car"               , "Gremlins", new List<string>() { "Accelerator Switches","Special Attack","Electricity" }),
            new Vehicle(1204, "Gadget-o-matic"         , "Gremlins", new List<string>() { "Accelerator Switches","Special Attack","Tow Bar","Electricity" }),
            new Vehicle(1205, "Scarlet Scorpion"       , "Gremlins", new List<string>() { "Super Jump","Vine Cut","Accelerator Switches","Special Attack","Tow Bar","Electricity" }),

            new Vehicle(1206, "Hogward Express"        , "Harry Potter", new List<string>() { "Accelerator Switches","Tow Bar","Sonar Smash" }),
            new Vehicle(1207, "Steam Warrior"          , "Harry Potter", new List<string>() { "Super Strength","Super Jump","Special Attack","Guardian Ability","Accelerator Switches","Tow Bar","Sonar Smash" }),
            new Vehicle(1208, "Soaring Steam Plane"    , "Harry Potter", new List<string>() { "Gold LEGO Blowup","Flight Docks and Flight Cargo Hooks","Super Strength","Super Jump","Special Attack","Guardian Ability","Accelerator Switches","Tow Bar","Sonar Smash" }),

            new Vehicle(1209, "Enchanted Car"          , "Harry Potter", new List<string>() { "Accelerator Switches","Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1210, "Shark Sub"              , "Harry Potter", new List<string>() { "Underwater Drone","Underwater Interactions","Sonar Smash","Accelerator Switches","Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1211, "Monstrous Mouth"        , "Harry Potter", new List<string>() { "Vine Cut","Gold LEGO Blowup","Underwater Drone","Underwater Interactions","Sonar Smash","Accelerator Switches","Tow Bar","Flight Docks and Flight Cargo Hooks" }),

            new Vehicle(1212, "IMF Scrambler"          , "Mission: Impossible", new List<string>() { "Accelerator Switches","Jump","Special Attack" }),
            new Vehicle(1213, "Shock Cycle"            , "Mission: Impossible", new List<string>() { "Accelerator Switches","Jump","Special Weapon","Silver LEGO Blowup","Special Attack" }),
            new Vehicle(1214, "IMF Covert Jet"         , "Mission: Impossible", new List<string>() { "Flight Docks and Flight Cargo Hooks","Underwater Interactions","Special Weapon","Tow Bar","Accelerator Switches","Jump","Silver LEGO Blowup","Special Attack" }),

            new Vehicle(1215, "IMF Sports Car"         , "Mission: Impossible", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(1216, "IMF Tank"               , "Mission: Impossible", new List<string>() { "Super Strength","Gold LEGO Blowup","Tow Bar","Accelerator Switches" }),
            new Vehicle(1217, "IMF Splorer"            , "Mission: Impossible", new List<string>() { "Underwater Drone","Underwater Interactions","Super Strength","Gold LEGO Blowup","Tow Bar","Accelerator Switches" }),

            new Vehicle(1218, "Sonic Speedster"        , "Sonic the Hedgehog", new List<string>() { "Accelerator Switches","Tow Bar" }),
            new Vehicle(1219, "Blue Typhoon"           , "Sonic the Hedgehog", new List<string>() { "Flight Docks and Flight Cargo Hooks","Sail","Sonar Smash","Tow Bar","Accelerator Switches" }),
            new Vehicle(1220, "Moto Bug"               , "Sonic the Hedgehog", new List<string>() { "Accelerator Switches","Dig","Vine Cut","Super Jump","Flight Docks and Flight Cargo Hooks","Sail","Sonar Smash","Tow Bar" }),

            new Vehicle(1221, "The Tornado"            , "Sonic the Hedgehog", new List<string>() { "Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1222, "Crabmeat"               , "Sonic the Hedgehog", new List<string>() { "Jump","Gold LEGO Blowup","Super Jump","Guardian Ability","Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1223, "Eggcatcher"             , "Sonic the Hedgehog", new List<string>() { "Tow Bar","Flight Docks and Flight Cargo Hooks","Electricity","Jump","Gold LEGO Blowup","Super Jump","Guardian Ability" }),

            new Vehicle(1224, "K.I.T.T."               , "Knight Rider", new List<string>() { "Accelerator Switches","Gold LEGO Blowup" }),
            new Vehicle(1225, "Goliath Armored Semi"   , "Knight Rider", new List<string>() { "Tow Bar","Super Strength","Electricity","Accelerator Switches","Gold LEGO Blowup" }),
            new Vehicle(1226, "K.I.T.T. Jet"           , "Knight Rider", new List<string>() { "Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup","Silver LEGO Blowup","Tow Bar","Super Strength","Electricity","Accelerator Switches" }),

            new Vehicle(1227, "Police helicopter"      , "LEGO City: Undercover", new List<string>() { "Flight Docks and Flight Cargo Hooks","Special Attack","Tow Bar" }),
            new Vehicle(1228, "Unknown"                , "LEGO City: Undercover", new List<string>() { "Gold LEGO Blowup","Accelerator Switches","Super Jump","Special Attack","Flight Docks and Flight Cargo Hooks","Tow Bar" }),
            new Vehicle(1229, "Unknown"                , "LEGO City: Undercover", new List<string>() { "Flight Docks and Flight Cargo Hooks","Tow Bar","Drone Mazes","Gold LEGO Blowup","Accelerator Switches","Super Jump","Special Attack" }),

            new Vehicle(1230, "Bionic Steed"           , "The LEGO Batman Movie", new List<string>() { "Super Jump","Special Weapon" }),
            new Vehicle(1231, "Bat Raptor"             , "The LEGO Batman Movie", new List<string>() { "Super Jump","Super Strength","Special Weapon" }),
            new Vehicle(1232, "Ultrabat"               , "The LEGO Batman Movie", new List<string>() { "Sonar Smash","Super Strength","Super Jump","Super Strength","Special Weapon" }),

            new Vehicle(1233, "Batwing"                , "The LEGO Batman Movie", new List<string>() { "Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1234, "The Black Thunder"      , "The LEGO Batman Movie", new List<string>() { "Accelerator Switches","Tow Bar","Flight Docks and Flight Cargo Hooks" }),
            new Vehicle(1235, "Bat-Tank"               , "The LEGO Batman Movie", new List<string>() { "Accelerator Switches","Tow Bar","Silver LEGO Blowup","Flight Docks and Flight Cargo Hooks" }),

            new Vehicle(1236, "Skeleton Orga"          , "The Goonies", new List<string>() { "Organ Docks","Special Weapon","Sonar Smash" }),
            new Vehicle(1237, "Skeleton Jukebox"       , "The Goonies", new List<string>() { "Jump","Special Weapon","Electricity","Organ Docks","Sonar Smash" }),
            new Vehicle(1238, "Skele-Turkey"           , "The Goonies", new List<string>() { "Gold LEGO Blowup","Flight Docks and Flight Cargo Hooks","Tow Bar","Jump","Special Weapon","Electricity","Organ Docks","Sonar Smash" }),

            new Vehicle(1239, "One-Eyed Willy's Pirate Ship", "The Goonies", new List<string>() { "Sail","Silver LEGO Blowup","Special Attack" }),
            new Vehicle(1240, "Fanged Fortune"         , "The Goonies", new List<string>() { "Water Spray","Special Attack","Vine Cut","Sail","Silver LEGO Blowup" }),
            new Vehicle(1241, "Inferno Cannon"         , "The Goonies", new List<string>() { "Special Attack","Special Weapon","Silver LEGO Blowup","Water Spray","Vine Cut","Sail" }),

            new Vehicle(1242, "Buckbeak"               , "Harry Potter", new List<string>() { "Super Strength","Flying","Guardian Ability","Silver LEGO Blowup" }),
            new Vehicle(1243, "Giant Owl"              , "Harry Potter", new List<string>() { "Flying","Electricity","Super Strength","Guardian Ability","Silver LEGO Blowup" }),
            new Vehicle(1244, "Fierce Falcon"          , "Harry Potter", new List<string>() { "Flying","Special Weapon","Sonar Smash","Electricity","Super Strength","Guardian Ability","Silver LEGO Blowup" }),

            new Vehicle(1245, "Saturn's Sandworm"      , "Beetlejuice", new List<string>() { "Sonar Smash" }),
            new Vehicle(1246, "Spooky Spider"          , "Beetlejuice", new List<string>() { "Special Weapon","Super Jump","Cursed Red LEGO Objects","Enhanced Stud Attraction","Sonar Smash" }),
            new Vehicle(1247, "Haunted Vacuum"         , "Beetlejuice", new List<string>() { "Gold LEGO Blowup","Tow Bar","Silver LEGO Blowup","Special Weapon","Super Jump","Cursed Red LEGO Objects","Enhanced Stud Attraction","Sonar Smash" }),

            new Vehicle(1248, "PPG Smartphone"         , "The Powerpuff Girls", new List<string>() { "Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup" }),
            new Vehicle(1249, "PPG Hotline"            , "The Powerpuff Girls", new List<string>() { "Special Weapon","Rainbow LEGO Objects","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup" }),
            new Vehicle(1250, "Powerpuff Mag-Net"      , "The Powerpuff Girls", new List<string>() { "Special Weapon","Enhanced Stud Attraction","Silver LEGO Blowup","Rainbow LEGO Objects","Flight Docks and Flight Cargo Hooks","Gold LEGO Blowup" }),

            new Vehicle(1251, "Ka-Pow Cannon"          , "The Powerpuff Girls", new List<string>() { "Special Weapon","Silver LEGO Blowup" }),
            new Vehicle(1252, "Slammin' Guitar"        , "The Powerpuff Girls", new List<string>() { "Special Weapon","Super Strength","Accelerator Switches","Tow Bar","Silver LEGO Blowup" }),
            new Vehicle(1253, "Megablast-Bot"          , "The Powerpuff Girls", new List<string>() { "Special Attack","Special Weapon","Sonar Smash","Super Strength","Accelerator Switches","Tow Bar","Silver LEGO Blowup" }),

            new Vehicle(1254, "Octi"                   , "The Powerpuff Girls", new List<string>() { "Gold LEGO Blowup" }),
            new Vehicle(1255, "Super SKunk"            , "The Powerpuff Girls", new List<string>() { "Vine Cut","Super Jump","Dig","Gold LEGO Blowup" }),
            new Vehicle(1256, "Sonic Squid"            , "The Powerpuff Girls", new List<string>() { "Water Spray","Vine Cut","Super Jump","Dig","Gold LEGO Blowup" }),

            new Vehicle(1257, "T-Car"                  , "Teen Titans Go!", new List<string>() { "Super Jump","Accelerator Switches","Tow Bar" }),
            new Vehicle(1258, "T-Forklift"             , "Teen Titans Go!", new List<string>() { "Super Jump","Accelerator Switches","Drone Mazes","Tow Bar" }),
            new Vehicle(1259, "T-Plane"                , "Teen Titans Go!", new List<string>() { "Accelerator Switches","Flight Docks and Flight Cargo Hooks","Tow Bar","Sail","Super Jump","Drone Mazes" }),

            new Vehicle(1260, "Spellbook of Azarath"   , "Teen Titans Go!", new List<string>() { "Summon Ability","Rainbow LEGO Objects","Cursed Red LEGO Objects","Sonar Smash" }),
            new Vehicle(1261, "Raven Wings"            , "Teen Titans Go!", new List<string>() { "Cursed Red LEGO Objects","Flying","Gold LEGO Blowup","Tow Bar","Sonar Smash","Summon Ability","Rainbow LEGO Objects" }),
            new Vehicle(1262, "Giant Hand"             , "Teen Titans Go!", new List<string>() { "Super Jump","Dig","Super Strength","Cursed Red LEGO Objects","Flying","Gold LEGO Blowup","Tow Bar","Sonar Smash","Summon Ability","Rainbow LEGO Objects" }),

            new Vehicle(1263, "Titan Robot"            , "Teen Titans Go!", new List<string>() { "Special Weapon","Super Strength","Tow Bar","Silver LEGO Blowup" }),
            new Vehicle(1264, "T-Rocket"               , "Teen Titans Go!", new List<string>() { "Gold LEGO Blowup","Flying","Special Weapon","Super Strength","Tow Bar","Silver LEGO Blowup" }),
            new Vehicle(1265, "Robot Retriever"        , "Teen Titans Go!", new List<string>() { "Super Jump","Dig","Electricity","Gold LEGO Blowup","Flying","Special Weapon","Super Strength","Tow Bar","Silver LEGO Blowup" }),
            };
    }
}
