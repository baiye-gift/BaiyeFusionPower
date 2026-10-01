using System;

namespace Baiye.FusionPower
{
    public sealed class FusionNeutronStatus : KMonoBehaviour
    {
        private KSelectable selectable;
        private StatusItem status;
        private Guid handle;
        private Guid powerHandle;
        private FusionReactorProcess reactor;
        private TritiumBreederProcess breeder;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            selectable = GetComponent<KSelectable>();
            reactor = GetComponent<FusionReactorProcess>();
            breeder = GetComponent<TritiumBreederProcess>();
            bool zh = FusionStrings.UseChinese();
            status = new StatusItem("BAIYE_NEUTRON_LINK",
                zh ? "中子通量：{STATE}" : "Neutron flux: {STATE}",
                zh ? "聚变中子只会供给 8 格内的一台氚增殖器；在辐射视图中可查看照射。" : "Fusion neutrons feed one tritium breeder within eight tiles. Radiation is visible in the radiation overlay.",
                "", StatusItem.IconType.Info, NotificationType.Neutral, false,
                OverlayModes.Radiation.ID, showWorldIcon: false);
            status.resolveStringCallback = (value, data) => value.Replace("{STATE}", CurrentState());
            if (selectable != null) handle = selectable.AddStatusItem(status, this);
            if (reactor != null && selectable != null)
            {
                var powerStatus = new StatusItem("BAIYE_FUSION_POWER",
                    zh ? "机内发电：{POWER} kW · 冷却储备：{COLD}/20 kg" : "Internal generation: {POWER} kW · Coolant reserve: {COLD}/20 kg",
                    zh ? "近 5 秒实际平均发电：{POWER} kW；过冷液余热：{HEAT} MDTU/s。\n入口缓冲：{COLD}/20 kg，{COLDTEMP}；出口缓冲：{HOT}/20 kg，{HOTTEMP}。\n额定发电 16 kW，堆内耗电 2 kW；额定剩余 14 kW，尚未扣除燃料制备、泵和冷却耗电。\n发电与耗电共用原有电力接点；点火需要外部电源。电力缓存：{JOULES} kJ；输出无空间时停机。"
                       : "Actual five-second average generation: {POWER} kW; coolant waste heat: {HEAT} MDTU/s.\nInlet buffer: {COLD}/20 kg, {COLDTEMP}; outlet buffer: {HOT}/20 kg, {HOTTEMP}.\nRated generation 16 kW, reactor demand 2 kW, leaving 14 kW before fuel preparation, pumps and cooling.\nGeneration shares the existing power connector; startup requires external power. Electrical buffer: {JOULES} kJ; stops when full.",
                    "", StatusItem.IconType.Info, NotificationType.Neutral, false,
                    OverlayModes.Power.ID, showWorldIcon: false);
                powerStatus.resolveStringCallback = (value, data) => value
                    .Replace("{POWER}", (reactor.AverageGrossWatts / 1000f).ToString("0.00"))
                    .Replace("{HEAT}", (reactor.AverageCoolantHeat / 1000f).ToString("0.000"))
                    .Replace("{COLD}", reactor.CoolantInputKg.ToString("0.0"))
                    .Replace("{HOT}", reactor.CoolantOutputKg.ToString("0.0"))
                    .Replace("{COLDTEMP}", Temperature(reactor.CoolantInputKelvin, zh))
                    .Replace("{HOTTEMP}", Temperature(reactor.CoolantOutputKelvin, zh))
                    .Replace("{JOULES}", (reactor.StoredJoules / 1000f).ToString("0.0"));
                powerHandle = selectable.AddStatusItem(powerStatus, this);
            }
        }

        private static string Temperature(float kelvin, bool zh) => float.IsNaN(kelvin)
            ? (zh ? "空" : "empty") : (kelvin - 273.15f).ToString("0.0") + " °C";

        private string CurrentState()
        {
            bool zh = FusionStrings.UseChinese();
            if (reactor != null)
            {
                if (!GetComponent<Operational>().IsActive)
                {
                    if (!GetComponent<Operational>().IsOperational) return zh ? "断电或禁用" : "unpowered or disabled";
                    switch (reactor.CurrentStopCause)
                    {
                        case FusionReactorProcess.StopCause.Deuterium: return zh ? "停机：缺少氘气" : "offline: deuterium missing";
                        case FusionReactorProcess.StopCause.Tritium: return zh ? "停机：缺少氚气" : "offline: tritium missing";
                        case FusionReactorProcess.StopCause.Coolant: return zh ? "停机：过冷液不足" : "offline: super coolant missing";
                        case FusionReactorProcess.StopCause.CoolantReserve: return zh ? "等待冷却储备：过冷液需达到 20 kg" : "waiting for coolant reserve: 20 kg";
                        case FusionReactorProcess.StopCause.PowerOutput: return zh ? "停机：电力缓存已满或发电组件未就绪" : "offline: electrical buffer full or generator not ready";
                        case FusionReactorProcess.StopCause.Output: return zh ? "停机：产物或冷却液出口堵塞" : "offline: output blocked";
                        case FusionReactorProcess.StopCause.CoolantHot: return zh ? "停机：过冷液过热" : "offline: super coolant too hot";
                        case FusionReactorProcess.StopCause.BodyHot: return zh ? "停机：堆体过热" : "offline: reactor too hot";
                        case FusionReactorProcess.StopCause.Reserve: return zh ? "等待启动储备：氚需达到 2 kg" : "waiting for startup reserve: 2 kg tritium";
                        default: return zh ? "停机" : "offline";
                    }
                }
                return reactor.LinkedBreeder != null
                    ? (zh ? "10 g/s，已连接增殖器" : "10 g/s, breeder linked")
                    : (zh ? "10 g/s，等待增殖器" : "10 g/s, awaiting breeder");
            }
            if (breeder != null)
            {
                if (breeder.LinkedReactor != null && breeder.LinkedReactor.GetComponent<Operational>().IsActive)
                    return breeder.IsBreeding && GetComponent<Operational>().IsActive
                        ? (zh ? "正在增殖，短时积压可追补" : "breeding; catching up earned credit")
                        : (zh ? "已连接，等待中子通量" : "linked, waiting for neutron flux");
                return GetComponent<Operational>().IsActive
                    ? (zh ? "氘—氘启动" : "D-D startup")
                    : (zh ? "未连接，等待氘" : "unlinked, waiting for deuterium");
            }
            return zh ? "未连接" : "unlinked";
        }

        protected override void OnCleanUp()
        {
            if (selectable != null && handle != Guid.Empty) selectable.RemoveStatusItem(handle);
            if (selectable != null && powerHandle != Guid.Empty) selectable.RemoveStatusItem(powerHandle);
            base.OnCleanUp();
        }
    }
}
