using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CSharp;
using NewLife.Collections;
using NewLife.Log;
using NewLife;
using NewLife.Reflection;

namespace XTemplate.Templating
{
    /// <summary>
    /// 模版引擎
    /// </summary>
    /// <remarks>
    /// 模版引擎分为快速用法和增强用法两种，其中增强用法可用于对模版处理的全程进行干预。
    /// 一个模版引擎实例，可用重复使用以处理多个模版。
    /// </remarks>
    /// <example>
    /// 快速用法：
    /// <code>
    /// Dictionary&lt;String, Object&gt; data = new Dictionary&lt;String, Object&gt;();
    /// data["name"] = "参数测试";
    /// String content = Template.Process("模版文件.txt", data);
    /// </code>
    /// 高级用法：
    /// <code>
    /// Template tt = new Template();
    /// tt.AddTemplateItem("模版1"， File.ReadAllText("模版文件1.txt"));
    /// tt.AddTemplateItem("模版2"， File.ReadAllText("模版文件2.txt"));
    /// tt.AddTemplateItem("模版3"， File.ReadAllText("模版文件3.txt"));
    /// tt.Process();
    /// tt.Compile();
    /// TemplateBase temp = tt.CreateInstance("模版1");
    /// temp.Data["name"] = "参数测试";
    /// return temp.Render();
    /// </code>
    /// </example>
    public class Template : DisposeBase
    {
        #region 属性
        private CompilerErrorCollection _Errors;
        /// <summary>编译错误集合</summary>
        public CompilerErrorCollection Errors { get { return _Errors ?? (_Errors = new CompilerErrorCollection()); } }

        private List<TemplateItem> _Templates;
        /// <summary>模版集合</summary>
        public List<TemplateItem> Templates { get { return _Templates ?? (_Templates = new List<TemplateItem>()); } }

        private List<String> _AssemblyReferences;
        /// <summary>程序集引用</summary>
        public List<String> AssemblyReferences { get { return _AssemblyReferences ?? (_AssemblyReferences = new List<String>()); } }

        private String _AssemblyName;
        /// <summary>程序集名称。一旦指定，编译时将会生成持久化的模版程序集文件。</summary>
        public String AssemblyName
        {
            get { return _AssemblyName; }
            set { _AssemblyName = value; }
        }

