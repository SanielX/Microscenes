using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

#nullable enable

namespace Microscenes.Editor
{
    /// <summary>
    /// Assign <see cref="OnSelected"/> and add entries you need using <see cref="AddEntry"/>. 
    /// Use <see cref="Build"/> to build context tree.
    /// </summary>
    public class GenericSearchBuilder : ScriptableObject, ISearchWindowProvider
    {
        List<SearchTreeEntry> searchTreeEntries = new List<SearchTreeEntry>(32);
        SearchTree tree = new SearchTree();

        public const int FOLDER_PRIORITY = -1000;

        public string Title = "";
        public Action<SearchTreeEntry, SearchWindowContext>?     OnSelected;
        public Func<SearchTreeEntry, SearchWindowContext, bool>? OnSelectedExit;

        public static Texture DefaultFolderIcon => EditorGUIUtility.IconContent("Folder Icon").image;
        public bool IconFoldersPriority { get => tree.IconFoldersPriority; set => tree.IconFoldersPriority = value; }

        public void Build(bool sort = true)
        {
            searchTreeEntries.Clear();
            searchTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent(Title)));
            tree.ResolveAttributes();

            if (sort)
                tree.Sort();

            tree.Build(searchTreeEntries);
        }

        public void ClearEntries()
        {
            tree.Clear();
        }

        public void SetFolderIcon(Texture tex) => tree.FolderIcon = (_) => tex;
        public void SetFolderIcon(Func<string, Texture> texSelection) => tree.FolderIcon = texSelection;

        public void SetFolderOrderResolveFunc(FolderResolveDelegate func) => tree.FolderResolveAction = func;

        /// <summary>
        /// Use order less than <see cref="FOLDER_PRIORITY"/> to make item appear before folders
        /// </summary>
        public void AddEntry(string path, int order = 0, object? userData = null, Texture? icon = null, Type? type = null)
        {
            tree.Add(path, order, userData, icon, type);
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            return searchTreeEntries;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (OnSelected != null)
            {
                OnSelected.Invoke(entry, context);
                return true;
            }
            else if (OnSelectedExit != null)
            {
                return OnSelectedExit.Invoke(entry, context);
            }

            return true;
        }
    }

    public delegate int FolderResolveDelegate(string fullName, string folderName);

    class SearchTree
    {

        private static Texture2D _indentTexture = null!;
        internal static Texture2D indentTexture
        {
            get
            {
                if (!_indentTexture)
                {
                    _indentTexture = new Texture2D(1, 1);
                    _indentTexture.SetPixel(0, 0, Color.clear);
                    _indentTexture.Apply();
                }

                return _indentTexture;
            }
        }
        private static Dictionary<Type, Texture> iconsDictionary = new Dictionary<Type, Texture>();

        private List<SearchNode> nodes = new List<SearchNode>();

        public bool IconFoldersPriority { get; set; } = true;

        public Func<string, Texture>? FolderIcon { get; set; }
        public FolderResolveDelegate? FolderResolveAction;

        public void Clear()
        {
            nodes.Clear();
        }

        public static Texture GetIcon(Type scriptableObjectType)
        {
            if (iconsDictionary.TryGetValue(scriptableObjectType, out var result))
                return result;

            var tempInstance = ScriptableObject.CreateInstance(scriptableObjectType);
            var preview      = AssetPreview.GetMiniThumbnail(tempInstance);
            iconsDictionary[scriptableObjectType] = preview;

            return preview;
        }

        public void Add(string path, int order, object? userData = null, Texture? icon = null, Type? type = null)
        {
            var pathWords = path.Trim().Split('/');
            if (pathWords.Length == 1 && string.IsNullOrWhiteSpace(pathWords[0]))
            {
                return;
            }
            else if (string.IsNullOrWhiteSpace(pathWords[pathWords.Length - 1]))
            {
                return;
            }

            var currentLevel = nodes;
            for (int i = 0; i < pathWords.Length; i++)
            {
                bool isLeaf = i == (pathWords.Length - 1);
                string word = pathWords[i].Trim();
                var index = IndexOf(currentLevel, word);

                if (index == -1)
                {
                    SearchNode searchNode = new SearchNode()
                    {
                        name       = word,
                        fullPath   = string.Join("/", pathWords, 0, i + 1),
                        order      = isLeaf? order    : GenericSearchBuilder.FOLDER_PRIORITY,
                        userData   = isLeaf? userData : null,
                        icon       = isLeaf? icon     : null,
                        objectType = type
                    };
                    currentLevel.Add(searchNode);
                    currentLevel = searchNode.children;
                }
                else
                {
                    currentLevel = currentLevel[index].children;
                }
            }
        }

        public void ResolveAttributes()
        {
            Resolve(nodes);

            void Resolve(List<SearchNode> nodes)
            {
                foreach (SearchNode node in nodes)
                {
                    ResolveAttributes(node);

                    Resolve(node.children);
                }
            }
        }

        public void Sort()
        {
            // This will make sure that folders are above leaves, which makes stuff easier to read
            List<SearchNode> nodes = this.nodes;
            sort(nodes);

            void sort(List<SearchNode> nodes)
            {
                nodes.Sort((SearchNode node0, SearchNode node1) =>
                {
                    int order0 = node0.order, order1 = node1.order;

                    if (IconFoldersPriority)
                    {
                        bool isFolder0 = node0.children.Count > 0;
                        bool isFolder1 = node1.children.Count > 0;

                        if (isFolder0 && isFolder1)
                        {
                            bool hasIcon0 = node0.icon;
                            bool hasIcon1 = node1.icon;

                            if (hasIcon0 ^ hasIcon1)
                                if (hasIcon0 && !hasIcon1) return -1;
                                else return 1;
                        }
                    }
                    // 
                    // if (isFolder0) order0 += GenericSearchBuilder.FOLDER_PRIORITY;
                    // if (isFolder1) order1 += GenericSearchBuilder.FOLDER_PRIORITY;

                    var cmp = order0.CompareTo(order1);
                    if (cmp == 0)
                        cmp = node0.name.CompareTo(node1.name);

                    return cmp;
                });

                foreach (var n in nodes)
                    sort(n.children);
            }
        }

        public void Build(List<SearchTreeEntry> entries)
        {
            foreach (var rootNode in nodes)
            {
                addEntries(rootNode, 1);
            }

            void addEntries(SearchNode node, int level)
            {
                if (node.children.Count == 0)
                {
                    var customType = node.objectType;
                    
                    GUIContent content = new GUIContent(node.name, node.icon); // level == 1? new GUIContent(node.name, FastCreate.indentTexture) : new GUIContent(node.name);
                    entries.Add(new SearchTreeEntry(content) { level = level, userData = node.userData });
                }
                else
                {
                    entries.Add(new SearchTreeGroupEntry(new GUIContent(node.name, node.icon)) { level = level });
                    foreach (var child in node.children)
                    {
                        addEntries(child, level + 1);
                    }
                }
            }
        }

        void ResolveAttributes(SearchNode node)
        {
            if (node.children.Count == 0)
            {
                if (node.icon is null)
                {
                    node.icon = indentTexture;
                    if (node.objectType != null)
                        node.icon = GetIcon(node.objectType);
                }
            }
            else
            {
                node.icon = FolderIcon is null ? null : FolderIcon.Invoke(node.name);

                if (FolderResolveAction != null)
                    node.order = GenericSearchBuilder.FOLDER_PRIORITY + FolderResolveAction.Invoke(node.fullPath, node.name);
            }
        }

        int IndexOf(List<SearchNode> nodes, string word)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].name == word)
                    return i;
            }

            return -1;
        }
    }

    class SearchNode
    {
        public string   name = null!;
        public string   fullPath = null!;
        public int      order;
        public Texture? icon;
        public object?  userData;
        public Type?    objectType;  // used for icon

        public List<SearchNode> children = new List<SearchNode>();
    }
}