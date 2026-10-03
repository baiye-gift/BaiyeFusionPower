using System;
using System.Collections.Generic;

namespace Baiye.FusionPower
{
    internal static class FusionStrings
    {
        private static readonly Dictionary<string, string> names = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> descriptions = new Dictionary<string, string>();

        internal static void Register()
        {
            bool zh = UseChinese();
            AddElement(FusionIds.Deuterium, zh ? "氘气" : "Deuterium Gas", zh ? "氢的稳定同位素。由同位素分离器处理水获得；气态送入氚增殖器启动制氚，或与氚分别输入聚变反应堆。普通氢气不能替代。" : "Stable hydrogen isotope extracted from water by the isotope separator. Gas feeds breeder bootstrap or the reactor alongside tritium; native hydrogen is not interchangeable.");
            AddElement(FusionIds.LiquidDeuterium, zh ? "液态氘" : "Liquid Deuterium", zh ? "气态氘降温所得的液相；属于同一燃料的低温储存相态。当前增殖器和反应堆入口要求气态氘，需升温气化后输送。" : "Cryogenic storage phase of deuterium. Current breeder and reactor ports require gas; warm and vaporize before feeding.");
            AddElement(FusionIds.SolidDeuterium, zh ? "固态氘" : "Solid Deuterium", zh ? "液态氘进一步降温所得固相；不是直接可配送的反应堆燃料。应先恢复气态，再经气体管道送入对应入口。" : "Frozen deuterium, not a directly delivered reactor fuel. Restore gaseous phase before piping into fuel ports.");
            AddElement(FusionIds.Tritium, zh ? "氚气" : "Tritium Gas", zh ? "放射性氢同位素。由氚增殖器启动制备或锂-6中子增殖获得；送入反应堆独立氚气口，点火需至少 2 kg 储备。氦产物稳定不意味着氚无放射性。" : "Radioactive hydrogen isotope made by breeder bootstrap or lithium-6 neutron breeding. Feed the separate reactor tritium port; ignition requires 2 kg reserve. Stable helium does not make tritium nonradioactive.");
            AddElement(FusionIds.LiquidTritium, zh ? "液态氚" : "Liquid Tritium", zh ? "氚的低温液相，仍是放射性同位素。当前聚变接口只接收气态氚，低温储存后需恢复气态。" : "Cryogenic liquid tritium remains radioactive. Current fusion ports require gaseous tritium; vaporize stored liquid first.");
            AddElement(FusionIds.SolidTritium, zh ? "固态氚" : "Solid Tritium", zh ? "氚的冻结相态，仍具有放射性。不能把固态直接当作反应堆管道燃料，需升温恢复气态。" : "Frozen tritium remains radioactive. Warm back to gas before feeding the reactor fuel pipe.");
            AddElement(FusionIds.Helium, zh ? "氦-4" : "Helium-4", zh ? "稳定的惰性氦-4，由氘氚反应堆及锂-6增殖尾气产生。可储存、排向太空，或输入三α核合成炉合成原版精炼碳并回收能量。不可呼吸。" : "Stable inert helium-4 from fusion and breeder tail gas. Store, vent to space or feed the triple-alpha furnace for refined carbon and energy recovery. Unbreathable.");
            AddElement(FusionIds.LiquidHelium, zh ? "液态氦-4" : "Liquid Helium-4", zh ? "稳定氦-4的极低温液相。三α炉接收气体，需恢复气态再送入；本模组不把液氦自动当作过冷液冷却剂。" : "Cryogenic stable helium-4. Vaporize before feeding the gas-only triple-alpha port; liquid helium does not replace super coolant in this mod.");
            AddElement(FusionIds.Lithium, zh ? "锂-6" : "Lithium-6", zh ? "由同位素分离器处理盐水（SaltWater）或浓盐水（Brine）获得，约 5 kg 批量掉落。复制人或清扫器把固态锂-6送入氚增殖器；与实际聚变中子额度反应产生氚和氦。" : "Extracted from SaltWater or Brine by the separator, dropped in about 5 kg batches. Deliver solid lithium-6 to the breeder for tritium and helium using earned fusion neutron credits.");
            AddElement(FusionIds.LiquidLithium, zh ? "液态锂-6" : "Liquid Lithium-6", zh ? "锂-6受热熔融的液相。当前增殖器通过固体配送接收锂，不接收液锂管道；需降温固化后使用。" : "Molten lithium-6. The current breeder takes solid deliveries, not liquid piping; cool and solidify before use.");
            AddElement(FusionIds.LithiumVapor, zh ? "锂-6蒸气" : "Lithium-6 Vapor", zh ? "锂-6的高温气相，不是可直接输入增殖器的气体燃料。冷却恢复固态后，按正常固体配送流程使用。" : "Hot lithium-6 vapor is not a breeder gas fuel. Cool to solid and deliver through the normal solid-material route.");

            AddBuilding(FusionIds.Separator, zh ? "同位素分离器" : "Isotope Separator",
                zh ? "从水、盐水和浓盐水中分离聚变燃料原料。" : "Separates fusion feedstock from water, salt water and brine.",
                zh ? "处理水得到氘，处理盐水（SaltWater）或浓盐水（Brine）得到锂-6；提取后的原类型液体经液管回流。锂以 60 g/s 累积在机内，约每满 5 kg 批量掉落；未满批次的库存会保留，供增殖器的首批锂需等待约 83 秒。"
                   : "Extracts deuterium from water and lithium-6 from SaltWater or Brine, returning the same liquid type through the liquid outlet. Lithium accumulates at 60 g/s and drops in approximately 5 kg batches; partial stock is retained. The first lithium batch for a breeder takes about 83 seconds.");
            AddBuilding(FusionIds.Breeder, zh ? "氚增殖器" : "Tritium Breeder",
                zh ? "用聚变中子照射锂-6制取氚。" : "Irradiates lithium-6 with fusion neutrons to breed tritium.",
                zh ? "启动时用氘制氚；连接运行中的聚变反应堆后，用中子和锂-6增殖氚，另排出氦。" : "Starts by making tritium from deuterium; when linked to an active fusion reactor, breeds tritium from lithium-6 and neutrons and emits helium.");
            AddBuilding(FusionIds.Reactor, zh ? "核聚变反应堆" : "Fusion Reactor",
                zh ? "约束氘氚等离子体，通过机内能量转换装置发电。" : "Confines deuterium-tritium plasma and generates electricity through an internal conversion system.",
                zh ? "额定发电 16 kW，堆内耗电 2 kW；10 kg/s 循环过冷液带走约 2 MDTU/s 余热。消耗氘和氚，排出氦-4并向附近增殖器提供中子。点火需外部电源、2 kg 氚及 20 kg 过冷液储备；缺料、断电、冷却受阻或电力缓存满时停机。"
                   : "Rated generation 16 kW with 2 kW reactor demand; 10 kg/s circulating super coolant removes about 2 MDTU/s waste heat. Consumes deuterium and tritium, emits helium-4 and supplies neutrons to a nearby breeder. Startup needs external power, 2 kg tritium and 20 kg coolant. Stops on fuel, power, cooling failure or a full electrical buffer.");
            AddBuilding(FusionIds.TripleAlpha, zh ? "三α核合成炉" : "Triple-Alpha Furnace",
                zh ? "模拟恒星中的三α核反应，将氦-4合成稳定的碳-12。小型人工约束为未来技术设定。"
                   : "Synthesizes stable carbon-12 from helium-4 through the stellar triple-alpha reaction. Compact artificial confinement is speculative technology.",
                zh ? "每秒消耗 80 g 氦，产出约 79.95 g 原版精炼碳；额定发电 4.4 kW，自耗 1.2 kW。需要 10 kg/s 过冷液，剩余热预算约 0.55 MDTU/s，包含产物显热。碳每满 20 kg 经运输轨道输出。点火需外部电力及 20 kg 冷却储备；缺料、冷却受阻、碳缓冲或电力缓存满时停机。运行产生γ辐射，不提供增殖中子。"
                   : "Consumes 80 g/s helium and produces about 79.95 g/s refined carbon; rated generation 4.4 kW, demand 1.2 kW. Requires 10 kg/s super coolant. Residual heat budget about 0.55 MDTU/s includes product sensible heat. Carbon leaves in 20 kg conveyor batches. Startup needs external power and 20 kg coolant reserve. Stops on missing fuel, cooling failure or full product/electrical buffers. Emits gamma radiation; no breeding neutrons.");
        }

