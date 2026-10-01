using System;
using Baiye.FusionPower;

static partial class Program
{
    static (TripleAlphaProcess p,Storage[] s,Operational op,UnityEngine.GameObject go) Alpha()
    {
        var f=Fixture<TripleAlphaProcess>(10,50,20,20,20);
        f.go.GetComponent<Generator>().Capacity=8800;
        SetMass(f.s[0],He,5,525);
        SetMass(f.s[2],SimHashes.SuperCoolant,20,373.15f);
        return f;
    }

    static void RunTripleAlphaScenarios()
    {
        Test("triple alpha paid step consumes helium and produces carbon plus 880 J",()=>
        {
            var f=Alpha();Paid(f.p);
            Near(f.s[0].GetMassAvailable(He),4.984f);
            Near(f.s[1].GetMassAvailable(SimHashes.RefinedCarbon),.016f*TripleAlphaBalance.CarbonMassRatio);
            Near(f.go.GetComponent<Generator>().TotalGenerated,880);
            Near(f.s[2].ExactMassStored()+f.s[3].ExactMassStored(),20);
            float carbonHeat=.016f*TripleAlphaBalance.CarbonMassRatio*.71f*350f;
            float heliumHeat=.016f*.14f*525f;
            float coolantHeat=(f.s[3].FindFirst(SimHashes.SuperCoolant.CreateTag()).GetComponent<PrimaryElement>().Temperature-373.15f)*2*8.44f;
            Near(coolantHeat+carbonHeat-heliumHeat,110,.002f);
        });
        Test("triple alpha unpaid or partially funded demand creates nothing",()=>
        {
            var f=Alpha();for(int i=0;i<100;i++)f.p.Sim200ms(.2f);
            f.p.OnPowerSettlement(1200,false);f.p.OnPowerSettlement(0,true);
            Near(f.p.HeliumKg,5);Near(f.p.CarbonKg,0);Near(f.p.CoolantInputKg,20);
            Near(f.go.GetComponent<Generator>().TotalGenerated,0);
            Assert(!f.go.GetComponent<RadiationEmitter>().emitting,"unpaid gamma radiation");
        });
        Test("triple alpha disabled building retains materials and loses radiation",()=>
        {
            var f=Alpha();Paid(f.p);float before=f.p.HeliumKg;
            Assert(f.go.GetComponent<RadiationEmitter>().emitting,"paid gamma missing");
            f.op.SetPowered(false);Paid(f.p);Near(f.p.HeliumKg,before);
            Assert(!f.go.GetComponent<RadiationEmitter>().emitting,"idle gamma persisted");
            f.op.SetPowered(true);Assert(!Ready(f.p),"power loss bypassed coolant reserve");
        });
        Test("triple alpha rejects ordinary hydrogen and requires helium",()=>
        {
            var f=Alpha();SetMass(f.s[0],He,0);f.s[0].AddGasChunk(SimHashes.Hydrogen,1,350,255,0);
            Paid(f.p);Near(f.p.CarbonKg,0);Near(f.p.CoolantInputKg,20);
            Assert(f.p.CurrentStopCause==TripleAlphaProcess.StopCause.Helium,"wrong fuel accepted");
        });
        Test("triple alpha primes once and rearms only after actual cold starvation",()=>
        {
            var f=Alpha();SetMass(f.s[2],SimHashes.SuperCoolant,10);Paid(f.p);
            Assert(f.p.CurrentStopCause==TripleAlphaProcess.StopCause.CoolantReserve,"premature ignition");
            SetMass(f.s[2],SimHashes.SuperCoolant,20);
            for(int i=0;i<10;i++){f.go.GetComponent<Generator>().ConsumeEnergy(880);Paid(f.p);}
            Near(f.p.CoolantInputKg,0);Assert(f.op.IsActive,"speculative stop on successful last slice");
            f.p.Sim200ms(.2f);Assert(!f.op.IsActive,"real cold starvation did not stop");
            SetMass(f.s[3],SimHashes.SuperCoolant,0);SetMass(f.s[2],SimHashes.SuperCoolant,10);
            Assert(!Ready(f.p),"starved processor chatter");SetMass(f.s[2],SimHashes.SuperCoolant,20);
            Assert(Ready(f.p),"full reserve did not recover");
        });
        Test("triple alpha blocked carbon or coolant stops before payment recipe",()=>
        {
            var f=Alpha();SetMass(f.s[1],SimHashes.RefinedCarbon,50);Paid(f.p);
            Assert(f.p.CurrentStopCause==TripleAlphaProcess.StopCause.CarbonOutput,"ignored carbon capacity");
            Near(f.p.HeliumKg,5);Near(f.p.CoolantInputKg,20);
            SetMass(f.s[1],SimHashes.RefinedCarbon,0);SetMass(f.s[3],SimHashes.SuperCoolant,20);Paid(f.p);
            Assert(f.p.CurrentStopCause==TripleAlphaProcess.StopCause.CoolantOutput,"ignored hot capacity");
            Near(f.p.HeliumKg,5);Near(f.go.GetComponent<Generator>().TotalGenerated,0);
        });
        Test("triple alpha full electrical buffer stops and resumes without free heat",()=>
        {
            var f=Alpha();var g=f.go.GetComponent<Generator>();g.GenerateJoules(8800);Paid(f.p);
            Assert(f.p.CurrentStopCause==TripleAlphaProcess.StopCause.PowerOutput,"ignored electrical capacity");
            Near(f.p.HeliumKg,5);Near(f.p.CoolantOutputKg,0);
            g.ConsumeEnergy(880);Paid(f.p);Near(g.JoulesAvailable,8800);Near(f.p.CoolantOutputKg,2);
        });
        Test("triple alpha unsafe cold inlet or body temperature preserves fuel",()=>
        {
            var f=Alpha();SetMass(f.s[2],SimHashes.SuperCoolant,20,679);Paid(f.p);
            Assert(f.p.CurrentStopCause==TripleAlphaProcess.StopCause.ThermalRange,"unsafe hot output");
            Near(f.p.HeliumKg,5);SetMass(f.s[2],SimHashes.SuperCoolant,20,373.15f);
            f.go.GetComponent<PrimaryElement>().Temperature=1473.15f;Paid(f.p);
            Assert(f.p.CurrentStopCause==TripleAlphaProcess.StopCause.BodyHot,"body protection missing");Near(f.p.CarbonKg,0);
        });
        Test("triple alpha heterogeneous coolant transfers exact slice enthalpy and germs",()=>
        {
            var f=Alpha();SetMass(f.s[2],SimHashes.SuperCoolant,1,300);
            var first=f.s[2].items[0].GetComponent<PrimaryElement>();first.DiseaseIdx=1;first.DiseaseCount=100;
            var second=new UnityEngine.GameObject();var e=second.Add<PrimaryElement>();e.ElementID=SimHashes.SuperCoolant;
            e.Mass=19;e.Temperature=400;e.DiseaseIdx=1;e.DiseaseCount=1900;f.s[2].items.Add(second);
            Near(f.p.CoolantInputKelvin,395);Paid(f.p);
            float added=110+.016f*.14f*525-.016f*TripleAlphaBalance.CarbonMassRatio*.71f*350;
            Near(f.p.CoolantOutputKelvin,350+added/(2*8.44f),.001f);
            var output=f.s[3].items[0].GetComponent<PrimaryElement>();
            Assert(output.DiseaseIdx==1&&output.DiseaseCount==200&&e.DiseaseCount==1800,"coolant germs lost");
            Near(e.Mass,18);Near(f.p.CoolantInputKg+f.p.CoolantOutputKg,20);
        });
        Test("triple alpha carbon batches wait for 20 kg and preserve remaining inventory",()=>
        {
            var f=Alpha();var batch=f.go.Add<TripleAlphaCarbonBatch>();Spawn(batch);
            SetMass(f.s[1],SimHashes.RefinedCarbon,19.99f);batch.Sim200ms(.2f);Near(f.s[4].ExactMassStored(),0);
            SetMass(f.s[1],SimHashes.RefinedCarbon,45,390);var product=f.s[1].items[0].GetComponent<PrimaryElement>();
            product.DiseaseIdx=1;product.DiseaseCount=4500;batch.Sim200ms(.2f);
            Near(f.s[1].ExactMassStored(),25);Near(f.s[4].ExactMassStored(),20);Near(f.p.CarbonKg,45);
            var packet=f.s[4].items[0].GetComponent<PrimaryElement>();Near(packet.Temperature,390);
            Assert(packet.DiseaseCount==2000&&product.DiseaseCount==2500,"transfer changed carbon germs");
            batch.Sim200ms(.2f);Near(f.s[1].ExactMassStored(),25);Near(f.s[4].ExactMassStored(),20);
            f.s[4].DropAll();batch.Sim200ms(.2f);Near(f.s[1].ExactMassStored(),5);Near(f.s[4].ExactMassStored(),20);
        });
        Test("triple alpha undersized or occupied carbon dispatch cannot receive another batch",()=>
        {
            var f=Alpha();var batch=f.go.Add<TripleAlphaCarbonBatch>();Spawn(batch);
            SetMass(f.s[1],SimHashes.RefinedCarbon,30);f.s[4].capacityKg=19;batch.Sim200ms(.2f);
            Near(f.s[1].ExactMassStored(),30);Near(f.s[4].ExactMassStored(),0);
            f.s[4].capacityKg=20;SetMass(f.s[4],SimHashes.RefinedCarbon,1);batch.Sim200ms(.2f);
            Near(f.s[1].ExactMassStored(),30);Near(f.s[4].ExactMassStored(),1);
        });
        Test("triple alpha steady averages decay and never supply breeding neutrons",()=>
        {
            var f=Alpha();var g=f.go.GetComponent<Generator>();for(int i=1;i<=25;i++)
            {GameClock.Instance.time=i*.2f;g.ConsumeEnergy(880);SetMass(f.s[2],SimHashes.SuperCoolant,20);SetMass(f.s[3],SimHashes.SuperCoolant,0);Paid(f.p);}
            Near(f.p.AverageGrossWatts,4400);Assert(f.p.AverageCoolantHeat>530&&f.p.AverageCoolantHeat<550,"heat budget wrong");
            var b=Breeder();Assert(FusionReactorProcess.FindProvider(b.p)==null,"triple alpha invented neutrons");
            GameClock.Instance.time=10;Near(f.p.AverageGrossWatts,0);Near(f.p.AverageCoolantHeat,0);Near(g.TotalGenerated,22000);
        });
        Test("600 seconds triple alpha accepts helium batches and one-second cold packets",()=>
        {
            var f=Alpha();var g=f.go.GetComponent<Generator>();var batch=f.go.Add<TripleAlphaCarbonBatch>();Spawn(batch);
            float coldSupplied=20,hotDrained=0,exported=0,heliumSupplied=5;int steps=0;
            for(int i=0;i<3000;i++)
            {
                GameClock.Instance.time=i*.2f;
                if(i>0&&i%5==0)
                {
                    f.s[2].AddLiquid(SimHashes.SuperCoolant,10,373.15f,255,0);coldSupplied+=10;
                    float take=MathF.Min(10,f.p.CoolantOutputKg);f.s[3].ConsumeIgnoringDisease(SimHashes.SuperCoolant.CreateTag(),take);hotDrained+=take;
                    exported+=f.s[4].ExactMassStored();f.s[4].DropAll();
                }
                // Two 0.96 kg helium batches per 24 s match one D-T reactor plus breeder.
                if(i>0&&i%120==0){f.s[0].AddGasChunk(He,1.92f,525,255,0);heliumSupplied+=1.92f;}
                g.ConsumeEnergy(880);Paid(f.p);batch.Sim200ms(.2f);
                if(f.op.IsActive&&f.p.CurrentStopCause==TripleAlphaProcess.StopCause.None)steps++;
                Assert(f.p.CoolantInputKg<=20.001f&&f.p.CoolantOutputKg<=20.001f,"coolant overfilled");
            }
            Assert(steps==3000,$"only {steps}/3000 alpha reactions");Near(g.TotalGenerated,2640000,1);
            Near(coldSupplied,hotDrained+f.p.CoolantInputKg+f.p.CoolantOutputKg,.001f);
            Near(exported+f.p.CarbonKg,48*TripleAlphaBalance.CarbonMassRatio,.006f);
            Near(heliumSupplied-f.p.HeliumKg,48,.006f);
            Near(f.p.TotalMassDefectKg,48*(1-TripleAlphaBalance.CarbonMassRatio),.00003f);
        });
    }
}
