using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using System;
using Object = UnityEngine.Object;
using UnityEngine.UIElements;
using UnityEditor.IMGUI;

[Serializable]
public class VoxelToolWindow : EditorWindow
{

    public struct Pixel
    {
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }

        public Color pixelColor { get; set; }

        public bool _checked;

        public bool partOfMesh;

        public int mesh;

        public List<Pixel> list;
        public Vector3 position { get; set; }

        public bool upClear;
        public bool downClear;
        public bool eastClear;
        public bool westClear;
        public bool northClear;
        public bool southClear;

        public Pixel(int x, int y, int z, Color c)
        {
            this.x = x;
            this.y = y;
            this.z = z;

            pixelColor = c;

            _checked = false;
            partOfMesh = false;

            mesh = 0;

            list = new List<Pixel>();
            position = new Vector3(x, z, y);

            upClear = false;
            downClear = false;
            eastClear = false;
            westClear = false;
            northClear = false;
            southClear = false;
        }
    }

    //Editor
    public GUIStyle style = GUIStyle.none;
    static Object[] presetTarget = new Object[1];
    Color currentColor;
    static VisualElement root;
    static Rect rect;
    static VoxelToolWindow window;


    //Input variables
    public Sprite spr;
    [SerializeField] public int width;
    public int length;
    public int modelHeight;

    //Output variables
    public string outputName;
    public string path;
    private string prefabPath;

    //References
    private bool fold;
    private bool fold_1;
    private bool fold_2;

    [SerializeField] public List<GameObject> uniqueObjectsReferences = new List<GameObject>();
    [SerializeField] public List<Color> uniqueObjectsColorReferences = new List<Color>();

    [SerializeField] public List<GameObject> terrainReferences = new List<GameObject>();
    [SerializeField] public List<Color> terrainColorReferences = new List<Color>();

    private Color[,,] color;
    private Pixel[,,] pixels;
    private int differentMeshes = 0;
    private List<Pixel> queue = new List<Pixel>();

    private List<Pixel>[] clusters;
    private List<Vector3>[] pointClouds;
    private List<int>[] triangles;

    private List<GameObject> finalObjects;
    private List<Mesh> meshes;

    private GameObject emptyParent;
    private GameObject uniqueObjectParent;
    private List<GameObject> uniqueParents;
    private GameObject terrainObjectParent;

    private Texture2D inputTexture;
    private Texture2D[] textures;

    public List<Color> palette = new List<Color>();

    [MenuItem("Window/VoxelTool &a")]
    static void OpenWindow()
    {
        window = (VoxelToolWindow)GetWindow(typeof(VoxelToolWindow));

        window.titleContent = new GUIContent("Voxel Tool");
        window.minSize = new Vector2(450, 200);
        window.Show();
        window.Focus();
        presetTarget[0] = window;
        root = window.rootVisualElement;

        SetRoot();
    }
    static void newGUI()
    {
        if (EditorWindow.focusedWindow == window)
            rect = EditorWindow.focusedWindow.rootVisualElement.layout;
        else
            rect = root.layout;


        GUILayout.Box(new GUIContent("Voxel Tool"), GUILayout.ExpandWidth(true), GUILayout.Height(20));
        PresetSelector.DrawPresetButton(new Rect(new Vector2(rect.position.x + rect.width - 20, 2), new Vector2(10,10)), presetTarget);
    }
    static void SetRoot()
    {
        IMGUIContainer container = new IMGUIContainer();
        root.Add(container);

        container.name = "VoxelToolWindow";

        container.StretchToParentSize();
        container.AddToClassList("unity-imgui-container");

        rect = EditorWindow.focusedWindow.rootVisualElement.layout;

        Action GUIHandler = newGUI;
        container.onGUIHandler = GUIHandler;
    }


    private void OnGUI()
    {
        currentColor = GUI.color;

        GUILayout.Space(20);

        GUI.color = Color.cyan;
        GUILayout.Label("Input", EditorStyles.boldLabel);
        GUI.color = currentColor;

        #region Input
        EditorGUILayout.BeginVertical();

        spr = (Sprite)EditorGUILayout.ObjectField("Sprite",spr, typeof(Sprite), false,GUILayout.ExpandWidth(false)) ;
        width = EditorGUILayout.IntField("Model Width", width, GUILayout.ExpandWidth(false));
        length = EditorGUILayout.IntField("Model Length", length, GUILayout.ExpandWidth(false));
        modelHeight = EditorGUILayout.IntField("Model Height", modelHeight, GUILayout.ExpandWidth(false));
     

        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);

        if(GUILayout.Button("Get Color Palette", GUILayout.ExpandWidth(false)))
        {
            if (spr != null)
                GetPalette();
        }
        
        EditorGUILayout.BeginVertical();
        fold_2 = EditorGUILayout.Foldout(fold_2, "Palette");
        if (fold_2)
        {
            for (int i = 0; i < palette.Count; i++)
            {
                EditorGUILayout.ColorField(palette[i], GUILayout.ExpandWidth(false));
            }
        }
        EditorGUILayout.EndVertical();

        #endregion 

        GUILayout.Space(20);
        GUI.color = Color.cyan;
        GUILayout.Label("Output", EditorStyles.boldLabel);
        GUI.color = currentColor;
        outputName = EditorGUILayout.TextField("Output Name", outputName);

        #region Output
        EditorGUILayout.BeginHorizontal();
        
        path = EditorGUILayout.TextField("Save Directory", path);
        if(GUILayout.Button("Browse",  GUILayout.ExpandWidth(false)))
        {
           path =  EditorUtility.SaveFolderPanel("Select Save Directory", "Assets/", "");
           if(!string.IsNullOrEmpty(path))
           {
                path = FileUtil.GetProjectRelativePath(path);
           }
        
        }
        EditorGUILayout.EndHorizontal();
        #endregion 

        GUILayout.Space(20);
        GUI.color = Color.cyan;
        GUILayout.Label("Unique Objects", EditorStyles.boldLabel);
        GUI.color = currentColor;
        #region Unique Objects
        EditorGUILayout.BeginHorizontal();

        //Object Refs
        EditorGUILayout.BeginVertical();
        fold = EditorGUILayout.Foldout(fold, "Object References", true);
        if(fold)
        {
            #region DropDown
            int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", uniqueObjectsReferences.Count, GUILayout.ExpandWidth(false)));
            while (newCount < uniqueObjectsReferences.Count)
                uniqueObjectsReferences.RemoveAt(uniqueObjectsReferences.Count - 1);

            while (newCount > uniqueObjectsReferences.Count)
            {
                if (uniqueObjectsReferences.Count > 0)
                    uniqueObjectsReferences.Add(uniqueObjectsReferences[uniqueObjectsReferences.Count - 1]);
                else
                    uniqueObjectsReferences.Add(null);
            }

            for (int i = 0; i < uniqueObjectsReferences.Count; i++)
            {
                uniqueObjectsReferences[i] = (GameObject)EditorGUILayout.ObjectField(uniqueObjectsReferences[i], typeof(GameObject), false, GUILayout.ExpandWidth(false));  
            }
            #endregion
        }
        EditorGUILayout.EndVertical();

        //Color Refs
        EditorGUILayout.BeginVertical();
        fold = EditorGUILayout.Foldout(fold, "Color References", true);
        if (fold)
        {
            #region DropDown
            int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", uniqueObjectsColorReferences.Count, GUILayout.ExpandWidth(false)));
            while(newCount < uniqueObjectsColorReferences.Count)
                uniqueObjectsColorReferences.RemoveAt(uniqueObjectsColorReferences.Count - 1);
            while (newCount > uniqueObjectsColorReferences.Count)
            {
                if (uniqueObjectsColorReferences.Count > 0)
                    uniqueObjectsColorReferences.Add(uniqueObjectsColorReferences[uniqueObjectsColorReferences.Count - 1]);
                else
                    uniqueObjectsColorReferences.Add(Color.clear);
            }

            for (int i = 0; i < uniqueObjectsColorReferences.Count; i++)
            {
                uniqueObjectsColorReferences[i] = EditorGUILayout.ColorField(uniqueObjectsColorReferences[i], GUILayout.ExpandWidth(false));
            }
            #endregion
        }
        EditorGUILayout.EndVertical();


        EditorGUILayout.EndHorizontal();
        
        #region Errors
        if (uniqueObjectsColorReferences.Count != uniqueObjectsReferences.Count)
        {
            EditorGUILayout.HelpBox("Color and reference objects list are not matching !", MessageType.Warning);
        }
        for (int i = 0; i < uniqueObjectsReferences.Count; i++)
        {
            if (uniqueObjectsReferences[i] == null)
            {
                EditorGUILayout.HelpBox("Reference object missing !", MessageType.Error);
                break;
            }
        }
        for (int i = 0; i < uniqueObjectsColorReferences.Count; i++)
        {
            if (uniqueObjectsColorReferences[i] == Color.clear)
            {
                EditorGUILayout.HelpBox("Reference color missing !", MessageType.Error);
                break;
            }
        }
        #endregion
        
        #endregion

        GUILayout.Space(20);
        GUI.color = Color.cyan;
        GUILayout.Label("Terrain", EditorStyles.boldLabel);
        GUI.color = currentColor;
        #region Terrain
        EditorGUILayout.BeginHorizontal();

        //Terrain Refs
        EditorGUILayout.BeginVertical();
        fold_1 = EditorGUILayout.Foldout(fold_1, "Terrain References", true);
        if (fold_1)
        {
            #region DropDown
            int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", terrainReferences.Count, GUILayout.ExpandWidth(false)));
            while (newCount < terrainReferences.Count)
                terrainReferences.RemoveAt(terrainReferences.Count - 1);

            while (newCount > terrainReferences.Count)
            {
                if (terrainReferences.Count > 0)
                    terrainReferences.Add(terrainReferences[terrainReferences.Count - 1]);
                else
                    terrainReferences.Add(null);
            }

            for (int i = 0; i < terrainReferences.Count; i++)
            {
                terrainReferences[i] = (GameObject)EditorGUILayout.ObjectField(terrainReferences[i], typeof(GameObject), false, GUILayout.ExpandWidth(false));
            }
            #endregion
        }
        EditorGUILayout.EndVertical();

        //Color Refs
        EditorGUILayout.BeginVertical();
        fold_1 = EditorGUILayout.Foldout(fold_1, "Color References", true);
        if (fold_1)
        {
            #region DropDown
            int newCount = Mathf.Max(0, EditorGUILayout.DelayedIntField("Size", terrainColorReferences.Count, GUILayout.ExpandWidth(false)));
            while (newCount < terrainColorReferences.Count)
                terrainColorReferences.RemoveAt(terrainColorReferences.Count - 1);
            while (newCount > terrainColorReferences.Count)
            {
                if (terrainColorReferences.Count > 0)
                    terrainColorReferences.Add(terrainColorReferences[terrainColorReferences.Count - 1]);
                else
                    terrainColorReferences.Add(Color.white);
            }

            for (int i = 0; i < terrainColorReferences.Count; i++)
            {
                terrainColorReferences[i] = EditorGUILayout.ColorField(terrainColorReferences[i], GUILayout.ExpandWidth(false));
            }
            #endregion

        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        
        #region Errors
        if (terrainReferences.Count != terrainColorReferences.Count)
        {
            EditorGUILayout.HelpBox("Color and reference terrain list are not matching !", MessageType.Warning);
        }
        for (int i = 0; i < terrainReferences.Count; i++)
        {
            if (terrainReferences[i] == null)
            {
                EditorGUILayout.HelpBox("Reference terrain missing !", MessageType.Error);
                break;
            }
        }
        for (int i = 0; i < terrainColorReferences.Count; i++)
        {
            if (terrainColorReferences[i] == Color.clear)
            {
                EditorGUILayout.HelpBox("Reference color missing !", MessageType.Error);
                break;
            }
        }
        #endregion

        #endregion

        GUILayout.Space(50);
        if (GUILayout.Button("Process Data"))
        {
            prefabPath = path;

            Initialize();
        }
    }

    private void Initialize()
    {
        //create object root
        emptyParent = new GameObject();
        emptyParent.name = outputName;
        emptyParent.transform.position = Vector3.zero;

        uniqueObjectParent = new GameObject();
        uniqueObjectParent.transform.SetParent(emptyParent.transform);
        uniqueObjectParent.name = "Unique";

        terrainObjectParent = new GameObject();
        terrainObjectParent.transform.SetParent(emptyParent.transform);
        terrainObjectParent.name = "Terrain";

        color = new Color[modelHeight, width, length];
        pixels = new Pixel[modelHeight, width, length];
        textures = new Texture2D[modelHeight];

        uniqueParents = new List<GameObject>();
        for (int i = 0; i < uniqueObjectsReferences.Count; i++)
        {
            uniqueParents.Add(new GameObject());
            uniqueParents[i].name = uniqueObjectsReferences[i].name;
            uniqueParents[i].transform.SetParent(uniqueObjectParent.transform);
        }

        inputTexture = spr.texture;
        Color[] temp;

        //split spriteSheet
        for (int i = 0; i < modelHeight; i++)
        {
            temp = inputTexture.GetPixels(0, i * length, width, length);
            textures[i] = new Texture2D(width, length);
            textures[i].SetPixels(temp);
        }

        //get every pixel color
        for (int i = 0; i < modelHeight; i++)
        {
            for (int j = 0; j < width; j++)
            {
                for (int k = 0; k < length; k++)
                {
                    color[i, j, k] = textures[i].GetPixel(j, k);
                    if (color[i, j, k].a == 1)
                    {
                        pixels[i, j, k] = new Pixel(j, k, i, color[i, j, k]);
                    }
                }
            }
        }

        differentMeshes = 0;

        finalObjects = new List<GameObject>();
        meshes = new List<Mesh>();

        ProcessData(pixels);
    }

    private void ProcessData(Pixel[,,] input)
    {
        for (int i = 0; i < modelHeight; i++)
        {
            for (int j = 0; j < width; j++)
            {
                for (int k = 0; k < length; k++)
                {
                    //Spawn unique object based on the pixel color
                    for (int l = 0; l < uniqueObjectsColorReferences.Count; l++)
                    {
                        if (input[i, j, k].pixelColor == uniqueObjectsColorReferences[l])
                        {
                            //Instantiate
                            GameObject go = Instantiate(uniqueObjectsReferences[l], pixels[i, j, k].position, uniqueObjectsReferences[l].transform.rotation, uniqueParents[l].transform);

                            //Disable this pixel from being checked in the future
                            input[i, j, k].pixelColor = Color.clear;
                        }
                    }

                    //Split other pixels into clusters of same color & connected  
                    for (int m = 0; m < terrainColorReferences.Count; m++)
                    {
                        if (input[i, j, k].pixelColor == terrainColorReferences[m])
                        {
                            if (!input[i, j, k]._checked && input[i, j, k].pixelColor != Color.clear)
                            {
                                differentMeshes++;
                                input[i, j, k].mesh = differentMeshes;
                                queue.Add(input[i, j, k]);

                                while (queue.Count > 0)
                                    CheckNeigbours(input, queue[0]);
                            }
                        }
                    }
                }
            }
        }

        //part clusters in different lists and then create pointClouds
        clusters = new List<Pixel>[differentMeshes];
        pointClouds = new List<Vector3>[differentMeshes];
        triangles = new List<int>[differentMeshes];

        for (int i = 1; i < differentMeshes + 1; i++)
        {
            clusters[i - 1] = new List<Pixel>();
            foreach (var p in pixels)
            {
                if (p.mesh == i)
                {
                    clusters[i - 1].Add(p);
                    if (!p.northClear && !p.southClear && !p.upClear && !p.downClear && !p.westClear && !p.eastClear)
                    {
                        CheckNeigbours(pixels, p);
                    }
                }
            }
        }



        for (int i = 0; i < differentMeshes; i++)
        {
            meshes.Add(new Mesh());

            for (int j = 0; j < terrainColorReferences.Count; j++)
            {
                Color c = clusters[i][0].pixelColor;
                if (c == terrainColorReferences[j])
                {
                    finalObjects.Add(Instantiate(terrainReferences[j], emptyParent.transform.position, Quaternion.identity, terrainObjectParent.transform));
                }
            }

            var filter = finalObjects[i].GetComponent<MeshFilter>();
            filter.mesh = new Mesh();
            meshes[i] = filter.sharedMesh;

            pointClouds[i] = new List<Vector3>();
            triangles[i] = new List<int>();

            //create points
            foreach (Pixel p in clusters[i])
            {
                Vector3 origin = p.position;


                if (p.southClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, -1);
                }


                if (p.northClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, 1);
                }

                if (p.eastClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, -1);
                }

                if (p.westClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, -1);
                }

                if (p.downClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y - 0.5f, origin.z + 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, 1);
                }

                if (p.upClear)
                {
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x - 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z - 0.5f));
                    pointClouds[i].Add(new Vector3(origin.x + 0.5f, origin.y + 0.5f, origin.z + 0.5f));
                    CreateTriangles(i, pointClouds[i].Count - 1, -1);
                }

            }

            meshes[i].vertices = pointClouds[i].ToArray();
            meshes[i].triangles = triangles[i].ToArray();
            meshes[i].OptimizeIndexBuffers();
            meshes[i].OptimizeReorderVertexBuffer();
            meshes[i].RecalculateNormals();
            meshes[i].RecalculateBounds();

            var collider = finalObjects[i].GetComponent<MeshCollider>();
            collider.sharedMesh = meshes[i];
            

            //SAVE MESH to path
            SaveMesh(meshes[i], outputName + "Mesh_" + (i + 1), path);
        }

        //Change Object Name
        emptyParent.name = outputName;
        Debug.Log("Different meshes : " + differentMeshes);

        prefabPath = prefabPath + "/" + emptyParent.name + ".prefab";
        prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

        PrefabUtility.SaveAsPrefabAssetAndConnect(emptyParent, prefabPath, InteractionMode.UserAction);
    }

    private void CreateTriangles(int mesh, int index, int orientation)
    {
        if (orientation == -1)
        {
            triangles[mesh].Add(index - 2);
            triangles[mesh].Add(index - 1);
            triangles[mesh].Add(index - 3);

            triangles[mesh].Add(index - 2);
            triangles[mesh].Add(index);
            triangles[mesh].Add(index - 1);
        }
        if (orientation == 1)
        {
            triangles[mesh].Add(index - 3);
            triangles[mesh].Add(index - 1);
            triangles[mesh].Add(index - 2);

            triangles[mesh].Add(index - 1);
            triangles[mesh].Add(index);
            triangles[mesh].Add(index - 2);
        }
    }

    private void CheckNeigbours(Pixel[,,] input, Pixel pixel)
    {
        if (queue.Count != 0)
            queue.RemoveAt(0);

        List<Pixel> checkedPixels = new List<Pixel>();

        int x = pixel.x;
        int y = pixel.y;
        int z = pixel.z;
        int mesh = pixel.mesh;

        pixels[z, x, y]._checked = true;

        checkedPixels.Clear();

        //Check above voxel;
        if (z + 1 < modelHeight)
        {
            if (pixels[z + 1, x, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z + 1, x, y]._checked)
            {
                pixels[z + 1, x, y]._checked = true;
                pixels[z + 1, x, y].mesh = mesh;


                checkedPixels.Add(pixels[z + 1, x, y]);
            }

            if (pixels[z + 1, x, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].upClear = true;
            }
        }
        else
        {
            pixels[z, x, y].upClear = true;
        }

        //Check Under voxel
        if (z > 0)
        {
            if (pixels[z - 1, x, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z - 1, x, y]._checked)
            {
                pixels[z - 1, x, y]._checked = true;
                pixels[z - 1, x, y].mesh = mesh;

                checkedPixels.Add(pixels[z - 1, x, y]);
            }

            if (pixels[z - 1, x, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].downClear = true;
            }
        }
        else
        {
            pixels[z, x, y].downClear = true;
        }

        //check west voxel;
        if (x > 0)
        {
            if (pixels[z, x - 1, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x - 1, y]._checked)
            {
                pixels[z, x - 1, y]._checked = true;
                pixels[z, x - 1, y].mesh = mesh;

                checkedPixels.Add(pixels[z, x - 1, y]);
            }

            if (pixels[z, x - 1, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].westClear = true;
            }
        }
        else
        {
            pixels[z, x, y].westClear = true;
        }

        //check east voxel;
        if (x + 1 < width)
        {
            if (pixels[z, x + 1, y].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x + 1, y]._checked)
            {
                pixels[z, x + 1, y]._checked = true;
                pixels[z, x + 1, y].mesh = mesh;

                checkedPixels.Add(pixels[z, x + 1, y]);
            }

            if (pixels[z, x + 1, y].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].eastClear = true;
            }
        }
        else
        {
            pixels[z, x, y].eastClear = true;
        }

        //check south voxel;
        if (y > 0)
        {
            if (pixels[z, x, y - 1].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x, y - 1]._checked)
            {
                pixels[z, x, y - 1]._checked = true;
                pixels[z, x, y - 1].mesh = mesh;

                checkedPixels.Add(pixels[z, x, y - 1]);

            }

            if (pixels[z, x, y - 1].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].southClear = true;
            }
        }
        else
        {
            pixels[z, x, y].southClear = true;
        }

        //check north voxel;
        if (y + 1 < length)
        {
            if (pixels[z, x, y + 1].pixelColor == pixels[z, x, y].pixelColor && !pixels[z, x, y + 1]._checked)
            {
                pixels[z, x, y + 1]._checked = true;
                pixels[z, x, y + 1].mesh = mesh;

                checkedPixels.Add(pixels[z, x, y + 1]);
            }

            if (pixels[z, x, y + 1].pixelColor != pixels[z, x, y].pixelColor)
            {
                pixels[z, x, y].northClear = true;
            }
        }
        else
        {
            pixels[z, x, y].northClear = true;
        }

        for (int i = 0; i < checkedPixels.Count; i++)
        {
        
            queue.Add(checkedPixels[i]); 

        }
    }

    private void SaveMesh(Mesh mesh, string name, string path)
    {
        path = path + "/" + name + ".asset";

        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
    }

    public void GetPalette()
    {
        palette.Clear();
        Texture2D tex = spr.texture;


        for (int i = 0; i < tex.width; i++)
        {
            for (int j = 0; j < tex.height; j++)
            {
                Color c = tex.GetPixel(i, j);
                if (!palette.Contains(c) && c.a != 0)
                {
                    palette.Add(c);
                }
            }
        }
    }

}   