        internal static bool UseChinese()
        {
            string language = Localization.GetCurrentLanguageCode();
            return language != null && (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                || language.EndsWith("chinese", StringComparison.OrdinalIgnoreCase));
        }

        internal static void RefreshLoadedElements()
        {
            if (ElementLoader.elementTable == null) return;
            RefreshElement(FusionIds.Deuterium);
            RefreshElement(FusionIds.LiquidDeuterium);
            RefreshElement(FusionIds.SolidDeuterium);
            RefreshElement(FusionIds.Tritium);
            RefreshElement(FusionIds.LiquidTritium);
            RefreshElement(FusionIds.SolidTritium);
            RefreshElement(FusionIds.Helium);
            RefreshElement(FusionIds.LiquidHelium);
            RefreshElement(FusionIds.Lithium);
            RefreshElement(FusionIds.LiquidLithium);
            RefreshElement(FusionIds.LithiumVapor);
        }

        internal static bool IsFusionElement(SimHashes hash)
        {
            return hash == FusionIds.HashOf(FusionIds.Deuterium)
                || hash == FusionIds.HashOf(FusionIds.LiquidDeuterium)
                || hash == FusionIds.HashOf(FusionIds.SolidDeuterium)
                || hash == FusionIds.HashOf(FusionIds.Tritium)
                || hash == FusionIds.HashOf(FusionIds.LiquidTritium)
                || hash == FusionIds.HashOf(FusionIds.SolidTritium)
                || hash == FusionIds.HashOf(FusionIds.Helium)
                || hash == FusionIds.HashOf(FusionIds.LiquidHelium)
                || hash == FusionIds.HashOf(FusionIds.Lithium)
                || hash == FusionIds.HashOf(FusionIds.LiquidLithium)
                || hash == FusionIds.HashOf(FusionIds.LithiumVapor);
        }

        private static void RefreshElement(string id)
        {
            if (!ElementLoader.elementTable.TryGetValue((int)FusionIds.HashOf(id), out Element element)) return;
            if (!names.TryGetValue(id, out string name)) return;
            element.name = name;
            element.nameUpperCase = name.ToUpperInvariant();
            element.description = descriptions[id];
            TagManager.Create(id, name);
            if (element.substance != null) element.substance.name = name;
        }

        private static void AddElement(string id, string name, string description)
        {
            names[id] = name;
            descriptions[id] = description;
            string key = "STRINGS.ELEMENTS." + id.ToUpperInvariant();
            Strings.Add(key + ".NAME", name);
            Strings.Add(key + ".DESC", description);
        }

        private static void AddBuilding(string id, string name, string description, string effect)
        {
            string key = "STRINGS.BUILDINGS.PREFABS." + id.ToUpperInvariant();
            Strings.Add(key + ".NAME", name);
            Strings.Add(key + ".DESC", description);
            Strings.Add(key + ".EFFECT", effect);
        }
    }
}
