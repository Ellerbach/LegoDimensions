// Licensed to Laurent Ellerbach and contributors under one or more agreements.
// Laurent Ellerbach and contributors license this file to you under the MIT license.

namespace LegoDimensions
{
    /// <summary>
    /// Character class for Lego Dimensions.
    /// </summary>
    public class Character
    {
        /// <summary>
        /// Gets or sets the ID of the character.
        /// </summary>
        public ushort Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the character.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the world the character is from..
        /// </summary>
        public string World { get; set; }

        /// <summary>
        /// Character class constructor.
        /// </summary>
        /// <param name="id">The ID of the character.</param>
        /// <param name="name">The name of the character.</param>
        /// <param name="world">The world the character is from.</param>
        public Character(ushort id, string name, string world)
        {
            Id = id;
            Name = name;
            World = world;
        }

        /// <summary>
        /// The list of all knonws characters.
        /// </summary>
        public static readonly List<Character> Characters = new List<Character>()
        {
            new Character(00, "Unknown"             , "Unknown" ),
            new Character(01, "Batman"              , "DC Comics"),
            new Character(02, "Gandalf"             , "Lord of the Rings"),
            new Character(03, "Wyldstyle"           , "The Lego Movie"),
            new Character(04, "Aquaman"             , "DC Comics"),
            new Character(05, "Bad Cop"             , "The Lego Movie"),
            new Character(06, "Bane"                , "DC Comics"),
            new Character(07, "Bart Simpson"        , "The Simpsons"),
            new Character(08, "Benny"               , "The Lego Movie"),
            new Character(09, "Chell"               , "Portal 2"),

            new Character(10, "Cole"                , "Lego Ninjago"),
            new Character(11, "Cragger"             , "Lego Legends of Chima"),
            new Character(12, "Cyborg"              , "DC Comics"),
            new Character(13, "Cyberman"            , "Doctor Who"),
            new Character(14, "Doc Brown"           , "Back to the Future"),
            new Character(15, "The Doctor"          , "Doctor Who"),
            new Character(16, "Emmet"               , "The Lego Movie"),
            new Character(17, "Eris"                , "Lego Legends of Chima"),
            new Character(18, "Gimli"               , "Lord of the Rings"),
            new Character(19, "Gollum"              , "Lord of the Rings"),

            new Character(20, "Harley Quinn"        , "DC Comics"),
            new Character(21, "Homer Simpson"       , "The Simpsons"),
            new Character(22, "Jay"                 , "Lego Ninjago"),
            new Character(23, "Joker"               , "DC Comics"),
            new Character(24, "Kai"                 , "Lego Ninjago"),
            new Character(25, "ACU Trooper"         , "Jurassic World"),
            new Character(26, "Gamer Kid"           , "Midway Arcade"),
            new Character(27, "Krusty"              , "The Simpsons"),
            new Character(28, "Laval"               , "Lego Legends of Chima"),
            new Character(29, "Leoglas"             , "Lord of the Rings"),

            new Character(30, "Lloyd"               , "Lego Ninjago"),
            new Character(31, "Marty McFly"         , "Back to the Future"),
            new Character(32, "Nya"                 , "Lego Ninjago"),
            new Character(33, "Owen"                , "Jurassic World"),
            new Character(34, "Peter Venkman"       , "Ghostbusters"),
            new Character(35, "Slimer"              , "Ghostbusters"),
            new Character(36, "Scooby Doo"          , "Scooby-Doo"),
            new Character(37, "Sensei Wu"           , "Lego Ninjago"),
            new Character(38, "Shaggy"              , "Scooby-Doo"),
            new Character(39, "Stay Puft"           , "Ghostbusters"),

            new Character(40, "Superman"            , "DC Comics"),
            new Character(41, "Unikitty"            , "The Lego Movie"),
            new Character(42, "Wicked Witch"        , "Wizard of Oz" ),
            new Character(43, "Wonder Woman"        , "DC Comics"),
            new Character(44, "Zane"                , "Lego Ninjago"),
            new Character(45, "Green Arrow"         , "DC Comics"),
            new Character(46, "Supergirl"           , "DC Comics"),
            new Character(47, "Abby Yates"          , "Ghostbusters 2016"),
            new Character(48, "Finn"                , "Adventure Time"),
            new Character(49, "Ethan Hunt"          , "Mission: Impossible"),

            new Character(50, "Lumpy Space Princess", "Adventure Time"),
            new Character(51, "Jake the Dog"        , "Adventure Time"),
            new Character(52, "Harry Potter"        , "Harry Potter"),
            new Character(53, "Lord Voldemort"      , "Harry Potter"),
            new Character(54, "Michael Knight"      , "Knight Rider"),
            new Character(55, "B.A.Baracus"         , "The A-Team"),
            new Character(56, "Newt Scamander"      , "Fantastic Beasts and Where to Find Them"),
            new Character(57, "Sonic the Hedgehog"  , "Sonic the Hedgehog"),
            new Character(58, "Unknown"             , "Unknown"),
            new Character(59, "Gizmo"               , "Gremlins"),

            new Character(60, "Stripe"              , "Gremlins"),
            new Character(61, "E.T."                , "E.T. the Extra-Terrestrial"),
            new Character(62, "Tina Goldstein"      , "Fantastic Beasts and Where to Find Them"),
            new Character(63, "Marceline Abadeer"   , "Adventure Time"),
            new Character(64, "Batgirl"             , "The LEGO Batman Movie"),
            new Character(65, "Robin (Lego Movie)"  , "The LEGO Batman Movie"),
            new Character(66, "Sloth"               , "The Goonies"),
            new Character(67, "Hermione Granger"    , "Harry Potter"),
            new Character(68, "Chase McCain"        , "LEGO City"),
            new Character(69, "Excalibur Batman"    , "The LEGO Batman Movie"),
            new Character(70, "Raven"               , "Teen Titans Go!"),
            new Character(71, "Beast Boy"           , "Teen Titans Go!"),
            new Character(72, "Betelgeuse"          , "Beetlejuice"),
            new Character(73, "Unknown"             , "Unknown"),
            new Character(74, "Blossom"             , "The Powerpuff Girls"),
            new Character(75, "Bubbles"             , "The Powerpuff Girls"),
            new Character(76, "Buttercup"           , "The Powerpuff Girls"),
            new Character(77, "Starfire"            , "Teen Titans Go!"),
            new Character(78, "Unknown"             , "Unknown"),
            new Character(79, "Unknown"             , "Unknown"),
            new Character(80, "Unknown"             , "Unknown"),
        };
    }
}
