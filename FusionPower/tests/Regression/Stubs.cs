// Minimal deterministic ports/storage/clock adapter for the actual production
// source. This is not a Unity simulation or an in-game compatibility test.
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
    public class Component
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public T[] GetComponents<T>() => gameObject.GetComponents<T>();
    }
    public class GameObject
    {
        public int Cell;
        public readonly Transform transform = new();
        public readonly List<object> components = new();
        public readonly Dictionary<int, List<Action<object>>> handlers = new();
        public T Add<T>() where T : Component, new() { var c = new T { gameObject = this }; components.Add(c); return c; }
        public T GetComponent<T>() where T : class => components.OfType<T>().FirstOrDefault();
        public T[] GetComponents<T>() => components.OfType<T>().ToArray();
    }
    public class Transform { public Vector3 position; }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;}
        public static float Distance(Vector3 a,Vector3 b) => MathF.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));
    }
    public static class Mathf
    {
        public static float Min(float a,float b)=>MathF.Min(a,b);
        public static float Max(float a,float b)=>MathF.Max(a,b);
    }
    public static class Debug { public static void Log(object s){} }
}
namespace KSerialization { public class SerializeAttribute : Attribute {} }
public interface ISaveLoadable {}
public interface ISim200ms { void Sim200ms(float dt); }
public enum GameHashes { OperationalChanged=1, OnStorageChange=2 }
public class KMonoBehaviour : UnityEngine.Component
{
    protected virtual void OnSpawn() {} protected virtual void OnCleanUp() {}
    public void Subscribe(int hash,Action<object> callback)
    {if(!gameObject.handlers.ContainsKey(hash))gameObject.handlers[hash]=new();gameObject.handlers[hash].Add(callback);}
    public void Trigger(int hash,object data)
    {if(gameObject.handlers.TryGetValue(hash,out var handlers))foreach(var h in handlers.ToArray())h(data);}
}
public static class Hash
{
    public static int SDBMLower(string s) {unchecked {int n=0;foreach(char c in s.ToLowerInvariant())n=c+(n<<6)+(n<<16)-n;return n;}}
}
public struct Tag { public int id; public Tag(int id){this.id=id;} }
public static class TagManager { public static Tag Create(string s)=>new(Hash.SDBMLower(s)); }
public enum SimHashes { Water=1, Brine=2, SuperCoolant=3, Hydrogen=4, RefinedCarbon=5 }
public static class GameTagExtensions { public static Tag CreateTag(this SimHashes s)=>new((int)s); }
public class Element { public int idx; public bool IsGas; public float specificHeatCapacity; }
public static class ElementLoader
{
    public static float SuperCoolantHeatCapacity=8.44f;
    public static Element FindElementByHash(SimHashes s)=>new(){idx=(int)s,specificHeatCapacity=s==SimHashes.SuperCoolant?SuperCoolantHeatCapacity:s==SimHashes.RefinedCarbon?.71f:(int)s==Hash.SDBMLower("BaiyeHelium4")?.14f:2.4f,IsGas=(int)s!=1&&(int)s!=2&&(int)s!=3&&(int)s!=5&&(int)s!=Hash.SDBMLower("BaiyeLithium6")};
}
public class PrimaryElement : KMonoBehaviour
{
    public SimHashes ElementID; public float Mass,Temperature; public byte DiseaseIdx=byte.MaxValue; public int DiseaseCount; public bool KeepZeroMassObject;
    public Element Element=>ElementLoader.FindElementByHash(ElementID);
    public void ModifyDiseaseCount(int amount,string source){DiseaseCount+=amount;}
}
public class Storage : KMonoBehaviour
{
    public float capacityKg=20f; public List<UnityEngine.GameObject> items=new();
    public bool showInUI;
    public readonly List<UnityEngine.GameObject> droppedItems=new();
    public int dropEvents;
    public float ExactMassStored()=>items.Sum(i=>i.GetComponent<PrimaryElement>().Mass);
    public float GetMassAvailable(SimHashes hash)=>items.Where(i=>i.GetComponent<PrimaryElement>().ElementID==hash).Sum(i=>i.GetComponent<PrimaryElement>().Mass);
    public UnityEngine.GameObject FindFirst(Tag tag)=>items.FirstOrDefault(i=>(int)i.GetComponent<PrimaryElement>().ElementID==tag.id);
    public void Drop(UnityEngine.GameObject i)=>items.Remove(i);
    public void DropAll()
    {
        var actual=items.Where(i=>i.GetComponent<PrimaryElement>().Mass>0).ToArray();
        if(actual.Length>0){dropEvents++;droppedItems.AddRange(actual);}
        items.Clear();
    }
    public void ConsumeAndGetDisease(Tag tag,float amount,out float consumed,out SimUtil.DiseaseInfo germs,out float temperature)
    {
        consumed=0;float weighted=0;germs=new(){idx=255};
        foreach(var item in items)
        {
            var e=item.GetComponent<PrimaryElement>();if((int)e.ElementID!=tag.id||e.Mass<=0)continue;
            float taken=MathF.Min(e.Mass,amount-consumed);weighted+=taken*e.Temperature;
            int count=(int)(e.DiseaseCount*taken/e.Mass);if(count>0)germs.idx=e.DiseaseIdx;
            germs.count+=count;e.DiseaseCount-=count;e.Mass-=taken;consumed+=taken;if(consumed>=amount)break;
        }
        temperature=consumed>0?weighted/consumed:float.NaN;
    }
    public bool TransferMass(Storage target,Tag tag,float amount,bool flatten=false,bool block_events=false,bool hide_popups=false)
    {
        ConsumeAndGetDisease(tag,amount,out float consumed,out var germs,out float temperature);
        if(consumed>0)target.AddOre((SimHashes)tag.id,consumed,temperature,germs.idx,germs.count);
        return consumed>=amount;
    }
    public void ConsumeIgnoringDisease(Tag tag,float amount)
    {
        if(GetMassAvailable((SimHashes)tag.id)+1e-7f<amount)throw new Exception("over-consumption");
        foreach(var item in items)
        {
            var p=item.GetComponent<PrimaryElement>();if((int)p.ElementID!=tag.id)continue;
            var consumed=MathF.Min(p.Mass,amount);p.Mass-=consumed;amount-=consumed;if(amount<=0)break;
        }
    }
    public PrimaryElement AddGasChunk(SimHashes h,float m,float t,byte d,int germs,bool keep=false)=>Add(h,m,t,d,germs);
    public PrimaryElement AddLiquid(SimHashes h,float m,float t,byte d,int germs)=>Add(h,m,t,d,germs);
    public PrimaryElement AddOre(SimHashes h,float m,float t,byte d,int germs)=>Add(h,m,t,d,germs);
    private PrimaryElement Add(SimHashes hash,float mass,float temperature,byte d,int germs)
    {
        var item=items.FirstOrDefault(i=>i.GetComponent<PrimaryElement>().ElementID==hash);
        PrimaryElement p;if(item==null){item=new();p=item.Add<PrimaryElement>();p.ElementID=hash;items.Add(item);}else p=item.GetComponent<PrimaryElement>();
        p.Temperature=(p.Mass*p.Temperature+mass*temperature)/(p.Mass+mass);p.Mass+=mass;p.DiseaseIdx=d;p.DiseaseCount+=germs;return p;
    }
}
public static class SimUtil { public struct DiseaseInfo {public byte idx;public int count;} }
public class Operational : KMonoBehaviour
{
    public bool IsOperational=true; public bool IsActive;
    public void SetActive(bool active)=>IsActive=active;
    public void SetPowered(bool value){IsOperational=value;if(!value)IsActive=false;Trigger((int)GameHashes.OperationalChanged,value);}
}
public class RadiationEmitter : KMonoBehaviour {public float emitRads; public bool emitting;public void Refresh(){}public void SetEmitting(bool v)=>emitting=v;}
public class Generator : KMonoBehaviour
{
    public float Capacity=32000f;
    public float JoulesAvailable {get;private set;}
    public float TotalGenerated {get;private set;}
    public void GenerateJoules(float joules,bool canOverPower=false)
    {TotalGenerated+=joules;JoulesAvailable=MathF.Min(Capacity,JoulesAvailable+joules);}
    public void ConsumeEnergy(float joules)=>JoulesAvailable=MathF.Max(0,JoulesAvailable-joules);
}
public enum ConduitType {Gas,Liquid}
public class ConduitConsumer : KMonoBehaviour { public ConduitType conduitType; public Storage storage;public bool isOn=true,IsConnected=true; public void SetOnState(bool v)=>isOn=v; }
public class ConduitDispenser : KMonoBehaviour
{public ConduitType conduitType;public Storage storage;public bool isOn=true,IsConnected=true,useSecondaryOutput,invertElementFilter,empty,blocked;public SimHashes[] elementFilter;public void SetOnState(bool v)=>isOn=v;}
public class Building : KMonoBehaviour {public int outputCell;public int GetUtilityOutputCell()=>outputCell;public int NaturalBuildingCell()=>gameObject.Cell;}
public struct CellOffset {public int x,y;}
public interface ISecondaryInput {bool HasSecondaryConduitType(ConduitType t);CellOffset GetSecondaryConduitOffset(ConduitType t);}
public interface ISecondaryOutput {bool HasSecondaryConduitType(ConduitType t);CellOffset GetSecondaryConduitOffset(ConduitType t);}
public class TestInput : KMonoBehaviour,ISecondaryInput
{public ConduitType type;public CellOffset offset;public bool HasSecondaryConduitType(ConduitType t)=>type==t;public CellOffset GetSecondaryConduitOffset(ConduitType t)=>offset;}
public class TestOutput : KMonoBehaviour,ISecondaryOutput
{public ConduitType type;public CellOffset offset;public bool HasSecondaryConduitType(ConduitType t)=>type==t;public CellOffset GetSecondaryConduitOffset(ConduitType t)=>offset;}
public enum Endpoint {Sink,Source}
public class FlowUtilityNetwork
{public class NetworkItem {public ConduitType ConduitType;public Endpoint EndpointType;public int Cell;public NetworkItem(ConduitType t,Endpoint e,int c,UnityEngine.GameObject p){ConduitType=t;EndpointType=e;Cell=c;}}}
public class TestNetworkManager
{
 public List<FlowUtilityNetwork.NetworkItem> items=new();
 public void AddToNetworks(int cell,FlowUtilityNetwork.NetworkItem item,bool is_endpoint){if(!is_endpoint||cell!=item.Cell)throw new Exception("bad endpoint");items.Add(item);}
 public void RemoveFromNetworks(int cell,FlowUtilityNetwork.NetworkItem item,bool is_endpoint){if(!items.Remove(item))throw new Exception("endpoint not registered");}
}
public static class Conduit
{public static TestNetworkManager gas=new(),liquid=new();public static TestNetworkManager GetNetworkManager(ConduitType t)=>t==ConduitType.Gas?gas:liquid;}
public static class Grid
{public static int[] WorldIdx=new int[100]; public static bool IsValidCell(int c)=>c>=0&&c<100;public static int PosToCell(UnityEngine.GameObject g)=>g.Cell;public static int OffsetCell(int c,CellOffset o)=>c+o.x;}
public enum ConduitFlowPriority {Dispense}
public class ConduitFlow
{
    public struct ConduitContents {public float mass,temperature;public SimHashes element;public byte diseaseIdx;public int diseaseCount;}
    public Dictionary<int,ConduitContents> contents=new();private List<Action<float>> updaters=new();
    public void AddConduitUpdater(Action<float> callback,ConduitFlowPriority p)=>updaters.Add(callback);
    public void RemoveConduitUpdater(Action<float> callback)=>updaters.Remove(callback);
    public ConduitContents GetContents(int c)=>contents.GetValueOrDefault(c);
    public float AddElement(int c,SimHashes e,float m,float t,byte d,int germs)
    {var old=GetContents(c);if(old.mass>0&&old.element!=e)return 0;float moved=MathF.Min(m,1-old.mass);old.mass+=moved;old.element=e;old.temperature=t;old.diseaseIdx=d;old.diseaseCount+=germs;contents[c]=old;return moved;}
    public void Tick(){foreach(var u in updaters.ToArray())u(.2f);}
}
public class Game {public static Game Instance=new();public ConduitFlow gasConduitFlow=new();}
public class GameClock {public static GameClock Instance=new();public float time;public float GetTime()=>time;}
public class StructureTemperatures
{public float energy;public object GetHandle(UnityEngine.GameObject g)=>g;public void ProduceEnergy(object h,float q,string name,float dt)=>energy+=q;}
public static class GameComps {public static StructureTemperatures StructureTemperatures=new();}
public class FallingWater {public static FallingWater instance;public void AddParticle(int c,int idx,float m,float t,byte d,int n){} }