        private Assembly _Assembly;
        /// <summary>程序集</summary>
        public Assembly Assembly
        {
            get
            {
                if (_Assembly == null && !String.IsNullOrEmpty(AssemblyName))
                {
                    String file = AssemblyName;
                    if (!Path.IsPathRooted(file)) file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file);
                    if (!File.Exists(file)) file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine("Bin", AssemblyName));
                    _Assembly = Assembly.LoadFile(file);
                }
                return _Assembly;
            }
            private set { _Assembly = value; }
        }

        private CodeDomProvider _Provider;
        /// <summary>代码生成提供者</summary>
        private CodeDomProvider Provider
        {
            get { return _Provider ?? (_Provider = new CSharpCodeProvider()); }
            set
            {
                if (_Provider != null && value == null) _Provider.Dispose();
                _Provider = value;
            }
        }

        private String _NameSpace;
        /// <summary>命名空间</summary>
        public String NameSpace
        {
            get
            {
                if (String.IsNullOrEmpty(_NameSpace))
                {
                    String namespaceName = this.GetType().Namespace;
                    //namespaceName = namespaceName.Substring(0, namespaceName.LastIndexOf("."));
                    namespaceName += "s";
                    return namespaceName;
                }
                return _NameSpace;
            }
            set { _NameSpace = value; }
        }

        private Int32 _Step;
        /// <summary>处理步骤</summary>
        private Int32 Step
        {
            get { return _Step; }
            set { _Step = value; }
        }
        #endregion

        #region 释放
        ///// <summary>
        ///// 释放资源
        ///// </summary>
        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        //private void Dispose(Boolean dispose)
        //{
        //    if (dispose && (_Provider != null))
        //    {
        //        //_Provider.Dispose();
        //        //_Provider = null;
        //        Provider = null;
        //    }
        //}

        ///// <summary>
        ///// 释放资源
        ///// </summary>
        //~Template()
        //{
        //    Dispose(false);
        //}
        #endregion

        #region 创建
        ///// <summary>
        ///// 构造一个模版实例。为了向下兼容，暂时还不能私有化构造函数
        ///// </summary>
        //[Obsolete("建议使用带有缓存的Create方法！")]
        //public Template() { }

        private static DictionaryCache<String, Template> cache = new DictionaryCache<string, Template>();
        /// <summary>
        /// 根据名称和模版创建模版实例，带缓存，避免重复编译
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="templates">模版</param>
        /// <returns></returns>
        public static Template Create(String name, params String[] templates)
        {
            if (templates == null || templates.Length < 1) throw new ArgumentNullException("templates");

            Dictionary<String, String> dic = new Dictionary<string, string>();

            String prefix = !String.IsNullOrEmpty(name) ? name : "Class";

            if (templates.Length == 1)
            {
                dic.Add(prefix, templates[0]);
            }
            else
            {
                for (int i = 0; i < templates.Length; i++)
                {
                    dic.Add(prefix + (i + 1), templates[i]);
                }
            }

            return Create(dic);
        }

        /// <summary>
        /// 根据名称和模版创建模版实例，带缓存，避免重复编译
        /// </summary>
        /// <param name="templates">模版集合</param>
        /// <returns></returns>
        public static Template Create(IDictionary<String, String> templates)
        {
            if (templates == null || templates.Count < 1) throw new ArgumentNullException("templates");

            // 计算hash
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<String, String> item in templates)
            {
                sb.Append(Hash(item.Key));
                sb.Append(Hash(item.Value));
            }

            String hash = Hash(sb.ToString());

            return cache.GetItem<IDictionary<String, String>>(hash, templates, delegate(String key, IDictionary<String, String> contents)
            {
                Template entity = new Template();
                //entity.AddTemplateItem(key, content);

                //// 如果只有一个名字，则作为所有模版名的前缀，否则使用Class作为前缀
                //String firstName = names2 != null && names2.Length == 1 && !String.IsNullOrEmpty(names[0]) ? names2[0] : "Class";
                //for (int i = 0; i < contents.Length; i++)
                //{
                //    // 如果存在名称，则使用名称，否则讲使用常量加上编号作为名称
                //    //String elm = names2 != null && names2.Length > i ? names2[i] : key + i;
                //    String elm = names2 != null && names2.Length > i ? names2[i] : firstName + i;
                //    entity.AddTemplateItem(elm, contents[i]);
                //}

                foreach (KeyValuePair<String, String> item in contents)
                {
                    entity.AddTemplateItem(item.Key, item.Value);
                }

                entity.Process();
                //entity.Compile();
                return entity;
            });
        }

        /// <summary>
        /// 释放提供者
        /// </summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            try
            {
                if (_Provider != null)
                {
                    //_Provider.Dispose();
                    //_Provider = null;
                    Provider = null;
                }
            }
            catch { }
        }
        #endregion

        #region 快速处理
        //static DictionaryCache<String, Template> tempCache = new DictionaryCache<String, Template>();
        /// <summary>
        /// 通过指定模版文件和传入模版的参数处理模版，返回结果
        /// </summary>
        /// <remarks>
        /// 该方法是处理模版的快速方法，把分析、编译和运行三步集中在一起。
        /// 带有缓存，避免重复分析模版。尽量以模版内容为key，防止模版内容改变后没有生效。
        /// </remarks>
        /// <example>
        /// 快速用法：
        /// <code>
        /// Dictionary&lt;String, Object&gt; data = new Dictionary&lt;String, Object&gt;();
        /// data["name"] = "参数测试";
        /// String content = Template.ProcessFile("模版文件.txt", data);
        /// </code>
        /// </example>
        /// <param name="templateFile">模版文件</param>
        /// <param name="data">传入模版的参数，模版中可以使用Data[名称]获取</param>
        /// <returns></returns>
        public static String ProcessFile(String templateFile, IDictionary<String, Object> data)
        {
            // 尽量以模版内容为key，防止模版内容改变后没有生效
            String template = File.ReadAllText(templateFile);

            //Template tt = tempCache.GetItem<String, String>(Hash(template), templateFile, template, delegate(String key, String file, String content)
            //{
            //    Template entity = new Template();
            //    entity.AddTemplateItem(file, content);
            //    entity.Process();
            //    entity.Compile();
            //    return entity;
            //});

            //TemplateBase temp = tt.CreateInstance(tt.Templates[0].ClassName);
            //temp.Data = data;
            //return temp.Render();

            return ProcessTemplate(templateFile, template, data);
        }

        /// <summary>
        /// 通过指定模版内容和传入模版的参数处理模版，返回结果
        /// </summary>
        /// <example>
        /// 快速用法：
        /// <code>
        /// Dictionary&lt;String, Object&gt; data = new Dictionary&lt;String, Object&gt;();
        /// data["name"] = "参数测试";
        /// String content = Template.ProcessTemplate("模版内容", data);
        /// </code>
        /// </example>
        /// <param name="template">模版内容</param>
        /// <param name="data">模版参数</param>
        /// <returns></returns>
        public static String ProcessTemplate(String template, IDictionary<String, Object> data)
        {
            return ProcessTemplate(null, template, data);
        }

        /// <summary>
        /// 通过指定模版内容和传入模版的参数处理模版，返回结果
        /// </summary>
        /// <param name="name">模版名字</param>
        /// <param name="template">模版内容</param>
        /// <param name="data">模版参数</param>
        /// <returns></returns>
        public static String ProcessTemplate(String name, String template, IDictionary<String, Object> data)
        {
            if (String.IsNullOrEmpty(template)) throw new ArgumentNullException("template");

            //// 尽量以模版内容为key，防止模版内容改变后没有生效
            //if (String.IsNullOrEmpty(name)) name = Hash(template);

            //Template tt = tempCache.GetItem<String>(name, template, delegate(String key, String content)
            //{
            //    Template entity = new Template();
            //    entity.AddTemplateItem(key, content);
            //    entity.Process();
            //    entity.Compile();
            //    return entity;
            //});

            Template tt = Create(name, template);

            return tt.Render(tt.Templates[0].ClassName, data);
        }
        #endregion

        #region 分析模版
        const Int32 Step_Process = 1;
        /// <summary>
        /// 处理预先写入Templates的模版集合，模版生成类的代码在Sources中返回
        /// </summary>
        public void Process()
        {
            if (Templates == null || Templates.Count < 1) throw new InvalidOperationException("在Templates中未找到待处理模版！");

            if (Step >= Step_Process) return;
            //if (Step > Step_Process) throw new InvalidOperationException("模版已分析处理！");

            //foreach (TemplateItem item in Templates)
            //{
            //    item.Source = Process(item);
            //}
            for (int i = 0; i < Templates.Count; i++)
            {
                Process(Templates[i]);
            }

            Step = Step_Process;
        }

        /// <summary>
        /// 添加模版项，实际上是添加到Templates集合中。
        /// 未指定模版名称时，使用模版的散列作为模版名称
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        public void AddTemplateItem(String name, String content)
        {
            if (String.IsNullOrEmpty(content)) throw new ArgumentNullException("content", "模版内容不能为空！");

            if (Step >= 1) throw new InvalidOperationException("模版已分析处理，不能再添加模版！");

            // 未指定模版名称时，使用模版的散列作为模版名称
            if (String.IsNullOrEmpty(name)) name = Hash(content);

            TemplateItem item = FindTemplateItem(name);
            if (item == null)
            {
                item = new TemplateItem();
                Templates.Add(item);
            }
            item.Name = name;
            item.Content = content;
        }

        /// <summary>
        /// 查找指定名称的模版
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private TemplateItem FindTemplateItem(String name)
        {
            if (Templates == null || Templates.Count < 1) return null;

            foreach (TemplateItem item in Templates)
            {
                if (item.Name == name) return item;
            }

            return null;
        }

        /// <summary>
        /// 处理指定模版
        /// </summary>
        /// <param name="item">模版项</param>
        private void Process(TemplateItem item)
        {
            // 拆分成块
            item.Blocks = TemplateParser.Parse(item.Name, item.Content);

            // 处理指令
            ProcessDirectives(item);
            //TemplateParser.StripExtraNewlines(item.Blocks);

            if (Imports != null) item.Imports.AddRange(Imports);
            //if (References != null) context.References.AddRange(References);

            // 生成代码
            item.Source = ConstructGeneratorCode(item, true, NameSpace, Provider);
        }
        #endregion

        #region 处理指令
        /// <summary>
        /// 处理指令
        /// </summary>
        /// <param name="item"></param>
        /// <returns>返回指令集合</returns>
        private void ProcessDirectives(TemplateItem item)
        {
            // 使用包含堆栈处理包含
            Stack<String> IncludeStack = new Stack<string>();
            IncludeStack.Push(item.Name);

            String[] directives = new String[] { "template", "assembly", "import", "include", "var" };

            for (Int32 i = 0; i < item.Blocks.Count; i++)
            {
                Block block = item.Blocks[i];
                if (block.Type != BlockType.Directive) continue;

                // 弹出当前块的模版名
                while (IncludeStack.Count > 0 && StringComparer.OrdinalIgnoreCase.Compare(IncludeStack.Peek(), block.Name) != 0)
                {
                    IncludeStack.Pop();
                }
                Directive directive = TemplateParser.ParseDirectiveBlock(block);
                if (directive == null || Array.IndexOf(directives, directive.Name.ToLower()) < 0)
                    throw new TemplateException(block, String.Format("无法识别的指令：{0}！", block.Text));

                // 包含指令时，返回多个代码块
                List<Block> list = ProcessDirective(directive, item);
                if ((list == null) || (list.Count == 0)) continue;

                if (IncludeStack.Contains(list[0].Name))
                    throw new TemplateException(block, String.Format("循环包含名为[{0}]的模版！", list[0].Name));

                IncludeStack.Push(list[0].Name);

                // 包含模版并入当前模版
                item.Blocks.InsertRange(i + 1, list);
            }
        }

        private List<Block> ProcessDirective(Directive directive, TemplateItem item)
        {
            #region 包含include
            if (String.Equals(directive.Name, "include", StringComparison.OrdinalIgnoreCase))
            {
                String name = directive.GetParameter("name");

                String content = null;
                TemplateItem ti = FindTemplateItem(name);
                if (ti != null)
                {
                    ti.Included = true;
                    content = ti.Content;
                }
                else
                {
                    // 尝试读取文件
                    if (File.Exists(name))
                    {
                        ti = new TemplateItem();
                        ti.Name = name;
                        ti.Content = File.ReadAllText(name);
                        Templates.Add(ti);

                        ti.Included = true;
                        content = ti.Content;
                    }
                }
                if (String.IsNullOrEmpty(content))
                    throw new TemplateException(directive.Block, String.Format("加载模版[{0}]失败！", name));

                return TemplateParser.Parse(name, content);
            }
            #endregion

            if (String.Equals(directive.Name, "assembly", StringComparison.OrdinalIgnoreCase))
            {
                String name = directive.GetParameter("name");
                if (!AssemblyReferences.Contains(name)) AssemblyReferences.Add(name);
            }
            else if (String.Equals(directive.Name, "import", StringComparison.OrdinalIgnoreCase))
            {
                item.Imports.Add(directive.GetParameter("namespace"));
            }
            else if (String.Equals(directive.Name, "template", StringComparison.OrdinalIgnoreCase))
            {
                if (!item.Processed)
                {
                    // 由模版指令指定类名
                    String name = directive.GetParameter("name");
                    if (!String.IsNullOrEmpty(name)) item.ClassName = name;

                    //item.BaseClassName = directive.GetParameter("inherits");
                    if (directive.TryGetParameter("inherits", out name)) item.BaseClassName = name;
                    item.Processed = true;
                }
                else
                    throw new TemplateException(directive.Block, "多个模版指令！");
            }
            else if (String.Equals(directive.Name, "var", StringComparison.OrdinalIgnoreCase))
            {
                String name = directive.GetParameter("name");
                String type = directive.GetParameter("type");

                if (item.Vars.ContainsKey(name)) throw new TemplateException(directive.Block, "模版变量" + name + "已存在！");

                Type ptype = TypeX.GetType(type, true);
                if (ptype == null) throw new TemplateException(directive.Block, "无法找到模版变量类型" + type + "！");

                // 因为TypeX.GetType的强大，模版可能没有引用程序集和命名空间，甚至type位于未装载的程序集中它也会自动装载，所以这里需要加上
                ImportType(item, ptype);
                item.Vars.Add(name, ptype);
            }
            return null;
        }

        /// <summary>
        /// 导入某类型，导入程序集引用及命名空间引用，主要处理泛型
        /// </summary>
        /// <param name="item"></param>
        /// <param name="type"></param>
        void ImportType(TemplateItem item, Type type)
        {
            String name = null;
            try
            {
                name = type.Assembly.Location;
            }
            catch { }
            if (!String.IsNullOrEmpty(name) && !AssemblyReferences.Contains(name)) AssemblyReferences.Add(name);
            name = type.Namespace;
            if (!item.Imports.Contains(name)) item.Imports.Add(name);

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type[] ts = type.GetGenericArguments();
                foreach (Type elm in ts)
                {
                    if (!elm.IsGenericParameter) ImportType(item, elm);
                }
            }
        }
        #endregion

        #region 生成代码
        private static String ConstructGeneratorCode(TemplateItem item, Boolean lineNumbers, String namespaceName, CodeDomProvider provider)
        {
            // 准备类名和命名空间
            CodeNamespace codeNamespace = new CodeNamespace(namespaceName);

            // 加入引用的命名空间
            foreach (String str in item.Imports)
            {
                if (!String.IsNullOrEmpty(str)) codeNamespace.Imports.Add(new CodeNamespaceImport(str));
            }
            CodeTypeDeclaration typeDec = new CodeTypeDeclaration(item.ClassName);
            typeDec.IsClass = true;
            codeNamespace.Types.Add(typeDec);

            // 基类
            if (String.IsNullOrEmpty(item.BaseClassName))
                typeDec.BaseTypes.Add(new CodeTypeReference(typeof(TemplateBase)));
            else
                typeDec.BaseTypes.Add(new CodeTypeReference(item.BaseClassName));

            if (!String.IsNullOrEmpty(item.Name)) typeDec.LinePragma = new CodeLinePragma(item.Name, 1);

            // Render方法
            CreateRenderMethod(item.Blocks, lineNumbers, typeDec);

            // 代码生成选项
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.VerbatimOrder = true;
            options.BlankLinesBetweenMembers = false;
            options.BracingStyle = "C";

            // 其它类成员代码块
            Boolean firstMemberFound = false;
            foreach (Block block in item.Blocks)
            {
                firstMemberFound = GenerateMemberForBlock(block, typeDec, lineNumbers, provider, options, firstMemberFound);
            }

            // 模版变量
            if (item.Vars != null && item.Vars.Count > 0)
            {
                // 构建静态构造函数，初始化静态属性Vars
                CreateCctorMethod(typeDec, item.Vars);

                //public Int32 VarName
                //{
                //    get { return (Int32)GetData("VarName"); }
                //    set { Data["VarName"] = value; }
                //}
                foreach (String v in item.Vars.Keys)
                {
                    TypeX vtype = TypeX.Create(item.Vars[v]);
                    String codeName = vtype.FullName;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine();
                    sb.AppendFormat("public {0} {1}", codeName, v);
                    sb.AppendLine("{");
                    sb.AppendFormat("    get {{ return GetData<{0}>(\"{1}\"); }}", codeName, v);
                    sb.AppendLine();
                    sb.AppendFormat("    set {{ Data[\"{0}\"] = value; }}", v);
                    sb.AppendLine();
                    sb.AppendLine("}");

                    CodeSnippetTypeMember member = new CodeSnippetTypeMember(sb.ToString());
                    typeDec.Members.Add(member);
                }
            }

            // 输出
            using (StringWriter writer = new StringWriter())
            {
                provider.GenerateCodeFromNamespace(codeNamespace, new IndentedTextWriter(writer), options);
                return writer.ToString();
            }
        }

        /// <summary>
        /// 生成Render方法
        /// </summary>
        /// <param name="blocks"></param>
        /// <param name="lineNumbers"></param>
        /// <param name="typeDec"></param>
        private static void CreateRenderMethod(List<Block> blocks, Boolean lineNumbers, CodeTypeDeclaration typeDec)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            typeDec.Members.Add(method);
            method.Name = "Render";
            method.Attributes = MemberAttributes.Override | MemberAttributes.Public;
            method.ReturnType = new CodeTypeReference(typeof(String));

            // 生成代码
            CodeStatementCollection statementsMain = method.Statements;
            Boolean firstMemberFound = false;
            foreach (Block block in blocks)
            {
                if (block.Type == BlockType.Directive) continue;
                if (block.Type == BlockType.Member)
                {
                    // 遇到类成员代码块，标识取反
                    firstMemberFound = !firstMemberFound;
                    continue;
                }
                // 只要现在还在类成员代码块区域内，就不做处理
                if (firstMemberFound) continue;

                if (block.Type == BlockType.Statement)
                {
                    // 代码语句，直接拼接
                    CodeSnippetStatement statement = new CodeSnippetStatement(block.Text);
                    if (lineNumbers)
                        AddStatementWithLinePragma(block, statementsMain, statement);
                    else
                        statementsMain.Add(statement);
                }
                else if (block.Type == BlockType.Text)
                {
                    // 模版文本，直接Write
                    if (!String.IsNullOrEmpty(block.Text))
                    {
                        CodeMethodInvokeExpression exp = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "Write", new CodeExpression[] { new CodePrimitiveExpression(block.Text) });
                        statementsMain.Add(exp);
                        //CodeExpressionStatement statement = new CodeExpressionStatement(exp);
                        ////if (lineNumbers)
                        ////    AddStatementWithLinePragma(block, statementsMain, statement);
                        ////else
                        //statementsMain.Add(statement);
                    }
                }
                else
                {
                    // 表达式，直接Write
                    CodeMethodInvokeExpression exp = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "Write", new CodeExpression[] { new CodeArgumentReferenceExpression(block.Text.Trim()) });
                    CodeExpressionStatement statement = new CodeExpressionStatement(exp);
                    if (lineNumbers)
                        AddStatementWithLinePragma(block, statementsMain, statement);
                    else
                        statementsMain.Add(statement);
                }
            }

            statementsMain.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "Output"), "ToString"), new CodeExpression[0])));
        }


        private static void CreateCctorMethod(CodeTypeDeclaration typeDec, IDictionary<String, Type> vars)
        {
            //CodeTypeConstructor method = new CodeTypeConstructor();
            CodeConstructor method = new CodeConstructor();
            typeDec.Members.Add(method);
            method.Attributes = MemberAttributes.Public;

            // 生成代码
            CodeStatementCollection statementsMain = method.Statements;
            // vars.Add(item, vars[item]);
            foreach (String item in vars.Keys)
            {
                CodeMethodReferenceExpression methodRef = new CodeMethodReferenceExpression(new CodePropertyReferenceExpression(null, "Vars"), "Add");
                statementsMain.Add(new CodeMethodInvokeExpression(methodRef, new CodePrimitiveExpression(item), new CodeTypeOfExpression(vars[item])));
            }
        }

        private static void AddStatementWithLinePragma(Block block, CodeStatementCollection statements, CodeStatement statement)
        {
            Int32 lineNumber = (block.StartLine > 0) ? block.StartLine : 1;

            if (String.IsNullOrEmpty(block.Name))
                statements.Add(new CodeSnippetStatement("#line " + lineNumber));
            else
                statement.LinePragma = new CodeLinePragma(block.Name, lineNumber);
            statements.Add(statement);
            if (String.IsNullOrEmpty(block.Name)) statements.Add(new CodeSnippetStatement("#line default"));
        }

        /// <summary>
        /// 生成成员代码块
        /// </summary>
        /// <param name="block"></param>
        /// <param name="generatorType"></param>
        /// <param name="lineNumbers"></param>
        /// <param name="provider"></param>
        /// <param name="options"></param>
        /// <param name="firstMemberFound"></param>
        /// <returns></returns>
        private static Boolean GenerateMemberForBlock(Block block, CodeTypeDeclaration generatorType, Boolean lineNumbers, CodeDomProvider provider, CodeGeneratorOptions options, Boolean firstMemberFound)
        {
            CodeSnippetTypeMember member = null;
            if (!firstMemberFound)
            {
                // 发现第一个<#!后，认为是类成员代码的开始，直到下一个<#!作为结束
                if (block.Type == BlockType.Member)
                {
                    firstMemberFound = true;
                    if (!String.IsNullOrEmpty(block.Text)) member = new CodeSnippetTypeMember(block.Text);
                }
            }
            else
            {
                // 再次遇到<#!，此时，成员代码准备结束
                if (block.Type == BlockType.Member)
                {
                    firstMemberFound = false;
                    if (!String.IsNullOrEmpty(block.Text)) member = new CodeSnippetTypeMember(block.Text);
                }
                else if (block.Type == BlockType.Text)
                {
                    CodeMethodInvokeExpression expression = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "Write", new CodeExpression[] { new CodePrimitiveExpression(block.Text) });
                    CodeExpressionStatement statement = new CodeExpressionStatement(expression);
                    using (StringWriter writer = new StringWriter())
                    {
                        provider.GenerateCodeFromStatement(statement, writer, options);
                        member = new CodeSnippetTypeMember(writer.ToString());
                    }
                }
                else if (block.Type == BlockType.Expression)
                {
                    CodeMethodInvokeExpression expression = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "Write", new CodeExpression[] { new CodeArgumentReferenceExpression(block.Text.Trim()) });
                    CodeExpressionStatement statement = new CodeExpressionStatement(expression);
                    using (StringWriter writer = new StringWriter())
                    {
                        provider.GenerateCodeFromStatement(statement, writer, options);
                        member = new CodeSnippetTypeMember(writer.ToString());
                    }
                }
                else if (block.Type == BlockType.Statement)
                {
                    member = new CodeSnippetTypeMember(block.Text);
                }
            }
            if (member != null)
            {
                if (lineNumbers)
                {
                    Boolean flag = String.IsNullOrEmpty(block.Name);
                    Int32 lineNumber = (block.StartLine > 0) ? block.StartLine : 1;
                    if (flag)
                        generatorType.Members.Add(new CodeSnippetTypeMember("#line " + lineNumber));
                    else
                        member.LinePragma = new CodeLinePragma(block.Name, lineNumber);
                    generatorType.Members.Add(member);
                    if (flag) generatorType.Members.Add(new CodeSnippetTypeMember("#line default"));
                }
                else
                    generatorType.Members.Add(member);
            }
            return firstMemberFound;
        }
        #endregion

        #region 编译模版
        const Int32 Step_Compile = Step_Process + 1;
        /// <summary>
        /// 编译运行
        /// </summary>
        /// <returns></returns>
        public Assembly Compile()
        {
            if (Step >= Step_Compile) return Assembly;

            if (Step < Step_Compile - 1) Process();

            //else if (Step > Step_Compile)
            //    throw new InvalidOperationException("模版已编译！");

            List<String> sources = new List<string>();
            foreach (TemplateItem item in Templates)
            {
                if (!item.Included) sources.Add(item.Source);
            }

            if (References != null) AssemblyReferences.AddRange(References);
            Assembly asm = Compile(AssemblyName, sources.ToArray(), AssemblyReferences, Provider, Errors);
            if (asm != null) Assembly = asm;

            // 释放提供者
            Provider = null;

            Step = Step_Compile;

            return asm;
        }

        private static Dictionary<String, Assembly> asmCache = new Dictionary<String, Assembly>();
        private static Assembly Compile(String outputAssembly, String[] sources, IEnumerable<String> references, CodeDomProvider provider, CompilerErrorCollection errors)
        {
            String key = outputAssembly;
            if (String.IsNullOrEmpty(key)) key = Hash(String.Join(Environment.NewLine, sources));

            Assembly assembly = null;
            if (asmCache.TryGetValue(key, out assembly)) return assembly;
            lock (asmCache)
            {
                if (asmCache.TryGetValue(key, out assembly)) return assembly;
                foreach (String str in references)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(str) && File.Exists(str)) Assembly.LoadFrom(str);
                    }
                    catch { }
                }

                assembly = CompileInternal(outputAssembly, sources, references, provider, errors);
                //if (assembly != null && !asmCache.ContainsKey(key)) asmCache.Add(key, assembly);
                if (assembly != null) asmCache.Add(key, assembly);

                return assembly;
            }
        }

        private static Assembly CompileInternal(String outputAssembly, String[] sources, IEnumerable<String> references, CodeDomProvider provider, CompilerErrorCollection Errors)
        {
            CompilerParameters options = new CompilerParameters();
            foreach (String str in references)
            {
                options.ReferencedAssemblies.Add(str);
            }
            options.WarningLevel = 4;

            String tempPath = "XTemp";
            tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, tempPath);
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
            options.TempFiles = new TempFileCollection(tempPath, Debug);

            if (Debug || !String.IsNullOrEmpty(outputAssembly))
            {
                options.OutputAssembly = outputAssembly;
                options.GenerateInMemory = false;
                if (Debug)
                {
                    options.IncludeDebugInformation = true;
                    options.TempFiles.KeepFiles = true;
                }
            }
            else
            {
                options.GenerateInMemory = true;
                options.IncludeDebugInformation = false;
                options.TempFiles.KeepFiles = false;
            }

            CompilerResults results = provider.CompileAssemblyFromSource(options, sources);
            if (results.Errors.Count > 0)
            {
                Errors.AddRange(results.Errors);
                foreach (CompilerError error in results.Errors)
                {
                    error.ErrorText = error.ErrorText;
                    //if (String.IsNullOrEmpty(error.FileName)) error.FileName = inputFile;

                    if (!error.IsWarning)
                    {
                        TemplateException ex = new TemplateException(error.ToString());
                        ex.Error = error;
                        throw ex;
                    }
                }
            }

            if (!Debug && String.IsNullOrEmpty(outputAssembly) && Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch { }
            }

            if (!results.Errors.HasErrors && String.IsNullOrEmpty(outputAssembly)) return results.CompiledAssembly;

            return null;
        }
        #endregion

        #region 运行生成
        /// <summary>
        /// 创建模版实例
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public TemplateBase CreateInstance(String className)
        {
            if (Step < Step_Compile) Compile();

            if (Assembly == null) throw new InvalidOperationException("尚未编译模版！");

            if (String.IsNullOrEmpty(className))
            {
                // 检查是否只有一个模版类
                Int32 n = 0;
                foreach (Type type in Assembly.GetTypes())
                {
                    if (!typeof(TemplateBase).IsAssignableFrom(type)) continue;

                    className = type.FullName;
                    if (n++ > 1) break;
                }

                // 如果只有一个模版类，则使用该类作为类名
                if (n != 1) throw new ArgumentNullException("className", "请指定模版类类名！");
            }
            else
                className = GetClassName(className);

            if (!className.Contains(".")) className = NameSpace + "." + className;
            TemplateBase temp = Assembly.CreateInstance(className) as TemplateBase;
            if (temp == null) throw new Exception(String.Format("没有找到模版类[{0}]！", className));

            return temp;
        }

        /// <summary>
        /// 运行代码
        /// </summary>
        /// <param name="className"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public String Render(String className, IDictionary<String, Object> data)
        {
            TemplateBase temp = CreateInstance(className);
            temp.Data = data;
            temp.Initialize();
            return temp.Render();
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// MD5散列
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        protected static String Hash(String str)
        {
            if (String.IsNullOrEmpty(str)) return null;

            MD5 md5 = MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(str))).Replace("-", null);
        }

        /// <summary>
        /// 把名称处理为标准类名
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static String GetClassName(String fileName)
        {
            String name = fileName;
            if (name.Contains(".")) name = name.Substring(0, name.LastIndexOf("."));
            name = name.Replace(@"\", "_").Replace(@"/", "_").Replace(".", "_");
            return name;
        }
        #endregion

        #region 调试
        private static Boolean? _Debug;
        /// <summary>
        /// 是否调试
        /// </summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;

                String str = ConfigurationManager.AppSettings["XTemplate.Debug"];
                if (String.IsNullOrEmpty(str))
                    _Debug = false;
                else if (str == "1" || str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
                    _Debug = true;
                else if (str == "0" || str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase))
                    _Debug = false;
                else
                    _Debug = Convert.ToBoolean(str);
                return _Debug.Value;
            }
            set { _Debug = value; }
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="msg"></param>
        public static void WriteLog(String msg)
        {
            XTrace.WriteLine(msg);
        }

        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLog(String format, params Object[] args)
        {
            XTrace.WriteLine(format, args);
        }
        #endregion

        #region 配置
        private static String _BaseClassName;
        /// <summary>
        /// 默认基类名称
        /// </summary>
        public static String BaseClassName
        {
            get
            {
                if (_BaseClassName == null)
                {
                    _BaseClassName = ConfigurationManager.AppSettings["XTemplate.BaseClassName"];
                    if (String.IsNullOrEmpty(_BaseClassName)) _BaseClassName = "";
                }
                return _BaseClassName;
            }
            set { _BaseClassName = value; }
        }

        private static List<String> _References;
        /// <summary>
        /// 标准程序集引用
        /// </summary>
        public static List<String> References
        {
            get
            {
                if (_References != null) return _References;

                // 程序集路径
                List<String> list = new List<String>();
                // 程序集名称，小写，用于重复判断
                List<String> names = new List<String>();

                // 加入配置的程序集
                String str = ConfigurationManager.AppSettings["XTemplate.References"];
                if (!String.IsNullOrEmpty(str))
                {
                    String[] ss = str.Split(new Char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (ss != null && ss.Length > 0)
                    {
                        //list.AddRange(ss);
                        foreach (String item in ss)
                        {
                            list.Add(item);

                            String name = Path.GetFileName(item);
                            names.Add(item.ToLower());
                        }
                    }
                }

                // 当前应用程序域所有程序集，虽然加上了很多引用，但是编译时会自动忽略没有实际引用的程序集！
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                if (asms == null || asms.Length < 1) return null;
                foreach (Assembly item in asms)
                {
                    try
                    {
                        if (String.IsNullOrEmpty(item.Location)) continue;

                        String name = Path.GetFileName(item.Location);
                        if (!String.IsNullOrEmpty(name)) name = name.ToLower();
                        if (names.Contains(name)) continue;
                        names.Add(name);
                        list.Add(item.Location);
                    }
                    catch { }
                }

                return _References = list;
            }
        }

        private static List<String> _Imports;
        /// <summary>
        /// 标准命名空间引用
        /// </summary>
        public static List<String> Imports
        {
            get
            {
                if (_Imports != null) return _Imports;

                // 命名空间
                List<String> list = new List<String>();

                // 加入配置的命名空间
                String str = ConfigurationManager.AppSettings["XTemplate.Imports"];
                if (!String.IsNullOrEmpty(str))
                {
                    String[] ss = str.Split(new Char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (ss != null && ss.Length > 0)
                    {
                        list.AddRange(ss);
                    }
                }

                String[] names = new String[] { "System", "System.Collections", "System.Collections.Generic", "System.Text" };
                if (names != null && names.Length > 0)
                {
                    foreach (String item in names)
                    {
                        if (!list.Contains(item)) list.Add(item);
                    }
                }

                // 特别支持
                Dictionary<String, String[]> supports = new Dictionary<String, String[]>();
                supports.Add("XCode", new String[] { "XCode", "XCode.DataAccessLayer" });
                supports.Add("XCommon", new String[] { "XCommon" });
                supports.Add("XControl", new String[] { "XControl" });
                supports.Add("NewLife.CommonEntity", new String[] { "NewLife.CommonEntity" });
                supports.Add("System.Web", new String[] { "System.Web" });
                supports.Add("System.Xml", new String[] { "System.Xml" });

                foreach (String item in supports.Keys)
                {
                    Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly asm in asms)
                    {
                        if (!asm.FullName.StartsWith(item + ",")) continue;

                        names = supports[item];
                        if (names != null && names.Length > 0)
                        {
                            foreach (String name in names)
                            {
                                if (!list.Contains(name)) list.Add(name);
                            }
                        }
                    }
                }

                return _Imports = list;
            }
        }
        #endregion
    }
}