using System.Collections.Generic;
using System.Linq;

namespace Baiye.FusionPower
{
    public static class FusionCodex
    {
        public static void Attach(BuildingDef def,System.Func<bool> language=null)
        {
            var previous=def.ExtendCodexEntry;
            def.ExtendCodexEntry=entry=>Append(previous!=null?previous(entry):entry,def.PrefabID,language!=null?language():FusionStrings.UseChinese());
        }
        public static CodexEntry Append(CodexEntry entry,string id,bool chinese)
        {
            string[] sections=Sections(id,chinese);
            if(entry==null||sections.Length==0)return entry;
            string marker="BAIYE_FUSION_GUIDE_"+id;
            if(entry.contentContainers.Any(c=>c!=null&&c.content!=null&&c.content.OfType<CodexText>().Any(t=>t.messageID==marker)))return entry;
            for(int n=0;n<sections.Length;n+=2)
                entry.AddContentContainer(new ContentContainer(new List<ICodexWidget>{new CodexText(sections[n],CodexTextStyle.Subtitle,n==0?marker:null),new CodexText(sections[n+1])},ContentContainer.ContentLayout.Vertical));
            return entry;
        }
        public static string[] Sections(string id,bool zh)
        {
            var sections=new List<string>();
            void Add(string title,string body,string enTitle,string enBody){sections.Add(zh?title:enTitle);sections.Add(zh?body:enBody);}
            if(id!=FusionIds.Separator&&id!=FusionIds.Breeder&&id!=FusionIds.Reactor&&id!=FusionIds.TripleAlpha)return sections.ToArray();
            Add("核子工艺链","同位素分离器：水提氘、盐水提锂-6。氚增殖器先用氘制备启动氚，反应堆运行后利用锂与已产生的中子增殖氚。反应堆消耗氘氚，排出氦-4；三α炉把氦变成精炼碳。准备外部启动电源，并分别接通燃料、产物及循环过冷液。",
                "Nuclear process chain","Separate deuterium from water and lithium-6 from brine. Bootstrap tritium from deuterium, then breed it from lithium and earned reactor neutrons. The reactor consumes deuterium/tritium and emits helium-4; the triple-alpha furnace converts helium into refined carbon. Supply external startup power and connect fuel, products and super-coolant loops.");
            if(id==FusionIds.Separator)
            {
                Add("接口与配方","液体入口 (-1,0)，氘气出口 (1,1)，处理后原液出口 (1,0)，电力/自动化 (0,0)。480 W。每秒处理 10 kg 水，产 20 g 氘与 9.98 kg 水；或处理 10 kg 盐水（Brine），产 60 g 锂-6 与 9.94 kg 盐水。两者同时存在时优先处理水。此处使用原版 Brine，不是 SaltWater。",
                    "Ports and recipes","Liquid in (-1,0), deuterium out (1,1), depleted liquid out (1,0), power/automation (0,0). Demand 480 W. Process 10 kg/s water into 20 g/s deuterium and 9.98 kg/s water, or 10 kg/s Brine into 60 g/s lithium-6 and 9.94 kg/s Brine. Water takes priority. Brine is distinct from SaltWater.");
                Add("锂批次与堵塞","锂累积约 5 kg 后整批掉落，满速首批约 83 秒；不足一批留仓，断电与读档不会丢失。复制人或清扫器把锂送到增殖器。原液必须接回储存/循环，气体与液体出口堵塞会暂停对应配方；不能靠无限输入绕过输出缓存。",
                    "Lithium batches and blockages","Lithium drops as a complete approximately 5 kg pile; first batch takes about 83 seconds at full rate. Partial stock persists through power loss and saves. Deliver it to breeders. Return depleted liquid to storage/circulation. Blocked output buffers pause the corresponding recipe.");
            }
            else if(id==FusionIds.Breeder)
            {
                Add("接口与启动","氘气入口 (-1,0)，氚气出口 (1,1)，尾气出口 (-1,1)，电力/自动化 (0,0)。耗电 1.2 kW；固体锂-6通过复制人或清扫器投送。未连接可用中子时，20 g/s 氘 → 15 g/s 氚 + 5 g/s 氢气；尾气出口要能接收氢与氦，不能只接收氦。",
                    "Ports and bootstrap","Deuterium in (-1,0), tritium out (1,1), tail gas out (-1,1), power/automation (0,0). Demand 1.2 kW. Deliver solid lithium-6 by duplicant or sweeper. Without a neutron link, 20 g/s deuterium yields 15 g/s tritium and 5 g/s hydrogen. Tail gas must accept both hydrogen and helium.");
                Add("中子增殖与安全","放在反应堆约 8 格距离内，按实际产生的中子额度联动；一个反应堆同时绑定一个增殖器，中子不会作为管道物质出现。稳态 60 g/s 锂-6 + 10 g/s 中子额度 → 30 g/s 氚 + 40 g/s 氦；已积存额度可短时提高处理速度。启动模式为游戏中的氘核反应抽象，稳态依据锂-6俘获中子。运行有辐射，氚也具有放射性。缺锂、缺启动氘或氚/尾气缓存满会停机或转入可用启动模式。",
                    "Neutrons and safety","Place within about 8 cells of a reactor. One breeder binds to each reactor, consuming only earned neutron credits; neutrons are not pipe material. Steady state: 60 g/s lithium + 10 g/s credits yields 30 g/s tritium and 40 g/s helium. Stored credits permit brief catch-up. Bootstrap is a game abstraction of deuterium reactions; steady breeding is based on lithium-6 neutron capture. Operation emits radiation and tritium is radioactive. Fuel shortages and full gas buffers block the available mode.");
            }
            else if(id==FusionIds.Reactor)
            {
                Add("五个物料接口","底部由左到右：氘气 (-2,0)、过冷液入口 (-1,0)、氚气 (0,0)、过冷液出口 (1,0)、氦气 (2,0)。燃料为气态氘和气态氚，不能用普通氢气替代。电力/自动化位于 (0,0)，与不同覆盖层的气口可共处同格。",
                    "Five material ports","Bottom row left to right: deuterium (-2,0), coolant in (-1,0), tritium (0,0), coolant out (1,0), helium (2,0). Use gaseous custom isotopes, not native hydrogen. Power/automation also (0,0) on a different overlay.");
                Add("点火与持续运行","点火前供外部电力，积存至少 2 kg 氚和 20 kg 过冷液；氘入口也必须有燃料。额定消耗 20 g/s 氘 + 30 g/s 氚，输出 40 g/s 氦，并形成 10 g/s 中子额度给附近增殖器。反应堆满速与一台稳态增殖器的氚产量匹配；水提氘设备还需覆盖启动或额外用氘需求。气体管道容量是输送上限，不是燃料消耗目标。断料后重新点火需要重建储备。",
                    "Ignition and continuous operation","Supply external power and accumulate at least 2 kg tritium plus 20 kg coolant, with deuterium available. Rated use: 20 g/s deuterium + 30 g/s tritium; output 40 g/s helium and 10 g/s earned neutron credits. One steady breeder matches tritium demand. Extra deuterium supply may be needed for bootstrap. Pipe capacity is a transport limit, not a fuel consumption target. Fuel loss requires rebuilding ignition reserves.");
                Add("电力与余热","总发电 16 kW，堆内自耗 2 kW，扣堆自耗约 14 kW；全工艺净电还要扣除分离、增殖、泵与冷却等辅助设备。机内能量转换后，10 kg/s 循环过冷液带走约 2 MDTU/s 余热；默认比热下温升约 23.7°C。液冷机只是搬热，最终必须有热交换和散热去向。电力与 DTU 预算按游戏比例压缩，不能当作现实转换效率。",
                    "Power and waste heat","Gross 16 kW, reactor demand 2 kW, about 14 kW after reactor demand. Plant net also subtracts separation, breeding, pumping and cooling. Circulating 10 kg/s super coolant carries about 2 MDTU/s residual heat, about 23.7 C rise with default heat capacity. Aquatuners move heat; provide a final heat sink. Electrical and DTU budgets are game-scaled, not a real efficiency calculation.");
                Add("停机与辐射","检查氚储备、独立气口、过冷液储备/实际流量、热液出口与氦出口。电力缓存满、冷却出口过热（680 K 上限）或机体达 1200°C 也会停机。使用足够承载的干线，并通过变压器分配用电。中子通过联动与状态表示，运行辐射仍需隔离；氦-4是稳定产物，但氘氚工艺不能称为完全没有放射性风险。",
                    "Stops and radiation","Check tritium reserves, separate gas ports, coolant reserve/flow, hot coolant and helium outputs. Full electrical buffers, predicted coolant output at 680 K or body at 1200 C stop operation. Use a suitable high-wattage trunk and transformers. Neutrons are represented by links/status; shield operational radiation. Helium-4 is stable, but the tritium process is not radiation-free.");
            }
            else
            {
                Add("接口与配方","氦气入口 (-1,0)，过冷液入口 (0,0)、出口 (1,0)，精炼碳轨道出口 (2,0)，电力/自动化 (0,0)。每秒消耗 80 g 氦-4，产约 79.95 g 精炼碳；一套反应堆与稳态增殖器各产 40 g/s 氦，合计匹配一台炉。无需新造废气建筑。",
                    "Ports and recipe","Helium in (-1,0), coolant in (0,0), coolant out (1,0), refined-carbon conveyor out (2,0), power/automation (0,0). Consumes 80 g/s helium-4, yields about 79.95 g/s refined carbon. Reactor and steady breeder each yield 40 g/s helium, together matching one furnace.");
                Add("点火、发电和产物","需外部启动电力及 20 kg 过冷液储备。总发电 4.4 kW，自耗 1.2 kW，扣炉自耗约 3.2 kW。10 kg/s 过冷液带走剩余热预算约 0.55 MDTU/s，预算包含产物显热。精炼碳每满 20 kg 经轨道送出，不足一批留仓；堵塞、缺氦、缺冷却或电力缓存满会停机。精炼碳可进入原版工艺。",
                    "Startup, power and products","Needs external startup power and 20 kg coolant reserve. Gross 4.4 kW, demand 1.2 kW, about 3.2 kW after furnace demand. At 10 kg/s, coolant carries a residual budget around 0.55 MDTU/s, including product sensible heat. Carbon leaves in 20 kg conveyor batches; partial stock stays. Fuel/cooling shortages and full conveyor/electrical buffers stop work. Carbon feeds native industry.");
                Add("理论依据与辐射","三α反应是恒星中由三个氦-4核合成碳-12的放能过程；小型可控装置、速率及产电是未来科技和游戏设定。运行产生 γ 辐射，不提供增殖中子。稳定碳产物不意味着设备无需辐射防护。",
                    "Theory and radiation","Stellar triple-alpha fusion converts three helium-4 nuclei into carbon-12 and releases energy. Compact controlled operation, rates and generation are speculative game technology. Emits gamma radiation, not breeding neutrons. Stable carbon does not remove the need for shielding.");
            }
            Add("原理与游戏参数","氘氚聚变与锂-6增殖有核反应依据，氦-4与碳-12为稳定产物。元素分离比例、材料质量额度、反应速率、发电和机内转换能力均为可玩的游戏抽象；不据此推算现实能量、可行性或原料同位素丰度。",
                "Physics and game scaling","Deuterium-tritium fusion and lithium-6 breeding have nuclear reaction bases; helium-4 and carbon-12 are stable products. Extraction ratios, material credits, rates, power and internal conversion are gameplay abstractions, not real isotope abundances, energy yields or engineering feasibility.");
            return sections.ToArray();
        }
    }
}
