﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;


public class Importer : EditorWindow
{
    private ImportGeneral impGeneral = new ImportGeneral();

    private string[] inputFileMethod = new string[] { "Single", "Directory" };
    private string[] mainCategory = new string[] { "Models", "Sprites", "Textures" };
    private string[] subCategory = new string[] { "Environment", "Props", "Characters", "Buildings", "Others" };

    private int inputFileMethodIndex = 0;
    private int mainCategoryIndex = 0;
    private int subCategoryIndex = 0;

    private string inputFilePath = "";
    private string outputFilePath = "";
    private bool isOverwrite = false;
    private bool isRecursiveImport = false;
    private bool isLayoutCopied = false;
    private bool isIncludeDestructible = false;
    private string targetFileDir = "";

    private bool importerEnabled = false;

    /* Jobs for linking materials and textures */
    private List<ImportBind> bindJobs = new List<ImportBind>();
    private List<ImportBind> deletedJobs = new List<ImportBind>();

    /* Exporting Package */
    private List<string> exportAssetNames = new List<string>();
    private bool shouldExport = false;

    [MenuItem("Window/Importer (Elson)")]
    public static void ShowWindow()
    {
        GetWindow<Importer>("Importer");

    }

    private void OnEnable()
    {
        FBXCustomImporter.customImport = false;

#if UNITY_EDITOR
        EditorApplication.update += OnEditorUpdate;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= OnEditorUpdate;
#endif
    }

    protected virtual void OnEditorUpdate()
    {
        /* Editor requires time to update assets, therefore textures are linked shortly after */
        if (bindJobs.Count > 0)
        {
            for (int i = 0; i < bindJobs.Count; ++i)
            {
                bindJobs[i].bindTimer -= Time.deltaTime;
                if (bindJobs[i].bindTimer <= 0.0f)
                {
                    bindTexturesToMaterials(bindJobs[i]);
                    deletedJobs.Add(bindJobs[i]);
                }
            }



            for (int i = 0; i < deletedJobs.Count; ++i)
                bindJobs.Remove(deletedJobs[i]);
            deletedJobs.Clear();


        }

        if (shouldExport && bindJobs.Count == 0)
            Export();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Asset Importer (Elson)", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Select the appropriate types and setting then click Export. Send the exported package to the programmers.", MessageType.Info);
        EditorGUILayout.Space();



