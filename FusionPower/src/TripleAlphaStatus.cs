using System;

namespace Baiye.FusionPower
{
    public sealed class TripleAlphaStatus : KMonoBehaviour
    {
        private KSelectable selectable;
        private TripleAlphaProcess process;
        private Guid handle;
        protected override void OnSpawn()
        {
            base.OnSpawn();
            selectable = GetComponent<KSelectable>();
            process = GetComponent<TripleAlphaProcess>();
            bool zh = FusionStrings.UseChinese();
            var status = new StatusItem("BAIYE_TRIPLE_ALPHA",
                zh ? "三α核合成：{STATE} · {POWER} kW" : "Triple-alpha synthesis: {STATE} · {POWER} kW",
                zh ? "3 个氦-4原子核合成碳-12，释放能量与γ辐射；不提供增殖中子。\n额定氦消耗 80 g/s，精炼碳产量约 79.95 g/s；发电 4.4 kW，自耗 1.2 kW。\n近 5 秒平均发电 {POWER} kW，实际注入冷却液 {HEAT} MDTU/s；剩余热预算包含碳产物显热。\n氦库存：{HELIUM}/10 kg；碳库存：{CARBON}/70 kg（每满 20 kg 送入轨道）。\n入口冷却储备：{COLD}/20 kg，{COLDTEMP}；出口缓冲：{HOT}/20 kg，{HOTTEMP}。\n电力缓存：{JOULES}/8.8 kJ；累计质量亏损：{DEFECT} g。\n点火需要外部电力、20 kg 过冷液及已连接的运输轨道。恒星反应的小型人工约束和能量数值为未来技术与游戏缩放。"
                   : "Three helium-4 nuclei form carbon-12, releasing energy and gamma radiation; no breeding neutrons.\nRated helium use 80 g/s, refined carbon about 79.95 g/s; generation 4.4 kW, demand 1.2 kW.\nFive-second averages: {POWER} kW generation, {HEAT} MDTU/s actual coolant heat. Residual heat includes product sensible heat.\nHelium: {HELIUM}/10 kg; carbon: {CARBON}/70 kg (20 kg conveyor batches).\nInlet coolant: {COLD}/20 kg, {COLDTEMP}; outlet: {HOT}/20 kg, {HOTTEMP}.\nElectrical buffer: {JOULES}/8.8 kJ; cumulative mass defect: {DEFECT} g.\nStartup requires external power, 20 kg super coolant and a connected conveyor rail. Compact artificial stellar confinement is speculative; energy values are game scaled.",
                "", StatusItem.IconType.Info, NotificationType.Neutral, false,
                OverlayModes.Radiation.ID, showWorldIcon: false);
            status.resolveStringCallback = (value, data) => value
                .Replace("{STATE}", State(zh))
                .Replace("{POWER}", (process.AverageGrossWatts / 1000f).ToString("0.00"))
                .Replace("{HEAT}", (process.AverageCoolantHeat / 1000f).ToString("0.000"))
                .Replace("{HELIUM}", process.HeliumKg.ToString("0.00"))
                .Replace("{CARBON}", process.CarbonKg.ToString("0.00"))
                .Replace("{COLD}", process.CoolantInputKg.ToString("0.0"))
                .Replace("{HOT}", process.CoolantOutputKg.ToString("0.0"))
                .Replace("{COLDTEMP}", Temperature(process.CoolantInputKelvin, zh))
                .Replace("{HOTTEMP}", Temperature(process.CoolantOutputKelvin, zh))
                .Replace("{JOULES}", (process.StoredJoules / 1000f).ToString("0.0"))
                .Replace("{DEFECT}", (process.TotalMassDefectKg * 1000f).ToString("0.000"));
            if (selectable != null) handle = selectable.AddStatusItem(status, this);
        }

        private string State(bool zh)
        {
            Operational op = GetComponent<Operational>();
            if (!op.IsOperational) return zh ? "断电、禁用或运输轨道未连接" : "unpowered, disabled or rail disconnected";
            if (op.IsActive) return zh ? "运行" : "running";
            switch (process.CurrentStopCause)
            {
                case TripleAlphaProcess.StopCause.Helium: return zh ? "等待氦气" : "waiting for helium";
                case TripleAlphaProcess.StopCause.Coolant: return zh ? "过冷液不足" : "coolant missing";
                case TripleAlphaProcess.StopCause.CoolantReserve: return zh ? "等待 20 kg 冷却储备" : "waiting for 20 kg coolant reserve";
                case TripleAlphaProcess.StopCause.CarbonOutput: return zh ? "碳产物缓冲已满" : "carbon buffer full";
                case TripleAlphaProcess.StopCause.CoolantOutput: return zh ? "冷却液出口缓冲已满" : "coolant outlet buffer full";
                case TripleAlphaProcess.StopCause.PowerOutput: return zh ? "电力缓存满或发电组件未就绪" : "electrical buffer full or generator not ready";
                case TripleAlphaProcess.StopCause.BodyHot: return zh ? "堆体过热" : "body too hot";
                case TripleAlphaProcess.StopCause.ThermalRange: return zh ? "出液温度超出安全范围" : "coolant temperature outside safe range";
                default: return zh ? "待机" : "idle";
            }
        }
        private static string Temperature(float t, bool zh) => float.IsNaN(t) ? (zh ? "空" : "empty") : (t - 273.15f).ToString("0.0") + " °C";
        protected override void OnCleanUp()
        {
            if (selectable != null && handle != Guid.Empty) selectable.RemoveStatusItem(handle);
            base.OnCleanUp();
        }
    }
}
