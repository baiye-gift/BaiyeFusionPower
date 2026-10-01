using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

// Uses real U59 Tag/GameTagExtensions and Harmony with an in-memory element
// table adapter; this does not start Unity or the native pipe network.
static class Program
{
    static string managed;
    static int passed;
    static void Main(string[] args)
    {
        if(args.Length<1)throw new ArgumentException("NativeTags <Managed directory>");
        managed=Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve+=(_,eventArgs)=>
        {
            var name=new AssemblyName(eventArgs.Name);
            string path=Path.Combine(managed,name.Name+".dll");
            return File.Exists(path)?Assembly.LoadFrom(path):null;
        };
        try {Run();}
        catch(Exception error)
        {
            for(Exception e=error;e!=null;e=e.InnerException)Console.Error.WriteLine(e.GetType().FullName+": "+e.Message);
            Environment.ExitCode=1;
        }
    }
    static void Assert(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Test(string name,System.Action action){action();passed++;Console.WriteLine("PASS "+name);}
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        string[] ids={"BaiyeDeuterium","BaiyeLiquidDeuterium","BaiyeSolidDeuterium",
            "BaiyeTritium","BaiyeLiquidTritium","BaiyeSolidTritium","BaiyeHelium4","BaiyeLiquidHelium4",
            "BaiyeLithium6","BaiyeLiquidLithium6","BaiyeLithium6Vapor"};
        ElementLoader.elementTable=new Dictionary<int,Element>();
        ElementLoader.elementTagTable=new Dictionary<Tag,Element>();
        foreach(string id in ids)
        {
            var e=new Element {id=(SimHashes)Hash.SDBMLower(id),tag=TagManager.Create(id)};
            ElementLoader.elementTable.Add((int)e.id,e);ElementLoader.elementTagTable.Add(e.tag,e);
        }
        var patch=typeof(Baiye.FusionPower.FusionFilterTagPatch);
        var harmony=new Harmony("baiye.tests.native.tags");
        Console.WriteLine("Applying production tag patch to native GameTagExtensions");
        harmony.CreateClassProcessor(patch).Patch();
        try
        {
            Test("native helium selection equals the native pipe packet tag",()=>
            {
                var id=(SimHashes)Hash.SDBMLower("BaiyeHelium4");
                var selected=GameTagExtensions.Create(id);var packet=id.CreateTag();
                Assert(selected==packet,$"helium list tag {selected.Name} differs from pipe tag {packet.Name}");
                Assert(ElementLoader.GetElement(packet).id==id,"packet tag does not resolve to helium");
            });
            Test("all 11 custom phases agree across list, packet and native element table",()=>
            {
                foreach(string name in ids)
                {
                    var id=(SimHashes)Hash.SDBMLower(name);var expected=TagManager.Create(name);
                    Assert(GameTagExtensions.Create(id)==expected,"wrong list tag: "+name);
                    Assert(id.CreateTag()==expected,"wrong packet tag: "+name);
                    Assert(ElementLoader.GetElement(id.CreateTag()).id==id,"wrong table lookup: "+name);
                }
            });
            Test("native filter branch sends matching gas and liquid to the selected outlet",()=>
            {
                // This is the exact comparison in ElementFilter.OnConduitTick.
                foreach(string name in new[]{"BaiyeHelium4","BaiyeTritium","BaiyeLiquidHelium4","BaiyeLiquidTritium"})
                {
                    var id=(SimHashes)Hash.SDBMLower(name);var selected=GameTagExtensions.Create(id);
                    int outlet=id.CreateTag()==selected?20:30;
                    Assert(outlet==20,"matching element diverted to normal outlet: "+name);
                }
                var helium=GameTagExtensions.Create((SimHashes)Hash.SDBMLower("BaiyeHelium4"));
                Assert(SimHashes.Hydrogen.CreateTag()!=helium,"hydrogen incorrectly passes helium selection");
                Assert(((SimHashes)Hash.SDBMLower("BaiyeTritium")).CreateTag()!=helium,"tritium incorrectly passes helium selection");
            });
            Test("original and unrelated custom elements retain native tag conversion",()=>
            {
                var unrelated=(SimHashes)123456789;
                ElementLoader.elementTable.Add((int)unrelated,new Element {id=unrelated,tag=TagManager.Create("SomeOtherModGas")});
                foreach(var id in new[]{SimHashes.Hydrogen,SimHashes.Water,SimHashes.RefinedCarbon,SimHashes.Vacuum,(SimHashes)123456789})
                {
                    var expected=TagManager.Create(id.ToString());
                    Assert(GameTagExtensions.Create(id)==expected&&id.CreateTag()==expected,"unrelated conversion changed");
                }
            });
            Test("existing canonical selections survive patching without reselection",()=>
            {
                var saved=TagManager.Create("BaiyeHelium4");
                Assert(((SimHashes)Hash.SDBMLower(saved.Name)).CreateTag()==saved,"existing selection invalidated");
            });
        }
        finally {harmony.UnpatchAll(harmony.Id);}
        Console.WriteLine($"{passed} native tag scenarios passed; no Unity conduit-flow simulation.");
    }
}
