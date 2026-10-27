using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;


namespace NGOTrainer {
public class NumericTrainer : MonoBehaviour {
    public static void Install() { Application.onBeforeRender += InstallOnMainThread; }
    private static void InstallOnMainThread() {
        Application.onBeforeRender -= InstallOnMainThread;
        if (GameObject.Find("NGO Numeric Trainer") != null) return;
        var go = new GameObject("NGO Numeric Trainer"); UnityEngine.Object.DontDestroyOnLoad(go); go.AddComponent<NumericTrainer>();
    }
    private readonly string[] names = {"Follower", "Stress", "Love", "Yami", "Tension"};
    private readonly string[] labels = {"Followers", "Stress", "Affection", "Darkness", "Tension"};
    private Type managerType, statusType, windowManagerType, appType, iwindowType;
    private object nativeWindow, windowManager;
    private MethodInfo get, min, max, set;
    private GameObject panel;
    private TMP_Text[] readouts = new TMP_Text[5];
    private TMP_Text info;
    private bool wanted;
    private void Awake() {
        try {
            var asm = Assembly.Load("Assembly-CSharp");
            managerType = asm.GetType("ngov3.StatusManager", true);
            statusType = asm.GetType("ngov3.StatusType", true);
            windowManagerType = asm.GetType("ngov3.WindowManager", true);
            appType = asm.GetType("ngov3.AppType", true);
            iwindowType = asm.GetType("ngov3.IWindow", true);
            get = managerType.GetMethod("GetStatus", new Type[] { statusType });
            min = managerType.GetMethod("GetMinStatus", new Type[] { statusType });
            max = managerType.GetMethod("GetMaxStatus", new Type[] { statusType });
            set = managerType.GetMethod("UpdateStatusToNumber", new Type[] { statusType, typeof(int) });
            if (get == null || min == null || max == null || set == null) throw new Exception("Status API missing");
        } catch (Exception e) { Debug.LogError(e); }
    }
    private void Update() {
        try {
            if (Input.GetKeyDown(KeyCode.F1)) {
                if (panel != null) { CloseWindow(); return; }
                Build();
            }
            if (panel == null) return;
            var manager = UnityEngine.Object.FindObjectOfType(managerType);
            for(int i=0;i<names.Length;i++) {
                if (manager == null) readouts[i].text = labels[i] + ": --";
                else readouts[i].text = labels[i] + ": " + get.Invoke(manager, new object[] { Enum.Parse(statusType, names[i]) });
            }
        } catch(Exception e) { Debug.LogError("Trainer: " + e); }
    }
    private void Build() {
        var wm = UnityEngine.Object.FindObjectOfType(windowManagerType);
        if (wm == null) return;
        var asm = Assembly.Load("Assembly-CSharp");
        var load = asm.GetType("ngov3.LoadAppData", true);
        var kind = Enum.Parse(appType, "MyPicture");
        var data = load.GetMethod("ReadAppContent", new Type[] { appType }).Invoke(null, new object[] { kind });
        bool is2d = (bool)data.GetType().GetField("is2DWindow").GetValue(data);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var parent = (Transform)windowManagerType.GetField(is2d ? "_windowparent2D" : "_windowparent", flags).GetValue(wm);
        var create = windowManagerType.GetMethod("NewWindow", new Type[] { appType, typeof(Transform) });
        nativeWindow = create.Invoke(wm, new object[] { kind, parent }); windowManager = wm;
        var windowComponent = nativeWindow as Component;
        var rect = windowComponent == null ? null : windowComponent.transform as RectTransform;
        if (rect != null) { rect.sizeDelta = new Vector2(600, 390); }
        foreach (var fieldName in new string[] { "firstWidth", "firstHeight" }) {
            var f = nativeWindow.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null) f.SetValue(nativeWindow, fieldName == "firstWidth" ? 600f : 390f);
        }
        var content = (GameObject)iwindowType.GetProperty("nakamiApp").GetValue(nativeWindow, null);
        if (content == null) throw new Exception("Native window has no content");
        var contentParent = content.transform.parent;
        content.SetActive(false);
        iwindowType.GetMethod("SetName").Invoke(nativeWindow, new object[] { "Numeric Trainer" });
        panel = NewUI("Numeric Trainer", contentParent, 500, 330, 0, 0);
        var bg = panel.AddComponent<Image>(); bg.color = new Color(0.98f,0.94f,1f,1f);
        Text(panel.transform, "F1: show / hide", 460, 35, 0, 140, 23);
        for(int i=0;i<5;i++) {
            int index=i; float y=95-i*45;
            readouts[i]=Text(panel.transform, labels[i], 190, 35, -130, y, 19);
            Button(panel.transform, "-100", -10, y, delegate { Change(index,-100); });
            Button(panel.transform, "-10", 65, y, delegate { Change(index,-10); });
            Button(panel.transform, "+10", 140, y, delegate { Change(index,10); });
            Button(panel.transform, "+100", 215, y, delegate { Change(index,100); });
        }
        info=Text(panel.transform, "Load a save. Values respect game limits.", 470, 28, 0, -140, 15);
    }
    private void CloseWindow() {
        if (nativeWindow != null && windowManager != null)
            windowManagerType.GetMethod("Close", new Type[] { iwindowType }).Invoke(windowManager, new object[] { nativeWindow });
        nativeWindow = null; windowManager = null; panel = null;
    }
    private GameObject NewUI(string name, Transform parent, float w, float h, float x, float y) {
        var go=new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rt=(RectTransform)go.transform; rt.anchorMin=rt.anchorMax=new Vector2(0.5f,0.5f); rt.sizeDelta=new Vector2(w,h); rt.anchoredPosition=new Vector2(x,y); return go;
    }
    private TMP_Text Text(Transform parent,string value,float w,float h,float x,float y,int size) {
        var go=NewUI("Label",parent,w,h,x,y); var t=go.AddComponent<TextMeshProUGUI>(); t.text=value; t.fontSize=size; t.color=new Color(0.12f,0.05f,0.48f,1f); t.alignment=TextAlignmentOptions.Center; t.raycastTarget=false; return t;
    }
    private void Button(Transform parent,string label,float x,float y,UnityAction action) {
        var go=NewUI(label,parent,68,32,x,y); var image=go.AddComponent<Image>(); image.color=new Color(0.86f,0.72f,0.92f,1f);
        var button=go.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=image; button.onClick.AddListener(action);
        Text(go.transform,label,66,30,0,0,16);
    }
    private void Change(int i,int delta) {
        try {
            var manager=UnityEngine.Object.FindObjectOfType(managerType); if(manager == null) { info.text="Load a save first."; return; }
            var status=Enum.Parse(statusType,names[i]); int current=(int)get.Invoke(manager,new object[]{status});
            int lo=(int)min.Invoke(manager,new object[]{status}); int hi=(int)max.Invoke(manager,new object[]{status});
            int v=(int)Math.Max((long)lo,Math.Min((long)hi,(long)current+delta));
            set.Invoke(manager,new object[]{status,v}); info.text=labels[i]+" set to "+v+" (range "+lo+" - "+hi+")";
        } catch(Exception e) { Debug.LogError(e); if(info!=null) info.text="Update failed."; }
    }
}

}
