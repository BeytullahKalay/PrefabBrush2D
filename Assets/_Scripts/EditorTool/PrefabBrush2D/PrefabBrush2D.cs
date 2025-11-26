using UnityEditor;
using UnityEngine;

public class PrefabBrush2D : EditorWindow
{
    // --- Ayarlar ---
    string toolName = "2D Prefab Brush";
    GameObject prefabToPaint;
    Transform parentObject;

    float brushRadius = 2.0f;
    int objectsPerStroke = 5;
    float minObjectDistance = 0.5f;

    //Surukleme sirasinda ne kadar mesafede bir boyasin?
    float dragThreshold = 1.0f;

    LayerMask obstacleLayer;

    Vector2 scrollPos;

    // Surukleme takibi icin degisken
    private Vector3 lastPaintPosition;

    [MenuItem("Tools/2D Prefab Brush")]
    public static void ShowWindow()
    {
        GetWindow<PrefabBrush2D>("2D Brush");
    }

    void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        obstacleLayer = Physics2D.AllLayers;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label(toolName, EditorStyles.boldLabel);
        GUILayout.Space(10);

        prefabToPaint = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabToPaint, typeof(GameObject), false);
        parentObject = (Transform)EditorGUILayout.ObjectField("Parent (Optional)", parentObject, typeof(Transform), true);

        GUILayout.Space(10);
        GUILayout.Label("Brush Settings", EditorStyles.boldLabel);

        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, 0.1f, 10f);
        objectsPerStroke = EditorGUILayout.IntSlider("Objects Per Click", objectsPerStroke, 1, 50);
        minObjectDistance = EditorGUILayout.Slider("Min Distance", minObjectDistance, 0.1f, 5f);

        // YENI UI ALANI
        dragThreshold = EditorGUILayout.Slider("Drag Spacing", dragThreshold, 0.1f, 5f);

        GUILayout.Space(5);
        GUILayout.Label("Collision Settings", EditorStyles.boldLabel);
        obstacleLayer = LayerMaskField("Obstacle Layers", obstacleLayer);

        GUILayout.Space(20);

        if (prefabToPaint == null)
            EditorGUILayout.HelpBox("Lutfen bir Prefab secin.", MessageType.Warning);


        EditorGUILayout.EndScrollView();
    }

    static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        var layers = UnityEditorInternal.InternalEditorUtility.layers;
        int layerMaskVal = layerMask.value;
        layerMaskVal = EditorGUILayout.MaskField(label, layerMaskVal, layers);
        return layerMaskVal;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (prefabToPaint == null) return;

        Event e = Event.current;
        Vector3 mousePosition = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
        mousePosition.z = 0;

        // Gorsellestirme
        Handles.color = new Color(0, 1, 0, 0.3f);
        Handles.DrawWireDisc(mousePosition, Vector3.forward, brushRadius);
        Handles.color = new Color(0, 1, 0, 0.05f);
        Handles.DrawSolidDisc(mousePosition, Vector3.forward, brushRadius);

        // --- MOUSE KONTROLLERI ---

        // 1. Tiklama Ani (MouseDown) - Her zaman boyar
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);

            Paint(mousePosition);
            lastPaintPosition = mousePosition; // Son boyanan yeri kaydet
            e.Use();
        }

        // 2. Surukleme Ani (MouseDrag) - Sadece belli mesafe gecildiyse boyar
        else if (e.type == EventType.MouseDrag && e.button == 0 && !e.alt)
        {
            GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);

            // Mesafe kontrolu: Su anki mouse pozisyonu ile son boyanan yer arasindaki fark
            float distance = Vector3.Distance(mousePosition, lastPaintPosition);

            // Eger mesafe "Drag Spacing"den buyukse boya
            if (distance > dragThreshold)
            {
                Paint(mousePosition);
                lastPaintPosition = mousePosition; // Son boyanan yeri guncelle
            }

            e.Use();
        }
    }

    void Paint(Vector3 center)
    {
        for (int i = 0; i < objectsPerStroke; i++)
        {
            Vector2 randomPoint = Random.insideUnitCircle * brushRadius;
            Vector3 spawnPos = center + new Vector3(randomPoint.x, randomPoint.y, 0);

            Collider2D hit = Physics2D.OverlapCircle(spawnPos, minObjectDistance, obstacleLayer);

            if (hit == null)
            {
                SpawnObject(spawnPos);
            }
        }
    }

    void SpawnObject(Vector3 position)
    {
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPaint);
        newObj.transform.position = position;

        if (parentObject != null)
        {
            newObj.transform.SetParent(parentObject);
        }

        Undo.RegisterCreatedObjectUndo(newObj, "Paint Object");
    }
}