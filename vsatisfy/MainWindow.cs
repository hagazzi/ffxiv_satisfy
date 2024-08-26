﻿using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Satisfy;

public unsafe class MainWindow() : Window("Satisfier"), IDisposable
{
    public void Dispose()
    {
    }

    public override void Draw()
    {
        DrawMainTable();
        DrawDebug();
    }

    private void DrawMainTable()
    {
        var inst = SatisfactionSupplyManager.Instance();
        var npcSheet = Service.LuminaSheet<SatisfactionNpc>()!;
        if (inst->Satisfaction.Length + 1 != npcSheet.RowCount)
        {
            ImGui.TextUnformatted($"WARNING: npc count mismatch between CS and lumina");
            return;
        }

        using var table = ImRaii.Table("main_table", 4);
        if (!table)
            return;
        ImGui.TableSetupColumn("NPC", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.TableSetupColumn("Bonus", ImGuiTableColumnFlags.WidthFixed, 20);
        ImGui.TableSetupColumn("Progress", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Achievement");

        var bonusOverrideRow = inst->BonusGuaranteeRowId != 0xFF ? inst->BonusGuaranteeRowId : Calculations.CalculateBonusGuarantee();
        var bonusOverride = bonusOverrideRow >= 0 ? Service.LuminaRow<SatisfactionBonusGuarantee>((uint)bonusOverrideRow) : null;
        for (int i = 0; i < inst->SatisfactionRanks.Length; ++i)
        {
            var npcData = npcSheet.GetRow((uint)(i + 1))!;
            var supplyIndex = (uint)npcData.SupplyIndex[inst->SatisfactionRanks[i]];
            var request = Calculations.CalculateRequestedItems(supplyIndex, inst->SupplySeed)[0];
            var isBonus = Service.LuminaRow<SatisfactionSupply>(supplyIndex, request)?.Unknown7 == true || bonusOverride != null && (bonusOverride.Unknown0 == i + 1 || bonusOverride.Unknown1 == i + 1);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"[{i}] {npcData.Npc.Value!.Singular}");

            ImGui.TableNextColumn();
            ImGui.Checkbox("###bonus", ref isBonus);

            ImGui.TableNextColumn();
            ImGui.ProgressBar((float)inst->UsedAllowances[i] / npcData.DeliveriesPerWeek, default, $"{inst->UsedAllowances[i]} / {npcData.DeliveriesPerWeek}");

            ImGui.TableNextColumn();
        }
    }

    private void DrawDebug()
    {
        if (!ImGui.CollapsingHeader("Debug data"))
            return;

        var inst = SatisfactionSupplyManager.Instance();
        var supplySheet = Service.LuminaSheet<SatisfactionSupply>()!;
        var calcBonus = Calculations.CalculateBonusGuarantee();
        var bonusOverrideRow = inst->BonusGuaranteeRowId != 0xFF ? inst->BonusGuaranteeRowId : calcBonus;
        var bonusOverride = bonusOverrideRow >= 0 ? Service.LuminaRow<SatisfactionBonusGuarantee>((uint)bonusOverrideRow) : null;

        ImGui.TextUnformatted($"Seed: {inst->SupplySeed}, fixed-rng={inst->FixedRandom}");
        ImGui.TextUnformatted($"Guarantee row: {inst->BonusGuaranteeRowId}, adj={inst->TimeAdjustmentForBonusGuarantee}, calculated={calcBonus}");
        for (int i = 0; i < inst->Satisfaction.Length; ++i)
        {
            var rank = inst->SatisfactionRanks[i];
            var supplyIndex = (uint)Service.LuminaRow<SatisfactionNpc>((uint)i + 1)!.SupplyIndex[rank];
            var numSubrows = supplySheet.GetRowParser(supplyIndex)!.RowCount;
            ImGui.TextUnformatted($"#{i}: rank={rank}, supply={supplyIndex} ({numSubrows} subrows), satisfaction={inst->Satisfaction[i]}, usedAllowances={inst->UsedAllowances[i]}");
            var req = Calculations.CalculateRequestedItems(supplyIndex, inst->SupplySeed);
            ImGui.TextUnformatted($"- {req[0]} '{supplySheet.GetRow(supplyIndex, req[0])!.Item.Value?.Name}'{(bonusOverride != null && (bonusOverride.Unknown0 == i + 1 || bonusOverride.Unknown1 == i + 1) ? " *****" : "")}");
            ImGui.TextUnformatted($"- {req[1]} '{supplySheet.GetRow(supplyIndex, req[1])!.Item.Value?.Name}'{(bonusOverride != null && (bonusOverride.Unknown2 == i + 1 || bonusOverride.Unknown3 == i + 1) ? " *****" : "")}");
            ImGui.TextUnformatted($"- {req[2]} '{supplySheet.GetRow(supplyIndex, req[2])!.Item.Value?.Name}'{(bonusOverride != null && (bonusOverride.Unknown4 == i + 1 || bonusOverride.Unknown5 == i + 1) ? " *****" : "")}");
        }
        ImGui.TextUnformatted($"Current NPC: {inst->CurrentNpc}, supply={inst->CurrentSupplyRowId}");
    }
}
