﻿using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using CustAmmoCategories;
using Harmony;
using HBS;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CustomUnits {
  [HarmonyPatch(typeof(LanceLoadoutSlot))]
  [HarmonyPatch("SetData")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(LanceConfiguratorPanel), typeof(SimGameState), typeof(DataManager), typeof(bool), typeof(bool), typeof(float), typeof(float) })]
  public static class LanceLoadoutSlot_SetData {
    public static void Prefix(LanceLoadoutSlot __instance, LanceConfiguratorPanel LC, SimGameState sim, DataManager dataManager, bool useDragAndDrop, ref bool locked, ref float minTonnage, ref float maxTonnage) {
      Log.TWL(0, "LanceLoadoutSlot.SetData " + __instance.GetInstanceID());
      try {
        if (maxTonnage == 0f) { return; }
        if (locked == true) { return; }
        CustomLanceSlot custSlot = __instance.gameObject.GetComponent<CustomLanceSlot>();
        if (custSlot == null) { return; }
        if (custSlot.lanceIndex < 0) { locked = true; return; }
        if (custSlot.lanceDef == null) { locked = true; return; }
        if (custSlot.slotDef == null) { locked = true; return; }
        if (custSlot.slotDef.Disabled) { locked = true; return; }
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }

  [HarmonyPatch(typeof(LanceConfiguratorPanel), "SetData")]

  [HarmonyPatch(typeof(LanceLoadoutSlot))]
  [HarmonyPatch("SetLockedData")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(IMechLabDraggableItem), typeof(IMechLabDraggableItem), typeof(bool) })]
  public static class LanceLoadoutSlot_SetLockedData {
    public static void Prefix(LanceLoadoutSlot __instance, IMechLabDraggableItem forcedMech, IMechLabDraggableItem forcedPilot, bool shouldBeLocked) {
      if (shouldBeLocked) { Thread.CurrentThread.SetFlag("LanceLoadoutSlot.LOCKED"); }
    }
    public static void Postfix(LanceLoadoutSlot __instance, IMechLabDraggableItem forcedMech, IMechLabDraggableItem forcedPilot, bool shouldBeLocked) {
      if (shouldBeLocked) { Thread.CurrentThread.ClearFlag("LanceLoadoutSlot.LOCKED"); }
    }
  }
  [HarmonyPatch(typeof(LanceLoadoutSlot))]
  [HarmonyPatch("OnAddItem")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(IMechLabDraggableItem), typeof(bool) })]
  public static class LanceLoadoutSlot_OnAddItem {
    public static bool Prefix(LanceLoadoutSlot __instance, IMechLabDraggableItem item, bool validate, bool __result, LanceConfiguratorPanel ___LC) {
      Log.TWL(0, "LanceLoadoutSlot.OnAddItem " + __instance.GetInstanceID());
      try {
        if (Thread.CurrentThread.isFlagSet("LanceLoadoutSlot.LOCKED")) { return true; };
        if ((item.ItemType != MechLabDraggableItemType.Mech) && (item.ItemType != MechLabDraggableItemType.Pilot)) { return true; }
        if (item.ItemType == MechLabDraggableItemType.Mech) {
          LanceLoadoutMechItem lanceLoadoutMechItem = item as LanceLoadoutMechItem;
          if (lanceLoadoutMechItem.MechDef.Chassis == null) {
            throw new Exception("lanceLoadoutMechItem.MechDef.Chassis is null");
          }
          CustomLanceSlot customSlot = __instance.gameObject.GetComponent<CustomLanceSlot>();
          if(customSlot != null) {
            if (lanceLoadoutMechItem.MechDef.Chassis.CanBeDropedInto(customSlot.slotDef, out string title, out string message) == false) {
              if (___LC != null) { ___LC.ReturnItem(item); }
              __result = false;
              GenericPopupBuilder.Create(title, message).AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0.0f, true).Render();
              return false;
            }
          }
          if (__instance.SelectedPilot != null) {
            if (lanceLoadoutMechItem.MechDef.Chassis.CanBePilotedBy(__instance.SelectedPilot.Pilot.pilotDef, out string title, out string message) == false) {
              if (___LC != null) { ___LC.ReturnItem(item); }
              __result = false;
              GenericPopupBuilder.Create(title, message).AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0.0f, true).Render();
              return false;
            }
          }
        } else if (item.ItemType == MechLabDraggableItemType.Pilot) {
          SGBarracksRosterSlot barracksRosterSlot = item as SGBarracksRosterSlot;
          if (__instance.SelectedMech != null) {
            if (__instance.SelectedMech.MechDef.Chassis == null) {
              throw new Exception("__instance.SelectedMech.MechDef.Chassis is null");
            }
            if (__instance.SelectedMech.MechDef.Chassis.CanBePilotedBy(barracksRosterSlot.Pilot.pilotDef, out string title, out string message) == false) {
              if (___LC != null) { ___LC.ReturnItem(item); }
              __result = false;
              GenericPopupBuilder.Create(title, message).AddFader(new UIColorRef?(LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.PopupBackfill), 0.0f, true).Render();
              return false;
            }
          }
        }
        return true;
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
        return true;
      }
    }
  }
  [HarmonyPatch(typeof(LanceConfiguratorPanel))]
  [HarmonyPatch("SetData")]
  [HarmonyPatch(MethodType.Normal)]
  public static class LanceConfiguratorPanel_SetData {
    //private static FieldInfo f_loadoutSlots = typeof(LanceConfiguratorPanel).GetField("loadoutSlots", BindingFlags.Instance | BindingFlags.NonPublic);
    //public static LanceLoadoutSlot[] loadoutSlots(this LanceConfiguratorPanel panel) { return (LanceLoadoutSlot[])f_loadoutSlots.GetValue(panel); }
    public static void Prefix(LanceConfiguratorPanel __instance, SimGameState sim, ref int maxUnits, Contract contract,ref LanceLoadoutSlot[] ___loadoutSlots, ref float[] ___slotMaxTonnages,ref float[] ___slotMinTonnages) {
      try {
        Log.TWL(0, "LanceConfiguratorPanel.SetData prefix");
        Transform buttons_tr = __instance.transform.FindRecursive("lanceSwitchButtons-layout");
        ShuffleLanceSlotsLayout customLanceSlotsLayout = ___loadoutSlots[0].transform.parent.gameObject.GetComponent<ShuffleLanceSlotsLayout>();
        if (customLanceSlotsLayout == null) { customLanceSlotsLayout = ___loadoutSlots[0].transform.parent.gameObject.AddComponent<ShuffleLanceSlotsLayout>(); }
        customLanceSlotsLayout.currentLanceIndex = 0;
        customLanceSlotsLayout.LayoutDef = sim.currentLayout();
        Log.WL(1, "current layout:"+ customLanceSlotsLayout.LayoutDef.Description.Id);
        if (buttons_tr == null) {
          Log.WL(1, "lanceSwitchButtons-layout not found");
          buttons_tr = __instance.transform.FindRecursive("lanceSaveButtons-layout");
          if (buttons_tr == null) {
            Log.WL(1, "lanceSaveButtons-layout not found");
            return;
          };
          Transform DeployBttn_layout = __instance.transform.FindRecursive("DeployBttn-layout");
          Transform nbtn = GameObject.Instantiate(buttons_tr.gameObject).transform;
          nbtn.SetParent(buttons_tr.parent);
          nbtn.localPosition = buttons_tr.localPosition;
          if (DeployBttn_layout != null) {
            Vector3 pos = nbtn.localPosition;
            pos.y = DeployBttn_layout.localPosition.y;
            nbtn.localPosition = pos;
          }
          buttons_tr = nbtn;
          buttons_tr.gameObject.name = "lanceSwitchButtons-layout";
          buttons_tr.Find("uixPrfBttn_BASE_button2-MANAGED-delete").gameObject.AddComponent<LanceConfigutationNextLance>().Init(customLanceSlotsLayout);
          buttons_tr.Find("uixPrfBttn_BASE_button2-MANAGED-save").gameObject.SetActive(false);
          buttons_tr.Find("uixPrfBttn_BASE_button2-MANAGED-copy (1)").gameObject.SetActive(false);
        } else {
          buttons_tr.Find("uixPrfBttn_BASE_button2-MANAGED-delete").gameObject.GetComponent<LanceConfigutationNextLance>().Init(customLanceSlotsLayout);
        }
        buttons_tr.gameObject.SetActive(true);
        List<float> listMaxTonnages = ___slotMaxTonnages.ToList();
        List<float> listMinTonnages = ___slotMinTonnages.ToList();
        Log.WL(0, "loadoutSlots:" + ___loadoutSlots.Length + "/" + UnityGameInstance.BattleTechGame.Simulation.currentLayout().slotsCount);
        List<LanceLoadoutSlot> slots = new List<LanceLoadoutSlot>();
        slots.AddRange(___loadoutSlots);
        GameObject lanceSlotSrc = ___loadoutSlots[0].gameObject;
        for (int t = ___loadoutSlots.Length; t < UnityGameInstance.BattleTechGame.Simulation.currentLayout().slotsCount; ++t) {
          GameObject lanceSlotNew = GameObject.Instantiate(lanceSlotSrc);
          lanceSlotNew.transform.SetParent(lanceSlotSrc.transform.parent);
          lanceSlotNew.transform.localPosition = lanceSlotSrc.transform.localPosition;
          lanceSlotNew.transform.localScale = lanceSlotSrc.transform.localScale;
          lanceSlotNew.transform.localRotation = lanceSlotSrc.transform.localRotation;
          lanceSlotNew.name = "lanceSlot" + (t + 1).ToString();
          slots.Add(lanceSlotNew.GetComponent<LanceLoadoutSlot>());
          Log.WL(0, lanceSlotNew.name + " parent:" + lanceSlotNew.transform.parent.name);
          listMaxTonnages.Add(listMaxTonnages[0]);
          listMinTonnages.Add(listMinTonnages[0]);
        }
        for (int t = 0; t < slots.Count; ++t) {
          slots[t].gameObject.SetActive(false);
          GameObject slot = slots[t].gameObject;
          CustomLanceSlot custSlot = slot.GetComponent<CustomLanceSlot>();
          if (custSlot == null) { custSlot = slot.AddComponent<CustomLanceSlot>(); }
          slot.transform.SetSiblingIndex(t);
          custSlot.weight = t;
          custSlot.lanceIndex = -1;
          custSlot.lanceDef = null;
          custSlot.slotDef = null;
        }
        {
          int slot_index = 0;
          for (int lance_index = 0; lance_index < customLanceSlotsLayout.LayoutDef.dropLances.Count; ++lance_index) {
            DropLanceDef lance = customLanceSlotsLayout.LayoutDef.dropLances[lance_index];
            foreach (DropSlotDef dropslot in lance.dropSlots) {
              if (slot_index >= slots.Count) { break; }
              GameObject slot = slots[slot_index].gameObject;
              CustomLanceSlot custSlot = slot.GetComponent<CustomLanceSlot>();
              custSlot.lanceIndex = lance_index;
              custSlot.lanceDef = lance;
              custSlot.slotDef = dropslot;
              custSlot.Awake();
              custSlot.ApplyDecoration(true);
              ++slot_index;
            }
          }
        }
        maxUnits = sim.currentLayout().slotsCount;
        Log.WL(1, "loadoutSlots:" + slots.Count + "/" + maxUnits);
        customLanceSlotsLayout.Refresh();
        OrderLanceSlotLayoutGroup horizontalLayout = slots[0].transform.parent.gameObject.GetComponent<OrderLanceSlotLayoutGroup>();
        if (horizontalLayout == null) {
          HorizontalLayoutGroup old_horizontalLayout = slots[0].transform.parent.gameObject.GetComponent<HorizontalLayoutGroup>();
          RectOffset padding = old_horizontalLayout.padding;
          float spacing = old_horizontalLayout.spacing;
          GameObject.DestroyImmediate(old_horizontalLayout);
          horizontalLayout = slots[0].transform.parent.gameObject.AddComponent<OrderLanceSlotLayoutGroup>();
          if (horizontalLayout == null) {
            throw new Exception("fail to add OrderedHorizontalLayoutGroup");
          } else {
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlHeight = false;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childForceExpandHeight = false;
            horizontalLayout.childForceExpandWidth = true;
            horizontalLayout.padding = padding;
            horizontalLayout.spacing = spacing;
            horizontalLayout.enabled = true;
          }
        }
        ___loadoutSlots = slots.ToArray();
        ___slotMaxTonnages = listMaxTonnages.ToArray();
        ___slotMinTonnages = listMinTonnages.ToArray();
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
    public static void Postfix(LanceConfiguratorPanel __instance,ref LanceLoadoutSlot[] ___loadoutSlots) {
      Log.TWL(0, "LanceConfiguratorPanel.SetData postfix:" + __instance.maxUnits + "/" + ___loadoutSlots.Length);
      ShuffleLanceSlotsLayout customLanceSlotsLayout = ___loadoutSlots[0].transform.parent.gameObject.GetComponent<ShuffleLanceSlotsLayout>();
      customLanceSlotsLayout.Refresh();
      customLanceSlotsLayout.UpdateSlots();
      //updateSlots(customLanceSlotsLayout);
    }
  }
  public class OrderLanceSlotLayoutGroup: HorizontalLayoutGroup {
    protected virtual List<KeyValuePair<RectTransform, CustomLanceSlot>> orderChildren { get; set; } = new List<KeyValuePair<RectTransform, CustomLanceSlot>>();
    public override void CalculateLayoutInputHorizontal() {
      this.rectChildren.Clear();
      this.orderChildren.Clear();
      CustomLanceSlot[] elements = this.gameObject.GetComponentsInChildren<CustomLanceSlot>();
      foreach (CustomLanceSlot el in elements) {
        if (el.transform.parent != this.transform) { continue; }
        RectTransform child = el.transform as RectTransform;
        if (child != null) { this.orderChildren.Add(new KeyValuePair<RectTransform, CustomLanceSlot>(child, el)); }
      }
      orderChildren.Sort((x, y) => { return x.Value.index - y.Value.index; });
      foreach(var el in orderChildren) {
        rectChildren.Add(el.Key);
      }
      this.m_Tracker.Clear();
    }
  }
  public class ShuffleLanceSlotsLayout : MonoBehaviour {
    public OrderLanceSlotLayoutGroup slotsLayout { get; set; }
    public RectTransform rectTransform { get; set; }
    public List<CustomLanceSlot> slots { get; set; } = new List<CustomLanceSlot>();
    public CustomLanceSlot currentSlot { get; set; }
    public DropSlotsDef LayoutDef { get; set; }
    public int currentLanceIndex { get; set; } = 0;
    public void UpdateSlots() {
      Log.TWL(0, "ShuffleLanceSlotsLayout.UpdateSlots:"+ slots.Count);
      foreach(CustomLanceSlot slot in slots) {
        Log.WL(1,"slot:"+slot.lanceIndex+"/"+ this.currentLanceIndex);
        slot.gameObject.SetActive(slot.lanceIndex == this.currentLanceIndex);
      }
    }
    public void Awake() {
      rectTransform = this.gameObject.GetComponent<RectTransform>();
      this.Refresh();
    }
    public void Refresh() {
      this.slotsLayout = this.gameObject.GetComponent<OrderLanceSlotLayoutGroup>();
      CustomLanceSlot[] trs = this.gameObject.GetComponentsInChildren<CustomLanceSlot>(true);
      slots.Clear();
      foreach (CustomLanceSlot tr in trs) {
        if (tr.transform.parent != this.transform) { continue; }
        slots.Add(tr);
      }
      slots.Sort((x, y) => { return x.weight - y.weight; });
      for (int t = 0; t < slots.Count; ++t) {
        slots[t].index = t;
      }
    }
    public void Update() {
      if (slotsLayout == null) { slotsLayout = this.gameObject.GetComponent<OrderLanceSlotLayoutGroup>(); }
      foreach (CustomLanceSlot slot in slots) {
        slot.transform.localScale = slot == this.currentSlot ? Vector3.one : (new Vector3(0.8f, 0.8f, 1.0f));
      }
      if (slotsLayout == null) { return; }
      float view_width = this.rectTransform.sizeDelta.x - this.slotsLayout.padding.left - this.slotsLayout.padding.right;
      float components_width = 0f;
      int count = 0;
      foreach (CustomLanceSlot slot in slots) {
        if (slot.gameObject.activeInHierarchy == false) { continue; }
        count += 1;
        components_width += slot.rectTransform.sizeDelta.x;
      }
      if (count > 1) {
        slotsLayout.spacing = (view_width - components_width) / ((float)(count - 1));
      }
    }
  }
  public class CustomLanceSlot: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public int index { get; set; }
    public int weight { get; set; }
    public ShuffleLanceSlotsLayout shuffleLayout { get; set; }
    public RectTransform rectTransform { get; set; }
    public int lanceIndex { get; set; } = -1;
    public DropLanceDef lanceDef { get; set; } = null;
    private DropSlotDef f_slotDef = null;
    public DropSlotDef slotDef { get { return f_slotDef; } set { f_slotDef = value; } }
    public RectTransform f_decorationLayout { get; set; } = null;
    public RectTransform decorationLayout { get { return f_decorationLayout; } set { f_decorationLayout = value; } }
    public RectTransform firstDecoration { get; set; } = null;
    private bool decorationApplied = false;
    public void Update() {
      if(decorationApplied == false) {
        this.ApplyDecoration(true, false);
      }
      if (firstDecoration == null) { return; }
      Vector3[] decorationCorners = new Vector3[4]; firstDecoration.GetWorldCorners(decorationCorners);
      Vector3[] layoutCorners = new Vector3[4]; rectTransform.GetWorldCorners(layoutCorners);
      if(Mathf.Abs(decorationCorners[1].x - layoutCorners[1].x) > (this.rectTransform.sizeDelta.x / 2f)) {
        HorizontalLayoutGroup group = decorationLayout.gameObject.GetComponent<HorizontalLayoutGroup>();
        Log.TWL(0, "CustomLanceSlot.Update " + this.gameObject.name + " delta:" + Mathf.Abs(decorationCorners[1].x - layoutCorners[1].x) + " border:" + (this.rectTransform.sizeDelta.x / 2f) + " aligin:" + (group==null?"null":group.childAlignment.ToString()));
        if (group != null) {
          group.childAlignment = group.childAlignment == TextAnchor.MiddleLeft? TextAnchor.MiddleRight: TextAnchor.MiddleLeft;
        }
      }
      //if (Mathf.Abs(firstDecoration.localPosition.x) > firstDecoration.sizeDelta.x * 2f) {
      //  HorizontalLayoutGroup group = decorationLayout.gameObject.GetComponent<HorizontalLayoutGroup>();
      //  if (group != null) {
      //    group.childAlignment = group.childAlignment == TextAnchor.MiddleLeft? TextAnchor.MiddleRight: TextAnchor.MiddleLeft;
      //  }
      //}
    }
    public void ApplyDecoration(bool loadDeps, bool async=true) {
      if (async) { decorationApplied = false; return; };
      if (decorationLayout == null) { return; }
      if (slotDef == null) { return; }
      Log.TWL(0, "CustomLanceSlot.ApplyDecoration " + (decorationLayout == null ? "null" : decorationLayout.parent.name + "." + decorationLayout.name + ":" + decorationLayout.GetInstanceID()) + " def:" + (slotDef == null ? "null" : slotDef.Description.Id));
      int childCount = decorationLayout.childCount;
      int decorCount = slotDef.decorations.Count;
      //if (decorCount == 1) { decorCount = 2; }
      Log.WL(1, "slotDef.decorations.Count:"+ childCount+"->"+ decorCount);
      for (int t = childCount; t < decorCount; ++t) {
        GameObject decorationGO = new GameObject("decoration"+t.ToString(), typeof(RectTransform));
        RectTransform tr = decorationGO.GetComponent<RectTransform>();
        tr.SetParent(decorationLayout);
        tr.localScale = Vector3.one;
        tr.localPosition = Vector3.zero;
        tr.localRotation = Quaternion.identity;
        tr.pivot = new Vector2(0.5f,0.5f);
        tr.sizeDelta = new Vector2(decorationLayout.sizeDelta.y, decorationLayout.sizeDelta.y);
        SVGImage svg = decorationGO.AddComponent<SVGImage>();
        HBSTooltip tooltip = decorationGO.AddComponent<HBSTooltip>();
      }
      DataManager.InjectedDependencyLoadRequest dependencyLoad = null;
      if (loadDeps) {
        dependencyLoad = new DataManager.InjectedDependencyLoadRequest(UnityGameInstance.BattleTechGame.DataManager);
      }
      for (int t = 0; t < slotDef.decorations.Count; ++t) {
        GameObject decorationGO = decorationLayout.GetChild(t).gameObject;
        DropSlotDecorationDef decorationDef = slotDef.decorations[t];
        if (decorationDef.DataManager == null) { decorationDef.DataManager = UnityGameInstance.BattleTechGame.DataManager; }
        if (loadDeps) {
          if (decorationDef.DependenciesLoaded(10u)) {
            decorationGO.GetComponent<SVGImage>().vectorGraphics = decorationDef.Icon;
          } else {
            decorationDef.GatherDependencies(UnityGameInstance.BattleTechGame.DataManager, dependencyLoad, 10u);
          }
        } else {
          decorationGO.GetComponent<SVGImage>().vectorGraphics = decorationDef.Icon;
        }
        HBSTooltip tooltip = decorationGO.GetComponent<HBSTooltip>();
        if (tooltip == null) { tooltip = decorationGO.AddComponent<HBSTooltip>(); }
        tooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(decorationDef.description));
        tooltip.enabled = true;
      }
      for (int t = 0; t < decorationLayout.childCount; ++t) {
        decorationLayout.GetChild(t).gameObject.SetActive(t < decorCount);
      }
      if (decorationLayout.childCount > 0) { firstDecoration = decorationLayout.GetChild(0) as RectTransform; }
      //if (slotDef.decorations.Count == 1) {
      //  decorationLayout.GetChild(slotDef.decorations.Count).gameObject.GetComponent<SVGImage>().vectorGraphics = null;
      //  HBSTooltip tooltip = decorationLayout.GetChild(slotDef.decorations.Count).gameObject.GetComponent<HBSTooltip>();
      //  if (tooltip == null) { tooltip = decorationLayout.GetChild(slotDef.decorations.Count).gameObject.AddComponent<HBSTooltip>(); }
      //  tooltip.enabled = false;
      //}
      HorizontalLayoutGroup group = decorationLayout.gameObject.GetComponent<HorizontalLayoutGroup>();
      if (group == null) {
        group = decorationLayout.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.spacing = 8f;
        group.padding = new RectOffset(10, 10, 0, 0);
        group.childAlignment = TextAnchor.MiddleRight;
        group.childControlHeight = false;
        group.childControlWidth = false;
        group.childForceExpandHeight = false;
        group.childForceExpandWidth = false;
      }
      decorationApplied = true;
      if (loadDeps) {
        if (dependencyLoad.DependencyCount() > 0) {
          dependencyLoad.RegisterLoadCompleteCallback(new Action(this.ApplyDecoration));
          UnityGameInstance.BattleTechGame.DataManager.InjectDependencyLoader(dependencyLoad, 10U);
        }
      }
    }
    public void ApplyDecoration() {
      ApplyDecoration(false, false);
    }
    public void Awake() {
      Log.TWL(0, "CustomLanceSlot.Awake " + this.gameObject.name);
      rectTransform = this.gameObject.GetComponent<RectTransform>();
      Transform mainBackground = this.transform.FindRecursive("mainBackground");
      Transform mainBorder = this.transform.FindRecursive("mainBorder");
      this.decorationLayout = this.transform.FindRecursive("decoration") as RectTransform;
      if ((mainBorder != null)&&(mainBackground == null)) {
        mainBackground = GameObject.Instantiate(mainBorder.gameObject).transform;
        mainBackground.gameObject.name = "mainBackground";
        mainBackground.SetParent(mainBorder.parent);
        mainBackground.localPosition = mainBorder.localPosition;
        mainBackground.localRotation = mainBorder.localRotation;
        mainBackground.localScale = mainBorder.localScale;
        mainBackground.SetAsFirstSibling();
        Image img = mainBackground.GetComponent<Image>();
        img.enabled = true;
        img.color = Color.black;
      }
      if((this.decorationLayout == null)&&(mainBorder != null)) {
        RectTransform decorationLocal = GameObject.Instantiate(mainBorder.gameObject).transform as RectTransform;
        decorationLocal.name = "decoration";
        HashSet<Transform> trs = new HashSet<Transform>();
        for (int t = 0; t < decorationLocal.childCount; ++t) { trs.Add(decorationLocal.GetChild(t)); }
        foreach (Transform tr in trs) {
          if (tr.gameObject.GetComponent<HBSTooltip>() == null) {
            GameObject.Destroy(tr.gameObject);
          }
        }
        decorationLocal.SetParent(mainBorder.parent);
        decorationLocal.localPosition = mainBorder.localPosition;
        decorationLocal.localRotation = mainBorder.localRotation;
        decorationLocal.localScale = mainBorder.localScale;
        decorationLocal.pivot = new Vector2(0.5f, -1.0f);
        decorationLocal.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y - (mainBorder as RectTransform).sizeDelta.y);
        decorationLocal.anchoredPosition = new Vector2(0f, 0f - ((mainBorder as RectTransform).sizeDelta.y / 2.0f));
        Image img = decorationLocal.gameObject.GetComponent<Image>();
        img.enabled = true;
        img.color = Color.black;
        HorizontalLayoutGroup group = decorationLocal.gameObject.GetComponent<HorizontalLayoutGroup>();
        if (group == null) {
          group = decorationLocal.gameObject.AddComponent<HorizontalLayoutGroup>();
          group.spacing = 8f;
          group.padding = new RectOffset(10, 10, 0, 0);
          group.childAlignment = TextAnchor.MiddleRight;
          group.childControlHeight = false;
          group.childControlWidth = false;
          group.childForceExpandHeight = false;
          group.childForceExpandWidth = false;
        }
        this.decorationLayout = decorationLocal;
      }
      this.shuffleLayout = this.transform.parent.gameObject.GetComponent<ShuffleLanceSlotsLayout>();
      this.transform.localScale = new Vector3(0.8f, 0.8f, 1.0f);
      //Log.WL(1, "decorationLayout.GetInstanceID:" + (decorationLayout==null?"null":decorationLayout.GetInstanceID().ToString()));
    }
    public void OnPointerEnter(PointerEventData eventData) {
      //Log.TWL(0, "CustomLanceSlot.OnPointerEnter");
      this.transform.SetAsLastSibling();
      this.transform.localScale = Vector3.one;
      this.shuffleLayout.currentSlot = this;
      int siblingIndex = this.transform.GetSiblingIndex();
      for (int t = this.index + 1; t < shuffleLayout.slots.Count; ++t) {
        siblingIndex -= 1;
        shuffleLayout.slots[t].transform.SetSiblingIndex(siblingIndex);
      }
      for (int t = this.index - 1; t >= 0; --t) {
        siblingIndex -= 1;
        shuffleLayout.slots[t].transform.SetSiblingIndex(siblingIndex);
      }
    }
    public void OnPointerExit(PointerEventData eventData) {
      if (this.shuffleLayout.currentSlot == this) { this.shuffleLayout.currentSlot = null; };
      this.transform.localScale = new Vector3(0.8f, 0.8f, 1.0f);
    }
  }
}