        importerEnabled = EditorGUILayout.Toggle("Enabled", importerEnabled);
        if (importerEnabled)
        {

            mainCategoryIndex = EditorGUILayout.Popup("Main Category", mainCategoryIndex, mainCategory);
            subCategoryIndex = EditorGUILayout.Popup("Sub Category", subCategoryIndex, subCategory);



            /* File Properties */
            inputFileMethodIndex = EditorGUILayout.Popup("Number of Files", inputFileMethodIndex, inputFileMethod);
            EditorGUILayout.Space();
            isOverwrite = EditorGUILayout.Toggle("Overwrite Existing Assets", isOverwrite);
            FBXCustomImporter.customImport = EditorGUILayout.Toggle("Custom FBX Import", FBXCustomImporter.customImport);
            FBXCustomImporter.autoScale = EditorGUILayout.Toggle("Autoscale FBX (x100)", FBXCustomImporter.autoScale);

            if (inputFileMethod[inputFileMethodIndex] == "Directory")
                isIncludeDestructible = EditorGUILayout.Toggle("Include Shattered", isIncludeDestructible);

            EditorGUILayout.Space();



            /* Input File */
            if (inputFileMethod[inputFileMethodIndex] == "Single")
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Input File", inputFilePath, GUILayout.MinWidth(150));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Browse", GUILayout.MinWidth(80)))
                    inputFilePath = EditorUtility.OpenFilePanel("Import Assets", inputFilePath, "");

                EditorGUILayout.EndHorizontal();
            }
            else if (inputFileMethod[inputFileMethodIndex] == "Directory")
            {
                isRecursiveImport = EditorGUILayout.Toggle("Recursive Import", isRecursiveImport);
                isLayoutCopied = EditorGUILayout.Toggle("Copy Directory's Layout", isLayoutCopied);
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Input Directory", inputFilePath, GUILayout.MinWidth(150));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Browse", GUILayout.MinWidth(80)))
                    inputFilePath = EditorUtility.OpenFolderPanel("Import Assets From", inputFilePath, "");
                EditorGUILayout.EndHorizontal();
            }






            /* Output Directory*/
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output Directory", outputFilePath, GUILayout.MinWidth(150));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.MinWidth(80)))
                outputFilePath = EditorUtility.SaveFilePanel("Save As", Application.dataPath, "Exported", "unitypackage");
            EditorGUILayout.EndHorizontal();




            EditorGUILayout.Space();


            if (GUILayout.Button("Import"))
            {
                Import();
                AssetDatabase.Refresh();
                if (FBXCustomImporter.customImport)
                    importFBX();
            }



            if (GUILayout.Button("Export"))
                ExportAll();


            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Programmer's Only", GUILayout.MinWidth(150));

            if (GUILayout.Button("Export Imported Files"))
                Export();

            if (GUILayout.Button("Package"))
            {

                Import();
                AssetDatabase.Refresh();

                if (FBXCustomImporter.customImport)
                {
                    importFBX();
                    shouldExport = true;
                }
                else
                {
                    Export();
                }

            }

        }

        if (GUILayout.Button("Clean up PhotonViews"))
            Cleanup();

        if (GUILayout.Button("Clean up DamageData"))
            CleanUpDamageData();
    }

    public string getCategoryDir(bool relativeToProject)
    {
        string path = "";

        if (!relativeToProject)
            path = Application.dataPath + "/";
        else
            path = "Assets/";

        path += mainCategory[mainCategoryIndex] + "/" + subCategory[subCategoryIndex];

        return path;
    }

    public string getSubCategoryDir()
    {
        return subCategory[subCategoryIndex];
    }

    private string getRelativePath(string line)
    {
        int pos = line.IndexOf("Assets");
        return line.Substring(pos, line.Length - pos).Replace('\\', '/');
    }

    /* Assets/Materials/Environment/BuildingA/Building.fbx to Environment/BuildingA */
    private string getSubCategoryFromPath(string line, bool isRelativeToProject)
    {
        if (!isRelativeToProject)
            line = getRelativePath(line);

        line = line.Replace('\\', '/');
        int firstPos = line.IndexOf('/');
        line = line.Substring(firstPos, line.Length - firstPos);
        int lastPos = line.LastIndexOf('/');
        line = line.Substring(1, lastPos);
        firstPos = line.IndexOf('/');
        line = line.Substring(firstPos + 1, line.Length - firstPos - 2);
        return line;
    }

    public void Cleanup()
    {
        string[] dirToClean = { "Assets/Prefabs/Buildings", "Assets/Prefabs/Environment", "Assets/Prefabs/Props", "Assets/Prefabs/Generation/Areas" };

        int count = 0;
        foreach (string path in dirToClean)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] info = dir.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (FileInfo f in info)
            {
                if (!f.ToString().EndsWith(".meta"))
                {
                    string assetPath = getRelativePath(f.ToString());
                    GameObject test = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath));
                    PrefabUtility.UnpackPrefabInstance(test, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                    Transform[] children = test.GetComponentsInChildren<Transform>();

                    foreach (Transform t in children)
                    {
                        PhotonView[] views = t.GetComponents<PhotonView>();

                        foreach(PhotonView v in views)
                        {
                            v.Synchronization = ViewSynchronization.UnreliableOnChange;
                        }

                        if (views.Length > 1)
                        {
                            //foreach(PhotonView view in views)
                            //{
                            //    if(view.ObservedComponents.Count == 0)
                            //}
                            if (views[0].ObservedComponents.Count == 0 && views[1].ObservedComponents.Count == 0)
                            {
                                Debug.Log("Something wrong: " + t.name);
                            }
                            else if (views[0].ObservedComponents.Count == 2 && views[1].ObservedComponents.Count == 2)
                            {
                                Debug.Log("Duplicate: " + t.name);
                            }
                            else
                            {
                                int toDelete = -1;

                                for (int i = 0; i < views.Length; ++i)
                                {
                                    if (views[i].ObservedComponents.Count == 0 || (views[i].ObservedComponents.Count == 1 && views[i].ObservedComponents[0] == null))
                                    {
                                        toDelete = i;
                                        break;
                                    }
                                }

                                if (toDelete == -1)
                                {
                                    Debug.Log("Multiple full: " + t.name);
                                    for (int i = 0; i < views.Length; ++i)
                                    {
                                        Debug.Log(i + ": " + views[i].ObservedComponents.Count);
                                        if (views[i].ObservedComponents.Count == 1)
                                            Debug.Log("Comp: " + views[i].ObservedComponents[0]);
                                    }

                                }
                                else
                                {
                                    ++count;
                                    Debug.Log("Cleaned: " + t.name);
                                    DestroyImmediate(views[toDelete]);


                                }
                            }

                        }

                    }
                    PrefabUtility.SaveAsPrefabAssetAndConnect(test, assetPath, InteractionMode.AutomatedAction);
                    DestroyImmediate(test);
                }
            }
        }
        Debug.Log("Cleaned up: " + count);
    }

    public void CleanUpDamageData()
    {
        string[] dirToClean = { "Assets/Prefabs/Buildings", "Assets/Prefabs/Environment", "Assets/Prefabs/Props", "Assets/Prefabs/Generation/Areas" };

        int count = 0;
        foreach (string path in dirToClean)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] info = dir.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (FileInfo f in info)
            {
                if (!f.ToString().EndsWith(".meta"))
                {
                    string assetPath = getRelativePath(f.ToString());
                    GameObject test = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath));
                    PrefabUtility.UnpackPrefabInstance(test, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                    Transform[] children = test.GetComponentsInChildren<Transform>();

                    foreach (Transform t in children)
                    {
                        PhotonView[] views = t.GetComponents<PhotonView>();

                        foreach (PhotonView v in views)
                        {
                            List<Component> components = v.ObservedComponents;
                            if(components.Count > 0)
                            {
                                foreach(Component c in components)
                                {
                                    ThrowableDamageData damageData = c as ThrowableDamageData;
                                    if (damageData != null)
                                    {
                                        ++count;
                                        components.Remove(damageData);
                                        break;
                                    }
                                }
                            }
                        }

                        

                    }
                    PrefabUtility.SaveAsPrefabAssetAndConnect(test, assetPath, InteractionMode.AutomatedAction);
                    DestroyImmediate(test);
                }
            }
        }
        Debug.Log("Cleaned up: " + count);
    }

    public void Import()
    {

        targetFileDir = getCategoryDir(false);

        /* Create target directory if it doesn't exist */
        if (!Directory.Exists(targetFileDir))
            Directory.CreateDirectory(targetFileDir);

        if (inputFileMethod[inputFileMethodIndex] == "Single")
            impGeneral.ImportFile(inputFilePath, targetFileDir, isOverwrite, ref exportAssetNames);
        else
            impGeneral.ImportDir(inputFilePath, targetFileDir, isOverwrite, isRecursiveImport, isLayoutCopied, ref exportAssetNames);


    }


    public void Export()
    {
        shouldExport = false;
        addDirToExport("Assets/Prefabs/ProceduralBuilding/BuildingVariants");

        string[] assetNames = exportAssetNames.ToArray();

        if (assetNames.Length == 0)
        {
            Debug.LogError("[Importer] Nothing to export! Have you imported first and set output directory?");
            return;
        }

        exportAssetNames.Clear();
        AssetDatabase.ExportPackage(assetNames, outputFilePath, ExportPackageOptions.IncludeDependencies);
        Debug.Log("[Importer] Exported Package!");
    }

    public void ExportAll()
    {
        shouldExport = false;

        addDirToExport("Assets/Prefabs/ProceduralBuilding/BuildingVariants");
        for (int i = 0; i < mainCategory.Length; ++i)
            for (int j = 0; j < subCategory.Length; ++j)
                addDirToExport("Assets/" + mainCategory[i] + "/" + subCategory[j]);

        string[] assetNames = exportAssetNames.ToArray();

        if (assetNames.Length == 0)
        {
            Debug.LogError("[Importer] Nothing to export! Have you imported first and set output directory?");
            return;
        }

        exportAssetNames.Clear();
        AssetDatabase.ExportPackage(assetNames, outputFilePath, ExportPackageOptions.IncludeDependencies);
        Debug.Log("[Importer] Exported Package!");
    }

    public void addDirToExport(string path)
    {
        if (!Directory.Exists(path)) return;

        DirectoryInfo dir = new DirectoryInfo(path);
        FileInfo[] info = dir.GetFiles("*.*", SearchOption.AllDirectories);
        foreach (FileInfo f in info)
            if (!f.ToString().EndsWith(".meta"))
                exportAssetNames.Add(getRelativePath(f.ToString()));
    }

    public void importFBX()
    {

        bindJobs.Clear();
        string relativeFileDir = getCategoryDir(true);

        if (inputFileMethod[inputFileMethodIndex] == "Single")
        {
            int pos = inputFilePath.LastIndexOf('/');
            string name = inputFilePath.Substring(pos + 1, inputFilePath.Length - pos - 1);
            string rawName = name.Substring(0, name.IndexOf('.'));
            string modelPath = relativeFileDir + "/" + name;
            AssetDatabase.Refresh();
            extractMaterials(rawName, modelPath, "");
        }
        else
        {
            DirectoryInfo dir = new DirectoryInfo(inputFilePath);
            FileInfo[] info;

            if (isRecursiveImport)
                info = dir.GetFiles("*.fbx*", SearchOption.AllDirectories);
            else
                info = dir.GetFiles("*.fbx*");


            string inputFormatted = inputFilePath.Replace('/', '\\');

            foreach (FileInfo f in info)
            {

                string layoutCategory = f.DirectoryName;
                layoutCategory = layoutCategory.Replace(inputFormatted, "");
                layoutCategory = layoutCategory.Replace('\\', '/');

                string modelPath = relativeFileDir;

                if (isLayoutCopied && layoutCategory != "")
                    modelPath += layoutCategory + "/";
                else
                    modelPath += "/";

                modelPath += f.Name;

                string rawName = f.Name.Substring(0, f.Name.IndexOf('.'));
                extractMaterials(rawName, modelPath, layoutCategory);
            }
        }

    }


    public void extractMaterials(string rawName, string assetPath, string layoutCategory)
    {

        string categoryPath = getSubCategoryDir();

        if (isLayoutCopied)
            categoryPath += layoutCategory;

        string destinationPath = "Assets/Materials/";


        destinationPath += categoryPath + "/" + rawName;

        HashSet<string> assetsToReload = new HashSet<string>();
        HashSet<string> hashSet = new HashSet<string>();
        IEnumerable<Object> enumerable = from x in AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                         where x.GetType() == typeof(Material)
                                         select x;

        if (!Directory.Exists(destinationPath))
            Directory.CreateDirectory(destinationPath);

        /* Create Materials */
        foreach (Object item in enumerable)
        {

            string path = destinationPath + "/" + item.name + ".mat";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            string value = AssetDatabase.ExtractAsset(item, path);

            /* Add to list of assets to export */
            exportAssetNames.Add(path);

            if (string.IsNullOrEmpty(value))
            {
                assetsToReload.Add(path);
                hashSet.Add(assetPath);
            }
        }

        foreach (string item2 in hashSet)
        {
            AssetDatabase.WriteImportSettingsIfDirty(item2);
            AssetDatabase.ImportAsset(item2, ImportAssetOptions.ForceUpdate);
        }

        /* Set up prefab directory */
        string prefabPath = "Assets/Prefabs/" + categoryPath + "/Resources";

        if (!Directory.Exists(prefabPath))
            Directory.CreateDirectory(prefabPath);

        if (isIncludeDestructible)
        {
            if (destinationPath.EndsWith("_Shattered"))
            {

                /* Instantiate shattered version */
                string shatteredPrefabPath = prefabPath + "/" + rawName + "/Resources/";

                if (!Directory.Exists(shatteredPrefabPath))
                    Directory.CreateDirectory(shatteredPrefabPath);

                GameObject shattered = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath));
                PrefabUtility.UnpackPrefabInstance(shattered, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                string nonShatteredName = rawName.Substring(0, rawName.LastIndexOf("_"));
                string nonnShatteredAssetPath = "Assets/Models/" + categoryPath + "/" + nonShatteredName + ".fbx";
                string nonShatteredPrefabPath = prefabPath + "/" + nonShatteredName + ".prefab";

                /* Instantiate non-shattered version */
                GameObject nonShattered = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadMainAssetAtPath(nonnShatteredAssetPath));
                PrefabUtility.UnpackPrefabInstance(nonShattered, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                /* Change relevant properties */
                nonShattered.AddComponent<Rigidbody>();
                MeshCollider meshCollider = nonShattered.AddComponent<MeshCollider>();
                meshCollider.convex = true;

                /* Link to the shattered prefab, not gameobject */
                Destructible destructible = nonShattered.AddComponent<Destructible>();
                destructible.destroyed = new List<GameObject>();

                Transform parent = shattered.transform;
                parent.position = Vector3.zero;

                for (int i = 0; i < parent.childCount; ++i)
                {
                    GameObject childGO = parent.GetChild(i).gameObject;
                    childGO.AddComponent<Rigidbody>();
                    MeshCollider collider = childGO.AddComponent<MeshCollider>();
                    collider.convex = true;
                    string currentShardPath = shatteredPrefabPath + childGO.name + ".prefab";
                    Debug.Log(currentShardPath);
                    PrefabUtility.SaveAsPrefabAssetAndConnect(childGO, currentShardPath, InteractionMode.AutomatedAction);
                    destructible.destroyed.Add((GameObject)AssetDatabase.LoadMainAssetAtPath(currentShardPath));
                }

                /* Save non-shattered prefab */
                PrefabUtility.SaveAsPrefabAssetAndConnect(nonShattered, nonShatteredPrefabPath, InteractionMode.AutomatedAction);

                /* Destroy both GameObjects in scene */
                DestroyImmediate(shattered);
                DestroyImmediate(nonShattered);

                /* Add to export list */
                exportAssetNames.Add(shatteredPrefabPath);
                exportAssetNames.Add(nonShatteredPrefabPath);
            }
        }


        if (!destinationPath.EndsWith("_Shattered"))
        {
            /* Instantiate clone in scene */
            GameObject clone = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath));
            PrefabUtility.UnpackPrefabInstance(clone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Transform parent = clone.transform;
            parent.position = Vector3.zero;

            string prefabFilePath = prefabPath + "/" + rawName + ".prefab";

            Debug.Log(prefabFilePath);
            /* Save clone as prefab */
            PrefabUtility.SaveAsPrefabAssetAndConnect(clone, prefabFilePath, InteractionMode.AutomatedAction);
            DestroyImmediate(clone);
            exportAssetNames.Add(prefabFilePath);
        }



        /* Set up texture binding */
        try
        {
            AssetDatabase.StartAssetEditing();
            ImportBind job = new ImportBind();

            /* Add all loaded materials to the material collection */
            foreach (string materialPath in assetsToReload)
            {
                //Material mat = AssetDatabase.LoadAssetAtPath(materialPath, typeof(Material)) as Material;
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (mat != null)
                    job.materials.Add(mat);
            }



            /* Find if any texture matches the material */
            string targetTextureDir = Application.dataPath + "/Textures/" + categoryPath + "/" + rawName;

            /* Terminate if there are no textures for this FBX */
            if (!Directory.Exists(targetTextureDir))
                return;

            string[] acceptedExtensions = new string[2] { ".jpg", ".png" };

            string targetTexturePath = "";
            DirectoryInfo dir = new DirectoryInfo(targetTextureDir);
            FileInfo[] info = dir.GetFiles("*.*");

            foreach (FileInfo f in info)
            {
                if (acceptedExtensions.Contains(f.Extension.ToLower()))
                {
                    /* Add to list of assets to export */
                    exportAssetNames.Add(getRelativePath(f.ToString()));

                    if (targetTexturePath == "")
                        targetTexturePath = f.ToString();
                }
            }

            /* Terminate if no texture found */
            if (string.IsNullOrEmpty(targetTexturePath))
                return;

            /* Trim path to be relative to project */
            targetTexturePath = getRelativePath(targetTexturePath);

            /* Add Job */
            job.textureFilePath = targetTexturePath;
            bindJobs.Add(job);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

    }


    private void bindTexturesToMaterials(ImportBind job)
    {

        AssetDatabase.Refresh();
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(job.textureFilePath);
        if (tex == null) return;

        foreach (Material m in job.materials)
            m.mainTexture = tex;

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        if (isIncludeDestructible)
        {
            //Debug.Log("END: " + job.textureFilePath);
            //if (job.textureFilePath.EndsWith("_Shattered"))
            //    Debug.Log(job.textureFilePath);
        }
    }



}
