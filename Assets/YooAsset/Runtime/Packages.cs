using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace YooAsset
{
    public class PackageVersion
    {
        public string _name;
        public string _version;
    }

    public class PackageInfo
    {
        public string _name { get; set; }
        public string _author { get; set; }
        public string _description { get; set; }
        public string _version { get; set; }
        public string _unityVersion { get; set; }
        public int _platforms { get; set; }
        public int _content { get; set; }
        public bool _fullScene { get; set; }
        public string _sceneName { get; set; }
        public string _assemblyName { get; set; }
        public string _managerClassName { get; set; }
        public List<string> _sceneObjs { get; set; }
        public List<PackageVersion> _dependencies { get; set; }
    }
}

