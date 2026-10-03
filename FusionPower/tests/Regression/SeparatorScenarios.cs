using System;
using System.Linq;
using System.Reflection;
using Baiye.FusionPower;

static partial class Program
{
    static (IsotopeSeparatorProcess p,Storage[] s,Operational op,UnityEngine.GameObject go) Separator()
    {
        var f=Fixture<IsotopeSeparatorProcess>(20,5,10,20);
        SetMass(f.s[0],SimHashes.Brine,20,310);
        return f;
    }
    static void Separate((IsotopeSeparatorProcess p,Storage[] s,Operational op,UnityEngine.GameObject go) f,float temperature=310,SimHashes kind=SimHashes.Brine)
    {
        SetMass(f.s[0],kind,20,temperature);
        SetMass(f.s[3],kind,0);
        GameClock.Instance.time+=.2f;Paid(f.p);
    }
    static float DroppedLithium(Storage s)=>s.droppedItems.Where(i=>i.GetComponent<PrimaryElement>().ElementID==Li)
        .Sum(i=>i.GetComponent<PrimaryElement>().Mass);

    static void RunSeparatorScenarios()
    {
        Test("separator ordinary salt water produces lithium and returns the same liquid without spilling",()=>
        {
            var f=Separator();SetMass(f.s[0],SimHashes.Brine,0);SetMass(f.s[0],SimHashes.SaltWater,20,330);
            Paid(f.p);
            Near(f.s[0].GetMassAvailable(SimHashes.SaltWater),18);
            Near(f.s[2].GetMassAvailable(Li),.012f);Near(f.s[3].GetMassAvailable(SimHashes.SaltWater),1.988f);
            Near(f.s[0].ExactMassStored()+f.s[2].ExactMassStored()+f.s[3].ExactMassStored(),20);
            Near(f.s[3].GetMassAvailable(SimHashes.Brine),0);Near(f.s[1].ExactMassStored(),0);
            Near(f.s[2].FindFirst(Li.CreateTag()).GetComponent<PrimaryElement>().Temperature,330);
            Near(f.s[3].FindFirst(SimHashes.SaltWater.CreateTag()).GetComponent<PrimaryElement>().Temperature,330);
        });
        Test("ordinary salt water stays stored through unpaid demand and blocked return then resumes",()=>
        {
            var f=Separator();SetMass(f.s[0],SimHashes.Brine,0);SetMass(f.s[0],SimHashes.SaltWater,20);
            f.p.Sim200ms(.2f);f.p.OnPowerSettlement(480,false);
            Near(f.s[0].GetMassAvailable(SimHashes.SaltWater),20);Near(f.s[2].ExactMassStored(),0);
            SetMass(f.s[3],SimHashes.SaltWater,20);Paid(f.p);
            Near(f.s[0].GetMassAvailable(SimHashes.SaltWater),20);Near(f.s[2].ExactMassStored(),0);
            SetMass(f.s[3],SimHashes.SaltWater,0);Paid(f.p);
            Near(f.s[0].GetMassAvailable(SimHashes.SaltWater),18);Near(f.s[2].GetMassAvailable(Li),.012f);
        });
        Test("600 seconds ordinary salt water creates seven whole lithium batches and retains the remainder",()=>
        {
            var f=Separator();SetMass(f.s[0],SimHashes.Brine,0);
            for(int i=0;i<3000;i++)Separate(f,310,SimHashes.SaltWater);
            Assert(f.s[2].dropEvents==7&&f.s[2].droppedItems.Count==7,"ordinary salt water did not produce whole batches");
            Near(DroppedLithium(f.s[2])+f.s[2].GetMassAvailable(Li),36,.003f);
            Near(f.s[2].GetMassAvailable(Li),.972f,.001f);Near(f.s[3].GetMassAvailable(SimHashes.Brine),0);
        });
        Test("separator retains lithium below 5 kg and releases one combined batch",()=>
        {
            var f=Separator();for(int i=0;i<416;i++)Separate(f);
            Assert(f.s[2].dropEvents==0,"lithium fell before a full batch");Near(f.s[2].GetMassAvailable(Li),4.992f,.001f);
            Assert(f.s[2].items.Count==1,"stock was fragmented inside the adapter");
            Separate(f);Assert(f.s[2].dropEvents==1&&f.s[2].droppedItems.Count==1,"batch not emitted as one pile");
            Near(DroppedLithium(f.s[2]),5.004f,.001f);Near(f.s[2].ExactMassStored(),0);
        });
        Test("600 seconds separator creates seven lithium drops and conserves all 36 kg",()=>
        {
            var f=Separator();for(int i=0;i<3000;i++)Separate(f);
            Assert(f.s[2].dropEvents==7&&f.s[2].droppedItems.Count==7,"wrong lithium drop frequency");
            foreach(var item in f.s[2].droppedItems)
            {float mass=item.GetComponent<PrimaryElement>().Mass;Assert(mass>=5&&mass<5.013f,"tiny or oversized pile");}
            Near(DroppedLithium(f.s[2])+f.s[2].GetMassAvailable(Li),36,.003f);
            Near(f.s[2].GetMassAvailable(Li),.972f,.001f);
        });
        Test("separator unpaid demand cannot add or release lithium",()=>
        {
            var f=Separator();SetMass(f.s[2],Li,4.998f);
            for(int i=0;i<100;i++)f.p.Sim200ms(.2f);
            f.p.OnPowerSettlement(480,false);f.p.OnPowerSettlement(0,true);
            Near(f.s[2].GetMassAvailable(Li),4.998f);Near(f.s[0].GetMassAvailable(SimHashes.Brine),20);
            Assert(f.s[2].dropEvents==0,"unpaid lithium was dropped");
        });
        Test("separator partial batch survives power loss and process respawn",()=>
        {
            var f=Separator();for(int i=0;i<100;i++)Separate(f);
            f.op.SetPowered(false);for(int i=0;i<100;i++)Separate(f);
            Near(f.s[2].GetMassAvailable(Li),1.2f,.001f);Assert(f.s[2].dropEvents==0,"power loss flushed partial stock");
            f.p.GetType().GetMethod("OnCleanUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f.p,null);spawned.Remove(f.p);
            Spawn(f.p);Near(f.s[2].GetMassAvailable(Li),1.2f,.001f);
            f.op.SetPowered(true);Separate(f);Near(f.s[2].GetMassAvailable(Li),1.212f,.001f);
            Assert(f.s[2].dropEvents==0,"respawn emitted partial stock");
            Assert(f.s[2].showInUI,"restored lithium inventory hidden");
            // A process respawn with retained Storage is not a native save round trip.
        });
        Test("separator restored near-threshold stock preserves mass and product temperature",()=>
        {
            var f=Separator();SetMass(f.s[2],Li,4.998f,300);Separate(f,400);
            Assert(f.s[2].droppedItems.Count==1,"near-threshold stock was split");
            var item=f.s[2].droppedItems[0].GetComponent<PrimaryElement>();
            Near(item.Mass,5.01f,.0001f);Near(item.Temperature,(4.998f*300+.012f*400)/5.01f,.001f);
            Near(f.s[0].GetMassAvailable(SimHashes.Brine)+f.s[3].GetMassAvailable(SimHashes.Brine)+.012f,20,.0001f);
        });
        Test("separator blocked liquid return preserves partial lithium then resumes",()=>
        {
            var f=Separator();SetMass(f.s[2],Li,4.998f);SetMass(f.s[3],SimHashes.Brine,20);Paid(f.p);
            Near(f.s[2].GetMassAvailable(Li),4.998f);Near(f.s[0].GetMassAvailable(SimHashes.Brine),20);
            Assert(f.s[2].dropEvents==0,"blocked separator flushed stock");
            SetMass(f.s[3],SimHashes.Brine,0);Paid(f.p);Assert(f.s[2].dropEvents==1,"separator failed to resume");
            Near(DroppedLithium(f.s[2]),5.01f,.0001f);
        });
        Test("separator water recipe does not flush partial lithium or change deuterium yield",()=>
        {
            var f=Separator();SetMass(f.s[2],Li,2);SetMass(f.s[0],SimHashes.Brine,0);SetMass(f.s[0],SimHashes.Water,20,320);
            Paid(f.p);Near(f.s[2].GetMassAvailable(Li),2);Assert(f.s[2].dropEvents==0,"water path flushed lithium");
            Near(f.s[1].GetMassAvailable(D),.004f);Near(f.s[3].GetMassAvailable(SimHashes.Water),1.996f);
            Near(f.s[0].GetMassAvailable(SimHashes.Water)+f.s[1].GetMassAvailable(D)+f.s[3].GetMassAvailable(SimHashes.Water),20);
        });
    }
}
