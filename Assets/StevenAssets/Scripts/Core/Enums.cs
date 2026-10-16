namespace Steven.MP2A
{
    /// <summary>Groups on the Pantry Board.</summary>
    public enum ResourceCategory
    {
        Business,
        Raw,
        Prepped,
        Dish
    }

    /// <summary>
    /// Every player Resource ("the Pantry Ledger"). Order matters only for display.
    /// New entries must be appended to keep serialized values stable.
    /// </summary>
    public enum ResourceId
    {
        // Business / brigade
        Gold = 0,
        Reputation = 1,
        Hygiene = 2,
        Temper = 3,
        LineCooks = 4,
        CoversServed = 5,
        Sanitizer = 6,
        Cheese = 7,
        SpareParts = 8,

        // Raw ingredients
        Beef = 20,
        Chicken = 21,
        Salmon = 22,
        Cod = 23,
        Scallops = 24,
        Lobster = 25,
        Potato = 26,
        Onion = 27,
        Carrot = 28,
        Mushroom = 29,
        Tomato = 30,
        Garlic = 31,
        Herbs = 32,
        Lemon = 33,
        Butter = 34,
        Egg = 35,
        Flour = 36,
        Rice = 37,
        Chocolate = 38,

        // Prepped components (mise en place)
        Mirepoix = 60,
        Duxelles = 61,
        PastryDough = 62,
        FreshPasta = 63,
        RawFries = 64,
        Stock = 65,
        TomatoSauce = 66,
        SearedBeef = 67,

        // Finished dishes
        BeefWellington = 100,
        PanSearedScallops = 101,
        CrispySalmon = 102,
        MushroomRisotto = 103,
        LobsterSpaghetti = 104,
        RoastChicken = 105,
        ChocolateSouffle = 106,
        Fries = 107,
        FishAndChips = 108,
        OnionRings = 109,
        GrilledSteak = 110,
        GrilledLobster = 111,
        CharredVeg = 112,
        TomatoSalad = 113,
        GarlicMushrooms = 114
    }

    /// <summary>Every Stat name that an Instance can carry.</summary>
    public enum StatId
    {
        Heat = 0,
        Cleanliness = 1,
        Integrity = 2,
        Progress = 3,
        Crew = 4,
        Flavor = 5,
        OilQuality = 6,
        Chill = 7,
        WaterLevel = 8,
        Ice = 9,
        Moisture = 10,
        Nutrients = 11,
        Growth = 12,
        Guests = 13,
        Patience = 14,
        Appetite = 15,
        Bait = 16,
        Fill = 17,
        Stench = 18,
        Wear = 19,
        Health = 20,
        Hunger = 21,
        Energy = 22,
        Intensity = 23,
        Warmth = 24,
        Freshness = 25,
        Stock = 26,
        Size = 27,
        Pressure = 28
    }

    /// <summary>Every grid Building prefab.</summary>
    public enum BuildingId
    {
        None = 0,
        FloorTile = 1,
        WalkwayMat = 2,
        GasLine = 3,
        WaterLine = 4,
        PowerOutlet = 5,
        Counter = 6,
        RangeStove = 7,
        CharGrill = 8,
        DeepFryer = 9,
        ConvectionOven = 10,
        ReachInFridge = 11,
        Sink = 12,
        IceMachine = 13,
        CuttingBoard = 14,
        SautePan = 15,
        Stockpot = 16,
        PlanterBox = 17,
        HerbPlant = 18,
        DiningTable = 19,
        PlaceSetting = 20,
        MouseTrap = 21,
        TrashBin = 22
    }

    /// <summary>Coarse classification used by collision rules, tools and AI targeting.</summary>
    public enum EntityKind
    {
        None = 0,
        Building = 1,
        Rat = 2,
        Customer = 3,
        LineCook = 4,
        GreaseFire = 5,
        Plate = 6,
        Crate = 7,
        Ingredient = 8,
        Mushroom = 9,
        IceCube = 10,
        FoodScraps = 11,
        Foam = 12,
        SanitizerMist = 13,
        Tool = 14,
        Fixture = 15
    }

    /// <summary>How a GridAgent is allowed to move across the grid.</summary>
    public enum PathMode
    {
        /// <summary>Plan only across Walkway Mat (pathway) buildings.</summary>
        PathwaysOnly = 0,
        /// <summary>Plan across any cell that is not blocked by a building or fixture.</summary>
        AvoidBuildings = 1
    }

    /// <summary>What a tool does to a target it touches.</summary>
    public enum ToolAction
    {
        ModifyStat = 0,
        Harvest = 1,
        Shatter = 2,
        Demolish = 3,
        ModifyStatAndTool = 4
    }

    /// <summary>When a TutorialTrigger fires its popup.</summary>
    public enum TutorialTriggerMode
    {
        Proximity = 0,
        Hover = 1,
        Grab = 2,
        Manual = 3
    }
}
