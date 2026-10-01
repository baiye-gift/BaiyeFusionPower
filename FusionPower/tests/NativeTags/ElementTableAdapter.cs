using System.Collections.Generic;

// Only the loaded element table is adapted: native ElementLoader's initializer
// calls Unity Application.streamingAssetsPath, unavailable outside the engine.
// Tag, hashing, both native conversions and Harmony itself are real game code.
public class Element
{
    public SimHashes id;
    public Tag tag;
    public string name,nameUpperCase,description;
    public TestSubstance substance;
}
public class TestSubstance {public string name;}
public static class ElementLoader
{
    public static Dictionary<int,Element> elementTable;
    public static Dictionary<Tag,Element> elementTagTable;
    public static Element FindElementByHash(SimHashes id)=>elementTable.TryGetValue((int)id,out var e)?e:null;
    public static Element GetElement(Tag tag)=>elementTagTable.TryGetValue(tag,out var e)?e:null;
}
