﻿using BepInEx;
using HarmonyLib;
using KSP.UI.Binding;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI;
using SpaceWarp.API.UI.Appbar;
using UnityEngine;
using MuMech;
using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim.Maneuver;
using KSP.Sim;
using KSP.Map;
using BepInEx.Logging;
using System.Collections;

namespace FlightPlan;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class FlightPlanPlugin : BaseSpaceWarpPlugin
{
    public static FlightPlanPlugin Instance { get; set; }

    // These are useful in case some other mod wants to add a dependency to this one
    public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    public const string ModName = MyPluginInfo.PLUGIN_NAME;
    public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    // Control click through to the game
    private bool gameInputState = true;
    // public readonly string vesselNameInput = "VesselRenamer.Input";
    public List<String> inputFields = new List<String>();

    static bool loaded = false;
    private bool interfaceEnabled = false;
    // private bool _isWindowOpen;
    private Rect _windowRect;
    private int windowWidth = Screen.width / 6; //384px on 1920x1080
    private int windowHeight = Screen.height / 3; //360px on 1920x1080
    private Rect closeBtnRect;

    // Button bools
    private bool circAp, circPe, circNow, newPe, newAp, newPeAp, newInc, matchPlanesA, matchPlanesD, hohmannT, interceptAtTime, courseCorrection, moonReturn, matchVCA, matchVNow, planetaryXfer;

    // Target Selection
    //private enum TargetOptions
    //{
    //    Kerbin,
    //    Mun,
    //    Minmus,
    //    Duna
    //}

    // Body selection.
    private string selectedBody = "Kerbin";
    private List<string> bodies;
    private bool selectingBody = false;
    private static Vector2 scrollPositionBodies;

    //private TargetOptions selectedTargetOption = TargetOptions.Mun;
    //private readonly List<string> targetOptions = new List<string> { "Kerbin", "Mun", "Minmus", "Duna" };
    //private bool selectingTargetOption = false;
    //private static Vector2 scrollPositionTargetOptions;
    //private bool applyTargetOption;

    // mod-wide data
    private VesselComponent activeVessel;
    private SimulationObjectModel currentTarget;
    private ManeuverNodeData currentNode = null;
    List<ManeuverNodeData> activeNodes;
    private Vector3d burnParams;

    // Text Inputs
    private string targetPeAStr  =  "20000"; // m
    private string targetApAStr  = "250000"; // m
    private string targetPeAStr1 =  "20000"; // m
    private string targetApAStr1 = "250000"; // m
    private string targetIncStr  =      "0"; // degrees
    private string interceptTStr =    "100"; // s

    private double targetPeR  =  20000; // m
    private double targetApR  = 250000; // m
    private double targetPeR1 =  20000; // m
    private double targetApR1 = 250000; // m
    private double targetInc  = 0;      // degrees
    private double interceptT = 100;    // s

    // GUI layout and style stuff
    private GUIStyle errorStyle, warnStyle, progradeStyle, normalStyle, radialStyle, labelStyle;
    private GameInstance game;
    private GUIStyle horizontalDivider = new GUIStyle();
    private GUISkin _spaceWarpUISkin;
    private GUIStyle ctrlBtnStyle;
    private GUIStyle bigBtnStyle;
    private GUIStyle smallBtnStyle;
    private GUIStyle mainWindowStyle;
    private GUIStyle textInputStyle;
    private GUIStyle sectionToggleStyle;
    private GUIStyle closeBtnStyle;
    private GUIStyle nameLabelStyle;
    private GUIStyle valueLabelStyle;
    private GUIStyle unitLabelStyle;
    private static GUIStyle boxStyle;
    private string unitColorHex;
    private int spacingAfterHeader = -12;
    private int spacingAfterEntry = -5;
    private int spacingAfterSection = 5;

    // App bar button(s)
    private const string ToolbarFlightButtonID = "BTN-FlightPlanFlight";
    // private const string ToolbarOABButtonID = "BTN-FlightPlanOAB";

    // Hot Key config setting
    private string hotKey;

    //public ManualLogSource logger;
    public new static ManualLogSource Logger { get; set; }

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();
        game = GameManager.Instance.Game;
        Logger = base.Logger;

        // Setup the list of input field names (most are the same as the entry string text displayed in the GUI window)
        inputFields.Add("New Pe");
        inputFields.Add("New Ap");
        inputFields.Add("New Pe & Ap");
        inputFields.Add("New Ap & Pe"); // kludgy name for the second input in a two input line
        inputFields.Add("New Inclination");
        inputFields.Add("Intercept at Time");
        inputFields.Add("Select Target");

        // logger = BepInEx.Logging.Logger.CreateLogSource("FlightPlan");

