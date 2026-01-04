using System;
using System.IO;
using UnityEngine;

namespace Scripts.Utility
{
    public static class WriteAndReadFile
    {
        public static void ReadFileCreateObject(ref FileInfo[] fileInfos)
        {
            string path = "Assets";

            DirectoryInfo folder = new DirectoryInfo(path);

            fileInfos = folder.GetFiles("*.json");

            foreach (FileInfo filePath in fileInfos)
            {
                Debug.Log(filePath.FullName);
            }
        }

        public static T CreateObject<T>(FileInfo fileInfo)
        {
            StreamReader reader = new StreamReader(fileInfo.FullName);

            string fileString = reader.ReadToEnd();

            string createTime = DateTime.Now.ToString("HH:mm:ss");

            string createFileName = createTime + " - " + Path.GetFileNameWithoutExtension(fileInfo.Name);

            T createObject = JsonUtility.FromJson<T>(fileString);

            return createObject;

            Debug.Log(createObject);
        }

        public static void WriteFile<T>(T writeObject)
        {
            string fileString = JsonUtility.ToJson(writeObject);

#if UNITY_EDITOR
            string filepath = Application.dataPath + "/" + "fileName" + ".json";
#else
            string filepath = Application.persistentDataPath + "/" + "fileName" + ".json";
#endif
            TextWriter textWriter = new StreamWriter(filepath, false);

            textWriter.WriteLine(fileString);

            textWriter.Close();
        }

        // private void createScriptableObject<T>(FileInfo fileInfo)
        // {
        //     StreamReader reader = new StreamReader(fileInfo.FullName);
        //
        //     string fileString = reader.ReadToEnd();
        //
        //     AnimationMapDataGroup animationMapDataGroup = JsonUtility.FromJson<AnimationMapDataGroup>(fileString);
        //
        //     string avatarPath = "Path" + "/" + "File Name" + ".asset";
        //
        //     //生成 
        //     ScriptableObject asset = ScriptableObject.CreateInstance<AnimationMapDataSo>();
        //
        //     //赋值
        //     AnimationMapDataSo animationMapDataSo = asset as AnimationMapDataSo;
        //
        //     if (animationMapDataSo is not null)
        //     {
        //         animationMapDataSo.AnimationMapDataGroup = animationMapDataGroup;
        //     }
        //
        //     //创建写入
        //     AssetDatabase.CreateAsset(asset, avatarPath);
        //
        //     AssetDatabase.SaveAssets();
        //
        //     AssetDatabase.Refresh();
        // }
    }
}