
using UnityEngine;
using FlightPlan.Tools;
using SpaceWarp.API.UI;

namespace FlightPlan.UI;

public class FPStyles
{
    private static bool guiLoaded = false;

    public static GUIStyle window, box, vertical_scrollbar;

    public static GUIStyle separator;

    public static GUIStyle progress_bar_empty, progress_bar_full;

    public static GUIStyle bigicon_button, icon_button, small_button, big_button, button;

    public static GUIStyle tab_normal, tab_active;

    public static GUIStyle foldout_close, foldout_open;

    public static GUIStyle error, warning, label, mid_text, console_text, phase_ok, phase_warning, phase_error;

    public static GUIStyle toggle;

    public static GUIStyle slider_line, slider_node, slider_text;

    public static GUIStyle icons_label, title;

    public static Texture2D gear, icon, k2d2_big_icon, mnc_icon, cross;

    public static void Init()
    {
        if (!guiLoaded)
        {
            GetStyles();
        }
    }


    private static void setAllFromNormal(GUIStyle style)
    {
        style.hover = style.normal;
        style.active = style.normal;
        style.focused = style.normal;
        style.onNormal = style.normal;
        style.onHover = style.normal;
        style.onActive = style.normal;
        style.onFocused = style.normal;
    }

    private static void setFromOn(GUIStyle style)
    {
        style.onHover = style.onNormal;
        style.onActive = style.onNormal;
        style.onFocused = style.onNormal;
    }

    public static GUISkin skin;