        // logger = Logger;
        Logger.LogInfo("Loaded");
        if (loaded)
        {
            Destroy(this);
        }
        loaded = true;

        gameObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(gameObject);

        // Instance = this;

        _spaceWarpUISkin = Skins.ConsoleSkin;

        boxStyle = new GUIStyle(_spaceWarpUISkin.box); // GUI.skin.GetStyle("Box");

        mainWindowStyle = new GUIStyle(_spaceWarpUISkin.window)
        {
            padding = new RectOffset(8, 8, 20, 8),
            contentOffset = new Vector2(0, -22),
            fixedWidth = windowWidth
        };

        textInputStyle = new GUIStyle(_spaceWarpUISkin.textField)
        {
            alignment = TextAnchor.LowerCenter,
            padding = new RectOffset(10, 10, 0, 0),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 20,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        ctrlBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 16,
            fixedWidth = 16, // windowWidth / 2,
            fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        bigBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 25, // 16,
            fixedWidth = (int)(windowWidth / 2),
            fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        smallBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(10, 10, 0, 3),
            contentOffset = new Vector2(0, 2),
            fixedHeight = 25, // 16,
            // fixedWidth = 95,
            fontSize = 16,
            clipping = TextClipping.Overflow,
            margin = new RectOffset(0, 0, 10, 0)
        };

        sectionToggleStyle = new GUIStyle(_spaceWarpUISkin.toggle)
        {
            padding = new RectOffset(14, 0, 3, 3)
        };

        nameLabelStyle = new GUIStyle(_spaceWarpUISkin.label);
        nameLabelStyle.normal.textColor = new Color(.7f, .75f, .75f, 1);

        valueLabelStyle = new GUIStyle(_spaceWarpUISkin.label)
        {
            alignment = TextAnchor.MiddleRight
        };
        valueLabelStyle.normal.textColor = new Color(.6f, .7f, 1, 1);

        unitLabelStyle = new GUIStyle(valueLabelStyle)
        {
            fixedWidth = 24,
            alignment = TextAnchor.MiddleLeft
        };
        unitLabelStyle.normal.textColor = new Color(.7f, .75f, .75f, 1);
        unitColorHex = ColorUtility.ToHtmlStringRGBA(unitLabelStyle.normal.textColor);

        closeBtnStyle = new GUIStyle(_spaceWarpUISkin.button)
        {
            fontSize = 12
        };

        closeBtnRect = new Rect(windowWidth - 23, 6, 16, 16);

        // Register Flight AppBar button
        Appbar.RegisterAppButton(
            "Flight Plan",
            "BTN-FlightPlan",
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
            ToggleButton);

        // Register OAB AppBar Button
        //Appbar.RegisterOABAppButton(
        //    "BTN-FlightPlan",
        //    ToolbarOABButtonID,
        //    AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
        //    isOpen =>
        //    {
        //        _isWindowOpen = isOpen;
        //        GameObject.Find(ToolbarOABButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(isOpen);
        //    }
        //);

        // Register all Harmony patches in the project
        Harmony.CreateAndPatchAll(typeof(FlightPlanPlugin).Assembly);

        // Try to get the currently active vessel, set its throttle to 100% and toggle on the landing gear
        //try
        //{
        //    var currentVessel = Vehicle.ActiveVesselVehicle;
        //    if (currentVessel != null)
        //    {
        //        currentVessel.SetMainThrottle(1.0f);
        //        currentVessel.SetGearState(true);
        //    }
        //}
        //catch (Exception e) {}

        // Fetch a configuration value or create a default one if it does not exist
        // var defaultValue = "LeftAlt P";
        // hotKey = Config.Bind<string>("Settings section", "Hot Key", defaultValue, "Keyboard shortcut key to launch mod").Value;

        // Log the config value into <KSP2 Root>/BepInEx/LogOutput.log
        // Logger.LogInfo($"Hot Key: {hotKey}");

        // activeVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        // targetPeR = double.Parse(targetPeAStr) + activeVessel.Orbit.referenceBody.radius;
        // targetApR = double.Parse(targetApAStr) + activeVessel.Orbit.referenceBody.radius;
    }

    private void ToggleButton(bool toggle)
    {
        interfaceEnabled = toggle;
        GameObject.Find("BTN-FlightPlan")?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(toggle);
    }

    void Awake()
    {
        _windowRect = new Rect((Screen.width * 0.7f) - (windowWidth / 2), (Screen.height / 2) - (windowHeight / 2), 0, 0);
        // Logger = base.Logger;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            ToggleButton(!interfaceEnabled);
            Logger.LogInfo("UI toggled with hotkey");
        }
    }

    /// <summary>
    /// Draws a simple UI window when <code>this._isWindowOpen</code> is set to <code>true</code>.
    /// </summary>
    private void OnGUI()
    {
        // activeVessel = Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        activeVessel = GameManager.Instance?.Game?.ViewController?.GetActiveVehicle(true)?.GetSimVessel(true);
        currentTarget = activeVessel?.TargetObject;

        // Set the UI
        if (interfaceEnabled)
        {
            GUI.skin = Skins.ConsoleSkin;

            _windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                _windowRect,
                FillWindow,
                "<color=#696DFF>// FLIGHT PLAN</color>",
                GUILayout.Height(windowHeight),
                GUILayout.Width(windowWidth)
            );

            if (gameInputState && inputFields.Contains(GUI.GetNameOfFocusedControl()))
            {
                // Logger.LogInfo($"[Flight Plan]: Disabling Game Input: Focused Item '{GUI.GetNameOfFocusedControl()}'");
                gameInputState = false;
                // game.Input.Flight.Disable();
                GameManager.Instance.Game.Input.Disable();
            }
            else if (!gameInputState && !inputFields.Contains(GUI.GetNameOfFocusedControl()))
            {
                // Logger.LogInfo($"[Flight Plan]: Enabling Game Input: FYI, Focused Item '{GUI.GetNameOfFocusedControl()}'");
                gameInputState = true;
                // game.Input.Flight.Enable();
                GameManager.Instance.Game.Input.Enable();
            }
            if (selectingBody)
            {
                // Do something here to disable mouse wheel control of zoom in and out.
                // Intent: allow player to scroll in the scroll view without causing the game to zoom in and out
                GameManager.Instance._game.MouseManager.enabled = false;
            }
            else
            {
                // Do something here to re-enable mouse wheel control of zoom in and out.
                GameManager.Instance._game.MouseManager.enabled = true;
            }
        }
        else
        {
            if (!gameInputState)
            {
                // Logger.LogInfo($"[Flight Plan]: Enabling Game Input due to GUI disabled: FYI, Focused Item '{GUI.GetNameOfFocusedControl()}'");
                gameInputState = true;
                // game.Input.Flight.Enable();
                GameManager.Instance.Game.Input.Enable();
            }
        }
    }

    /// <summary>
    /// Defines the content of the UI window drawn in the <code>OnGui</code> method.
    /// </summary>
    /// <param name="windowID"></param>
    private void FillWindow(int windowID)
    {
        if (CloseButton())
        {
            CloseWindow();
        }

        labelStyle = warnStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        errorStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        errorStyle.normal.textColor = Color.red;
        warnStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        warnStyle.normal.textColor = Color.yellow;
        progradeStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        progradeStyle.normal.textColor = Color.yellow;
        normalStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        normalStyle.normal.textColor = Color.magenta;
        radialStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        radialStyle.normal.textColor = Color.cyan;
        horizontalDivider.fixedHeight = 2;
        horizontalDivider.margin = new RectOffset(0, 0, 4, 4);
        // game = GameManager.Instance.Game;
        activeNodes = game.SpaceSimulation.Maneuvers.GetNodesForVessel(GameManager.Instance.Game.ViewController.GetActiveVehicle(true).Guid);
        currentNode = (activeNodes.Count() > 0) ? activeNodes[0] : null;

        string tgtName;
        if (currentTarget == null)
            tgtName = "None";
        else
            tgtName = currentTarget.Name;
        DrawSectionHeader("Target", tgtName);

        //GUILayout.BeginHorizontal();
        //GUILayout.Label("New Target:");
        //GUILayout.FlexibleSpace();
        //if (!selectingTargetOption)
        //{
        //    if (GUILayout.Button(Enum.GetName(typeof(TargetOptions), selectedTargetOption)))
        //        selectingTargetOption = true;
        //}
        //else
        //{
        //    GUILayout.BeginVertical(GUI.skin.GetStyle("Box"), GUILayout.Width(windowWidth / 4));
        //    scrollPositionTargetOptions = GUILayout.BeginScrollView(scrollPositionTargetOptions, false, true, GUILayout.Height(70));

        //    foreach (string snapOption in Enum.GetNames(typeof(TargetOptions)).ToList())
        //    {
        //        if (GUILayout.Button(snapOption))
        //        {
        //            Enum.TryParse(snapOption, out selectedTargetOption);
        //            selectingTargetOption = false;
        //        }
        //    }

        //    GUILayout.EndScrollView();
        //    GUILayout.EndVertical();
        //}
        BodySelectionGUI();

        //applyTargetOption = GUILayout.Button("Select", GUILayout.Width(windowWidth / 5));
        //GUILayout.EndHorizontal();

        DrawSectionHeader("Ownship Maneuvers");
        DrawButton("Circularize at Ap", ref circAp);
        DrawButton("Circularize at Pe", ref circPe);
        DrawButton("Circularize Now", ref circNow);

        DrawButtonWithTextField("New Pe", ref newPe, ref targetPeAStr, "m");
        try { targetPeR = double.Parse(targetPeAStr) + activeVessel.Orbit.referenceBody.radius; }
        catch { targetPeR = 0; }

        DrawButtonWithTextField("New Ap", ref newAp, ref targetApAStr, "m");
        try { targetApR = double.Parse(targetApAStr) + activeVessel.Orbit.referenceBody.radius; }
        catch { targetApR = 0; }

        DrawButtonWithDualTextField("New Pe & Ap", "New Ap & Pe", ref newPeAp, ref targetPeAStr1, ref targetApAStr1);
        try { targetPeR1 = double.Parse(targetPeAStr1) + activeVessel.Orbit.referenceBody.radius; }
        catch { targetPeR1 = 0; };
        try { targetApR1 = double.Parse(targetApAStr1) + activeVessel.Orbit.referenceBody.radius; }
        catch { targetApR1 = 0; }

        DrawButtonWithTextField("New Inclination", ref newInc, ref targetIncStr, "°");
        try { targetInc = double.Parse(targetIncStr); }
        catch { targetInc = 0; }

        if (currentTarget != null)
        {
            DrawSectionHeader("Maneuvers Relative to Target");
            DrawButton("Match Planes at AN", ref matchPlanesA);
            DrawButton("Match Planes at DN", ref matchPlanesD);
            DrawButton("Hohmann Xfer", ref hohmannT);

            DrawButtonWithTextField("Intercept at Time", ref interceptAtTime, ref interceptTStr, "s");
            try { interceptT = double.Parse(interceptTStr); }
            catch { interceptT = 0; }

            DrawButton("Course Correction", ref courseCorrection);
            DrawButton("Match Velocity @CA", ref matchVCA);
            DrawButton("Match Velocity Now", ref matchVNow);

            if (currentTarget.Orbit.referenceBody.GlobalId == activeVessel.Orbit.referenceBody.Orbit.referenceBody.GlobalId)
            {
                DrawSectionHeader("Interplanetary Maneuvers");
                DrawButton("Interplanetary Transfer", ref planetaryXfer);
            }
        }
        var referenceBody = activeVessel.Orbit.referenceBody;
        if (referenceBody.referenceBody != null)
        {
            DrawSectionHeader("Moon Specific Maneuvers");
            DrawButton("Moon Return", ref moonReturn);
        }

        // Indication to User that its safe to type, or why vessel controls aren't working
        GUILayout.BeginHorizontal();
        string inputStateString = gameInputState ? "Enabled" : "Disabled";
        GUILayout.Label($"Game Input: {inputStateString}");
        GUILayout.EndHorizontal();

        handleButtons();

        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    private bool CloseButton()
    {
        return GUI.Button(closeBtnRect, "x", closeBtnStyle);
    }

    private void CloseWindow()
    {
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(false);
        interfaceEnabled = false;
        Logger.LogInfo("[Flight Plan]: Restoring game.Input.Flight on window close.");
        // game.Input.Flight.Enable();
        GameManager.Instance.Game.Input.Enable();
        ToggleButton(interfaceEnabled);
    }

    void BodySelectionGUI()
    {
        bodies = GameManager.Instance.Game.SpaceSimulation.GetBodyNameKeys().ToList();
        string baseName = "Select Target";
        GUILayout.BeginHorizontal();
        GUILayout.Label("Target Celestial Body: ", GUILayout.Width(windowWidth / 2));
        if (!selectingBody)
        {
            GUI.SetNextControlName(baseName);
            if (GUILayout.Button(selectedBody))
                selectingBody = true;
        }
        else
        {
            GUILayout.BeginVertical(boxStyle);
            GUI.SetNextControlName("Select Target");
            scrollPositionBodies = GUILayout.BeginScrollView(scrollPositionBodies, false, true, GUILayout.Height(150));
            int index = 0;
            foreach (string body in bodies)
            {
                var thisName = baseName + index.ToString("d2");
                if (!inputFields.Contains(thisName)) inputFields.Add(thisName);
                GUI.SetNextControlName(thisName);
                if (GUILayout.Button(body))
                {
                    selectedBody = body;
                    selectingBody = false;
                }
                index++;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSectionHeader(string sectionName, string value = "") // was (string sectionName, ref bool isPopout, string value = "")
    {
        GUILayout.BeginHorizontal();
        // Don't need popout buttons for ROC
        // isPopout = isPopout ? !CloseButton() : GUILayout.Button("⇖", popoutBtnStyle);

        GUILayout.Label($"<b>{sectionName}</b>");
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, valueLabelStyle);
        GUILayout.Space(5);
        // GUILayout.Label("", unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterHeader);
    }

    private void DrawEntryButton(string entryName, ref bool button, string buttonStr, string value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(entryName, nameLabelStyle);
        GUILayout.FlexibleSpace();
        button = GUILayout.Button(buttonStr, ctrlBtnStyle);
        GUILayout.Label(value, valueLabelStyle);
        GUILayout.Space(5);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntry2Button(string entryName, ref bool button1, string button1Str, ref bool button2, string button2Str, string value, string unit = "")
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(entryName, nameLabelStyle);
        GUILayout.FlexibleSpace();
        button1 = GUILayout.Button(button1Str, ctrlBtnStyle);
        button2 = GUILayout.Button(button2Str, ctrlBtnStyle);
        GUILayout.Label(value, valueLabelStyle);
        GUILayout.Space(5);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawEntryTextField(string entryName, ref string textEntry, string unit = "")
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(entryName, nameLabelStyle);
        GUILayout.FlexibleSpace();
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, textInputStyle);
        GUILayout.Space(5);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawButton(string buttonStr, ref bool button)
    {
        GUILayout.BeginHorizontal();
        button = GUILayout.Button(buttonStr, bigBtnStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawButtonWithTextField(string entryName, ref bool button, ref string textEntry, string unit = "")
    {
        GUILayout.BeginHorizontal();
        button = GUILayout.Button(entryName, smallBtnStyle);
        GUILayout.Space(10);
        GUI.SetNextControlName(entryName);
        textEntry = GUILayout.TextField(textEntry, textInputStyle);
        GUILayout.Space(3);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void DrawButtonWithDualTextField(string entryName1, string entryName2, ref bool button, ref string textEntry1, ref string textEntry2, string unit = "")
    {
        GUILayout.BeginHorizontal();
        button = GUILayout.Button(entryName1, smallBtnStyle);
        GUILayout.Space(5);
        GUI.SetNextControlName(entryName1);
        textEntry1 = GUILayout.TextField(textEntry1, textInputStyle);
        GUILayout.Space(5);
        GUI.SetNextControlName(entryName2);
        textEntry2 = GUILayout.TextField(textEntry2, textInputStyle);
        GUILayout.Space(3);
        GUILayout.Label(unit, unitLabelStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(spacingAfterEntry);
    }

    private void handleButtons()
    {
        if (circAp || circPe || circNow|| newPe || newAp || newPeAp || newInc || matchPlanesA || matchPlanesD || hohmannT || interceptAtTime || courseCorrection || moonReturn || matchVCA || matchVNow || planetaryXfer)
        {
            Vector3d burnParams;
            double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;

            if (circAp) // Working
            {
                Logger.LogInfo("Circularize at Ap");
                var TimeToAp = activeVessel.Orbit.TimeToAp;
                var burnUT = UT + TimeToAp;
                var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(activeVessel.Orbit, burnUT);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (circPe) // Working
            {
                Logger.LogInfo("Circularize at Pe");
                var TimeToPe = activeVessel.Orbit.TimeToPe;
                var burnUT = UT + TimeToPe;
                var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(activeVessel.Orbit, burnUT);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (circNow) // Working
            {
                Logger.LogInfo("Circularize Now");
                var burnUT = UT + 30;
                var deltaV = OrbitalManeuverCalculator.DeltaVToCircularize(activeVessel.Orbit, burnUT);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (newPe) // Seems to work, but this is a kludge! Does not show Pe on new orbit if there is/was a target selected!
            {
                Logger.LogInfo("Set New Pe");
                Debug.Log("Set New Pe");
                var TimeToAp = activeVessel.Orbit.TimeToAp;
                var burnUT = UT + TimeToAp;
                Logger.LogInfo($"Seeking Solution: targetPeR {targetPeR} m, currentPeR {activeVessel.Orbit.Periapsis} m, body.radius {activeVessel.Orbit.referenceBody.radius} m");
                Debug.Log($"Seeking Solution: targetPeR {targetPeR} m, currentPeR {activeVessel.Orbit.Periapsis} m, body.radius {activeVessel.Orbit.referenceBody.radius} m");
                // var deltaV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(activeVessel.Orbit, burnUT, targetPeR);
                var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(activeVessel.Orbit, burnUT, targetPeR, activeVessel.Orbit.Apoapsis);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                burnParams.z *= -1; // Why do we need this?
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (newAp) // Seems to work but this is a kludge! Does not show Ap on new orbit if there is/was a target selected!
            {
                Logger.LogInfo("Set New Ap");
                Debug.Log("Set New Ap");
                var TimeToPe = activeVessel.Orbit.TimeToPe;
                var burnUT = UT + TimeToPe;
                Logger.LogInfo($"Seeking Solution: targetApR {targetApR} m, currentApR {activeVessel.Orbit.Apoapsis} m");
                Debug.Log($"Seeking Solution: targetApR {targetApR} m, currentApR {activeVessel.Orbit.Apoapsis} m");
                // var deltaV = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(activeVessel.Orbit, burnUT, targetApR);
                var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(activeVessel.Orbit, burnUT, activeVessel.Orbit.Periapsis, targetApR);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                burnParams.z *= -1; // Why do we need this?
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (newPeAp) // Sorta almost works, but this is a kludge!
            {
                Logger.LogInfo("Set New Pe and Ap");
                Logger.LogInfo($"Seeking Solution: targetPeR {targetPeR1} m, targetApR {targetApR1} m, body.radius {activeVessel.Orbit.referenceBody.radius} m");
                var burnUT = UT + 30;
                var deltaV = OrbitalManeuverCalculator.DeltaVToEllipticize(activeVessel.Orbit, burnUT, targetPeR1, targetApR1);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
                burnParams.z *= -1; // Why do we need this?
                burnParams.y = 0; // Why do we need this?
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (newInc) // Not Working at all
            {
                Logger.LogInfo("Set New Inclination");
                var TAN = activeVessel.Orbit.TimeOfAscendingNodeEquatorial(UT);
                var TDN = activeVessel.Orbit.TimeOfDescendingNodeEquatorial(UT);
                double burnUT;
                Vector3d deltaVTest, deltaV;
                Logger.LogInfo($"Seeking Solution: targetInc {targetInc}°");
                var deltaV1 = OrbitalManeuverCalculator.DeltaVToChangeInclination(activeVessel.Orbit, TAN, targetInc);
                var deltaV2 = OrbitalManeuverCalculator.DeltaVToChangeInclination(activeVessel.Orbit, TDN, targetInc);
                Logger.LogInfo($"deltaV1 (AN) [{deltaV1.x}, {deltaV1.y}, {deltaV1.z}] = {deltaV1.magnitude} m/s {TAN - UT} s from UT");
                Logger.LogInfo($"deltaV2 (DN) [{deltaV2.x}, {deltaV2.y}, {deltaV2.z}] = {deltaV2.magnitude} m/s {TDN - UT} s from UT");
                if (deltaV1.magnitude < deltaV2.magnitude)
                {
                    Logger.LogInfo($"Selecting maneuver at Ascending Node");
                    burnUT = TAN;
                    deltaV = deltaV1;
                    burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV1, burnUT);
                }
                else
                {
                    Logger.LogInfo($"Selecting maneuver at Descending Node");
                    burnUT = TDN;
                    deltaV = deltaV2;
                    burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV2, burnUT);
                }
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] = {burnParams.magnitude} m/s {burnUT - UT} s from UT");
                deltaVTest = OrbitalManeuverCalculator.BurnVecToDv(activeVessel.Orbit, burnParams, burnUT);
                Logger.LogInfo($"BurnVecToDv Test: deltaV - deltaVTest [{deltaV.x - deltaVTest.x}, {deltaV.y - deltaVTest.y}, {deltaV.z - deltaVTest.z}] m/s");
                CreateManeuverNodeAtUT(burnParams, TAN);
            }
            else if (matchPlanesA) // Seems OK
            {
                Logger.LogInfo("Match Planes at AN");
                double burnUT;
                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(activeVessel.Orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s {burnUT - UT} s from UT");
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (matchPlanesD) // Seems OK
            {
                Logger.LogInfo("Match Planes at DN");
                double burnUT;
                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(activeVessel.Orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                Logger.LogInfo($"Solution Found: deltaV     [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s {burnUT - UT} s from UT");
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (hohmannT) // Untested
            {
                Logger.LogInfo("Hohmann Transfer");
                Debug.Log("Hohmann Transfer");
                double burnUT;
                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(activeVessel.Orbit, currentTarget.Orbit as PatchedConicsOrbit, UT, out burnUT);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (interceptAtTime) // Untested - adapted from call found in MechJebModuleScriptActionRendezvous.cs for "Get Closer"
                // Similar to code in MechJebModuleRendezvousGuidance.cs for "Get Closer" button code.
            {
                Logger.LogInfo("Intercept at Time");
                var burnUT = UT + 30;
                var interceptUT = UT + interceptT;
                double offsetDistance;
                Logger.LogInfo($"Seeking Solution: interceptT {interceptT} s");
                if (currentTarget.GetType() == typeof(CelestialBodyComponent)) // For a target that is a celestial
                    offsetDistance = currentTarget.Orbit.referenceBody.radius + 50000;
                else
                    offsetDistance = 100;
                var deltaV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(activeVessel.Orbit, burnUT, currentTarget.Orbit as PatchedConicsOrbit, interceptUT, offsetDistance);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {interceptUT - UT} s from UT");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (courseCorrection) // Untested
            {
                Logger.LogInfo("Course Correction");
                double burnUT;
                Vector3d deltaV;
                var tgtType = currentTarget.GetType();
                Logger.LogInfo($"currentTarget.GetType() = {tgtType}");
                if (tgtType == typeof(CelestialBodyComponent)) // For a target that is a celestial
                {
                    Logger.LogInfo($"Seeking Solution for Celestial Target");
                    double finalPeR = currentTarget.Orbit.referenceBody.radius + 50000; // m (PeR at celestial target)                           
                    deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(activeVessel.Orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, currentTarget.Orbit.referenceBody, finalPeR, out burnUT);
                }
                else // For a tartget that is not a celestial
                {
                    Logger.LogInfo($"Seeking Solution for Non-Celestial Target");
                    double caDistance = 100; // m (closest approach to non-celestial target)
                    deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForCheapestCourseCorrection(activeVessel.Orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, caDistance, out burnUT);
                }
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (moonReturn) // Does not create node
            {
                Logger.LogInfo("Moon Return");
                var e = activeVessel.Orbit.eccentricity;
                if (e > 0.2)
                {
                    Logger.LogInfo($"Moon Return Error: Starting Orbit Eccentrity Too Large");
                    Logger.LogError($"Moon Return starting orbit eccentricty {e.ToString("F2")} is > 0.2");
                }
                else
                {
                    double burnUT;
                    double primaryRaidus = activeVessel.Orbit.referenceBody.Orbit.referenceBody.radius + 100000; // m
                    Logger.LogInfo($"Moon Return Attempting to Solve...");
                    var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForMoonReturnEjection(activeVessel.Orbit, UT, primaryRaidus, out burnUT);
                    burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                    Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                    CreateManeuverNodeAtUT(burnParams, burnUT);
                }
            }
            else if (matchVCA) // untested
            {
                Logger.LogInfo("Match Velocity with Target at Closest Approach");
                double closestApproachTime = activeVessel.Orbit.NextClosestApproachTime(currentTarget.Orbit as PatchedConicsOrbit, UT + 2); //+2 so that closestApproachTime is definitely > UT
                var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(activeVessel.Orbit, closestApproachTime, currentTarget.Orbit as PatchedConicsOrbit);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, closestApproachTime);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {closestApproachTime - UT} s from UT");
                CreateManeuverNodeAtUT(burnParams, closestApproachTime);
            }
            else if (matchVNow) // untested
            {
                Logger.LogInfo("Match Velocity with Target at Closest Approach");
                var burnUT = UT + 30;
                var deltaV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(activeVessel.Orbit, burnUT, currentTarget.Orbit as PatchedConicsOrbit);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
            else if (planetaryXfer) // untested
            {
                Logger.LogInfo("Planetary Transfer");
                double burnUT;
                bool syncPhaseAngle = true;
                var deltaV = OrbitalManeuverCalculator.DeltaVAndTimeForInterplanetaryTransferEjection(activeVessel.Orbit, UT, currentTarget.Orbit as PatchedConicsOrbit, syncPhaseAngle, out burnUT);
                burnParams = OrbitalManeuverCalculator.DvToBurnVec(activeVessel.Orbit, deltaV, burnUT);
                Logger.LogInfo($"Solution Found: burnParams [{burnParams.x}, {burnParams.y}, {burnParams.z}] m/s {burnUT - UT} s from UT");
                CreateManeuverNodeAtUT(burnParams, burnUT);
            }
        }
    }

    private IEnumerator UpdateNode(ManeuverNodeData nodeData)
    {
        MapCore mapCore = null;
        Game.Map.TryGetMapCore(out mapCore);
        var m3d = mapCore.map3D;
        var maneuverManager = m3d.ManeuverManager;

        var universeModel = game.UniverseModel;
        VesselComponent vesselComponent;
        if (currentNode != null)
        {
            vesselComponent = universeModel.FindVesselComponent(currentNode.RelatedSimID);
        }
        else
        {
            vesselComponent = activeVessel;
        }
        var simObject = vesselComponent.SimulationObject;
        var maneuverPlanComponent = simObject.FindComponent<ManeuverPlanComponent>();

        // Wait a tick for things to get created
        yield return new WaitForFixedUpdate();

        // Manage the maneuver on the map
        maneuverManager.RemoveAll();
        try { maneuverManager?.GetNodeDataForVessels(); }
        catch { Logger.LogError("UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.GetNodeDataForVessels()"); }
        try { maneuverManager.UpdateAll(); }
        catch { Logger.LogError("UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdateAll()"); }
        try { maneuverManager.UpdatePositionForGizmo(nodeData.NodeID); }
        catch { Logger.LogError("UpdateNode: caught exception on call to mapCore.map3D.ManeuverManager.UpdatePositionForGizmo()"); }

        // Wait a tick for things to get created
        yield return new WaitForFixedUpdate();

        try { maneuverPlanComponent.RefreshManeuverNodeState(0); } // Occasionally getting NREs here...
        catch (NullReferenceException e) { Logger.LogError($"UpdateNode: caught NRE on call to maneuverPlanComponent.RefreshManeuverNodeState(0): {e}"); }
    }

    private IPatchedOrbit GetLastOrbit()
    {
        Logger.LogInfo("GetLastOrbit()");
        List<ManeuverNodeData> patchList =
            Game.SpaceSimulation.Maneuvers.GetNodesForVessel(activeVessel.SimulationObject.GlobalId);

        Logger.LogMessage(patchList.Count);

        if (patchList.Count == 0)
        {
            Logger.LogMessage(activeVessel.Orbit);
            return activeVessel.Orbit;
        }
        Logger.LogMessage(patchList[patchList.Count - 1].ManeuverTrajectoryPatch);
        IPatchedOrbit orbit = patchList[patchList.Count - 1].ManeuverTrajectoryPatch;

        return orbit;
    }

    private void CreateManeuverNodeAtTA(Vector3d burnVector, double TrueAnomalyRad)
    {
        Logger.LogInfo("CreateManeuverNodeAtTA");
        PatchedConicsOrbit referencedOrbit = GetLastOrbit() as PatchedConicsOrbit;
        if (referencedOrbit == null)
        {
            Logger.LogError("CreateManeuverNode: referencedOrbit is null!");
            return;
        }

        double UT = referencedOrbit.GetUTforTrueAnomaly(TrueAnomalyRad, 0);

        CreateManeuverNodeAtUT(burnVector, UT);
    }

    private void CreateManeuverNodeAtUT(Vector3d burnVector, double UT)
    {
        Logger.LogInfo("CreateManeuverNodeAtUT");

        PatchedConicsOrbit referencedOrbit = GetLastOrbit() as PatchedConicsOrbit;
        if (referencedOrbit == null)
        {
            Logger.LogError("CreateManeuverNode: referencedOrbit is null!");
            return;
        }

        ManeuverNodeData nodeData = new ManeuverNodeData(activeVessel.SimulationObject.GlobalId, true, UT);

        IPatchedOrbit orbit = referencedOrbit;

        orbit.PatchStartTransition = PatchTransitionType.Maneuver;
        orbit.PatchEndTransition = PatchTransitionType.Final;

        nodeData.SetManeuverState((PatchedConicsOrbit)orbit);

        nodeData.BurnVector = burnVector;

        Logger.LogInfo($"CreateManeuverNodeAtUT: BurnVector [{burnVector.x}, {burnVector.y}, {burnVector.z}] m/s");
        Logger.LogInfo($"CreateManeuverNodeAtUT: BurnDuration {nodeData.BurnDuration} s");
        Logger.LogInfo($"CreateManeuverNodeAtUT: Burn Time {nodeData.Time} s");

        AddManeuverNode(nodeData);
    }

    private void AddManeuverNode(ManeuverNodeData nodeData)
    {
        Logger.LogInfo("AddManeuverNode");

        // Add the node to the vessel's orbit
        GameManager.Instance.Game.SpaceSimulation.Maneuvers.AddNodeToVessel(nodeData);

        // For KSP2, We want the to start burns early to make them centered on the node
        nodeData.Time -= nodeData.BurnDuration / 2;

        Logger.LogInfo($"AddManeuverNode: BurnVector   [{nodeData.BurnVector.x}, {nodeData.BurnVector.y}, {nodeData.BurnVector.z}] m/s");
        Logger.LogInfo($"AddManeuverNode: BurnDuration {nodeData.BurnDuration} s");
        Logger.LogInfo($"AddManeuverNode: Burn Time    {nodeData.Time}");

        // Set the currentNode  to the node we just created and added to the vessel
        currentNode = nodeData;

        StartCoroutine(UpdateNode(nodeData));

        Logger.LogInfo("AddManeuverNode Done");
    }
}
