using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ProtoCreator : EditorWindow
{
    private const string ProtoFilesPath = "./Assets/Editor/ProtoCreator/ProtoFiles/";
    private const string GenerateFilesPath = "./Assets/Editor/ProtoCreator/GenerateFiles/";
    private const string protocPath = "/Editor/ProtoCreator/ProtocolBuffersCSharpBuilder/protobuf-bin/protoc.exe";
    private const string protocC4Path = "/Editor/ProtoCreator/ProtocolBuffersCSharpBuilder/protobuf-bin-c#/protogen.exe";


    private VisualTreeAsset ItemImportItemTemplate;
    private VisualTreeAsset ItemListTempTemplate;
    private VisualTreeAsset ItemFieldTempTemplate;
    private VisualTreeAsset ItemEnumValueTempTemplate;
    private VisualTreeAsset ItemServerTempTemplate;


    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    private List<TreeNodeData> listTreeNodeData = new List<TreeNodeData>();


    private TreeView _treeProto;
    private TextField _txtSyntax;
    private TextField _txtPackageName;
    private TextField _txtNewProtoName;



    private ListView _listImport;
    private ListView _listMessges;
    private ListView _listEnum;
    private ListView _listServices;



    ProtoParser protoParser = new ProtoParser();
    ProtoDetail _CurData;
    string _CurDataPath;
    TreeNodeData _CurTreeNodeData;


    [MenuItem("Tools/ProtoCreator")]
    public static void ShowExample()
    {
        ProtoCreator wnd = GetWindow<ProtoCreator>();
        wnd.titleContent = new GUIContent("ProtoCreator");
    }

    public void CreateGUI()
    {
        ItemImportItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ProtoCreator/ItemTemplate/ItemImportTemp.uxml");
        ItemListTempTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ProtoCreator/ItemTemplate/ItemListTemp.uxml");
        ItemFieldTempTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ProtoCreator/ItemTemplate/ItemFieldTemp.uxml");
        ItemEnumValueTempTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ProtoCreator/ItemTemplate/ItemEnumValueTemp.uxml");
        ItemServerTempTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/ProtoCreator/ItemTemplate/ItemServerTemp.uxml");

        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        _treeProto = root.Q<TreeView>("treeProto");

        _txtNewProtoName = root.Q<TextField>("textNewName"); 
        _txtSyntax = root.Q<TextField>("txtSyntax");
        _txtSyntax.RegisterCallback<ChangeEvent<string>>((evt) =>
        {
            if(_CurData != null)
            {
                _CurData.Syntax = evt.newValue;
            }
        });
        _txtPackageName = root.Q<TextField>("txtPackageName");
        _txtPackageName.RegisterCallback<ChangeEvent<string>>((evt) =>
        {
            if (_CurData != null)
            {
                _CurData.PackageName = evt.newValue;
            }
        });
        _listImport = root.Q<ListView>("ListImport");
        _listImport.makeItem = () => { return ItemImportItemTemplate.CloneTree(); };
        _listImport.bindItem = ImportBindItem;
        root.Q<Button>("BtnImportAdd").RegisterCallback<MouseUpEvent>(OnImportAdd);

        _listMessges = root.Q<ListView>("ListMessages");
        _listMessges.makeItem = () => { return ItemListTempTemplate.CloneTree(); };
        _listMessges.bindItem = MessgesBindItem;
        root.Q<Button>("BtnMessagesAdd").RegisterCallback<MouseUpEvent>(OnMessageAdd);


        _listEnum = root.Q<ListView>("ListEnums");
        _listEnum.makeItem = () => { return ItemListTempTemplate.CloneTree(); };
        _listEnum.bindItem = EnumBindItem;
        root.Q<Button>("BtnEnumsAdd").RegisterCallback<MouseUpEvent>(OnEnumsAdd);


        _listServices = root.Q<ListView>("ListServices");
        _listServices.makeItem = () => { return ItemListTempTemplate.CloneTree(); };
        _listServices.bindItem = ServerBindItem;
        root.Q<Button>("BtnServicesAdd").RegisterCallback<MouseUpEvent>(OnServerAdd);

        root.Q<Button>("btnSave").RegisterCallback<MouseUpEvent>(SaveProtoDetail);
        root.Q<Button>("btnSaveAll").RegisterCallback<MouseUpEvent>(SaveAllProtoDetail);
        root.Q<Button>("btnAddNew").RegisterCallback<MouseUpEvent>(AddProtoDetail);
        root.Q<Button>("btnDelete").RegisterCallback<MouseUpEvent>(DeleteProtoDetail);
        root.Q<Button>("btnCreat").RegisterCallback<MouseUpEvent>(CreatProtoDetail);
        root.Q<Button>("btnCreatAll").RegisterCallback<MouseUpEvent>(CreatAllProtoDetail);



        LoadData();



    }


    private void LoadData()
    {
        int index = 0;
        List<TreeViewItemData<TreeNodeData>> items = ListDirectory(ProtoFilesPath, ref index);
        _treeProto.SetRootItems(items);
        _treeProto.makeItem = () => new Label();
        _treeProto.bindItem = TreeBindItem;
        _treeProto.itemsChosen += OnItemSelect;
        _treeProto.selectedIndicesChanged += OnItemSelectChange;
    }
    private void TreeBindItem(VisualElement e, int i)
    {
        TreeNodeData item = _treeProto.GetItemDataForIndex<TreeNodeData>(i);
        if(item.type == 1)
        {
            (e as Label).text = item.name + ".proto";
        }
        else
        {
            (e as Label).text = "Folder - " + item.name ;

        }
    }
    private void OnItemSelect(object obj)
    {
        foreach (var item in (IEnumerable<dynamic>)obj)
        {
            TreeNodeData data = (TreeNodeData)item;
            _CurTreeNodeData = data;
            if (data.type == 1)
            {
                _CurDataPath = data.path;
                ShowItem(data.protoDetail);
            }
        }
    }

    private void OnItemSelectChange(IEnumerable<int> indexs)
    {
        foreach (int index in indexs)
        {
            TreeNodeData item = _treeProto.GetItemDataForIndex<TreeNodeData>(index);
            _CurTreeNodeData = item;

        }

    }


    void ShowItem(ProtoDetail data)
    {
        _CurData = data;
        _txtSyntax.value = data.Syntax;
        _txtPackageName.value = data.PackageName;
        _listImport.itemsSource = data.Imports;
        _listImport.Rebuild();
        _listMessges.itemsSource = data.Messages;
        _listMessges.Rebuild();
        _listEnum.itemsSource = data.Enums;
        _listEnum.Rebuild();
        _listServices.itemsSource = data.Services;
        _listServices.Rebuild();
    }

    #region Import
    private void ImportBindItem(VisualElement e, int i)
    {
        if (_CurData != null)
        {
            TextField textimport = e.Q<TextField>("textimport");
            textimport.value = _CurData.Imports[i];
            textimport.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                _CurData.Imports[i] = evt.newValue;
            });
            Button btnRemove = e.Q<Button>("btnRemove");
            btnRemove.userData = i;
            btnRemove.RegisterCallback<MouseUpEvent>(OnImportRemove);
        }
    }

    private void OnImportAdd(MouseUpEvent e)
    {
        _CurData.Imports.Add("NewImport");
        _listImport.Rebuild();

    }
    private void OnImportRemove(MouseUpEvent e)
    {
        int index = (int)((Button)e.currentTarget).userData;
        if (index < _CurData.Imports.Count)
        {
            _CurData.Imports.RemoveAt(index);
            _listImport.Rebuild();
        }
        else
        {
            Debug.Log("import index Out");
        }
    }
    #endregion

    #region Messges

    private void MessgesBindItem(VisualElement e, int i)
    {
        if (_CurData != null)
        {
            if (i >= _CurData.Messages.Count)
            {
                Debug.Log("Message index Out");
                return;

            }

            MessageDetail messageDetail = _CurData.Messages[i];

            TextField textName = e.Q<TextField>("textName");
            textName.value = messageDetail.Name;
            textName.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                messageDetail.Name = evt.newValue;
            });

            Button btnRemove = e.Q<Button>("btnRemove");
            btnRemove.userData = messageDetail;
            btnRemove.RegisterCallback<MouseUpEvent>(OnMessageRemove);
            ListView listField = e.Q<ListView>("ListField");
            listField.makeItem = () =>
            {
                VisualElement e = ItemFieldTempTemplate.CloneTree();
                e.userData = messageDetail;
                return e;
            };
            listField.bindItem = FieldBindItem;
            listField.itemsSource = _CurData.Messages[i].Fields;
            listField.Rebuild();

            Button btnAdd = e.Q<Button>("btnAdd");
            btnAdd.RegisterCallback<MouseUpEvent>((evt)=> {
                messageDetail.Fields.Add(new FieldDetail());
                listField.Rebuild();
            });


        }
    }

    private void OnMessageAdd(MouseUpEvent e)
    {
        MessageDetail a = new MessageDetail();
        a.Name = "NewMessage";
        _CurData.Messages.Add(a);
        _listMessges.Rebuild();
    }

    private void OnMessageRemove(MouseUpEvent e)
    {
        MessageDetail messageDetail = (MessageDetail)((Button)e.currentTarget).userData;
        _CurData.Messages.Remove(messageDetail);
        _listMessges.Rebuild();
    }

    private void FieldBindItem(VisualElement e, int i)
    {
        MessageDetail messageDetail = (MessageDetail)e.userData;
        if (i < messageDetail.Fields.Count)
        {
            HandleFieldDetail(e, messageDetail, i);
        }
    }

    private void HandleFieldDetail(VisualElement e, MessageDetail messageDetail, int filedIndex)
    {
        FieldDetail filed = messageDetail.Fields[filedIndex];

        e.Q<Button>("btnRemove").RegisterCallback<MouseUpEvent>((evt) => {
            messageDetail.Fields.Remove(filed);
            ((ListView)(e.parent.parent)).Rebuild();
        });

        EnumField enumField = e.Q<EnumField>("enumField");
        enumField.value = StringToProtoFiledModifierType(filed.Modifier);
        enumField.RegisterCallback<ChangeEvent<Enum>>((evt) =>
        {
            filed.Modifier = evt.newValue.ToString();
        });

        TextField textType = e.Q<TextField>("textType");
        textType.value = filed.Type;
        textType.RegisterCallback<ChangeEvent<string>>((evt) =>
        {
            filed.Type = evt.newValue;
        });

        TextField textName = e.Q<TextField>("textName");
        textName.value = filed.Name;
        textName.RegisterCallback<ChangeEvent<string>>((evt) =>
        {
            filed.Name = evt.newValue;
        });

        IntegerField textNumber = e.Q<IntegerField>("textNumber");
        textNumber.value = filed.Number;
        textNumber.RegisterCallback<ChangeEvent<int>>((evt) =>
        {
            filed.Number = evt.newValue;
        });
    }


    #endregion

    #region Enum


    private void EnumBindItem(VisualElement e, int i)
    {
        if (_CurData != null)
        {
            if (i >= _CurData.Enums.Count)
            {
                Debug.Log("Enums index Out");
                return;

            }
            EnumDetail enumDetail = _CurData.Enums[i];

            TextField textName = e.Q<TextField>("textName");
            textName.value = enumDetail.Name;
            textName.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                enumDetail.Name = evt.newValue;
            });

            Button btnRemove = e.Q<Button>("btnRemove");
            btnRemove.userData = enumDetail;
            btnRemove.RegisterCallback<MouseUpEvent>(OnEnumsRemove);

            ListView listField = e.Q<ListView>("ListField");
            listField.makeItem = () =>
            {
                VisualElement e = ItemEnumValueTempTemplate.CloneTree();
                e.userData = enumDetail;
                return e;
            };
            listField.bindItem = EnumsValueBindItem;
            listField.itemsSource = _CurData.Enums[i].Values;
            listField.Rebuild();

            Button btnAdd = e.Q<Button>("btnAdd");
            btnAdd.RegisterCallback<MouseUpEvent>((evt) => {
                enumDetail.Values.Add(new EnumValueDetail());
                listField.Rebuild();
            });
        }
    }

    private void EnumsValueBindItem(VisualElement e, int i)
    {
        EnumDetail EnumDetail = (EnumDetail)e.userData;
        if (i < EnumDetail.Values.Count)
        {
            EnumValueDetail enumValueDetail = EnumDetail.Values[i];
            e.Q<Button>("btnRemove").RegisterCallback<MouseUpEvent>((evt) => {
                EnumDetail.Values.Remove(enumValueDetail);
                ((ListView)(e.parent.parent)).Rebuild();
            });

            TextField textName = e.Q<TextField>("textName");
            textName.value = enumValueDetail.Name;
            textName.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                enumValueDetail.Name = evt.newValue;
            });

            IntegerField textValue = e.Q<IntegerField>("textValue");
            textValue.value = enumValueDetail.Value;
            textValue.RegisterCallback<ChangeEvent<int>>((evt) =>
            {
                enumValueDetail.Value = evt.newValue;
            });

        }
    }

    private void OnEnumsAdd(MouseUpEvent e)
    {
        EnumDetail a = new EnumDetail();
        a.Name = "NewEnum";
        _CurData.Enums.Add(a);
        _listEnum.Rebuild();
    }

    private void OnEnumsRemove(MouseUpEvent e)
    {
        EnumDetail enumDetail = (EnumDetail)((Button)e.currentTarget).userData;
        _CurData.Enums.Remove(enumDetail);
        _listEnum.Rebuild();

    }
    #endregion

    #region Server

    private void ServerBindItem(VisualElement e, int i)
    {
        if (_CurData != null)
        {
            if (i >= _CurData.Services.Count)
            {
                Debug.Log("Services index Out");
                return;

            }
            ServiceDetail serviceDetail = _CurData.Services[i];
            TextField textName = e.Q<TextField>("textName");
            textName.value = serviceDetail.Name;
            textName.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                serviceDetail.Name = evt.newValue;
            });

            Button btnRemove = e.Q<Button>("btnRemove");
            btnRemove.userData = serviceDetail;
            btnRemove.RegisterCallback<MouseUpEvent>(OnServicesRemove);
            ListView listField = e.Q<ListView>("ListField");
            listField.makeItem = () =>
            {
                VisualElement e = ItemServerTempTemplate.CloneTree();
                e.userData = serviceDetail;
                return e;
            };
            listField.bindItem = ServerMethodsBindItem;
            listField.itemsSource = _CurData.Services[i].Methods;
            listField.Rebuild();

            Button btnAdd = e.Q<Button>("btnAdd");
            btnAdd.RegisterCallback<MouseUpEvent>((evt) => {
                serviceDetail.Methods.Add(new MethodDetail());
                listField.Rebuild();
            });
        }
    }

    private void ServerMethodsBindItem(VisualElement e, int i)
    {
        ServiceDetail serviceDetail = (ServiceDetail)e.userData;
        if (i < serviceDetail.Methods.Count)
        {
            MethodDetail methodDetail = serviceDetail.Methods[i];

            e.Q<Button>("btnRemove").RegisterCallback<MouseUpEvent>((evt) => {
                serviceDetail.Methods.Remove(methodDetail);
                ((ListView)(e.parent.parent)).Rebuild();
            });

            TextField textName = e.Q<TextField>("TextName");
            textName.value = methodDetail.Name;
            textName.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                methodDetail.Name = evt.newValue;
            });

            TextField textInput = e.Q<TextField>("TextInput");
            textInput.value = methodDetail.InputType;
            textInput.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                methodDetail.InputType = evt.newValue;
            });

            TextField textReturn = e.Q<TextField>("TextReturn");
            textReturn.value = methodDetail.OutputType;
            textReturn.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                methodDetail.OutputType = evt.newValue;
            });

        }
    }

    private void OnServerAdd(MouseUpEvent e)
    {
        ServiceDetail a = new ServiceDetail();
        a.Name = "NewService";
        _CurData.Services.Add(a);
        _listServices.Rebuild();
    }

    private void OnServicesRemove(MouseUpEvent e)
    {
        ServiceDetail index = (ServiceDetail)((Button)e.currentTarget).userData;
        _CurData.Services.Remove(index);
        _listServices.Rebuild();
    }

    #endregion



    private List<TreeViewItemData<TreeNodeData>> ListDirectory(string path, ref int index)
    {
        List<TreeViewItemData<TreeNodeData>> treeViewItemDatas = new List<TreeViewItemData<TreeNodeData>>();
        DirectoryInfo theFolder = new DirectoryInfo(@path);

        foreach (DirectoryInfo NextFolder in theFolder.GetDirectories())
        {
            List<TreeViewItemData<TreeNodeData>> treeViewSubItemsData = ListDirectory(NextFolder.FullName, ref index);
            TreeNodeData td = new TreeNodeData()
            {
                type = 0,
                path = NextFolder.FullName,
                name = NextFolder.Name,
            };
            listTreeNodeData.Add(td);
            var treeViewItemData = new TreeViewItemData<TreeNodeData>(index, td, treeViewSubItemsData);
            treeViewItemDatas.Add(treeViewItemData);
            index++;
        }

        foreach (FileInfo NextFile in theFolder.GetFiles())
        {
            if (NextFile.Name.EndsWith(".proto"))
            {
                ProtoDetail protoDetail = protoParser.Parse(NextFile.FullName);
                TreeNodeData td = new TreeNodeData()
                {
                    type = 1,
                    protoDetail = protoDetail,
                    path = NextFile.FullName,
                    name = NextFile.Name.Replace(".proto", ""),
                };
                listTreeNodeData.Add(td);
                var treeViewItemData = new TreeViewItemData<TreeNodeData>(index, td, null);
                treeViewItemDatas.Add(treeViewItemData);
                index++;

            }
        }



        return treeViewItemDatas;

    }


    private void SaveProtoDetail(MouseUpEvent e)
    {
        if(_CurData != null)
        {
            if (!CheckProtoDetail(_CurData))
            {
                EditorUtility.DisplayDialog("提示", "生成失败", "好的");
                return;
            }
            protoParser.GenerateProtoFile(_CurData, _CurDataPath);
            EditorUtility.DisplayDialog("提示", "保存成功，路径：" + _CurDataPath, "好的");

        }
    }

    private void SaveAllProtoDetail(MouseUpEvent e)
    {
        for(int i = 0;i< listTreeNodeData.Count; i++)
        {
            if(listTreeNodeData[i].type == 1)
            {
                if (!CheckProtoDetail(listTreeNodeData[i].protoDetail))
                {
                    return;
                }
                protoParser.GenerateProtoFile(listTreeNodeData[i].protoDetail, listTreeNodeData[i].path);
            }
        }
        EditorUtility.DisplayDialog("提示", "保存成功", "好的");

    }

    private bool CheckProtoDetail(ProtoDetail pd)
    {

        foreach(EnumDetail enumDetail in pd.Enums)
        {
            if(enumDetail.Values.Count == 0)
            {
                Debug.LogWarning(pd.PackageName + " " + enumDetail.Name + " 字段不能为空");
                return false;

            }
        }
        return true;
    }

    private void DeleteProtoDetail(MouseUpEvent e)
    {
        if(_CurData != null && !string.IsNullOrEmpty(_CurDataPath))
        {
            File.Delete(_CurDataPath);

            listTreeNodeData.Clear();
            int index = 0;
            List<TreeViewItemData<TreeNodeData>> items = ListDirectory(ProtoFilesPath, ref index);
            _treeProto.SetRootItems(items);
            _treeProto.Rebuild();

        }
    }


    private void AddProtoDetail(MouseUpEvent e)
    {
        string PackageName = "proto.";
        string OutPutPath = Application.dataPath;
        if (_CurTreeNodeData == null)
        {
            PackageName += _txtNewProtoName.value;
            OutPutPath = ProtoFilesPath + "\\" + _txtNewProtoName.value + ".proto";
        }
        else
        {
            if (_CurTreeNodeData.type == 1)
            {
                string[] strs = Directory.GetDirectories(_CurTreeNodeData.path);
                bool mark = false;
                foreach (string str in strs)
                {
                    if (str == "ProtoFiles")
                    {
                        mark = true;
                        continue;
                    }
                    if (mark)
                    {
                        PackageName += str + ".";
                    }
                }

                PackageName += _txtNewProtoName.value;

                string path = Path.GetDirectoryName(_CurTreeNodeData.path);
                OutPutPath = path + "\\" + _txtNewProtoName.value + ".proto";

            }
            else
            {
                string[] strs = Directory.GetDirectories(_CurTreeNodeData.path);
                bool mark = false;
                foreach (string str in strs)
                {
                    if (str == "ProtoFiles")
                    {
                        mark = true;
                        continue;
                    }
                    if (mark)
                    {
                        PackageName += str + ".";
                    }
                }

                PackageName += _txtNewProtoName.value;
                OutPutPath = _CurTreeNodeData.path + "\\" + _txtNewProtoName.value + ".proto";

            }
        }

        ProtoDetail newProtoDetail = new ProtoDetail();
        newProtoDetail.Syntax = "proto3";
        newProtoDetail.PackageName = PackageName;

        protoParser.GenerateProtoFile(newProtoDetail, OutPutPath);

        listTreeNodeData.Clear();
        int index = 0;
        List<TreeViewItemData<TreeNodeData>> items = ListDirectory(ProtoFilesPath, ref index);
        _treeProto.SetRootItems(items);
        _treeProto.Rebuild();

    }


    private void CreatProtoDetail(MouseUpEvent e)
    {
        CreatC4File(_CurDataPath, GenerateFilesPath) ;
        EditorUtility.DisplayDialog("提示", "生成成功", "好的");
        AssetDatabase.Refresh();
    }

    private void CreatAllProtoDetail(MouseUpEvent e)
    {
        foreach(TreeNodeData treeNodeData in listTreeNodeData)
        {
            if(treeNodeData.type == 1)
            {
                CreatC4File(treeNodeData.path, GenerateFilesPath);
            }
        }

        EditorUtility.DisplayDialog("提示", "生成成功", "好的");
        AssetDatabase.Refresh();

    }

    private void CreatC4File(string OriginPath,string OutPath)
    {
        string FileName = Path.GetFileNameWithoutExtension(OriginPath);
        string OutDirectoryPath = Path.GetDirectoryName(OutPath);
        string protoFilesPath = Application.dataPath + "/Editor/ProtoCreator/ProtoFiles";
        if (!Directory.Exists(OutDirectoryPath))
        {
            Directory.CreateDirectory(OutDirectoryPath);
        }
        string BinOutPath = OutDirectoryPath + "\\" + FileName + ".bin";
        string csOutPath = OutDirectoryPath + "\\" + FileName + ".cs";

        // 配置进程启动信息
        System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = Application.dataPath + protocPath,          // 指定命令行程序
            Arguments = string.Format("--proto_path={0} --descriptor_set_out={1}  {2}", protoFilesPath, BinOutPath, OriginPath),     // 执行ipconfig命令后退出
            RedirectStandardOutput = true, // 重定向标准输出
            UseShellExecute = false,       // 禁用Shell执行以重定向流
            CreateNoWindow = true          // 不创建新窗口
        };

        using (System.Diagnostics.Process process = new System.Diagnostics.Process())
        {
            process.StartInfo = psi;
            process.Start();

            // 读取输出
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(); // 等待进程结束
            if (string.IsNullOrEmpty(output))
            {
                Debug.Log(FileName + ".bin 生成成功");

            }
            else
            {
                Debug.LogError(output);
            }
        }


        System.Diagnostics.ProcessStartInfo psi2 = new System.Diagnostics.ProcessStartInfo
        {
            FileName = Application.dataPath + protocC4Path,          // 指定命令行程序
            Arguments = string.Format("-i:{0} -o:{1} -p:detectMissing", BinOutPath, csOutPath),     // 执行ipconfig命令后退出
            RedirectStandardOutput = true, // 重定向标准输出
            UseShellExecute = false,       // 禁用Shell执行以重定向流
            CreateNoWindow = true          // 不创建新窗口
        };

        using (System.Diagnostics.Process process = new System.Diagnostics.Process())
        {
            process.StartInfo = psi2;
            process.Start();

            // 读取输出
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(); // 等待进程结束
            if (string.IsNullOrEmpty(output))
            {
                Debug.Log(FileName + ".cs 生成成功");

            }
            else
            {
                Debug.LogError(output);
            }
        }
    }

    ProtoFiledModifierType StringToProtoFiledModifierType(string str)
    {
        if(str == "optional")
        {
            return ProtoFiledModifierType.optional;
        }else if (str == "repeated")
        {
            return ProtoFiledModifierType.repeated;
        }
        return ProtoFiledModifierType.required;

    }


    class TreeNodeData
    {
        public ProtoDetail protoDetail;
        public int type = 0;
        public string path;
        public string name;
    }



}

public enum ProtoFiledModifierType
{
    required,
    optional,
    repeated,
}
