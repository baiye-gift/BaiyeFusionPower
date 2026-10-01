using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using Baiye.FusionPower;

static class Program
{
    static int passed;
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(args[0],new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
        try{Run();}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
    }
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    static void Test(string name,System.Action body){body();passed++;Console.WriteLine("PASS "+name);}
    static CodexEntry Entry(string id)
    {
        var e=new CodexEntry{id=id,parentId="original-parent",category="original-category"};
        e.AddContentContainer(new ContentContainer(new List<ICodexWidget>{new CodexText("original native content")},ContentContainer.ContentLayout.Vertical));
        return e;
    }
    static string Body(CodexEntry e)=>string.Join("\n",e.contentContainers.Where(c=>c!=null).SelectMany(c=>c.content).OfType<CodexText>().Select(t=>t.text));
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        foreach(string id in new[]{"BaiyeIsotopeSeparator","BaiyeTritiumBreeder","BaiyeFusionReactor","BaiyeTripleAlphaFurnace"})
        {
            Func<CodexEntry,bool,CodexEntry> append=(e,zh)=>FusionCodex.Append(e,id,zh);
            Test(id+" append preserves native identity and is idempotent",()=>
            {
                var e=Entry(id);var original=e.contentContainers[0];var returned=append(e,true);int count=e.contentContainers.Count;
                Check(ReferenceEquals(returned,e)&&count>=5,"no guide or replaced entry");
                append(e,true);append(e,false);
                Check(e.contentContainers.Count==count&&ReferenceEquals(original,e.contentContainers[0]),"duplicate guide or lost content");
                Check(e.id==id&&e.parentId=="original-parent"&&e.category=="original-category","changed native ID/category");
                Check(Body(e).Contains("original native content"),"lost native description");
            });
            Test(id+" chains an existing callback and tolerates repeated attachment",()=>
            {
                var def=(BuildingDef)RuntimeHelpers.GetUninitializedObject(typeof(BuildingDef));def.PrefabID=id;
                var replacement=Entry(id);int calls=0;
                def.ExtendCodexEntry=_=>{calls++;return replacement;};
                FusionCodex.Attach(def,()=>true);FusionCodex.Attach(def,()=>true);
                var returned=def.ExtendCodexEntry(Entry(id));int count=returned.contentContainers.Count;
                Check(calls==1&&ReferenceEquals(returned,replacement)&&count>=5,"existing callback bypassed");
                def.ExtendCodexEntry(Entry(id));Check(calls==2&&replacement.contentContainers.Count==count,"callback repeated or guide duplicated");
            });
            Test(id+" guide covers operation and has English counterpart",()=>
            {
                var e=append(Entry(id),true);var en=append(Entry(id),false);
                Check(e.contentContainers.Count==en.contentContainers.Count&&!Body(en).Contains("核子工艺链"),"missing English sections");
                var b=Body(e);Check(b.Contains("接口")&&b.Contains("游戏"),"missing operation explanation");
                if(id=="BaiyeFusionReactor")Check(b.Contains("2 kg 氚")&&b.Contains("2 MDTU/s")&&b.Contains("(-2,0)")&&b.Contains("680 K"),"wrong reactor guide");
            });
        }
        Test("unknown or null entries are untouched",()=>
        {
            var e=Entry("OtherBuilding");FusionCodex.Append(e,"OtherBuilding",true);
            Check(e.contentContainers.Count==1&&FusionCodex.Append(null,"BaiyeFusionReactor",true)==null,"foreign entries changed");
        });
        Console.WriteLine($"{passed} native Codex scenarios passed; no Unity UI was instantiated");
    }
}
