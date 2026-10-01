using System;
using System.Collections.Generic;
using System.Reflection;
using Baiye.FusionPower;
using UnityEngine;

static partial class Program
{
    static readonly List<KMonoBehaviour> spawned = new();
    static int nextCell, passed;
    static SimHashes D=>FusionIds.HashOf(FusionIds.Deuterium);
    static SimHashes T=>FusionIds.HashOf(FusionIds.Tritium);
    static SimHashes Li=>FusionIds.HashOf(FusionIds.Lithium);
    static SimHashes He=>FusionIds.HashOf(FusionIds.Helium);
    static void Assert(bool v,string message) {if(!v)throw new Exception(message);}
    static void Near(float a,float b,float tolerance=2e-5f)=>Assert(MathF.Abs(a-b)<=tolerance,$"expected {b}, got {a}");
    static void Spawn(KMonoBehaviour p){p.GetType().GetMethod("OnSpawn",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);spawned.Add(p);}
    static bool Ready(FusionProcessBase p)=> (bool)p.GetType().GetMethod("CanProcess",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,new object[]{.2f});
    static void SetMass(Storage s,SimHashes h,float m,float temp=350f)
    {var i=s.FindFirst(new Tag((int)h));if(i==null){if(m>0)s.AddGasChunk(h,m,temp,255,0);}else{i.GetComponent<PrimaryElement>().Mass=m;i.GetComponent<PrimaryElement>().Temperature=temp;}}
    static (P p,Storage[] s,Operational op,GameObject go) Fixture<P>(params float[] capacities) where P:FusionProcessBase,new()
    {
        var go=new GameObject {Cell=nextCell++};go.transform.position=new Vector3(0,0,0);
        go.Add<PrimaryElement>().Temperature=300f;var op=go.Add<Operational>();go.Add<RadiationEmitter>();
        var consumer=go.Add<ConduitConsumer>();var stores=new Storage[capacities.Length];
        for(int i=0;i<stores.Length;i++){stores[i]=go.Add<Storage>();stores[i].capacityKg=capacities[i];}
        consumer.storage=stores[0];if(typeof(P)==typeof(FusionReactorProcess)||typeof(P)==typeof(TripleAlphaProcess))go.Add<Generator>();var p=go.Add<P>();Spawn(p);return(p,stores,op,go);
    }
    static (FusionReactorProcess p,Storage[] s,Operational op,GameObject go) Reactor(float tritium=2.1f)
    {var f=Fixture<FusionReactorProcess>(2,10,5,20,20);SetMass(f.s[0],D,1);SetMass(f.s[1],T,tritium);SetMass(f.s[3],SimHashes.SuperCoolant,20,373.15f);return f;}
    static (TritiumBreederProcess p,Storage[] s,Operational op,GameObject go) Breeder(float lithium=1)
    {var f=Fixture<TritiumBreederProcess>(50,5,5,5);SetMass(f.s[0],Li,lithium);SetMass(f.s[1],D,.1f);return f;}
    static void Paid(FusionProcessBase p){p.Sim200ms(.2f);p.OnPowerSettlement(p.GetComponent<Operational>().IsActive?2000:0,true);}
    static void Test(string name,Action run)
    {
        try {run();Console.WriteLine("PASS "+name);passed++;}
        finally
        {
            for(int i=spawned.Count-1;i>=0;i--)spawned[i].GetType().GetMethod("OnCleanUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(spawned[i],null);
            spawned.Clear();nextCell=0;Array.Clear(Grid.WorldIdx);Game.Instance=new();GameClock.Instance=new();GameComps.StructureTemperatures=new();Conduit.gas=new();Conduit.liquid=new();
            ElementLoader.SuperCoolantHeatCapacity=8.44f;
        }
    }
    static void Main()
    {
        Test("secondary reactor ports register gas sink and liquid directions",()=>
        {
            var go=new GameObject {Cell=40};go.Add<Building>();
            var t=go.Add<TestInput>();t.type=ConduitType.Gas;
            var ci=go.Add<TestInput>();ci.type=ConduitType.Liquid;ci.offset=new(){x=-1};
            var co=go.Add<TestOutput>();co.type=ConduitType.Liquid;co.offset=new(){x=1};
            Spawn(go.Add<FusionSecondaryEndpoints>());
            Assert(Conduit.gas.items.Count==1&&Conduit.gas.items[0].Cell==40&&Conduit.gas.items[0].EndpointType==Endpoint.Sink,"tritium sink missing");
            Assert(Conduit.liquid.items.Count==2&&Conduit.liquid.items[0].Cell==39&&Conduit.liquid.items[0].EndpointType==Endpoint.Sink&&Conduit.liquid.items[1].Cell==41&&Conduit.liquid.items[1].EndpointType==Endpoint.Source,"coolant direction wrong");
        });
        Test("secondary registration rebuilds without duplicates and cleans up",()=>
        {
            var go=new GameObject {Cell=40};go.Add<Building>();var o=go.Add<TestOutput>();o.type=ConduitType.Gas;o.offset=new(){x=-1};
            var component=go.Add<FusionSecondaryEndpoints>();Spawn(component);component.RegisterEndpoints();
            Assert(Conduit.gas.items.Count==1&&Conduit.gas.items[0].Cell==39&&Conduit.gas.items[0].EndpointType==Endpoint.Source,"tail outlet missing or duplicated");
            component.GetType().GetMethod("OnCleanUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(component,null);spawned.Remove(component);
            Assert(Conduit.gas.items.Count==0&&Conduit.liquid.items.Count==0,"removed building retained endpoints");
            Spawn(component);Assert(Conduit.gas.items.Count==1,"restored building lacks endpoint");
        });
        Test("readiness cannot produce unpaid material",()=>{var r=Reactor();for(int i=0;i<100;i++)r.p.Sim200ms(.2f);Near(r.s[0].GetMassAvailable(D),1);Near(r.s[2].ExactMassStored(),0);Near(r.go.GetComponent<Generator>().TotalGenerated,0);});
        Test("partial and idle payments grant no recipe",()=>{var r=Reactor();r.p.Sim200ms(.2f);r.p.OnPowerSettlement(2000,false);r.p.OnPowerSettlement(0,true);Near(r.s[2].ExactMassStored(),0);Near(r.s[3].ExactMassStored(),20);Near(r.go.GetComponent<Generator>().TotalGenerated,0);});
        Test("one full payment buys only 200 ms fusion",()=>{var r=Reactor();Paid(r.p);Near(r.s[0].GetMassAvailable(D),.996f);Near(r.s[1].GetMassAvailable(T),2.094f);Near(r.s[2].GetMassAvailable(He),.008f);Near(r.p.AvailableNeutrons,.002f);Near(r.s[4].ExactMassStored(),2);});
        Test("paid step generates 3200 J and only 400 kDTU coolant heat",()=>{var r=Reactor();Paid(r.p);float t=r.s[4].FindFirst(SimHashes.SuperCoolant.CreateTag()).GetComponent<PrimaryElement>().Temperature;Near(t,373.15f+2000f/(10f*8.44f),.001f);Near((t-373.15f)*2f*8.44f,400f,.001f);Near(r.go.GetComponent<Generator>().JoulesAvailable,3200);Near(r.s[3].ExactMassStored()+r.s[4].ExactMassStored(),20);});
        Test("insufficient startup reserve waits without consumption",()=>{var r=Reactor(.95f);Assert(!Ready(r.p),"started on first packet");Paid(r.p);Near(r.s[0].GetMassAvailable(D),1);Near(r.s[2].ExactMassStored(),0);});
        Test("fuel exhaustion requires reserve before restart",()=>{var r=Reactor();Paid(r.p);SetMass(r.s[1],T,.001f);Assert(!Ready(r.p),"did not stop");SetMass(r.s[1],T,.95f);Assert(!Ready(r.p),"restart thrashing");SetMass(r.s[1],T,2.05f);Assert(Ready(r.p),"did not recover");});
        Test("power loss discards transient neutron link",()=>{var r=Reactor();Paid(r.p);r.op.SetPowered(false);Near(r.p.AvailableNeutrons,0);r.op.SetPowered(true);SetMass(r.s[1],T,.95f);Assert(!Ready(r.p),"brownout bypassed reserve");});
        Test("blocked coolant output does not consume fuel",()=>{var r=Reactor();SetMass(r.s[4],SimHashes.SuperCoolant,20);Paid(r.p);Near(r.s[0].GetMassAvailable(D),1);Near(r.s[2].ExactMassStored(),0);});
        Test("overheated coolant stops before reaction",()=>{var r=Reactor();SetMass(r.s[3],SimHashes.SuperCoolant,20,660);Paid(r.p);Near(r.s[0].GetMassAvailable(D),1);Assert(r.p.CurrentStopCause==FusionReactorProcess.StopCause.CoolantHot,"wrong stop cause");});
        Test("coolant primes once and requires reserve after real starvation",()=>
        {
            var r=Reactor();SetMass(r.s[3],SimHashes.SuperCoolant,10);Paid(r.p);
            Assert(r.p.CurrentStopCause==FusionReactorProcess.StopCause.CoolantReserve,"started before reserve");Near(r.go.GetComponent<Generator>().TotalGenerated,0);
            SetMass(r.s[3],SimHashes.SuperCoolant,20);Paid(r.p);Near(r.s[3].ExactMassStored(),18);
            SetMass(r.s[3],SimHashes.SuperCoolant,1);Assert(!Ready(r.p),"did not stop on shortage");
            SetMass(r.s[3],SimHashes.SuperCoolant,10);Assert(!Ready(r.p),"starved reactor immediately restarted");
            SetMass(r.s[3],SimHashes.SuperCoolant,20);Assert(Ready(r.p),"did not restart on full reserve");
        });
        Test("last successful coolant slice retains earned neutron credit",()=>
        {
            var r=Reactor();for(int i=0;i<10;i++)Paid(r.p);
            Near(r.s[3].ExactMassStored(),0);Near(r.p.AvailableNeutrons,.02f);
            Assert(r.op.IsActive&&r.p.CurrentStopCause==FusionReactorProcess.StopCause.None,"post-reaction speculative stop");
            r.p.Sim200ms(.2f);Assert(!r.op.IsActive,"real starvation did not stop");Near(r.p.AvailableNeutrons,0);
        });
        Test("full generator pauses before consuming fuel or adding heat",()=>
        {
            var r=Reactor();var g=r.go.GetComponent<Generator>();g.GenerateJoules(g.Capacity);Paid(r.p);
            Assert(r.p.CurrentStopCause==FusionReactorProcess.StopCause.PowerOutput,"wrong electrical stop cause");
            Near(r.s[0].GetMassAvailable(D),1);Near(r.s[3].ExactMassStored(),20);Near(r.s[4].ExactMassStored(),0);
            g.ConsumeEnergy(3200);Paid(r.p);Near(g.JoulesAvailable,32000);Near(r.s[4].ExactMassStored(),2);
        });
        Test("200 C coolant return is accepted with a 23.7 C rise",()=>
        {
            var r=Reactor();SetMass(r.s[3],SimHashes.SuperCoolant,20,473.15f);Paid(r.p);
            Near(r.p.CoolantOutputKelvin,496.84668f,.001f);Near(r.go.GetComponent<Generator>().TotalGenerated,3200);
        });
        Test("loaded coolant specific heat controls the actual 2 MDTU allocation",()=>
        {
            ElementLoader.SuperCoolantHeatCapacity=10;var r=Reactor();Paid(r.p);
            Near(r.p.CoolantOutputKelvin,393.15f,.001f);
            Near((r.p.CoolantOutputKelvin-373.15f)*2*10,400,.001f);
        });
        Test("mixed coolant chunks conserve slice heat instead of using tank mean",()=>
        {
            var r=Reactor();SetMass(r.s[3],SimHashes.SuperCoolant,1,300);
            var other=new GameObject();var pe=other.Add<PrimaryElement>();pe.ElementID=SimHashes.SuperCoolant;pe.Mass=19;pe.Temperature=400;r.s[3].items.Add(other);
            Near(r.p.CoolantInputKelvin,395);Paid(r.p);Near(r.p.CoolantOutputKelvin,373.69668f,.001f);
            float remainingHeat=0;foreach(var item in r.s[3].items){var e=item.GetComponent<PrimaryElement>();remainingHeat+=e.Mass*e.Temperature*8.44f;}
            Near(remainingHeat+r.p.CoolantOutputKelvin*2*8.44f,(1*300+19*400)*8.44f+400,.02f);
        });
        Test("measured generation averages paid steps and decays to zero",()=>
        {
            var r=Reactor();var g=r.go.GetComponent<Generator>();for(int i=1;i<=25;i++)
            {GameClock.Instance.time=i*.2f;g.ConsumeEnergy(3200);SetMass(r.s[3],SimHashes.SuperCoolant,20);SetMass(r.s[4],SimHashes.SuperCoolant,0);Paid(r.p);}
            Near(r.p.AverageGrossWatts,16000);Near(r.p.AverageCoolantHeat,2000);
            r.op.SetPowered(false);GameClock.Instance.time=10;Near(r.p.AverageGrossWatts,0);Near(r.p.AverageCoolantHeat,0);
            Near(g.TotalGenerated,80000);Near(g.JoulesAvailable,3200); // stored energy survives; no idle generation
        });
        Test("allocation and three-turbine waste budget use distinct game units",()=>
        {
            Near(FusionReactorBalance.ConvertedHeatKDTUPerSecond+FusionReactorBalance.CoolantHeatKDTUPerSecond,20000);
            Near(FusionReactorBalance.GrossWatts-FusionReactorBalance.OperatingWatts-2*480-1200,11840);
            float turbineExtracted=2*4.179f*(200-95);
            float turbineNetDeletion=turbineExtracted*.9f-4; // own heat returned to chamber
            Assert(3*turbineNetDeletion>2000+30+100,"three turbines lack even ideal headroom");
            Assert(4*10*8.44f*14>2000,"four aquatuners cannot even move rated waste heat");
            // This is a theoretical heat budget, not a radiant-pipe transfer test.
        });
        Test("600 seconds with one-second coolant packet cadence does not chatter",()=>
        {
            var r=Reactor();var g=r.go.GetComponent<Generator>();int reactions=0;
            float supplied=20,drained=0;for(int i=0;i<3000;i++)
            {
                GameClock.Instance.time=i*.2f;
                if(i>0&&i%5==0)
                {
                    SetMass(r.s[3],SimHashes.SuperCoolant,r.s[3].ExactMassStored()+10,373.15f);supplied+=10;
                    float removed=MathF.Min(10,r.s[4].ExactMassStored());drained+=removed;
                    SetMass(r.s[4],SimHashes.SuperCoolant,r.s[4].ExactMassStored()-removed);SetMass(r.s[2],He,0);
                }
                SetMass(r.s[0],D,1);SetMass(r.s[1],T,2.1f);g.ConsumeEnergy(3200);Paid(r.p);
                if(r.op.IsActive&&r.p.CurrentStopCause==FusionReactorProcess.StopCause.None)reactions++;
            }
            Assert(reactions==3000,$"only {reactions} packet-cadence reactions");
            Near(supplied,drained+r.s[3].ExactMassStored()+r.s[4].ExactMassStored(),.001f);
            Near(g.TotalGenerated,9600000,1);Near(r.p.AverageGrossWatts,16000,1);
        });
        Test("missing lithium falls back to D-D beside running reactor",()=>{var r=Reactor();Paid(r.p);var b=Breeder(0);Paid(b.p);Near(b.s[2].GetMassAvailable(T),.003f);Near(b.s[1].GetMassAvailable(D),.096f);Assert(!b.p.IsBreeding&&b.go.GetComponent<ConduitConsumer>().isOn,"startup feed remained locked");});
        Test("breeding conserves lithium plus earned neutrons",()=>{var r=Reactor();Paid(r.p);var b=Breeder();float before=r.p.AvailableNeutrons+b.s[0].GetMassAvailable(Li);Paid(b.p);Near(before,r.p.AvailableNeutrons+b.s[0].GetMassAvailable(Li)+b.s[2].GetMassAvailable(T)+b.s[3].GetMassAvailable(He));});
        Test("delayed breeder catches up without invented neutrons",()=>{var r=Reactor();Paid(r.p);Paid(r.p);Paid(r.p);var b=Breeder();Paid(b.p);Near(b.s[2].GetMassAvailable(T),.018f);Near(b.s[0].GetMassAvailable(Li),.964f);Near(r.p.AvailableNeutrons,0);});
        Test("unusable owner releases reactor to spare breeder",()=>{var r=Reactor();Paid(r.p);var a=Breeder();Paid(a.p);Paid(r.p);SetMass(a.s[0],Li,0);var b=Breeder();Paid(b.p);Assert(r.p.LinkedBreeder==b.p,"spare was blocked by empty owner");});
        Test("disabled owner releases lease",()=>{var r=Reactor();Paid(r.p);var a=Breeder();Paid(a.p);a.op.SetPowered(false);Paid(r.p);var b=Breeder();Paid(b.p);Assert(r.p.LinkedBreeder==b.p,"disabled owner retained lease");});
        Test("world boundary blocks neutron transfer",()=>{var r=Reactor();Paid(r.p);var b=Breeder();Grid.WorldIdx[b.go.Cell]=1;Assert(FusionReactorProcess.FindProvider(b.p)==null,"cross-world link");});
        Test("starved nearest reactor cannot hide funded provider",()=>{var a=Reactor();a.p.Sim200ms(.2f);var r=Reactor();r.go.transform.position=new Vector3(4,0,0);Paid(r.p);var b=Breeder();Paid(b.p);Assert(b.p.LinkedReactor==r.p,"nearest empty source selected");});
        Test("packet waits for entire required pipe space",()=>{Near(FusionBatchDispenser.PacketSize(1,.5f),0);Near(FusionBatchDispenser.PacketSize(.96f,.1f),0);Near(FusionBatchDispenser.PacketSize(.94f,0),0);Near(FusionBatchDispenser.PacketSize(.96f,0),.96f);Near(FusionBatchDispenser.PacketSize(2,0),1);});
        Test("real batch updater keeps blocked material and transfers germs",()=>
        {
            var b=Breeder();var building=b.go.Add<Building>();building.outputCell=30;var d=b.go.Add<ConduitDispenser>();d.storage=b.s[2];d.conduitType=ConduitType.Gas;d.elementFilter=new[]{T};
            b.s[2].AddGasChunk(T,1,350,1,1000);var batches=b.go.Add<FusionBatchDispenser>();Spawn(batches);var flow=Game.Instance.gasConduitFlow;
            flow.contents[30]=new(){mass=.5f,element=T};flow.Tick();Near(b.s[2].GetMassAvailable(T),1);Near(flow.GetContents(30).mass,.5f);
            flow.contents[30]=new();flow.Tick();Near(b.s[2].GetMassAvailable(T),0);Near(flow.GetContents(30).mass,1);Assert(flow.GetContents(30).diseaseCount==1000,"germs lost");
        });
        Test("600 seconds self-supply with 5 seconds lithium interruption",()=>
        {
            var r=Reactor(2.856f);var b=Breeder(50);var building=b.go.Add<Building>();building.outputCell=30;var d=b.go.Add<ConduitDispenser>();d.storage=b.s[2];d.conduitType=ConduitType.Gas;d.elementFilter=new[]{T};var batches=b.go.Add<FusionBatchDispenser>();Spawn(batches);
            int reactions=0;for(int i=0;i<3000;i++)
            {
                GameClock.Instance.time+=.2f;
                if(i==1000)SetMass(b.s[0],Li,0);if(i==1025)SetMass(b.s[0],Li,50);
                if(b.go.GetComponent<ConduitConsumer>().isOn){b.s[1].AddGasChunk(D,.004f,350,255,0);}else{r.s[0].AddGasChunk(D,.004f,350,255,0);}
                SetMass(r.s[3],SimHashes.SuperCoolant,20,373.15f);SetMass(r.s[4],SimHashes.SuperCoolant,0);SetMass(r.s[2],He,0);SetMass(b.s[3],He,0);SetMass(b.s[3],SimHashes.Hydrogen,0);
                r.go.GetComponent<Generator>().ConsumeEnergy(3200); // explicit external electrical load
                r.p.Sim200ms(.2f);b.p.Sim200ms(.2f);float dw=b.op.IsActive?1200:0;float rw=r.op.IsActive?2000:0;
                b.p.OnPowerSettlement(dw,true);r.p.OnPowerSettlement(rw,true);if(r.s[4].ExactMassStored()>0)reactions++;
                var flow=Game.Instance.gasConduitFlow;flow.Tick();var packet=flow.GetContents(30);float pulled=MathF.Min(.2f,packet.mass);if(pulled>0){r.s[1].AddGasChunk(T,pulled,350,255,0);packet.mass-=pulled;flow.contents[30]=packet;}
            }
            Assert(reactions==3000,$"only {reactions}/3000 paid reactions");Assert(r.s[1].GetMassAvailable(T)>1.8f,"fuel reserve depleted");
        });
        RunTripleAlphaScenarios();
        RunSeparatorScenarios();
        Console.WriteLine($"{passed} regression scenarios passed (offline adapters; Unity/Harmony still requires game validation).");
    }
}