    public static void GetStyles()
    {
        if (guiLoaded)
            return;

        skin = Skins.ConsoleSkin;

        // Define the GUIStyle for the window
        window = new GUIStyle(skin.window);

        window.border = new RectOffset(25, 25, 35, 25);
        window.margin = new RectOffset(0, 0, 0, 0);
        window.padding = new RectOffset(20, 13, 44, 17);
        window.overflow = new RectOffset(0, 0, 0, 0);

        // window.fontSize = 20;
        window.contentOffset = new Vector2(31, -40);

        // Set the background color of the window
        window.normal.background = AssetsLoader.loadIcon("window");
        window.normal.textColor = Color.black;
        setAllFromNormal(window);
        window.alignment = TextAnchor.UpperLeft;
        window.stretchWidth = true;
         // window.fontSize = 20;
        window.contentOffset = new Vector2(31, -40);
        skin.window = window;


        // Define the GUIStyle for the window
        box = new GUIStyle(window);
        box.normal.background = AssetsLoader.loadIcon("Box");
        setAllFromNormal(box);
        box.border = new RectOffset(10, 10, 10, 10);
        box.margin = new RectOffset(0, 0, 0, 0);
        box.padding = new RectOffset(10, 10, 10, 10);
        box.overflow = new RectOffset(0, 0, 0, 0);
        skin.box = box;
        skin.scrollView = box;

        vertical_scrollbar  = new GUIStyle(GUI.skin.verticalScrollbar);
        vertical_scrollbar.normal.background = AssetsLoader.loadIcon("VerticalScroll");
        setAllFromNormal(vertical_scrollbar);
        vertical_scrollbar.border = new RectOffset(5, 5, 5, 5);
        vertical_scrollbar.fixedWidth = 10;
        skin.verticalScrollbar = vertical_scrollbar;

        skin.verticalScrollbarThumb.normal.background = AssetsLoader.loadIcon("VerticalScroll_thumb");
        setAllFromNormal(skin.verticalScrollbarThumb);
        skin.verticalScrollbarThumb.border = new RectOffset(5, 5, 5, 5);
        skin.verticalScrollbarThumb.fixedWidth = 10;

        slider_line = new GUIStyle(GUI.skin.horizontalSlider);
        slider_line.normal.background = AssetsLoader.loadIcon("Slider");
        setAllFromNormal(slider_line);
        slider_line.border = new RectOffset(5, 5, 0, 0);

        slider_line.border = new RectOffset(12, 14, 0, 0);
        slider_line.fixedWidth = 0;
        slider_line.fixedHeight = 21;
        slider_line.margin = new RectOffset(0, 0, 2, 5);

        slider_node = new GUIStyle(GUI.skin.horizontalSliderThumb);
        slider_node.normal.background = AssetsLoader.loadIcon("SliderNode");
        setAllFromNormal(slider_node);
        slider_node.border = new RectOffset(0, 0, 0, 0);
        slider_node.fixedWidth = 21;
        slider_node.fixedHeight = 21;


        tab_normal = new GUIStyle(button);
        tab_normal.border = new RectOffset(5, 5, 5, 5);
        tab_normal.padding = new RectOffset(4, 3, 4, 3);
        tab_normal.overflow = new RectOffset(0, 0, 0, 0);
        // big_button.fontSize = 20;
        tab_normal.alignment = TextAnchor.MiddleCenter;

        tab_normal.normal.background = AssetsLoader.loadIcon("Tab_Normal");
        setAllFromNormal(tab_normal);

        tab_normal.hover.background = AssetsLoader.loadIcon("Tab_Hover");
        tab_normal.active.background = AssetsLoader.loadIcon("Tab_Active");
        tab_normal.onNormal = tab_normal.active;
        setFromOn(tab_normal);

        tab_active = new GUIStyle(tab_normal);
        tab_active.normal.background = AssetsLoader.loadIcon("Tab_On_normal");
        setAllFromNormal(tab_active);

        tab_active.hover.background = AssetsLoader.loadIcon("Tab_On_hover");
        tab_active.active.background = AssetsLoader.loadIcon("Tab_On_Active");
        tab_active.onNormal = tab_active.active;
        setFromOn(tab_active);


        // button std
        button = new GUIStyle(GUI.skin.GetStyle("Button"));
        button.normal.background = AssetsLoader.loadIcon("BigButton_Normal");
        button.normal.textColor = ColorTools.parseColor("#FFFFFF");
        setAllFromNormal(button);

        button.hover.background = AssetsLoader.loadIcon("BigButton_hover");
        button.active.background = AssetsLoader.loadIcon("BigButton_hover");
        // button.active.background = AssetsLoader.loadIcon("BigButton_on");
        // button.onNormal = button.active;
        // setFromOn(button);

        button.border = new RectOffset(5, 5, 5, 5);
        button.padding = new RectOffset(4, 3, 4, 3);
        button.overflow = new RectOffset(0, 0, 0, 0);
        // button.fontSize = 20;
        button.alignment = TextAnchor.MiddleCenter;
        skin.button = button;

        // Small Button
        small_button = new GUIStyle(GUI.skin.GetStyle("Button"));
        small_button.normal.background = AssetsLoader.loadIcon("Small_Button");
        setAllFromNormal(small_button);
        small_button.hover.background = AssetsLoader.loadIcon("Small_Button_hover");
        small_button.active.background = AssetsLoader.loadIcon("Small_Button_active");
        small_button.onNormal = small_button.active;
        setFromOn(small_button);

        small_button.border = new RectOffset(5, 5, 5, 5);
        small_button.padding = new RectOffset(0, 0, 0, 0);
        small_button.overflow = new RectOffset(0, 0, 0, 0);
        small_button.alignment = TextAnchor.MiddleCenter;

        big_button = new GUIStyle(GUI.skin.GetStyle("Button"));
        big_button.normal.background = AssetsLoader.loadIcon("BigButton_Normal");
        big_button.normal.textColor = ColorTools.parseColor("#FFFFFF");
        setAllFromNormal(big_button);

        big_button.hover.background = AssetsLoader.loadIcon("BigButton_Hover");
        big_button.active.background = AssetsLoader.loadIcon("BigButton_Active");
        big_button.onNormal = big_button.active;
        setFromOn(big_button);

        big_button.border = new RectOffset(5, 5, 5, 5);
        big_button.padding = new RectOffset(4, 3, 4, 3);
        big_button.overflow = new RectOffset(0, 0, 0, 0);
        // big_button.fontSize = 20;
        big_button.alignment = TextAnchor.MiddleCenter;

        // Small Button
        icon_button = new GUIStyle(small_button);
        icon_button.padding = new RectOffset(4, 4, 4, 4);

        bigicon_button = new GUIStyle(icon_button);
        bigicon_button.fixedWidth = 50;
        bigicon_button.fixedHeight = 50;
        bigicon_button.fontStyle = FontStyle.Bold;

        foldout_close = new GUIStyle(small_button);
        foldout_close.fixedHeight = 30;
        foldout_close.padding = new RectOffset(23, 2, 2, 2);
        foldout_close.border = new RectOffset(23, 7, 27, 3);

        foldout_close.normal.background = AssetsLoader.loadIcon("Chapter_Off_Normal");
        foldout_close.normal.textColor = ColorTools.parseColor("#D4D4D4");
        foldout_close.alignment = TextAnchor.MiddleLeft;
        setAllFromNormal(foldout_close);
        foldout_close.hover.background = AssetsLoader.loadIcon("Chapter_Off_Hover");
        foldout_close.active.background = AssetsLoader.loadIcon("Chapter_Off_Active");

        foldout_open = new GUIStyle(foldout_close);
        foldout_open.normal.background = AssetsLoader.loadIcon("Chapter_On_Normal");
        foldout_open.normal.textColor = ColorTools.parseColor("#8BFF95");
        setAllFromNormal(foldout_open);

        foldout_open.hover.background = AssetsLoader.loadIcon("Chapter_On_Hover");
        foldout_open.active.background = AssetsLoader.loadIcon("Chapter_On_Active");

       

        // Toggle Button
        toggle = new GUIStyle(GUI.skin.GetStyle("Button"));
        toggle.normal.background = AssetsLoader.loadIcon("Toggle_Off");
        toggle.normal.textColor = ColorTools.parseColor("#C0C1E2");


        setAllFromNormal(toggle);
        toggle.onNormal.background = AssetsLoader.loadIcon("Toggle_On");
        toggle.onNormal.textColor = ColorTools.parseColor("#C0E2DC");
        setFromOn(toggle);
        toggle.fixedHeight = 32;
        toggle.stretchWidth = false;

        toggle.border = new RectOffset(45, 5, 5, 5);
        toggle.padding = new RectOffset(34, 16, 0, 0);
        toggle.overflow = new RectOffset(0, 0, 0, 2);

        // separator
        separator = new GUIStyle(GUI.skin.box);
        separator.normal.background = AssetsLoader.loadIcon("line");
        separator.border = new RectOffset(2,2,0,0);
        separator.margin = new RectOffset(10,10,5,5);
        separator.fixedHeight = 3;
        setAllFromNormal(separator);

        // progress bar
        progress_bar_empty = new GUIStyle(GUI.skin.box);
        progress_bar_empty.normal.background = AssetsLoader.loadIcon("progress_empty");
        progress_bar_empty.border = new RectOffset(2,2,2,2);
        progress_bar_empty.margin = new RectOffset(5,5,5,5);
        progress_bar_empty.fixedHeight = 20;
        setAllFromNormal(progress_bar_empty);

        progress_bar_full = new GUIStyle(progress_bar_empty);
        progress_bar_full.normal.background = AssetsLoader.loadIcon("progress_full");
        setAllFromNormal(progress_bar_empty);

        // icons
        gear = AssetsLoader.loadIcon("gear");
        icon = AssetsLoader.loadIcon("icon");
        k2d2_big_icon = AssetsLoader.loadIcon("k2d2_big_icon");
        mnc_icon = AssetsLoader.loadIcon("mnc_icon");
        cross = AssetsLoader.loadIcon("Cross");

        icons_label = new GUIStyle(GUI.skin.GetStyle("Label"));
        icons_label.border = new RectOffset(0, 0, 0, 0);
        icons_label.padding = new RectOffset(0, 0, 0, 0);
        icons_label.margin = new RectOffset(0, 0, 0, 0);
        icons_label.overflow = new RectOffset(0, 0, 0, 0);

        error = new GUIStyle(GUI.skin.GetStyle("Label"));
        warning = new GUIStyle(GUI.skin.GetStyle("Label"));
        error.normal.textColor = Color.red;
        warning.normal.textColor = Color.yellow;
        //labelColor = GUI.skin.GetStyle("Label").normal.textColor;

        phase_ok = new GUIStyle(GUI.skin.GetStyle("Label"));
        phase_ok.normal.textColor = ColorTools.parseColor("#00BC16");
        // phase_ok.fontSize = 20;

        phase_warning = new GUIStyle(GUI.skin.GetStyle("Label"));
        phase_warning.normal.textColor = ColorTools.parseColor("#BC9200");
        // phase_warning.fontSize = 20;

        phase_error = new GUIStyle(GUI.skin.GetStyle("Label"));
        phase_error.normal.textColor = ColorTools.parseColor("#B30F0F");
        // phase_error.fontSize = 20;

        console_text = new GUIStyle(GUI.skin.GetStyle("Label"));
        console_text.normal.textColor = ColorTools.parseColor("#B6B8FA");
        // console_text.fontSize = 15;
        console_text.padding = new RectOffset(0, 0, 0, 0);
        console_text.margin = new RectOffset(0, 0, 0, 0);

        slider_text = new GUIStyle(console_text);
        slider_text.normal.textColor = ColorTools.parseColor("#C0C1E2");

        mid_text = new GUIStyle(slider_text);

        slider_text.margin = new RectOffset(5, 0, 0, 0);
        slider_text.contentOffset = new Vector2(8, 5);

        label = new GUIStyle(GUI.skin.GetStyle("Label"));
        // label.fontSize = 17;
        label.margin = new RectOffset(0, 0, 0, 0);
        label.padding = new RectOffset(0, 0, 0, 0);

        title = new GUIStyle();
        title.normal.textColor = ColorTools.parseColor("#C0C1E2");
        // title.fontSize = 19;

        guiLoaded = true;
    }
}

